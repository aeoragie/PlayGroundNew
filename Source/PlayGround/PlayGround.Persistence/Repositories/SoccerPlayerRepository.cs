using Microsoft.Extensions.Options;
using PlayGround.Shared.Result;
using PlayGround.Infrastructure.Database;
using PlayGround.Infrastructure.Database.Base;
using PlayGround.Infrastructure.Logging;
using PlayGround.Application.Interfaces;
using PlayGround.Application.Player.Models;
using PlayGround.Persistence.Database.Generated.Soccer.Entities;
using PlayGround.Persistence.Database.Generated.Soccer.Procedures;

namespace PlayGround.Persistence.Repositories
{
    /// <summary>선수 프로필 저장 (Soccer DB). 생성된 프로시저 객체 + SoccerCreatePlayerRecord 사용.</summary>
    public class SoccerPlayerRepository : RepositoryBase, IPlayerRepository
    {
        public override DatabaseTypes Database => DatabaseTypes.Soccer;

        public SoccerPlayerRepository(IOptions<DatabaseConfiguration> options) : base(options)
        {
        }

        public async Task<Result<Guid>> CreateAsync(CreatePlayerInput input, CancellationToken cancellation = default)
        {
            Logger.InfoWith("Player profile creation requested", ("UserId", input.UserId));

            var procedure = new UspCreatePlayer(this)
            {
                UserId = input.UserId,
                Name = input.Name,
                BirthDate = input.BirthDate?.ToDateTime(TimeOnly.MinValue),
                AgeGroup = input.AgeGroup!,
                Region = input.Region!
            };

            var queryResult = await procedure.QueryAsync<SoccerCreatePlayerRecord>(cancellation: cancellation);
            if (queryResult.IsError)
            {
                Logger.ErrorWith("Player profile creation failed", ("ResultCode", queryResult.ResultCode));
                return Result<Guid>.Error(ErrorCode.DatabaseError);
            }

            var row = queryResult.Values1.FirstOrDefault();
            if (row is null)
            {
                Logger.ErrorWith("Player profile creation returned no row");
                return Result<Guid>.Error(ErrorCode.OperationFailed, "no row returned");
            }

            Logger.InfoWith("Player profile created", ("PlayerId", row.PlayerId));
            return Result<Guid>.Success(row.PlayerId);
        }
    }
}
