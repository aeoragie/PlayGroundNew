using Microsoft.Extensions.Logging;
using PlayGround.Application.Interfaces;
using PlayGround.Contracts.Team;
using PlayGround.Shared.Logging;
using PlayGround.Shared.Result;
using PlayGround.Shared.Time;
using System.Diagnostics;

namespace PlayGround.Application.Team.Commands
{
    /// <summary>팀 일정 유즈케이스 — 공개 열람(슬러그) + 소유자 편집(작성·수정·삭제).
    /// 경기 결과 연결(MatchId)은 별도 경로 — 여기는 일정 자체만 다룬다.</summary>
    public class SoccerScheduleCommand
    {
        private const int MaxTitleLength = 100;
        private const int MaxVenueLength = 100;

        private static readonly string[] AllowedTypes = { "Match", "Tournament", "Training" };

        private readonly ISoccerTeamRepository mRepository;

        private readonly ILogger<SoccerScheduleCommand> mLogger;

        public SoccerScheduleCommand(ISoccerTeamRepository repository, ILogger<SoccerScheduleCommand> logger)
        {
            Debug.Assert(repository != null, "repository is required");
            mRepository = repository ?? throw new ArgumentNullException(nameof(repository));
            mLogger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<Result<SchedulesResponse>> GetMineAsync(Guid managerUserId, CancellationToken cancellation = default) =>
            (await GetMineCoreAsync(managerUserId, cancellation)).LogWith(mLogger, "GetMine", ("ManagerUserId", managerUserId));

        private async Task<Result<SchedulesResponse>> GetMineCoreAsync(Guid managerUserId, CancellationToken cancellation = default)
        {
            if (managerUserId == Guid.Empty)
            {
                return Result<SchedulesResponse>.Error(ErrorCode.Unauthorized, "managerUserId is empty");
            }

            return await mRepository.GetSchedulesByManagerAsync(managerUserId, cancellation);
        }

        public async Task<Result<SchedulesResponse>> GetBySlugAsync(string slug, CancellationToken cancellation = default) =>
            (await GetBySlugCoreAsync(slug, cancellation)).LogWith(mLogger, "GetBySlug", ("Slug", slug));

        private async Task<Result<SchedulesResponse>> GetBySlugCoreAsync(string slug, CancellationToken cancellation = default)
        {
            if (string.IsNullOrWhiteSpace(slug))
            {
                return Result<SchedulesResponse>.Error(ErrorCode.InvalidInput, "slug is required");
            }

            return await mRepository.GetSchedulesBySlugAsync(slug.Trim(), cancellation);
        }

        public async Task<Result<ScheduleDto>> SaveAsync(
            Guid managerUserId, SaveScheduleRequest request, CancellationToken cancellation = default) =>
            (await SaveCoreAsync(managerUserId, request, cancellation)).LogWith(mLogger, "Save", ("ManagerUserId", managerUserId));

        private async Task<Result<ScheduleDto>> SaveCoreAsync(
            Guid managerUserId, SaveScheduleRequest request, CancellationToken cancellation = default)
        {
            if (managerUserId == Guid.Empty || request is null)
            {
                return Result<ScheduleDto>.Error(ErrorCode.Unauthorized, "managerUserId/request required");
            }

            // 클라이언트 인라인 검증과 같은 규칙 — 우회 요청도 같은 기준으로 막는다 (저장 화이트리스트)
            request.Type = request.Type?.Trim() ?? string.Empty;
            request.Title = request.Title?.Trim();
            request.OpponentName = request.OpponentName?.Trim();
            request.Venue = request.Venue?.Trim() ?? string.Empty;

            if (!AllowedTypes.Contains(request.Type))
            {
                return Result<ScheduleDto>.Error(ErrorCode.InvalidInput, "invalid schedule type");
            }

            if (request.Venue.Length is 0 or > MaxVenueLength)
            {
                return Result<ScheduleDto>.Error(ErrorCode.InvalidInput, "venue is required");
            }

            // 신규·수정 모두 미래 일정이어야 한다 (지난 일정은 결과 입력으로 다룬다)
            if (request.StartsAt <= SystemTime.Now)
            {
                return Result<ScheduleDto>.Error(ErrorCode.InvalidInput, "startsAt is in the past");
            }

            // 경기·대회는 상대명 필수, 훈련은 상대가 없다 (null 처리)
            if (request.Type is "Match" or "Tournament")
            {
                if (string.IsNullOrEmpty(request.OpponentName))
                {
                    return Result<ScheduleDto>.Error(ErrorCode.InvalidInput, "opponentName is required");
                }
            }
            else
            {
                request.OpponentName = null;
            }

            // 대회·훈련은 제목 필수, 경기는 상대명에서 파생하므로 제목을 두지 않는다
            if (request.Type is "Tournament" or "Training")
            {
                if (string.IsNullOrEmpty(request.Title) || request.Title.Length > MaxTitleLength)
                {
                    return Result<ScheduleDto>.Error(ErrorCode.InvalidInput, "title is required");
                }
            }
            else
            {
                request.Title = null;
            }

            Result<ScheduleDto?> saved = await mRepository.SaveScheduleByManagerAsync(managerUserId, request, cancellation);
            if (saved.IsError)
            {
                return Result<ScheduleDto>.Failure(saved.ResultData);
            }

            mLogger.Info("Schedule saved", ("ManagerUserId", managerUserId));

            if (saved.Value is null)
            {
                return Result<ScheduleDto>.Error(ErrorCode.Forbidden, "schedule not editable");
            }

            return Result<ScheduleDto>.Success(saved.Value);
        }

        public async Task<Result<bool>> DeleteAsync(
            Guid managerUserId, Guid scheduleId, bool restore, CancellationToken cancellation = default) =>
            (await DeleteCoreAsync(managerUserId, scheduleId, restore, cancellation)).LogWith(mLogger, "Delete", ("ManagerUserId", managerUserId));

        private async Task<Result<bool>> DeleteCoreAsync(
            Guid managerUserId, Guid scheduleId, bool restore, CancellationToken cancellation = default)
        {
            if (managerUserId == Guid.Empty || scheduleId == Guid.Empty)
            {
                return Result<bool>.Error(ErrorCode.InvalidInput, "managerUserId/scheduleId required");
            }

            Result<bool> applied = await mRepository.DeleteScheduleByManagerAsync(managerUserId, scheduleId, restore, cancellation);
            if (applied.IsError)
            {
                return applied;
            }

            mLogger.Info("Schedule deleted", ("ManagerUserId", managerUserId));

            if (!applied.Value)
            {
                return Result<bool>.Error(ErrorCode.Forbidden, "schedule not deletable");
            }

            return Result<bool>.Success(true);
        }
    }
}
