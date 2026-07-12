using Microsoft.Extensions.Options;
using PlayGround.Shared.Result;
using PlayGround.Contracts.Landing;
using PlayGround.Infrastructure.Database;
using PlayGround.Infrastructure.Database.Base;
using PlayGround.Infrastructure.Logging;
using PlayGround.Application.Interfaces;
using PlayGround.Persistence.Database.Generated.Soccer.Entities;
using PlayGround.Persistence.Database.Generated.Soccer.Procedures;

namespace PlayGround.Persistence.Repositories
{
    /// <summary>랜딩 콘텐츠 조회 (Soccer DB). 생성된 프로시저 호출 객체 + 엔티티 사용.</summary>
    public class SoccerLandingContentRepository : RepositoryBase, ILandingContentRepository
    {
        public override DatabaseTypes Database => DatabaseTypes.Soccer;

        public SoccerLandingContentRepository(IOptions<DatabaseConfiguration> options) : base(options)
        {
        }

        public async Task<Result<LandingContentsResponse>> GetContentsAsync(CancellationToken cancellation = default)
        {
            Logger.InfoWith("Landing contents requested");

            var procedure = new UspGetLandingContents(this);
            var queryResult = await procedure.QueryAsync<LandingContentRecord>(cancellation: cancellation);
            if (queryResult.IsError)
            {
                Logger.ErrorWith("Landing contents query failed", ("ResultCode", queryResult.ResultCode));
                return Result<LandingContentsResponse>.Error(ErrorCode.DatabaseError);
            }

            var rows = queryResult.Values1;
            var response = new LandingContentsResponse
            {
                Features = MapSection(rows, "Feature"),
                Steps = MapSection(rows, "HowStep")
            };

            Logger.InfoWith("Landing contents received",
                ("Features", response.Features.Count), ("Steps", response.Steps.Count));

            return Result<LandingContentsResponse>.Success(response);
        }

        private static List<LandingItemDto> MapSection(List<LandingContentRecord> rows, string section)
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
