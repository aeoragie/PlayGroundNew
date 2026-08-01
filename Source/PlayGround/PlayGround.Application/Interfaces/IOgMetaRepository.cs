using PlayGround.Shared.Result;
using PlayGround.Application.Og;

namespace PlayGround.Application.Interfaces
{
    /// <summary>링크 공유 미리보기(OG) 카드 원자료 조회 (DECISION.OGMETA — 크롤러 경로 전용 최소 조회).
    /// 미존재·비공개는 Success(null) → 호출측이 랜딩 카드로 폴백한다.</summary>
    public interface IOgMetaRepository
    {
        /// <summary>공개 팀 카드 — 비공개·미존재는 Success(null).</summary>
        Task<Result<TeamOgCard?>> GetTeamOgAsync(string slug, CancellationToken cancellation = default);

        /// <summary>선수 이름만 — 공개(Profile) 꺼짐·미존재는 Success(null)(태그 미생성 → 폴백).</summary>
        Task<Result<string?>> GetPlayerNameOgAsync(string slug, CancellationToken cancellation = default);

        /// <summary>대회 카드 — 미존재는 Success(null).</summary>
        Task<Result<TournamentOgCard?>> GetTournamentOgAsync(Guid tournamentId, CancellationToken cancellation = default);
    }
}
