using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PlayGround.Shared.Http;
using PlayGround.Shared.Result;
using PlayGround.Contracts.Notification;
using PlayGround.Server.Actors;

namespace PlayGround.Server.Controllers.Soccer
{
    /// <summary>알림 센터 — 벨 카운트·목록(GET 하나로 공유)·읽음 처리.</summary>
    [ApiController]
    [Route("api/soccer/notifications")]
    [Authorize]
    public class SoccerNotificationController : ControllerBase
    {
        private readonly ActorGateway mGateway;

        public SoccerNotificationController(ActorGateway gateway)
        {
            mGateway = gateway;
        }

        private Guid CurrentUserId =>
            Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out Guid id) ? id : Guid.Empty;

        /// <summary>미읽음 카운트 + 최근 목록. 기록 수정 심사 결과는 이 조회 시점에 지연 생성된다.</summary>
        [HttpGet("me")]
        public async Task<Envelope<NotificationsResponse>> GetAsync(CancellationToken cancellation)
        {
            Guid userId = CurrentUserId;
            if (userId == Guid.Empty)
            {
                return Result<NotificationsResponse>.Error(ErrorCode.Unauthorized, "Invalid subject").ToEnvelope();
            }

            Result<NotificationsResponse> result = await mGateway.AskAsync<NotificationsResponse>(
                ActorNames.SoccerClaim, new GetNotificationsMessage(userId), cancellation);
            return result.ToEnvelope();
        }

        /// <summary>알림 센터 페이지 — 세그먼트 필터(all|action|unread) + 페이지네이션(더 보기 20).</summary>
        [HttpGet("me/page")]
        public async Task<Envelope<NotificationPageResponse>> GetPageAsync(
            [FromQuery] string? filter, [FromQuery] int offset, [FromQuery] int limit, CancellationToken cancellation)
        {
            Guid userId = CurrentUserId;
            if (userId == Guid.Empty)
            {
                return Result<NotificationPageResponse>.Error(ErrorCode.Unauthorized, "Invalid subject").ToEnvelope();
            }

            Result<NotificationPageResponse> result = await mGateway.AskAsync<NotificationPageResponse>(
                ActorNames.SoccerClaim, new GetNotificationPageMessage(userId, filter ?? "all", offset, limit == 0 ? 20 : limit), cancellation);
            return result.ToEnvelope();
        }

        /// <summary>여러 건 읽음 처리 — 페이지 진입 시 화면에 보인 알림을 한 번에.</summary>
        [HttpPut("me/read")]
        public async Task<Envelope<int>> MarkReadBulkAsync([FromBody] MarkNotificationsReadRequest request, CancellationToken cancellation)
        {
            Guid userId = CurrentUserId;
            if (userId == Guid.Empty)
            {
                return Result<int>.Error(ErrorCode.Unauthorized, "Invalid subject").ToEnvelope();
            }

            Result<int> result = await mGateway.AskAsync<int>(
                ActorNames.SoccerClaim,
                new MarkNotificationsReadMessage(userId, request?.NotificationIds ?? new List<Guid>()), cancellation);
            return result.ToEnvelope();
        }

        /// <summary>읽음 처리 — 이동형 클릭 시 (액션형은 승인/거절 처리 시 서버가 마킹).</summary>
        [HttpPut("me/{notificationId:guid}/read")]
        public async Task<Envelope<bool>> MarkReadAsync(Guid notificationId, CancellationToken cancellation)
        {
            Guid userId = CurrentUserId;
            if (userId == Guid.Empty)
            {
                return Result<bool>.Error(ErrorCode.Unauthorized, "Invalid subject").ToEnvelope();
            }

            Result<bool> result = await mGateway.AskAsync<bool>(
                ActorNames.SoccerClaim, new MarkNotificationReadMessage(userId, notificationId), cancellation);
            return result.ToEnvelope();
        }
    }
}
