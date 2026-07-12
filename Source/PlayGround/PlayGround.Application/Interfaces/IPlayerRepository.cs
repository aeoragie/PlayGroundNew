using PlayGround.Shared.Result;
using PlayGround.Application.Player.Models;

namespace PlayGround.Application.Interfaces
{
    /// <summary>선수 프로필 저장 포트 (Persistence에서 구현). 생성 시 새 PlayerId 반환.</summary>
    public interface IPlayerRepository
    {
        Task<Result<Guid>> CreateAsync(CreatePlayerInput input, CancellationToken cancellation = default);
    }
}
