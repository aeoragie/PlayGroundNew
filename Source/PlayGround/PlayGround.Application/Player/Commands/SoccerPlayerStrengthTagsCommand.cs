using System.Diagnostics;
using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;
using PlayGround.Shared.Logging;
using PlayGround.Shared.Result;
using PlayGround.Contracts.Player;
using PlayGround.Application.Interfaces;

namespace PlayGround.Application.Player.Commands
{
    /// <summary>선수 강점 태그 조회(프리셋)·저장 유즈케이스 (Design.StrengthTags). 관리 주체(보호자·선수 본인)만 —
    /// UserId로 소유 선수를 해석하므로 타인 프로필은 변경할 수 없다. 개수·길이·금지 패턴 검증이 이 계층의 몫이다.</summary>
    public class SoccerPlayerStrengthTagsCommand
    {
        private const int MaxTags = 5;

        /// <summary>태그 한 개 길이 범위 (한글은 1자로 센다 = string.Length).</summary>
        private const int MinTagLength = 1;
        private const int MaxTagLength = 12;

        // 연락처·링크 유입 차단 — 8자리 이상 연속 숫자(전화·계좌)는 금지.
        private static readonly Regex LongDigitRun = new(@"\d{8,}", RegexOptions.Compiled);

        private readonly IPlayerRepository mRepository;

        private readonly ILogger<SoccerPlayerStrengthTagsCommand> mLogger;

        public SoccerPlayerStrengthTagsCommand(IPlayerRepository repository, ILogger<SoccerPlayerStrengthTagsCommand> logger)
        {
            Debug.Assert(repository != null, "repository is required");
            mRepository = repository ?? throw new ArgumentNullException(nameof(repository));
            mLogger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<Result<StrengthTagPresetsResponse>> GetPresetsAsync(CancellationToken cancellation = default) =>
            (await GetPresetsCoreAsync(cancellation)).LogWith(mLogger, "GetPresets");

        private async Task<Result<StrengthTagPresetsResponse>> GetPresetsCoreAsync(CancellationToken cancellation = default)
        {
            Result<List<StrengthTagPresetDto>> result = await mRepository.GetStrengthTagPresetsAsync(cancellation);
            if (result.IsError)
            {
                return Result<StrengthTagPresetsResponse>.Failure(result.ResultData);
            }

            return Result<StrengthTagPresetsResponse>.Success(new StrengthTagPresetsResponse { Presets = result.Value });
        }

        public async Task<Result<bool>> SaveAsync(Guid userId, SaveStrengthTagsRequest request, Guid? playerId = null, CancellationToken cancellation = default) =>
            (await SaveCoreAsync(userId, request, playerId, cancellation)).LogWith(mLogger, "Save", ("UserId", userId));

        private async Task<Result<bool>> SaveCoreAsync(Guid userId, SaveStrengthTagsRequest request, Guid? playerId = null, CancellationToken cancellation = default)
        {
            if (userId == Guid.Empty)
            {
                return Result<bool>.Error(ErrorCode.Unauthorized, "userId is empty");
            }

            if (request is null)
            {
                return Result<bool>.Error(ErrorCode.InvalidInput, "request is null");
            }

            // 정규화 — 공백 정리 + 앞 '#' 하나 제거 후 다시 정리, 빈 값은 버린다
            List<string> cleaned = new();
            foreach (string raw in request.Tags ?? new List<string>())
            {
                if (raw is null)
                {
                    continue;
                }

                string tag = raw.Trim();
                if (tag.StartsWith('#'))
                {
                    tag = tag[1..].Trim();
                }

                if (tag.Length == 0)
                {
                    continue;
                }

                // 중복은 조용히 제거 — 대소문자 구분 완전 일치, 첫 등장 순서 유지
                if (!cleaned.Contains(tag, StringComparer.Ordinal))
                {
                    cleaned.Add(tag);
                }
            }

            if (cleaned.Count > MaxTags)
            {
                return Result<bool>.Error(ErrorCode.InvalidInput, "too many tags");
            }

            foreach (string tag in cleaned)
            {
                if (tag.Length < MinTagLength || tag.Length > MaxTagLength)
                {
                    return Result<bool>.Error(ErrorCode.OutOfRange, "tag length out of range");
                }

                // 연락처·링크 유입 차단 — 저장 화이트리스트라 클라이언트 우회도 서버가 막는다
                if (tag.Contains('@')
                    || tag.Contains("http", StringComparison.OrdinalIgnoreCase)
                    || LongDigitRun.IsMatch(tag))
                {
                    return Result<bool>.Error(ErrorCode.InvalidInput, "contact or link not allowed");
                }
            }

            // 빈 목록은 태그를 비우는 것 — 프로시저가 NULL을 '없음'으로 처리한다
            string? tagsJson = cleaned.Count > 0 ? JsonSerializer.Serialize(cleaned) : null;

            Result<bool> applied = await mRepository.SaveStrengthTagsAsync(userId, tagsJson, playerId, cancellation);
            if (applied.IsError)
            {
                return applied;
            }

            // 반영 안 됨 = 소유 선수 없음 → 타인 프로필 시도(존재 여부 미노출)
            if (!applied.Value)
            {
                return Result<bool>.Error(ErrorCode.Forbidden, "player not found for user");
            }

            return Result<bool>.Success(true);
        }
    }
}
