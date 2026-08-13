using PlayGround.Application.Interfaces;
using PlayGround.Application.Payment.Models;
using PlayGround.Domain.Payment;
using PlayGround.Shared.Result;
using PlayGround.Shared.Time;
using System.Diagnostics;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json.Serialization;

namespace PlayGround.Server.Services
{
    /// <summary>
    /// 토스페이먼츠 결제 어댑터 — PG 벤더를 아는 유일한 클래스.
    /// 승인은 POST /v1/payments/confirm, 인증은 Basic base64(secretKey + ":").
    /// 오류 로깅은 하지 않는다 — Result로 돌려주면 Application이 LogWith로 남긴다.
    /// </summary>
    public class TossPaymentGatewayService : IPaymentGateway
    {
        private readonly IHttpClientFactory mHttpClientFactory;
        private readonly PaymentConfiguration mConfiguration;

        public TossPaymentGatewayService(IHttpClientFactory httpClientFactory, PaymentConfiguration configuration)
        {
            Debug.Assert(httpClientFactory != null, "httpClientFactory is required");
            Debug.Assert(configuration != null, "configuration is required");
            mHttpClientFactory = httpClientFactory ?? throw new ArgumentNullException(nameof(httpClientFactory));
            mConfiguration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        }

        public PaymentProvider Provider => PaymentProvider.Toss;

        public async Task<Result<PaymentApproval>> ConfirmAsync(
            string paymentKey, string orderId, int amount, CancellationToken cancellation = default)
        {
            Debug.Assert(!string.IsNullOrEmpty(paymentKey), "paymentKey is required");
            Debug.Assert(!string.IsNullOrEmpty(orderId), "orderId is required");

            try
            {
                HttpClient client = mHttpClientFactory.CreateClient();
                using var request = new HttpRequestMessage(HttpMethod.Post, $"{mConfiguration.ApiBaseUrl}/v1/payments/confirm");
                request.Headers.Authorization = new AuthenticationHeaderValue("Basic",
                    Convert.ToBase64String(Encoding.ASCII.GetBytes($"{mConfiguration.SecretKey}:")));
                request.Content = JsonContent.Create(new { paymentKey, orderId, amount });

                using HttpResponseMessage response = await client.SendAsync(request, cancellation);
                if (!response.IsSuccessStatusCode)
                {
                    TossErrorResponse? error = await ReadSafeAsync<TossErrorResponse>(response, cancellation);
                    return Result<PaymentApproval>.Error(ErrorCode.ExternalServiceError,
                        $"{error?.Code ?? ((int)response.StatusCode).ToString()}: {error?.Message ?? "confirm request failed"}");
                }

                TossConfirmResponse? confirmed = await ReadSafeAsync<TossConfirmResponse>(response, cancellation);
                if (confirmed is null || string.IsNullOrEmpty(confirmed.PaymentKey))
                {
                    return Result<PaymentApproval>.Error(ErrorCode.ExternalServiceError, "confirm response is malformed");
                }

                return Result<PaymentApproval>.Success(new PaymentApproval
                {
                    PaymentKey = confirmed.PaymentKey,
                    OrderId = confirmed.OrderId,
                    Method = confirmed.Method,
                    ApprovedAt = confirmed.ApprovedAt ?? SystemTime.Now,
                    RawStatus = confirmed.Status ?? string.Empty
                });
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                return Result<PaymentApproval>.FromException(ex);
            }
        }

        private static async Task<T?> ReadSafeAsync<T>(HttpResponseMessage response, CancellationToken cancellation) where T : class
        {
            try
            {
                return await response.Content.ReadFromJsonAsync<T>(cancellationToken: cancellation);
            }
            catch
            {
                return null;
            }
        }

        // 토스 응답 중 우리가 쓰는 필드만 받는다. approvedAt은 오프셋 포함 ISO-8601 (+09:00) —
        // SystemTime 컨버터가 순간을 보존하며 UTC로 정규화한다.
        private sealed class TossConfirmResponse
        {
            [JsonPropertyName("paymentKey")]
            public string PaymentKey { get; set; } = string.Empty;

            [JsonPropertyName("orderId")]
            public string OrderId { get; set; } = string.Empty;

            [JsonPropertyName("method")]
            public string? Method { get; set; }

            [JsonPropertyName("approvedAt")]
            public SystemTime? ApprovedAt { get; set; }

            [JsonPropertyName("status")]
            public string? Status { get; set; }
        }

        private sealed class TossErrorResponse
        {
            [JsonPropertyName("code")]
            public string? Code { get; set; }

            [JsonPropertyName("message")]
            public string? Message { get; set; }
        }
    }
}
