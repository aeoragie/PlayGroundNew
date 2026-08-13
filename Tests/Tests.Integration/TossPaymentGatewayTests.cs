using FluentAssertions;
using Moq;
using Moq.Protected;
using PlayGround.Application.Payment.Models;
using PlayGround.Server.Services;
using PlayGround.Shared.Result;
using PlayGround.Shared.Time;
using System.Net;
using System.Text;
using System.Text.Json;
using Xunit;

namespace PlayGround.Tests.Integration
{
    /// <summary>
    /// 토스 결제 어댑터 계약 — 실제 API는 부르지 않는다. HttpMessageHandler 목으로
    /// 요청 형태(URL·Basic 인증·본문)와 응답 파싱(성공·오류·시각 정규화)만 검증한다.
    /// </summary>
    public class TossPaymentGatewayTests
    {
        private const string SecretKey = "test_sk_dummy";

        private static PaymentConfiguration Settings => new()
        {
            Provider = PaymentProviderKind.Toss,
            ClientKey = "test_ck_dummy",
            SecretKey = SecretKey,
            ApiBaseUrl = "https://api.test",
        };

        private static TossPaymentGatewayService ServiceOf(Mock<HttpMessageHandler> handler)
        {
            var factory = new Mock<IHttpClientFactory>();
            factory.Setup(f => f.CreateClient(It.IsAny<string>())).Returns(new HttpClient(handler.Object));
            return new TossPaymentGatewayService(factory.Object, Settings);
        }

        private static Mock<HttpMessageHandler> Handler(HttpStatusCode status, string json, Action<HttpRequestMessage>? capture = null)
        {
            var handler = new Mock<HttpMessageHandler>(MockBehavior.Strict);
            handler.Protected()
                .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
                .Callback<HttpRequestMessage, CancellationToken>((request, _) => capture?.Invoke(request))
                .ReturnsAsync(new HttpResponseMessage(status)
                {
                    Content = new StringContent(json, Encoding.UTF8, "application/json"),
                });
            return handler;
        }

        [Fact]
        public async Task ConfirmAsync_SendsBasicAuthAndBody_ToConfirmEndpoint()
        {
            HttpRequestMessage? captured = null;
            string? body = null;
            var handler = Handler(HttpStatusCode.OK,
                """{"paymentKey":"pay_1","orderId":"order_1","method":"카드","approvedAt":"2026-08-13T12:00:00+09:00","status":"DONE"}""",
                request =>
                {
                    captured = request;
                    body = request.Content!.ReadAsStringAsync().GetAwaiter().GetResult();
                });

            Result<PaymentApproval> result = await ServiceOf(handler).ConfirmAsync("pay_1", "order_1", 1000, TestContext.Current.CancellationToken);

            result.IsSuccess.Should().BeTrue();
            captured.Should().NotBeNull();
            captured!.RequestUri!.ToString().Should().Be("https://api.test/v1/payments/confirm");
            captured.Headers.Authorization!.Scheme.Should().Be("Basic");
            captured.Headers.Authorization.Parameter.Should()
                .Be(Convert.ToBase64String(Encoding.ASCII.GetBytes($"{SecretKey}:")));
            body.Should().Contain("\"paymentKey\":\"pay_1\"").And.Contain("\"orderId\":\"order_1\"").And.Contain("\"amount\":1000");
        }

        [Fact]
        public async Task ConfirmAsync_ParsesApproval_AndNormalizesOffsetToUtcInstant()
        {
            var handler = Handler(HttpStatusCode.OK,
                """{"paymentKey":"pay_1","orderId":"order_1","method":"간편결제","approvedAt":"2026-08-13T12:00:00+09:00","status":"DONE"}""");

            Result<PaymentApproval> result = await ServiceOf(handler).ConfirmAsync("pay_1", "order_1", 1000, TestContext.Current.CancellationToken);

            result.IsSuccess.Should().BeTrue();
            result.Value.PaymentKey.Should().Be("pay_1");
            result.Value.Method.Should().Be("간편결제");
            result.Value.RawStatus.Should().Be("DONE");

            // KST 오프셋 시각이 같은 순간의 UTC로 정규화돼야 한다 (+09:00 12시 = UTC 03시)
            SystemTime expected = JsonSerializer.Deserialize<SystemTime>("\"2026-08-13T03:00:00Z\"");
            result.Value.ApprovedAt.Should().Be(expected);
        }

        [Fact]
        public async Task ConfirmAsync_NonSuccess_ReturnsExternalServiceErrorWithPgCode()
        {
            var handler = Handler(HttpStatusCode.BadRequest,
                """{"code":"REJECT_CARD_COMPANY","message":"card rejected"}""");

            Result<PaymentApproval> result = await ServiceOf(handler).ConfirmAsync("pay_1", "order_1", 1000, TestContext.Current.CancellationToken);

            result.ResultData.DetailCode.Should().Be(ErrorCode.ExternalServiceError);
            result.ResultData.Message.Should().Contain("REJECT_CARD_COMPANY");
        }

        [Fact]
        public async Task ConfirmAsync_MalformedSuccessBody_IsExternalServiceError()
        {
            var handler = Handler(HttpStatusCode.OK, """{"unexpected":true}""");

            Result<PaymentApproval> result = await ServiceOf(handler).ConfirmAsync("pay_1", "order_1", 1000, TestContext.Current.CancellationToken);

            result.ResultData.DetailCode.Should().Be(ErrorCode.ExternalServiceError);
        }
    }
}
