using PlayGround.Contracts.Payment;
using PlayGround.Domain.Payment;
using PlayGround.Shared.Http;
using System.Net.Http.Json;

namespace PlayGround.Client.Services
{
    /// <summary>결제 API (종목 공통 — api/payment). 코드 오류와 요청 실패를 IsNetworkError로 가른다.</summary>
    public class PaymentClient
    {
        private readonly HttpClient mHttp;

        public PaymentClient(HttpClient http)
        {
            mHttp = http ?? throw new ArgumentNullException(nameof(http));
        }

        /// <summary>위젯 초기화용 공개 설정. 실패면 null.</summary>
        public async Task<PaymentConfigResponse?> GetConfigAsync()
        {
            try
            {
                Envelope<PaymentConfigResponse>? envelope =
                    await mHttp.GetFromJsonAsync<Envelope<PaymentConfigResponse>>("api/payment/config");
                return envelope is { IsSuccess: true } ? envelope.Data : null;
            }
            catch
            {
                return null;
            }
        }

        public async Task<PaymentActionResult> CreateOrderAsync(Sport sport, string orderName, int amount)
        {
            try
            {
                HttpResponseMessage response = await mHttp.PostAsJsonAsync(
                    "api/payment/me/orders",
                    new CreatePaymentOrderRequest { Sport = sport, OrderName = orderName, Amount = amount });
                return await ReadActionAsync(response);
            }
            catch
            {
                return new PaymentActionResult(null, null, true);
            }
        }

        public async Task<PaymentActionResult> ConfirmAsync(string paymentKey, string orderId, int amount)
        {
            try
            {
                HttpResponseMessage response = await mHttp.PostAsJsonAsync(
                    "api/payment/me/confirm",
                    new ConfirmPaymentRequest { PaymentKey = paymentKey, OrderId = orderId, Amount = amount });
                return await ReadActionAsync(response);
            }
            catch
            {
                return new PaymentActionResult(null, null, true);
            }
        }

        public async Task<PaymentActionResult> FailAsync(string orderId, string failCode, string failMessage)
        {
            try
            {
                HttpResponseMessage response = await mHttp.PostAsJsonAsync(
                    "api/payment/me/fail",
                    new FailPaymentRequest { OrderId = orderId, FailCode = failCode, FailMessage = failMessage });
                return await ReadActionAsync(response);
            }
            catch
            {
                return new PaymentActionResult(null, null, true);
            }
        }

        /// <summary>내 결제 내역 최근순. 실패면 null.</summary>
        public async Task<List<PaymentSummaryResponse>?> GetHistoryAsync()
        {
            try
            {
                Envelope<List<PaymentSummaryResponse>>? envelope =
                    await mHttp.GetFromJsonAsync<Envelope<List<PaymentSummaryResponse>>>("api/payment/me/history");
                return envelope is { IsSuccess: true } ? envelope.Data : null;
            }
            catch
            {
                return null;
            }
        }

        private static async Task<PaymentActionResult> ReadActionAsync(HttpResponseMessage response)
        {
            Envelope<PaymentSummaryResponse>? envelope =
                await response.Content.ReadFromJsonAsync<Envelope<PaymentSummaryResponse>>();
            return envelope is { IsSuccess: true, Data: not null }
                ? new PaymentActionResult(envelope.Data, null, false)
                : new PaymentActionResult(null, envelope?.Message ?? envelope?.CodeName, false);
        }
    }

    public record PaymentActionResult(PaymentSummaryResponse? Payment, string? ErrorMessage, bool IsNetworkError);
}
