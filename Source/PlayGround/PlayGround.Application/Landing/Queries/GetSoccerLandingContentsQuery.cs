using System.Diagnostics;
using PlayGround.Shared.Result;
using PlayGround.Contracts.Landing;
using PlayGround.Application.Interfaces;

namespace PlayGround.Application.Landing.Queries
{
    /// <summary>랜딩 콘텐츠 조회 유즈케이스. 컨트롤러 → 이 핸들러 → 포트.</summary>
    public class GetSoccerLandingContentsQuery
    {
        private readonly ILandingContentRepository mRepository;

        public GetSoccerLandingContentsQuery(ILandingContentRepository repository)
        {
            Debug.Assert(repository != null, "repository is required");
            mRepository = repository ?? throw new ArgumentNullException(nameof(repository));
        }

        public async Task<Result<LandingContentsResponse>> ExecuteAsync(CancellationToken cancellation = default)
        {
            return await mRepository.GetContentsAsync(cancellation);
        }
    }
}
