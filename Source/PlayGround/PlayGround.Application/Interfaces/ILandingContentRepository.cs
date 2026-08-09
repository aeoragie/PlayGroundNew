using PlayGround.Contracts.Landing;
using PlayGround.Shared.Result;

namespace PlayGround.Application.Interfaces
{
    public interface ILandingContentRepository
    {
        Task<Result<LandingContentsResponse>> GetContentsAsync(CancellationToken cancellation = default);
    }
}
