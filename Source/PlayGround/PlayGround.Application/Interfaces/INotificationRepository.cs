using PlayGround.Shared.Result;
using PlayGround.Contracts.Notification;

namespace PlayGround.Application.Interfaces
{
    /// <summary>알림 수신자 한 명 (친선경기 결과 발송 대상 — 자녀별 1행).</summary>
    public class NotificationRecipient
    {
        public Guid UserId { get; set; }
        public Guid PlayerId { get; set; }
        public string PlayerName { get; set; } = string.Empty;
        public string TeamName { get; set; } = string.Empty;
    }

    /// <summary>알림 센터 포트. 기록 수정 심사 결과는 조회 시점에 지연 생성된다 (발송 훅이 없다 — 설계 결정 6·7).</summary>
    public interface INotificationRepository
    {
        /// <summary>미읽음 카운트 + 최근 목록. 조회 전에 심사 완료 신청을 알림으로 동기화한다.</summary>
        Task<Result<NotificationsResponse>> GetByUserAsync(Guid userId, CancellationToken cancellation = default);

        /// <summary>읽음 처리 — 본인 것만. 남의 알림·미존재는 Success(false).</summary>
        Task<Result<bool>> MarkReadAsync(Guid userId, Guid notificationId, CancellationToken cancellation = default);

        /// <summary>친선경기 결과 알림 수신자 — 관리자 팀의 Claimed 선수들 (설정 필터는 호출측이 Account에서).</summary>
        Task<Result<List<NotificationRecipient>>> GetMatchResultRecipientsAsync(Guid managerUserId, CancellationToken cancellation = default);

        /// <summary>범용 단건 발송 (멱등 — 같은 수신자·유형·참조가 있으면 만들지 않는다).</summary>
        Task<Result<bool>> CreateAsync(Guid recipientUserId, string type, Guid refId, Guid? targetPlayerId,
            string? actorName, string? playerName, string? teamName, string? metaText, string? subText,
            CancellationToken cancellation = default);
    }
}
