using System.Diagnostics;
using PlayGround.Shared.Result;
using PlayGround.Contracts.Team;
using PlayGround.Domain.Soccer;
using PlayGround.Application.Interfaces;

namespace PlayGround.Application.Team.Commands
{
    /// <summary>진학·진로 사례 유즈케이스 (Design.TeamPublicHome ⑤) — 공개 조회 + 관리자 편집.
    /// "선수 개인이 공개에 동의한 사례만" 규칙은 입력 폼 안내 카피로 강제 — 동의 데이터는 없다.</summary>
    public class SoccerTeamCareerOutcomeCommand
    {
        private const int MaxTitleLength = 100;
        private const int MaxDetailLength = 200;
        private const int MinYear = 1990;
        private const int MaxPlayerCount = 99;

        private readonly ISoccerTeamRepository mRepository;

        public SoccerTeamCareerOutcomeCommand(ISoccerTeamRepository repository)
        {
            Debug.Assert(repository != null, "repository is required");
            mRepository = repository ?? throw new ArgumentNullException(nameof(repository));
        }

        public async Task<Result<TeamCareerOutcomesResponse>> GetBySlugAsync(string slug, CancellationToken cancellation = default)
        {
            if (string.IsNullOrWhiteSpace(slug))
            {
                return Result<TeamCareerOutcomesResponse>.Error(ErrorCode.InvalidInput, "slug is required");
            }

            return await mRepository.GetCareerOutcomesBySlugAsync(slug.Trim(), cancellation);
        }

        public async Task<Result<TeamCareerOutcomesResponse>> GetMineAsync(Guid managerUserId, CancellationToken cancellation = default)
        {
            if (managerUserId == Guid.Empty)
            {
                return Result<TeamCareerOutcomesResponse>.Error(ErrorCode.Unauthorized, "managerUserId is empty");
            }

            return await mRepository.GetCareerOutcomesByManagerAsync(managerUserId, cancellation);
        }

        public async Task<Result<TeamCareerOutcomeDto>> SaveAsync(
            Guid managerUserId, SaveTeamCareerOutcomeRequest request, CancellationToken cancellation = default)
        {
            if (managerUserId == Guid.Empty || request is null)
            {
                return Result<TeamCareerOutcomeDto>.Error(ErrorCode.Unauthorized, "managerUserId/request required");
            }

            // 클라이언트 인라인 검증과 같은 규칙 — 우회 요청도 같은 기준으로 막는다
            request.Title = request.Title?.Trim() ?? string.Empty;
            request.Detail = string.IsNullOrWhiteSpace(request.Detail) ? null : request.Detail.Trim();

            if (request.Title.Length is 0 or > MaxTitleLength)
            {
                return Result<TeamCareerOutcomeDto>.Error(ErrorCode.InvalidInput, "title is required");
            }

            if (request.Detail is { Length: > MaxDetailLength })
            {
                return Result<TeamCareerOutcomeDto>.Error(ErrorCode.InvalidInput, "detail is too long");
            }

            // 유형은 enum 화이트리스트 — 미지의 문자열은 저장 자체를 거부한다
            if (!Enum.TryParse(request.OutcomeType, out SoccerCareerOutcomeType _))
            {
                return Result<TeamCareerOutcomeDto>.Error(ErrorCode.InvalidInput, "unknown outcome type");
            }

            if (request.OutcomeYear < MinYear || request.OutcomeYear > DateTime.Now.Year + 1)
            {
                return Result<TeamCareerOutcomeDto>.Error(ErrorCode.InvalidInput, "outcome year out of range");
            }

            if (request.PlayerCount is < 1 or > MaxPlayerCount)
            {
                return Result<TeamCareerOutcomeDto>.Error(ErrorCode.InvalidInput, "player count out of range");
            }

            Result<TeamCareerOutcomeDto?> saved = await mRepository.SaveCareerOutcomeByManagerAsync(managerUserId, request, cancellation);
            if (saved.IsError)
            {
                return Result<TeamCareerOutcomeDto>.Failure(saved.ResultData);
            }

            if (saved.Value is null)
            {
                return Result<TeamCareerOutcomeDto>.Error(ErrorCode.Forbidden, "outcome not editable");
            }

            return Result<TeamCareerOutcomeDto>.Success(saved.Value);
        }

        public async Task<Result<bool>> DeleteAsync(
            Guid managerUserId, Guid outcomeId, bool restore, CancellationToken cancellation = default)
        {
            if (managerUserId == Guid.Empty || outcomeId == Guid.Empty)
            {
                return Result<bool>.Error(ErrorCode.InvalidInput, "managerUserId/outcomeId required");
            }

            Result<bool> deleted = await mRepository.DeleteCareerOutcomeByManagerAsync(managerUserId, outcomeId, restore, cancellation);
            if (deleted.IsError)
            {
                return Result<bool>.Failure(deleted.ResultData);
            }

            if (!deleted.Value)
            {
                return Result<bool>.Error(ErrorCode.Forbidden, "outcome not deletable");
            }

            return Result<bool>.Success(true);
        }
    }
}
