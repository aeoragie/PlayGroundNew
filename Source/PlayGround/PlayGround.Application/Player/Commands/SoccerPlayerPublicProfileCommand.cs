using System.Diagnostics;
using PlayGround.Shared.Result;
using PlayGround.Contracts.Player;
using PlayGround.Application.Interfaces;

namespace PlayGround.Application.Player.Commands
{
    /// <summary>공개 선수 프로필 조회 유즈케이스 (비로그인, Slug 기준 — Design.PlayerPublicProfile 디테일 공개 뷰).
    /// 미존재·프로필 비공개는 NotFound 하나로 응답 — 비공개 여부를 흘리지 않는다.</summary>
    public class SoccerPlayerPublicProfileCommand
    {
        private readonly IPlayerRepository mRepository;

        public SoccerPlayerPublicProfileCommand(IPlayerRepository repository)
        {
            Debug.Assert(repository != null, "repository is required");
            mRepository = repository ?? throw new ArgumentNullException(nameof(repository));
        }

        public async Task<Result<PlayerPublicProfileResponse>> ExecuteAsync(string slug, int seasonYear, CancellationToken cancellation = default)
        {
            if (string.IsNullOrWhiteSpace(slug))
            {
                return Result<PlayerPublicProfileResponse>.Error(ErrorCode.InvalidInput, "slug is empty");
            }

            Result<PlayerPublicProfileResponse?> profile =
                await mRepository.GetPublicProfileBySlugAsync(slug.Trim(), seasonYear, cancellation);
            if (profile.IsError)
            {
                return Result<PlayerPublicProfileResponse>.Failure(profile.ResultData);
            }

            if (profile.Value is null)
            {
                return Result<PlayerPublicProfileResponse>.Error(ErrorCode.NotFound, "player not found for slug");
            }

            return Result<PlayerPublicProfileResponse>.Success(profile.Value);
        }
    }
}
