using PlayGround.Shared.Result;
using PlayGround.Contracts.Claim;

namespace PlayGround.Application.Interfaces
{
    /// <summary>Claim 플로우(연결 요청) 포트 — /claim 4스텝 + 팀 관리자 승인.
    /// 무효 코드·권한 없음은 전부 Success(null) — 사유를 구분해 흘리지 않는다(코드 추측 대비).</summary>
    public interface IClaimRepository
    {
        /// <summary>스텝 ①→②: 코드로 선수 카드 조회 (소진 없음).</summary>
        Task<Result<ClaimInviteCardResponse?>> GetInviteCardAsync(string code, CancellationToken cancellation = default);

        /// <summary>스텝 ②→③: 연결 요청 생성 + 팀 관리자 알림 (멱등 — 기존 Pending이 있으면 그대로 반환).</summary>
        Task<Result<ClaimRequestSummaryResponse?>> CreateRequestAsync(Guid userId, string requesterName, string code, string relation, CancellationToken cancellation = default);

        /// <summary>재방문 복원 — 내 최신 요청 1건. 없으면 Success(null).</summary>
        Task<Result<ClaimRequestSummaryResponse?>> GetOwnRequestAsync(Guid userId, CancellationToken cancellation = default);

        /// <summary>승인/거절 — 소유 팀 관리자만. 승인은 선수 연결·가족 연결·코드 소진·알림을 한 트랜잭션으로.</summary>
        Task<Result<ReviewClaimResponse?>> ReviewAsync(Guid managerUserId, Guid requestId, bool approve, CancellationToken cancellation = default);
    }
}
