using PlayGround.Shared.Result;
using PlayGround.Contracts.Landing;

namespace PlayGround.Application.Interfaces
{
    /// <summary>랜딩 콘텐츠 조회 포트 (Persistence에서 구현)</summary>
    public interface ILandingContentRepository
    {
        Task<Result<LandingContentsResponse>> GetContentsAsync(CancellationToken cancellation = default);
    }
}
