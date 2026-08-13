using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using PlayGround.Application.Interfaces;
using PlayGround.Application.Payment.Commands;
using PlayGround.Application.Payment.Models;
using PlayGround.Contracts.Payment;
using PlayGround.Domain.Payment;
using PlayGround.Shared.Result;
using PlayGround.Shared.Time;
using Xunit;

namespace PlayGround.Tests.Unit.Application
{
    /// <summary>결제 승인 — 금액 위·변조 방어(저장 주문 대조 후에만 PG 호출)와 멱등·실패 기록을 가드한다.</summary>
    public class PaymentConfirmCommandTests
    {
        private static readonly Guid User = Guid.NewGuid();
        private const string OrderId = "0123456789abcdef0123456789abcdef";
        private const string PaymentKey = "tosskey_123";

        private static ConfirmPaymentRequest Request(string paymentKey = PaymentKey, string orderId = OrderId, int amount = 1000) =>
            new() { PaymentKey = paymentKey, OrderId = orderId, Amount = amount };

        private static PaymentSummaryResponse Stored(PaymentStatus status = PaymentStatus.Pending, int amount = 1000) =>
            new()
            {
                OrderId = OrderId,
                OrderName = "테스트 결제",
                Sport = Sport.Soccer,
                Amount = amount,
                Currency = "KRW",
                Status = status,
                PgProvider = PaymentProvider.Toss,
            };

        private sealed class Harness
        {
            public Mock<IPaymentRepository> Repository { get; } = new();
            public Mock<IPaymentGateway> Gateway { get; } = new();
            public ApprovePaymentInput? ApproveInput { get; private set; }

            public Harness(PaymentSummaryResponse? stored, bool gatewayFails = false, bool approveReturnsNull = false)
            {
                Repository.Setup(r => r.FindByOrderAsync(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
                    .ReturnsAsync(Result<PaymentSummaryResponse?>.Success(stored));
                Repository.Setup(r => r.ApproveAsync(It.IsAny<ApprovePaymentInput>(), It.IsAny<CancellationToken>()))
                    .Callback((ApprovePaymentInput input, CancellationToken _) => ApproveInput = input)
                    .ReturnsAsync(Result<PaymentSummaryResponse?>.Success(
                        approveReturnsNull ? null : Stored(status: PaymentStatus.Approved)));
                Repository.Setup(r => r.FailAsync(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
                    .ReturnsAsync(Result<PaymentSummaryResponse?>.Success(Stored(status: PaymentStatus.Failed)));

                Gateway.SetupGet(g => g.Provider).Returns(PaymentProvider.Toss);
                Gateway.Setup(g => g.ConfirmAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
                    .ReturnsAsync(gatewayFails
                        ? Result<PaymentApproval>.Error(ErrorCode.ExternalServiceError, "REJECT_CARD_COMPANY: rejected")
                        : Result<PaymentApproval>.Success(new PaymentApproval
                        {
                            PaymentKey = PaymentKey,
                            OrderId = OrderId,
                            Method = "카드",
                            ApprovedAt = SystemTime.Now,
                            RawStatus = "DONE",
                        }));
            }

            public PaymentConfirmCommand Command =>
                new(Repository.Object, Gateway.Object, NullLogger<PaymentConfirmCommand>.Instance);
        }

        //.// 인가·검증

        [Fact]
        public async Task ExecuteAsync_EmptyUser_IsUnauthorized()
        {
            Result<PaymentSummaryResponse> result =
                await new Harness(Stored()).Command.ExecuteAsync(Guid.Empty, Request());

            result.ResultData.DetailCode.Should().Be(ErrorCode.Unauthorized);
        }

        [Theory]
        [InlineData("", OrderId)]
        [InlineData(PaymentKey, "")]
        public async Task ExecuteAsync_MissingKeys_IsMissingRequired(string paymentKey, string orderId)
        {
            Result<PaymentSummaryResponse> result =
                await new Harness(Stored()).Command.ExecuteAsync(User, Request(paymentKey: paymentKey, orderId: orderId));

            result.ResultData.DetailCode.Should().Be(ErrorCode.MissingRequired);
        }

        //.// 저장 주문 대조

        [Fact]
        public async Task ExecuteAsync_OrderNotFound_IsNotFound()
        {
            Result<PaymentSummaryResponse> result =
                await new Harness(stored: null).Command.ExecuteAsync(User, Request());

            result.ResultData.DetailCode.Should().Be(ErrorCode.NotFound);
        }

        [Fact]
        public async Task ExecuteAsync_AlreadyApproved_IsIdempotentSuccess()
        {
            var harness = new Harness(Stored(status: PaymentStatus.Approved));

            Result<PaymentSummaryResponse> result = await harness.Command.ExecuteAsync(User, Request());

            // 복귀 페이지 새로고침 멱등 — PG 재호출 없이 저장 결과를 돌려준다
            result.IsSuccess.Should().BeTrue();
            harness.Gateway.Verify(g => g.ConfirmAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task ExecuteAsync_FailedOrder_IsInvalidState()
        {
            Result<PaymentSummaryResponse> result =
                await new Harness(Stored(status: PaymentStatus.Failed)).Command.ExecuteAsync(User, Request());

            result.ResultData.DetailCode.Should().Be(ErrorCode.InvalidState);
        }

        [Fact]
        public async Task ExecuteAsync_AmountMismatch_IsRejectedWithoutGatewayCall()
        {
            var harness = new Harness(Stored(amount: 1000));

            Result<PaymentSummaryResponse> result =
                await harness.Command.ExecuteAsync(User, Request(amount: 999999));

            // 금액 위·변조 방어의 핵심 — 승인 API 자체가 호출되면 안 된다
            result.ResultData.DetailCode.Should().Be(ErrorCode.InvalidInput);
            harness.Gateway.Verify(g => g.ConfirmAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        //.// PG 결과 반영

        [Fact]
        public async Task ExecuteAsync_GatewayFails_RecordsFailureAndPropagates()
        {
            var harness = new Harness(Stored(), gatewayFails: true);

            Result<PaymentSummaryResponse> result = await harness.Command.ExecuteAsync(User, Request());

            result.ResultData.DetailCode.Should().Be(ErrorCode.ExternalServiceError);
            harness.Repository.Verify(r => r.FailAsync(User, OrderId, It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task ExecuteAsync_GatewaySucceeds_AppliesApprovalToLedger()
        {
            var harness = new Harness(Stored());

            Result<PaymentSummaryResponse> result = await harness.Command.ExecuteAsync(User, Request());

            result.IsSuccess.Should().BeTrue();
            result.Value.Status.Should().Be(PaymentStatus.Approved);
            harness.ApproveInput.Should().NotBeNull();
            harness.ApproveInput!.PaymentKey.Should().Be(PaymentKey);
            harness.ApproveInput.Method.Should().Be("카드");
        }

        [Fact]
        public async Task ExecuteAsync_LedgerUpdateRejected_IsConflict()
        {
            // PG 승인은 났는데 원장 조건이 어긋난 경합 — 운영 대응 신호라 Error로 남는다
            Result<PaymentSummaryResponse> result =
                await new Harness(Stored(), approveReturnsNull: true).Command.ExecuteAsync(User, Request());

            result.ResultData.DetailCode.Should().Be(ErrorCode.Conflict);
        }
    }
}
