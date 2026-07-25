using System.Diagnostics;
using System.Text.RegularExpressions;
using PlayGround.Shared.Result;
using PlayGround.Contracts.Player;
using PlayGround.Application.Interfaces;

namespace PlayGround.Application.Player.Commands
{
    /// <summary>선수 프로필 수치(키·몸무게·주발·학교)·주소(슬러그) 편집 유즈케이스. 관리 주체(보호자)만 —
    /// UserId로 소유 선수를 해석하므로 타인 프로필은 변경할 수 없다.
    /// SoccerPreferredFoot enum은 Client 전용이라 여기선 화이트리스트를 직접 검사한다(로스터 AgeGroup 전례).</summary>
    public class SoccerPlayerProfileInfoUpdateCommand
    {
        private static readonly string[] AllowedFeet = { "Left", "Right", "Both" };

        // 소문자 영숫자, 하이픈 허용(양끝·연속 하이픈 금지), 3~30자.
        private static readonly Regex SlugPattern = new(@"^[a-z0-9]+(-[a-z0-9]+)*$", RegexOptions.Compiled);

        // 공개 프로필 주소로 혼동을 줄 수 있는 예약어 — 슬러그로 못 쓴다.
        private static readonly HashSet<string> ReservedSlugs = new(StringComparer.OrdinalIgnoreCase)
        {
            "me", "api", "admin", "new", "edit", "player", "team", "teams",
            "settings", "dashboard", "card", "login", "records", "claim"
        };

        private readonly IPlayerRepository mRepository;

        public SoccerPlayerProfileInfoUpdateCommand(IPlayerRepository repository)
        {
            Debug.Assert(repository != null, "repository is required");
            mRepository = repository ?? throw new ArgumentNullException(nameof(repository));
        }

        public async Task<Result<UpdatePlayerProfileInfoResponse>> ExecuteAsync(Guid userId, UpdatePlayerProfileInfoRequest request, Guid? playerId = null, CancellationToken cancellation = default)
        {
            if (userId == Guid.Empty)
            {
                return Result<UpdatePlayerProfileInfoResponse>.Error(ErrorCode.Unauthorized, "userId is empty");
            }

            if (request is null)
            {
                return Result<UpdatePlayerProfileInfoResponse>.Error(ErrorCode.InvalidInput, "request is null");
            }

            // 값 검증 — 저장 화이트리스트라 클라이언트 우회도 서버가 막는다.
            if (request.HeightCm is { } h && (h < 100 || h > 230))
            {
                return Result<UpdatePlayerProfileInfoResponse>.Error(ErrorCode.OutOfRange, "heightCm must be 100-230");
            }

            if (request.WeightKg is { } w && (w < 20 || w > 150))
            {
                return Result<UpdatePlayerProfileInfoResponse>.Error(ErrorCode.OutOfRange, "weightKg must be 20-150");
            }

            string? foot = string.IsNullOrWhiteSpace(request.PreferredFoot) ? null : request.PreferredFoot.Trim();
            if (foot is not null && Array.IndexOf(AllowedFeet, foot) < 0)
            {
                return Result<UpdatePlayerProfileInfoResponse>.Error(ErrorCode.InvalidInput, "unknown preferred foot");
            }

            string? school = string.IsNullOrWhiteSpace(request.SchoolName) ? null : request.SchoolName.Trim();
            if (school is { Length: > 100 })
            {
                return Result<UpdatePlayerProfileInfoResponse>.Error(ErrorCode.OutOfRange, "schoolName too long");
            }

            // 슬러그(선택) — 소문자화 후 형식·길이·예약어 검사. null = 변경 안 함.
            string? slug = string.IsNullOrWhiteSpace(request.Slug) ? null : request.Slug.Trim().ToLowerInvariant();
            if (slug is not null)
            {
                if (slug.Length < 3 || slug.Length > 30)
                {
                    return Result<UpdatePlayerProfileInfoResponse>.Error(ErrorCode.OutOfRange, "slug must be 3-30 chars");
                }

                if (!SlugPattern.IsMatch(slug))
                {
                    return Result<UpdatePlayerProfileInfoResponse>.Error(ErrorCode.InvalidInput, "slug format invalid");
                }

                if (ReservedSlugs.Contains(slug))
                {
                    return Result<UpdatePlayerProfileInfoResponse>.Error(ErrorCode.InvalidInput, "slug is reserved");
                }
            }

            Result<string?> result = await mRepository.UpdateProfileInfoAsync(
                userId, request.HeightCm, request.WeightKg, foot, school, slug, playerId, cancellation);
            if (result.IsError)
            {
                return Result<UpdatePlayerProfileInfoResponse>.Failure(result.ResultData);
            }

            // 반환 슬러그 null = 소유 선수 없음 → 타인 프로필 시도(존재 여부 미노출).
            if (result.Value is null)
            {
                return Result<UpdatePlayerProfileInfoResponse>.Error(ErrorCode.Forbidden, "player not found for user");
            }

            // 슬러그를 바꾸려 했는데 반환값이 요청과 다르면 = 이미 남이 쓰는 중(수치는 반영됨).
            bool slugTaken = slug is not null && !string.Equals(result.Value, slug, StringComparison.Ordinal);
            return Result<UpdatePlayerProfileInfoResponse>.Success(new UpdatePlayerProfileInfoResponse { SlugTaken = slugTaken });
        }
    }
}
