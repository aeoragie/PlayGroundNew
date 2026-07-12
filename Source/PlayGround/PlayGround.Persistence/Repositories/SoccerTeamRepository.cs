using System.Linq;
using System.Text.Json;
using Microsoft.Extensions.Options;
using PlayGround.Shared.Result;
using PlayGround.Infrastructure.Database;
using PlayGround.Infrastructure.Database.Base;
using PlayGround.Infrastructure.Logging;
using PlayGround.Application.Interfaces;
using PlayGround.Application.Team.Models;
using PlayGround.Persistence.Database.Generated.Soccer.Entities;
using PlayGround.Persistence.Database.Generated.Soccer.Procedures;

namespace PlayGround.Persistence.Repositories
{
    /// <summary>팀+로스터 저장 (Soccer DB). 로스터는 JSON으로 넘겨 단일 프로시저가 원자적으로 생성.</summary>
    public class SoccerTeamRepository : RepositoryBase, ISoccerTeamRepository
    {
        public override DatabaseTypes Database => DatabaseTypes.Soccer;

        public SoccerTeamRepository(IOptions<DatabaseConfiguration> options) : base(options)
        {
        }

        public async Task<Result<string>> CreateWithRosterAsync(CreateTeamInput input, CancellationToken cancellation = default)
        {
            Logger.InfoWith("Team creation requested",
                ("ManagerUserId", input.ManagerUserId), ("RosterCount", input.Roster.Count));

            string rosterJson = JsonSerializer.Serialize(input.Roster);

            var procedure = new UspCreateSoccerTeamWithRoster(this)
            {
                ManagerUserId = input.ManagerUserId,
                TeamName = input.TeamName,
                TeamType = input.TeamType!,
                Region = input.Region!,
                Slug = input.Slug,
                RosterJson = rosterJson
            };

            var queryResult = await procedure.QueryAsync<SoccerCreateTeamRecord>(cancellation: cancellation);
            if (queryResult.IsError)
            {
                Logger.ErrorWith("Team creation failed", ("ResultCode", queryResult.ResultCode));
                return Result<string>.Error(ErrorCode.DatabaseError);
            }

            var row = queryResult.Values1.FirstOrDefault();
            if (row is null)
            {
                Logger.ErrorWith("Team creation returned no row");
                return Result<string>.Error(ErrorCode.OperationFailed, "no row returned");
            }

            Logger.InfoWith("Team created", ("TeamId", row.TeamId), ("Slug", row.Slug));
            return Result<string>.Success(row.Slug);
        }
    }
}
