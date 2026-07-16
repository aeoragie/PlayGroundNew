using Microsoft.Extensions.Options;
using PlayGround.Shared.Result;
using PlayGround.Infrastructure.Database;
using PlayGround.Infrastructure.Database.Base;
using PlayGround.Infrastructure.Logging;
using PlayGround.Contracts.Records;
using PlayGround.Application.Interfaces;
using PlayGround.Persistence.Database.Generated.Soccer.Entities;
using PlayGround.Persistence.Database.Generated.Soccer.Procedures;

namespace PlayGround.Persistence.Repositories
{
    /// <summary>공개 경기기록(Records) 조회 (Soccer DB). 다중 결과셋 — MultiQueryReader 소비.</summary>
    public class SoccerRecordsRepository : RepositoryBase, ISoccerRecordsRepository
    {
        public override DatabaseTypes Database => DatabaseTypes.Soccer;

        public SoccerRecordsRepository(IOptions<DatabaseConfiguration> options) : base(options)
        {
        }

        public async Task<Result<RecordsTournamentsResponse>> GetTournamentsBySeasonAsync(int seasonYear, CancellationToken cancellation = default)
        {
            Logger.InfoWith("Records tournaments requested", ("SeasonYear", seasonYear));

            var procedure = new UspGetSoccerTournamentsBySeason(this) { SeasonYear = seasonYear };
            Result<MultiQueryReader> opened = await ProcedureMultipleAsync(procedure, cancellation: cancellation);
            if (opened.IsError)
            {
                Logger.ErrorWith("Records tournaments query failed", ("DetailCode", opened.ResultData.DetailCode));
                return Result<RecordsTournamentsResponse>.Error(ErrorCode.DatabaseError);
            }

            using MultiQueryReader reader = opened.Value;
            var tournaments = (await reader.ReadAsync<SoccerTournamentsEntity>()).ToList();
            var champions = (await reader.ReadAsync<SoccerTournamentAwardsEntity>()).ToList();
            var seasonYears = (await reader.ReadAsync<int>()).ToList();

            var response = new RecordsTournamentsResponse
            {
                SeasonYear = seasonYear,
                SeasonYears = seasonYears,
                Tournaments = tournaments
                    .Select(t => new RecordsTournamentDto
                    {
                        TournamentId = t.TournamentId,
                        Name = t.Name,
                        Format = t.Format,
                        Scope = t.Scope,
                        AgeGroup = t.AgeGroup,
                        RegionGroup = NullIfEmpty(t.RegionGroup),
                        Status = t.Status,
                        TeamCount = t.TeamCount,
                        ChampionTeamName = champions.FirstOrDefault(c => c.TournamentId == t.TournamentId)?.TeamName
                    })
                    .ToList()
            };

            Logger.InfoWith("Records tournaments received",
                ("SeasonYear", seasonYear), ("Tournaments", response.Tournaments.Count), ("Years", seasonYears.Count));

            return Result<RecordsTournamentsResponse>.Success(response);
        }

        private static string? NullIfEmpty(string? value)
        {
            return string.IsNullOrEmpty(value) ? null : value;
        }
    }
}
