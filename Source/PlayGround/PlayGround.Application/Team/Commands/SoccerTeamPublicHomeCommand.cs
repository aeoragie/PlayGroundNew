using System.Diagnostics;
using PlayGround.Shared.Result;
using PlayGround.Contracts.Team;
using PlayGround.Application.Interfaces;

namespace PlayGround.Application.Team.Commands
{
    /// <summary>공개 팀 홈페이지 조회 유즈케이스 (비로그인, Slug 기준). 비공개 팀은 NotFound.</summary>
    public class SoccerTeamPublicHomeCommand
    {
        private readonly ISoccerTeamRepository mRepository;

        public SoccerTeamPublicHomeCommand(ISoccerTeamRepository repository)
        {
            Debug.Assert(repository != null, "repository is required");
            mRepository = repository ?? throw new ArgumentNullException(nameof(repository));
        }

        /// <param name="viewerUserId">로그인 열람자 — 관리자 본인 판정(IsManager)에만 쓴다. 게스트는 null.</param>
        public async Task<Result<TeamPublicHomeResponse>> ExecuteAsync(string slug, Guid? viewerUserId = null, CancellationToken cancellation = default)
        {
            if (string.IsNullOrWhiteSpace(slug))
            {
                return Result<TeamPublicHomeResponse>.Error(ErrorCode.InvalidInput, "slug is empty");
            }

            Result<TeamPublicHomeResponse?> home = await mRepository.GetTeamHomeBySlugAsync(slug.Trim(), viewerUserId, cancellation);
            if (home.IsError)
            {
                return Result<TeamPublicHomeResponse>.Failure(home.ResultData);
            }

            if (home.Value is null)
            {
                return Result<TeamPublicHomeResponse>.Error(ErrorCode.NotFound, "team not found for slug");
            }

            return Result<TeamPublicHomeResponse>.Success(home.Value);
        }
    }
}
