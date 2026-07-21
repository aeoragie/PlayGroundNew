using System.Diagnostics;
using PlayGround.Shared.Result;
using PlayGround.Contracts.Team;
using PlayGround.Application.Interfaces;

namespace PlayGround.Application.Team.Commands
{
    /// <summary>모집 공고 유즈케이스 — 공개 열람(슬러그) + 소유자 편집(작성·수정·마감·삭제).
    /// 지원(Application) 플로우는 별도 스키마로 후속 — 여기는 공고만 다룬다.</summary>
    public class SoccerTeamRecruitmentCommand
    {
        private const int MaxTitleLength = 100;
        private const int MaxDescriptionLength = 500;
        private const int MaxConditions = 4;
        private const int MaxConditionLength = 30;

        private readonly ISoccerTeamRepository mRepository;

        public SoccerTeamRecruitmentCommand(ISoccerTeamRepository repository)
        {
            Debug.Assert(repository != null, "repository is required");
            mRepository = repository ?? throw new ArgumentNullException(nameof(repository));
        }

        public async Task<Result<TeamRecruitmentsResponse>> GetBySlugAsync(string slug, CancellationToken cancellation = default)
        {
            if (string.IsNullOrWhiteSpace(slug))
            {
                return Result<TeamRecruitmentsResponse>.Error(ErrorCode.InvalidInput, "slug is required");
            }

            return await mRepository.GetRecruitmentsBySlugAsync(slug.Trim(), cancellation);
        }

        public async Task<Result<TeamRecruitmentsResponse>> GetMineAsync(Guid managerUserId, CancellationToken cancellation = default)
        {
            if (managerUserId == Guid.Empty)
            {
                return Result<TeamRecruitmentsResponse>.Error(ErrorCode.Unauthorized, "managerUserId is empty");
            }

            return await mRepository.GetRecruitmentsByManagerAsync(managerUserId, cancellation);
        }

        public async Task<Result<TeamRecruitmentDto>> SaveAsync(
            Guid managerUserId, SaveTeamRecruitmentRequest request, CancellationToken cancellation = default)
        {
            if (managerUserId == Guid.Empty || request is null)
            {
                return Result<TeamRecruitmentDto>.Error(ErrorCode.Unauthorized, "managerUserId/request required");
            }

            // 클라이언트 인라인 검증과 같은 규칙 — 우회 요청도 같은 기준으로 막는다
            request.Title = request.Title?.Trim() ?? string.Empty;
            request.Description = request.Description?.Trim() ?? string.Empty;
            request.Conditions = (request.Conditions ?? new List<string>())
                .Select(c => c.Trim())
                .Where(c => c.Length > 0)
                .ToList();

            if (request.Title.Length is 0 or > MaxTitleLength)
            {
                return Result<TeamRecruitmentDto>.Error(ErrorCode.InvalidInput, "title is required");
            }

            if (request.Description.Length is 0 or > MaxDescriptionLength)
            {
                return Result<TeamRecruitmentDto>.Error(ErrorCode.InvalidInput, "description is required");
            }

            if (request.Conditions.Count > MaxConditions || request.Conditions.Any(c => c.Length > MaxConditionLength))
            {
                return Result<TeamRecruitmentDto>.Error(ErrorCode.InvalidInput, "too many or too long conditions");
            }

            // 마감일은 과거일 수 없다 (수정으로 이미 지난 마감일을 유지하는 것도 막는다 — 마감 처리로 해결)
            if (request.DeadlineDate is not null && request.DeadlineDate.Value.Date < DateTime.Now.Date)
            {
                return Result<TeamRecruitmentDto>.Error(ErrorCode.InvalidInput, "deadline is in the past");
            }

            Result<TeamRecruitmentDto?> saved = await mRepository.SaveRecruitmentByManagerAsync(managerUserId, request, cancellation);
            if (saved.IsError)
            {
                return Result<TeamRecruitmentDto>.Failure(saved.ResultData);
            }

            if (saved.Value is null)
            {
                return Result<TeamRecruitmentDto>.Error(ErrorCode.Forbidden, "recruitment not editable");
            }

            return Result<TeamRecruitmentDto>.Success(saved.Value);
        }

        public async Task<Result<TeamRecruitmentDto>> CloseAsync(
            Guid managerUserId, Guid recruitmentId, CancellationToken cancellation = default)
        {
            if (managerUserId == Guid.Empty || recruitmentId == Guid.Empty)
            {
                return Result<TeamRecruitmentDto>.Error(ErrorCode.InvalidInput, "managerUserId/recruitmentId required");
            }

            Result<TeamRecruitmentDto?> closed = await mRepository.CloseRecruitmentByManagerAsync(managerUserId, recruitmentId, cancellation);
            if (closed.IsError)
            {
                return Result<TeamRecruitmentDto>.Failure(closed.ResultData);
            }

            if (closed.Value is null)
            {
                return Result<TeamRecruitmentDto>.Error(ErrorCode.Forbidden, "recruitment not closable");
            }

            return Result<TeamRecruitmentDto>.Success(closed.Value);
        }

        public async Task<Result<bool>> DeleteAsync(
            Guid managerUserId, Guid recruitmentId, bool restore, CancellationToken cancellation = default)
        {
            if (managerUserId == Guid.Empty || recruitmentId == Guid.Empty)
            {
                return Result<bool>.Error(ErrorCode.InvalidInput, "managerUserId/recruitmentId required");
            }

            Result<bool> applied = await mRepository.DeleteRecruitmentByManagerAsync(managerUserId, recruitmentId, restore, cancellation);
            if (applied.IsError)
            {
                return applied;
            }

            if (!applied.Value)
            {
                return Result<bool>.Error(ErrorCode.Forbidden, "recruitment not deletable");
            }

            return Result<bool>.Success(true);
        }
    }
}
