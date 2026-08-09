using Microsoft.Extensions.Options;
using PlayGround.Application.Interfaces;
using PlayGround.Contracts.Landing;
using PlayGround.Infrastructure.Database;
using PlayGround.Infrastructure.Database.Base;
using PlayGround.Persistence.Database.Generated.Soccer.Entities;
using PlayGround.Persistence.Database.Generated.Soccer.Procedures;
using PlayGround.Shared.Result;

namespace PlayGround.Persistence.Repositories
{
    public class SoccerLandingContentRepository : RepositoryBase, ILandingContentRepository
    {
        public override DatabaseTypes Database => DatabaseTypes.Soccer;

        public SoccerLandingContentRepository(IOptions<DatabaseConfiguration> options) : base(options)
        {
        }

        public async Task<Result<LandingContentsResponse>> GetContentsAsync(CancellationToken cancellation = default)
        {
            var procedure = new UspGetLandingContents(this);
            var queryResult = await procedure.QueryAsync<SoccerLandingContentRecord>(cancellation: cancellation);
            if (queryResult.IsError)
            {
                return Result<LandingContentsResponse>.Error(ErrorCode.DatabaseError);
            }

            var rows = queryResult.Values1;
            var response = new LandingContentsResponse
            {
                Features = MapSection(rows, "Feature"),
                Steps = MapSection(rows, "HowStep")
            };

            return Result<LandingContentsResponse>.Success(response);
        }

        private static List<LandingItemDto> MapSection(List<SoccerLandingContentRecord> rows, string section)
        {
            return rows
                .Where(r => string.Equals(r.Section, section, StringComparison.OrdinalIgnoreCase))
                .Select(r => new LandingItemDto
                {
                    Icon = r.Icon,
                    Title = r.Title,
                    Body = r.Body
                })
                .ToList();
        }
    }
}
