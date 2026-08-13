using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PlayGround.Application.Payment.Commands;
using PlayGround.Contracts.Payment;
using PlayGround.Server.Actors;
using PlayGround.Server.Services;
using PlayGround.Shared.Http;
using PlayGround.Shared.Result;
using System.Security.Claims;

namespace PlayGround.Server.Controllers.Payment
{
    // 결제는 종목 공통이라 비종목 라우트(api/payment)다 — Auth 계열과 같은 방식.
    [ApiController]
    [Route("api/payment")]
    [Authorize]
    public class PaymentController : ControllerBase
    {
        private readonly ActorGateway mGateway;
        private readonly PaymentConfiguration mConfiguration;

        public PaymentController(ActorGateway gateway, PaymentConfiguration configuration)
        {
            mGateway = gateway;
            mConfiguration = configuration;
        }

        /// <summary>위젯 초기화용 공개 설정. 시크릿 키는 절대 싣지 않는다.</summary>
        [HttpGet("config")]
        public Envelope<PaymentConfigResponse> GetConfig()
        {
            var response = new PaymentConfigResponse
            {
                Enabled = mConfiguration.IsEnabled,
                ClientKey = mConfiguration.ClientKey
            };
            return Result<PaymentConfigResponse>.Success(response).ToEnvelope();
        }

        [HttpPost("me/orders")]
        public async Task<Envelope<PaymentSummaryResponse>> CreateOrderAsync(
            [FromBody] CreatePaymentOrderRequest request, CancellationToken cancellation)
        {
            string? sub = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!Guid.TryParse(sub, out Guid userId))
            {
                return Result<PaymentSummaryResponse>.Error(ErrorCode.Unauthorized, "Invalid subject").ToEnvelope();
            }

            Result<PaymentSummaryResponse> result = await mGateway.AskAsync<PaymentSummaryResponse>(
                ActorNames.Payment, new CreatePaymentOrderMessage(userId, request), cancellation);
            return result.ToEnvelope();
        }

        [HttpPost("me/confirm")]
        public async Task<Envelope<PaymentSummaryResponse>> ConfirmAsync(
            [FromBody] ConfirmPaymentRequest request, CancellationToken cancellation)
        {
            string? sub = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!Guid.TryParse(sub, out Guid userId))
            {
                return Result<PaymentSummaryResponse>.Error(ErrorCode.Unauthorized, "Invalid subject").ToEnvelope();
            }

            Result<PaymentSummaryResponse> result = await mGateway.AskAsync<PaymentSummaryResponse>(
                ActorNames.Payment, new ConfirmPaymentMessage(userId, request), cancellation);
            return result.ToEnvelope();
        }

        [HttpPost("me/fail")]
        public async Task<Envelope<PaymentSummaryResponse>> FailAsync(
            [FromBody] FailPaymentRequest request, CancellationToken cancellation)
        {
            string? sub = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!Guid.TryParse(sub, out Guid userId))
            {
                return Result<PaymentSummaryResponse>.Error(ErrorCode.Unauthorized, "Invalid subject").ToEnvelope();
            }

            Result<PaymentSummaryResponse> result = await mGateway.AskAsync<PaymentSummaryResponse>(
                ActorNames.Payment, new FailPaymentMessage(userId, request), cancellation);
            return result.ToEnvelope();
        }

        // 조회는 액터를 거치지 않는다 (ActorVsDirectCall — 단순 조회는 직접 호출).
        [HttpGet("me/history")]
        public async Task<Envelope<List<PaymentSummaryResponse>>> GetHistoryAsync(
            [FromServices] PaymentHistoryCommand command, CancellationToken cancellation)
        {
            string? sub = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!Guid.TryParse(sub, out Guid userId))
            {
                return Result<List<PaymentSummaryResponse>>.Error(ErrorCode.Unauthorized, "Invalid subject").ToEnvelope();
            }

            Result<List<PaymentSummaryResponse>> result = await command.ExecuteAsync(userId, cancellation);
            return result.ToEnvelope();
        }
    }
}
