using Microsoft.Extensions.Options;
using PlayGround.Shared.Result;
using PlayGround.Infrastructure.Database;
using PlayGround.Infrastructure.Database.Base;
using PlayGround.Application.Og;
using PlayGround.Application.Interfaces;
using PlayGround.Persistence.Database.Generated.Soccer.Entities;
using PlayGround.Persistence.Database.Generated.Soccer.Procedures;

namespace PlayGround.Persistence.Repositories
{
    /// <summary>OG 카드 원자료 조회 (Soccer DB). 크롤러 경로라 최소 조회 — 비공개·미존재는 null.</summary>
    public class OgMetaRepository : RepositoryBase, IOgMetaRepository
    {
        public override DatabaseTypes Database => DatabaseTypes.Soccer;

        public OgMetaRepository(IOptions<DatabaseConfiguration> options) : base(options)
        {
        }

        public async Task<Result<TeamOgCard?>> GetTeamOgAsync(string slug, CancellationToken cancellation = default)
        {
            var procedure = new UspGetSoccerTeamOgBySlug(this) { Slug = slug };
            Result<MultiQueryReader> opened = await ProcedureMultipleAsync(procedure, cancellation: cancellation);
            if (opened.IsError)
            {
                return Result<TeamOgCard?>.Error(ErrorCode.DatabaseError, "GetTeamOg");
            }

            using MultiQueryReader reader = opened.Value;
            SoccerTeamsEntity? team = await reader.ReadSingleOrDefaultAsync<SoccerTeamsEntity>();
            int playerCount = await reader.ReadSingleOrDefaultAsync<int>();

            if (team is null)
            {
                return Result<TeamOgCard?>.Success(null);
            }

            return Result<TeamOgCard?>.Success(new TeamOgCard
            {
                TeamName = team.TeamName,
                Region = NullIfEmpty(team.Region),
                AgeGroup = NullIfEmpty(team.AgeGroup),
                PlayerCount = playerCount,
                LogoUrl = NullIfEmpty(team.LogoUrl)
            });
        }

        public async Task<Result<string?>> GetPlayerNameOgAsync(string slug, CancellationToken cancellation = default)
        {
            var procedure = new UspGetSoccerPlayerOgBySlug(this) { Slug = slug };
            var queryResult = await procedure.QueryAsync<string>(cancellation: cancellation);
            if (queryResult.IsError)
            {
                return Result<string?>.Error(ErrorCode.DatabaseError, "GetPlayerNameOg");
            }

            return Result<string?>.Success(NullIfEmpty(queryResult.Values1.FirstOrDefault()));
        }

        public async Task<Result<TournamentOgCard?>> GetTournamentOgAsync(Guid tournamentId, CancellationToken cancellation = default)
        {
            var procedure = new UspGetSoccerTournamentOgById(this) { TournamentId = tournamentId };
            var queryResult = await procedure.QueryAsync<SoccerTournamentsEntity>(cancellation: cancellation);
            if (queryResult.IsError)
            {
                return Result<TournamentOgCard?>.Error(ErrorCode.DatabaseError, "GetTournamentOg");
            }

            SoccerTournamentsEntity? row = queryResult.Values1.FirstOrDefault();
            if (row is null)
            {
                return Result<TournamentOgCard?>.Success(null);
            }

            return Result<TournamentOgCard?>.Success(new TournamentOgCard
            {
                Name = row.Name,
                AgeGroup = NullIfEmpty(row.AgeGroup),
                StartDate = row.StartDate,
                EndDate = row.EndDate,
                TeamCount = row.TeamCount ?? 0
            });
        }

        private static string? NullIfEmpty(string? value)
        {
            return string.IsNullOrEmpty(value) ? null : value;
        }
    }
}
