using System.Diagnostics;
using PlayGround.Shared.Result;
using PlayGround.Contracts.Team;
using PlayGround.Application.Interfaces;

namespace PlayGround.Application.Team.Commands
{
    /// <summary>
    /// 경기 결과 입력 유즈케이스 (팀 대시보드).
    /// 저장 프로시저가 경기 저장 + 순위표 재계산을 한 경로에서 처리한다 — 여기서 따로 재계산을 부르지 않는다(D5).
    /// </summary>
    public class SoccerTeamMatchResultCommand
    {
        /// <summary>한 경기에 기록할 수 있는 득점 수 상한 — 오타·자동입력 폭주 방어.</summary>
        private const int MaxScore = 99;

        private readonly ISoccerTeamRepository mRepository;

        public SoccerTeamMatchResultCommand(ISoccerTeamRepository repository)
        {
            Debug.Assert(repository != null, "repository is required");
            mRepository = repository ?? throw new ArgumentNullException(nameof(repository));
        }

        public async Task<Result<CreateTeamMatchResultResponse>> ExecuteAsync(
            Guid managerUserId, CreateTeamMatchResultRequest request, CancellationToken cancellation = default)
        {
            if (managerUserId == Guid.Empty)
            {
                return Result<CreateTeamMatchResultResponse>.Error(ErrorCode.Unauthorized, "managerUserId is empty");
            }

            if (request is null)
            {
                return Result<CreateTeamMatchResultResponse>.Error(ErrorCode.InvalidInput, "request is null");
            }

            Result<CreateTeamMatchResultResponse> validation = Validate(request);
            if (validation.IsError)
            {
                return validation;
            }

            request.OpponentName = request.OpponentName.Trim();
            request.VenueName = string.IsNullOrWhiteSpace(request.VenueName) ? null : request.VenueName.Trim();

            Result<Guid?> saved = await mRepository.CreateMatchResultByManagerAsync(managerUserId, request, cancellation);
            if (saved.IsError)
            {
                return Result<CreateTeamMatchResultResponse>.Failure(saved.ResultData);
            }

            if (saved.Value is null)
            {
                return Result<CreateTeamMatchResultResponse>.Error(ErrorCode.NotFound, "team or tournament not found");
            }

            return Result<CreateTeamMatchResultResponse>.Success(
                new CreateTeamMatchResultResponse { MatchId = saved.Value.Value });
        }

        public async Task<Result<TeamTournamentOptionsResponse>> GetTournamentOptionsAsync(
            Guid managerUserId, int seasonYear, CancellationToken cancellation = default)
        {
            if (managerUserId == Guid.Empty)
            {
                return Result<TeamTournamentOptionsResponse>.Error(ErrorCode.Unauthorized, "managerUserId is empty");
            }

            return await mRepository.GetTournamentOptionsByManagerAsync(managerUserId, seasonYear, cancellation);
        }

        // 클라이언트 인라인 검증과 같은 규칙 — 우회 요청도 같은 기준으로 막는다
        private static Result<CreateTeamMatchResultResponse> Validate(CreateTeamMatchResultRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.OpponentName))
            {
                return Result<CreateTeamMatchResultResponse>.Error(ErrorCode.InvalidInput, "opponentName is required");
            }

            if (request.OurScore < 0 || request.OpponentScore < 0
                || request.OurScore > MaxScore || request.OpponentScore > MaxScore)
            {
                return Result<CreateTeamMatchResultResponse>.Error(ErrorCode.InvalidInput, "score out of range");
            }

            if (request.MatchedAt == default)
            {
                return Result<CreateTeamMatchResultResponse>.Error(ErrorCode.InvalidInput, "matchedAt is required");
            }

            // 아직 열리지 않은 경기의 결과는 있을 수 없다 (시간대 차이를 감안해 하루 여유)
            if (request.MatchedAt.Date > DateTime.Now.Date.AddDays(1))
            {
                return Result<CreateTeamMatchResultResponse>.Error(ErrorCode.InvalidInput, "matchedAt is in the future");
            }

            return Result<CreateTeamMatchResultResponse>.Success(new CreateTeamMatchResultResponse());
        }
    }
}
