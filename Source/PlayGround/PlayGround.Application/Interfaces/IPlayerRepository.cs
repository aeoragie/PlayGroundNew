using PlayGround.Shared.Result;
using PlayGround.Contracts.Player;
using PlayGround.Application.Player.Models;

namespace PlayGround.Application.Interfaces
{
    /// <summary>선수 프로필 저장·조회 포트 (Persistence에서 구현). 생성 시 새 PlayerId 반환.</summary>
    public interface IPlayerRepository
    {
        Task<Result<Guid>> CreateAsync(CreatePlayerInput input, CancellationToken cancellation = default);

        /// <summary>관리 주체(UserId) 기준 선수 프로필 묶음 조회. 선수 미존재 시 Success(null) — 에러가 아니다.</summary>
        Task<Result<PlayerInfoResponse?>> GetInfoByUserAsync(Guid userId, CancellationToken cancellation = default);

        /// <summary>항목 공개 설정 업서트. 관리 주체 소유 선수가 없으면 Success(false).</summary>
        Task<Result<bool>> SetFieldVisibilityAsync(Guid userId, string fieldName, bool isPublic, CancellationToken cancellation = default);

        /// <summary>선수 사진 설정·삭제(photoUrl null = 삭제). 권한(보호자·소속팀 관리자)은 프로시저가 판정하며
        /// 거부되거나 선수가 없으면 Success(false) — 존재 여부를 구분해 흘리지 않는다.</summary>
        Task<Result<bool>> SetPhotoAsync(Guid userId, Guid playerId, string? photoUrl, CancellationToken cancellation = default);

        /// <summary>초대코드 Claim — 성공 시 연결된 선수·팀 요약, 무효 코드·이미 연결된 선수면 Success(null).</summary>
        Task<Result<ClaimPlayerInviteResponse?>> ClaimInviteAsync(Guid userId, string code, CancellationToken cancellation = default);

        /// <summary>관리 주체(UserId) 기준 커리어 목록 조회. 이력 없으면 빈 목록 — 에러가 아니다.</summary>
        Task<Result<PlayerCareerResponse>> GetCareersByUserAsync(Guid userId, CancellationToken cancellation = default);

        /// <summary>관리 주체(UserId) 기준 포트폴리오 영상 목록 조회. 없으면 빈 목록 — 에러가 아니다.</summary>
        Task<Result<PlayerPortfolioResponse>> GetPortfolioByUserAsync(Guid userId, CancellationToken cancellation = default);

        /// <summary>관리 주체(UserId) 기준 시즌 통계(경기별 기록·팀 관점 변환) 조회. 출전 없으면 빈 목록 — 에러가 아니다.</summary>
        Task<Result<PlayerSeasonStatsResponse>> GetSeasonStatsByUserAsync(Guid userId, int seasonYear, CancellationToken cancellation = default);
    }
}
