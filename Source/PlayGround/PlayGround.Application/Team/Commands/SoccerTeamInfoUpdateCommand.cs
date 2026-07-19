using System.Diagnostics;
using PlayGround.Shared.Result;
using PlayGround.Contracts.Team;
using PlayGround.Application.Interfaces;

namespace PlayGround.Application.Team.Commands
{
    /// <summary>팀 정보 수정 유즈케이스 (팀 대시보드 "정보 수정"). 가치·코치는 통째로 교체된다.</summary>
    public class SoccerTeamInfoUpdateCommand
    {
        private const int MaxValues = 6;
        private const int MaxCoaches = 12;

        /// <summary>창단연도 하한 — 오타(19년·202년)를 걸러낸다.</summary>
        private const int MinFoundedYear = 1900;

        private readonly ISoccerTeamRepository mRepository;

        public SoccerTeamInfoUpdateCommand(ISoccerTeamRepository repository)
        {
            Debug.Assert(repository != null, "repository is required");
            mRepository = repository ?? throw new ArgumentNullException(nameof(repository));
        }

        public async Task<Result<UpdateTeamInfoResponse>> ExecuteAsync(
            Guid managerUserId, UpdateTeamInfoRequest request, CancellationToken cancellation = default)
        {
            if (managerUserId == Guid.Empty)
            {
                return Result<UpdateTeamInfoResponse>.Error(ErrorCode.Unauthorized, "managerUserId is empty");
            }

            if (request is null)
            {
                return Result<UpdateTeamInfoResponse>.Error(ErrorCode.InvalidInput, "request is null");
            }

            Result<UpdateTeamInfoResponse> validation = Validate(request);
            if (validation.IsError)
            {
                return validation;
            }

            // 빈 항목은 저장 전에 걷어낸다 — 화면에서 추가만 하고 안 채운 행이 그대로 넘어온다
            request.Values = request.Values
                .Where(v => !string.IsNullOrWhiteSpace(v.Title))
                .ToList();
            request.Coaches = request.Coaches
                .Where(c => !string.IsNullOrWhiteSpace(c.Name))
                .ToList();

            Result<string?> saved = await mRepository.UpdateTeamInfoByManagerAsync(managerUserId, request, cancellation);
            if (saved.IsError)
            {
                return Result<UpdateTeamInfoResponse>.Failure(saved.ResultData);
            }

            if (saved.Value is null)
            {
                return Result<UpdateTeamInfoResponse>.Error(ErrorCode.NotFound, "team not found for manager");
            }

            return Result<UpdateTeamInfoResponse>.Success(
                new UpdateTeamInfoResponse { Slug = string.IsNullOrEmpty(saved.Value) ? null : saved.Value });
        }

        private static Result<UpdateTeamInfoResponse> Validate(UpdateTeamInfoRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.TeamName))
            {
                return Result<UpdateTeamInfoResponse>.Error(ErrorCode.InvalidInput, "teamName is required");
            }

            if (request.FoundedYear is int year && (year < MinFoundedYear || year > DateTime.Now.Year))
            {
                return Result<UpdateTeamInfoResponse>.Error(ErrorCode.InvalidInput, "foundedYear out of range");
            }

            if (request.Values.Count > MaxValues || request.Coaches.Count > MaxCoaches)
            {
                return Result<UpdateTeamInfoResponse>.Error(ErrorCode.InvalidInput, "too many items");
            }

            return Result<UpdateTeamInfoResponse>.Success(new UpdateTeamInfoResponse());
        }
    }
}
