using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using PlayGround.Application.Interfaces;
using PlayGround.Application.Payment.Commands;
using PlayGround.Application.Payment.Models;
using PlayGround.Contracts.Payment;
using PlayGround.Domain.Payment;
using PlayGround.Shared.Result;
using Xunit;

namespace PlayGround.Tests.Unit.Application
{
    /// <summary>결제 주문 생성 — 금액 경계·Sport 필수·정규화·비활성 Provider 차단을 가드한다.</summary>
    public class PaymentOrderCommandTests
    {
        private static readonly Guid User = Guid.NewGuid();

        private static CreatePaymentOrderRequest Request(
            Sport sport = Sport.Soccer, string orderName = " 테스트 결제 ", int amount = 1000) =>
            new() { Sport = sport, OrderName = orderName, Amount = amount };

        private sealed class Harness
        {
            public Mock<IPaymentRepository> Repository { get; } = new();
            public Mock<IPaymentGateway> Gateway { get; } = new();
            public CreatePaymentInput? Captured { get; private set; }

            public Harness(PaymentProvider provider = PaymentProvider.Toss, bool repositoryReturnsNull = false)
            {
                Gateway.SetupGet(g => g.Provider).Returns(provider);
                Repository.Setup(r => r.CreateAsync(It.IsAny<CreatePaymentInput>(), It.IsAny<CancellationToken>()))
                    .Callback((CreatePaymentInput input, CancellationToken _) => Captured = input)
                    .ReturnsAsync((CreatePaymentInput input, CancellationToken _) =>
                        Result<PaymentSummaryResponse?>.Success(repositoryReturnsNull ? null : new PaymentSummaryResponse
                        {
                            OrderId = input.OrderId,
                            OrderName = input.OrderName,
                            Sport = input.Sport,
                            Amount = input.Amount,
                            Currency = input.Currency,
                            Status = PaymentStatus.Pending,
                            PgProvider = input.PgProvider,
                        }));
            }

            public PaymentOrderCommand Command =>
                new(Repository.Object, Gateway.Object, NullLogger<PaymentOrderCommand>.Instance);
        }

        //.// 인가·검증

        [Fact]
        public async Task ExecuteAsync_EmptyUser_IsUnauthorized()
        {
            Result<PaymentSummaryResponse> result = await new Harness().Command.ExecuteAsync(Guid.Empty, Request());

            result.ResultData.DetailCode.Should().Be(ErrorCode.Unauthorized);
        }

        [Fact]
        public async Task ExecuteAsync_ProviderNotConfigured_IsFeatureNotAvailable()
        {
            Result<PaymentSummaryResponse> result =
                await new Harness(provider: PaymentProvider.Unknown).Command.ExecuteAsync(User, Request());

            result.ResultData.DetailCode.Should().Be(ErrorCode.FeatureNotAvailable);
        }

        [Fact]
        public async Task ExecuteAsync_UnknownSport_IsInvalidInput()
        {
            Result<PaymentSummaryResponse> result =
                await new Harness().Command.ExecuteAsync(User, Request(sport: Sport.Unknown));

            result.ResultData.DetailCode.Should().Be(ErrorCode.InvalidInput);
        }

        [Theory]
        [InlineData("")]
        [InlineData("   ")]
        public async Task ExecuteAsync_EmptyOrderName_IsMissingRequired(string orderName)
        {
            Result<PaymentSummaryResponse> result =
                await new Harness().Command.ExecuteAsync(User, Request(orderName: orderName));

            result.ResultData.DetailCode.Should().Be(ErrorCode.MissingRequired);
        }

        [Theory]
        [InlineData(99)]           // 하한 100 미만 — 토스 최소 결제금액
        [InlineData(0)]
        [InlineData(-1000)]
        [InlineData(10_000_001)]   // 상한 초과
        public async Task ExecuteAsync_AmountOutOfRange_IsRejected(int amount)
        {
            Result<PaymentSummaryResponse> result =
                await new Harness().Command.ExecuteAsync(User, Request(amount: amount));

            result.ResultData.DetailCode.Should().Be(ErrorCode.OutOfRange);
        }

        [Theory]
        [InlineData(100)]          // 경계는 허용
        [InlineData(10_000_000)]
        public async Task ExecuteAsync_AcceptsBoundaryAmounts(int amount)
        {
            Result<PaymentSummaryResponse> result =
                await new Harness().Command.ExecuteAsync(User, Request(amount: amount));

            result.IsSuccess.Should().BeTrue();
        }

        //.// 정규화 — 저장소에 전달된 값으로 확인

        [Fact]
        public async Task ExecuteAsync_NormalizesInputBeforeSaving()
        {
            var harness = new Harness();

            Result<PaymentSummaryResponse> result =
                await harness.Command.ExecuteAsync(User, Request(orderName: " 테스트 결제 " + new string('가', 200)));

            result.IsSuccess.Should().BeTrue();
            harness.Captured.Should().NotBeNull();
            harness.Captured!.OrderName.Should().HaveLength(PaymentOrderCommand.MaxOrderNameLength);
            harness.Captured.OrderName.Should().StartWith("테스트 결제");
            harness.Captured.UserId.Should().Be(User);
            harness.Captured.Currency.Should().Be("KRW");
            harness.Captured.PgProvider.Should().Be(PaymentProvider.Toss);
            harness.Captured.OrderId.Should().MatchRegex("^[0-9a-f]{32}$");   // 토스 orderId 규격(6~64자 영숫자) 안
        }

        //.// 저장소 결과 해석

        [Fact]
        public async Task ExecuteAsync_RepositoryReturnsNull_IsOperationFailed()
        {
            Result<PaymentSummaryResponse> result =
                await new Harness(repositoryReturnsNull: true).Command.ExecuteAsync(User, Request());

            result.ResultData.DetailCode.Should().Be(ErrorCode.OperationFailed);
        }
    }
}
