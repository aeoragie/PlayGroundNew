using PlayGround.Shared.Result;
using PlayGround.Contracts.Records;

namespace PlayGround.Application.Interfaces
{
    /// <summary>공개 경기기록(Records) 조회 포트 (Persistence에서 구현).</summary>
    public interface ISoccerRecordsRepository
    {
        /// <summary>시즌(연도) 기준 대회/리그 목록 + 우승팀 + 기록 연도 목록. 대회 없으면 빈 목록.</summary>
        Task<Result<RecordsTournamentsResponse>> GetTournamentsBySeasonAsync(int seasonYear, CancellationToken cancellation = default);

        /// <summary>대회 상세 묶음 (순위표·경기·수상·역대 우승·미디어). 미존재 시 Success(null) — 에러가 아니다.</summary>
        Task<Result<RecordsTournamentDetailResponse?>> GetTournamentDetailAsync(Guid tournamentId, CancellationToken cancellation = default);
    }
}
