using System.Diagnostics;
using Microsoft.Extensions.Logging;
using PlayGround.Shared.Logging;
using PlayGround.Shared.Result;
using PlayGround.Contracts.Team;
using PlayGround.Application.Interfaces;

namespace PlayGround.Application.Team.Commands
{
    /// <summary>선수 지원(Application) 유즈케이스 — 생성(보호자)·조회(관리자/보호자)·상태 전환(관리자)·취소(보호자).
    /// **수락(Accepted)→로스터 편입·알림은 이 유즈케이스 범위 밖**이다(별도 단계 — 설계 결정 7).</summary>
    public class SoccerApplicationCommand
    {
        private const int MaxIntroductionLength = 500;

        // 관리자가 전환할 수 있는 상태 화이트리스트 — 저장 게이트라 클라이언트가 우회해도 서버가 막는다.
        private static readonly HashSet<string> AllowedTransitions = new(StringComparer.Ordinal)
        {
            "Reviewing", "Accepted", "Rejected"
        };

        private readonly ISoccerTeamRepository mRepository;

        private readonly ILogger<SoccerApplicationCommand> mLogger;

        public SoccerApplicationCommand(ISoccerTeamRepository repository, ILogger<SoccerApplicationCommand> logger)
        {
            Debug.Assert(repository != null, "repository is required");
            mRepository = repository ?? throw new ArgumentNullException(nameof(repository));
            mLogger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<Result<Guid>> ApplyAsync(
            Guid guardianUserId, CreateApplicationRequest request, CancellationToken cancellation = default) =>
            (await ApplyCoreAsync(guardianUserId, request, cancellation)).LogWith(mLogger, "Apply", ("GuardianUserId", guardianUserId));

        private async Task<Result<Guid>> ApplyCoreAsync(
            Guid guardianUserId, CreateApplicationRequest request, CancellationToken cancellation = default)
        {
            if (guardianUserId == Guid.Empty || request is null)
            {
                return Result<Guid>.Error(ErrorCode.Unauthorized, "guardianUserId/request required");
            }

            if (request.RecruitmentId == Guid.Empty || request.PlayerId == Guid.Empty)
            {
                return Result<Guid>.Error(ErrorCode.InvalidInput, "recruitmentId/playerId required");
            }

            request.DesiredPosition = string.IsNullOrWhiteSpace(request.DesiredPosition) ? null : request.DesiredPosition.Trim();
            request.Introduction = string.IsNullOrWhiteSpace(request.Introduction) ? null : request.Introduction.Trim();

            if (request.Introduction is not null && request.Introduction.Length > MaxIntroductionLength)
            {
                return Result<Guid>.Error(ErrorCode.InvalidInput, "introduction is too long");
            }

            Result<(string Status, Guid? ApplicationId)> created =
                await mRepository.CreateApplicationAsync(guardianUserId, request, cancellation);
            if (created.IsError)
            {
                return Result<Guid>.Failure(created.ResultData);
            }

            // 프로시저 상태 신호를 결과로 옮긴다 — Duplicate만 별도 인라인 메시지, 나머지는 상황에 맞는 오류
            (string status, Guid? applicationId) = created.Value;
            return status switch
            {
                "Ok" when applicationId is not null => Result<Guid>.Success(applicationId.Value),
                "Duplicate" => Result<Guid>.Error(ErrorCode.DuplicateValue, "already applied to this recruitment"),
                "Closed" => Result<Guid>.Error(ErrorCode.Gone, "recruitment is closed"),
                "Full" => Result<Guid>.Error(ErrorCode.QuotaExceeded, "recruitment is full"),
                "Cooldown" => Result<Guid>.Error(ErrorCode.TooManyRequests, "reapply cooldown is active"),
                _ => Result<Guid>.Error(ErrorCode.Forbidden, "application not allowed")
            };
        }

        public async Task<Result<TeamApplicationsResponse>> GetForManagerAsync(Guid managerUserId, CancellationToken cancellation = default) =>
            (await GetForManagerCoreAsync(managerUserId, cancellation)).LogWith(mLogger, "GetForManager", ("ManagerUserId", managerUserId));

        private async Task<Result<TeamApplicationsResponse>> GetForManagerCoreAsync(Guid managerUserId, CancellationToken cancellation = default)
        {
            if (managerUserId == Guid.Empty)
            {
                return Result<TeamApplicationsResponse>.Error(ErrorCode.Unauthorized, "managerUserId is empty");
            }

            return await mRepository.GetApplicationsByManagerAsync(managerUserId, cancellation);
        }

        public async Task<Result<MyApplicationsResponse>> GetForGuardianAsync(Guid guardianUserId, CancellationToken cancellation = default) =>
            (await GetForGuardianCoreAsync(guardianUserId, cancellation)).LogWith(mLogger, "GetForGuardian", ("GuardianUserId", guardianUserId));

        private async Task<Result<MyApplicationsResponse>> GetForGuardianCoreAsync(Guid guardianUserId, CancellationToken cancellation = default)
        {
            if (guardianUserId == Guid.Empty)
            {
                return Result<MyApplicationsResponse>.Error(ErrorCode.Unauthorized, "guardianUserId is empty");
            }

            return await mRepository.GetApplicationsByGuardianAsync(guardianUserId, cancellation);
        }

        public async Task<Result<bool>> UpdateStatusAsync(
            Guid managerUserId, Guid applicationId, string newStatus, CancellationToken cancellation = default) =>
            (await UpdateStatusCoreAsync(managerUserId, applicationId, newStatus, cancellation)).LogWith(mLogger, "UpdateStatus", ("ManagerUserId", managerUserId));

        private async Task<Result<bool>> UpdateStatusCoreAsync(
            Guid managerUserId, Guid applicationId, string newStatus, CancellationToken cancellation = default)
        {
            if (managerUserId == Guid.Empty || applicationId == Guid.Empty)
            {
                return Result<bool>.Error(ErrorCode.InvalidInput, "managerUserId/applicationId required");
            }

            newStatus = newStatus?.Trim() ?? string.Empty;
            if (!AllowedTransitions.Contains(newStatus))
            {
                return Result<bool>.Error(ErrorCode.InvalidInput, "invalid target status");
            }

            Result<bool> applied = await mRepository.UpdateApplicationStatusAsync(managerUserId, applicationId, newStatus, cancellation);
            if (applied.IsError)
            {
                return applied;
            }

            if (!applied.Value)
            {
                return Result<bool>.Error(ErrorCode.Forbidden, "status transition not allowed");
            }

            return Result<bool>.Success(true);
        }

        public async Task<Result<bool>> CancelAsync(Guid guardianUserId, Guid applicationId, CancellationToken cancellation = default) =>
            (await CancelCoreAsync(guardianUserId, applicationId, cancellation)).LogWith(mLogger, "Cancel", ("GuardianUserId", guardianUserId));

        private async Task<Result<bool>> CancelCoreAsync(Guid guardianUserId, Guid applicationId, CancellationToken cancellation = default)
        {
            if (guardianUserId == Guid.Empty || applicationId == Guid.Empty)
            {
                return Result<bool>.Error(ErrorCode.InvalidInput, "guardianUserId/applicationId required");
            }

            Result<bool> applied = await mRepository.CancelApplicationAsync(guardianUserId, applicationId, cancellation);
            if (applied.IsError)
            {
                return applied;
            }

            if (!applied.Value)
            {
                return Result<bool>.Error(ErrorCode.Forbidden, "application not cancelable");
            }

            return Result<bool>.Success(true);
        }

        /// <summary>선수단 초대 확인(보호자) → 로스터 편입. 수락(Accepted) 상태의 내 지원일 때만.
        /// 소유·상태 검증은 프로시저가 빈 결과로 거부하고, 여기서 Forbidden으로 변환한다.</summary>
        public async Task<Result<bool>> ConfirmInviteAsync(Guid guardianUserId, Guid applicationId, CancellationToken cancellation = default) =>
            (await ConfirmInviteCoreAsync(guardianUserId, applicationId, cancellation)).LogWith(mLogger, "ConfirmInvite", ("GuardianUserId", guardianUserId));

        private async Task<Result<bool>> ConfirmInviteCoreAsync(Guid guardianUserId, Guid applicationId, CancellationToken cancellation = default)
        {
            if (guardianUserId == Guid.Empty || applicationId == Guid.Empty)
            {
                return Result<bool>.Error(ErrorCode.InvalidInput, "guardianUserId/applicationId required");
            }

            Result<bool> applied = await mRepository.ConfirmApplicationInviteAsync(guardianUserId, applicationId, cancellation);
            if (applied.IsError)
            {
                return applied;
            }

            if (!applied.Value)
            {
                return Result<bool>.Error(ErrorCode.Forbidden, "invite not confirmable");
            }

            return Result<bool>.Success(true);
        }
    }
}
