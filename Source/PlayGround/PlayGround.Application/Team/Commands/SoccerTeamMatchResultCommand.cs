using System.Diagnostics;
using PlayGround.Shared.Result;
using PlayGround.Contracts.Team;
using PlayGround.Domain.Account;
using PlayGround.Domain.Soccer;
using PlayGround.Application.Interfaces;

namespace PlayGround.Application.Team.Commands
{
    /// <summary>
    /// 친선경기 결과 입력 유즈케이스 (팀 대시보드).
    /// **팀이 입력하는 경기는 항상 친선이다** — 공식 기록의 주체는 주최측이다(설계 결정 7).
    /// 프로시저가 MatchType='Friendly'로 저장하고, 순위표는 Official만 집계하므로 재계산 경로가 없다.
    /// (순위표 재계산 D5는 주최측 입력 경로의 책임으로 옮겨갔다 — UspRecalculateSoccerTournamentStandings 참조.)
    /// </summary>
    public class SoccerTeamMatchResultCommand
    {
        /// <summary>한 경기에 기록할 수 있는 득점 수 상한 — 오타·자동입력 폭주 방어.</summary>
        private const int MaxScore = 99;

        private readonly ISoccerTeamRepository mRepository;
        private readonly INotificationRepository mNotificationRepository;
        private readonly IAccountRepository mAccountRepository;

        public SoccerTeamMatchResultCommand(
            ISoccerTeamRepository repository,
            INotificationRepository notificationRepository,
            IAccountRepository accountRepository)
        {
            Debug.Assert(repository != null && notificationRepository != null && accountRepository != null);
            mRepository = repository ?? throw new ArgumentNullException(nameof(repository));
            mNotificationRepository = notificationRepository ?? throw new ArgumentNullException(nameof(notificationRepository));
            mAccountRepository = accountRepository ?? throw new ArgumentNullException(nameof(accountRepository));
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

            // 알림 발송은 부가 작업 — 실패해도 경기 저장 결과에는 영향을 주지 않는다 (오류는 저장소가 로깅)
            await NotifyGuardiansAsync(managerUserId, saved.Value.Value, request, cancellation);

            return Result<CreateTeamMatchResultResponse>.Success(
                new CreateTeamMatchResultResponse { MatchId = saved.Value.Value });
        }

        /// <summary>친선경기 결과 반영 알림 — 팀의 Claimed 자녀 보호자에게, 수신 설정(MatchResult)이
        /// 켜진 계정만 (저장 행이 없으면 enum 기본값). 자녀별 1건, 재시도 멱등은 프로시저가 보장.</summary>
        private async Task NotifyGuardiansAsync(
            Guid managerUserId, Guid matchId, CreateTeamMatchResultRequest request, CancellationToken cancellation)
        {
            Result<List<NotificationRecipient>> recipients =
                await mNotificationRepository.GetMatchResultRecipientsAsync(managerUserId, cancellation);
            if (recipients.IsError || recipients.Value.Count == 0)
            {
                return;
            }

            List<Guid> userIds = recipients.Value.Select(r => r.UserId).Distinct().ToList();
            Result<Dictionary<Guid, bool>> states = await mAccountRepository.GetNotificationStatesAsync(
                userIds, NotificationPreferenceItem.MatchResult.ToString(), cancellation);
            if (states.IsError)
            {
                return;
            }

            bool defaultEnabled = NotificationPreferenceItem.MatchResult.DefaultIsEnabled();
            string score = $"{request.OurScore}:{request.OpponentScore}";

            foreach (NotificationRecipient recipient in recipients.Value)
            {
                bool enabled = states.Value.TryGetValue(recipient.UserId, out bool saved) ? saved : defaultEnabled;
                if (!enabled)
                {
                    continue;
                }

                await mNotificationRepository.CreateAsync(
                    recipient.UserId, SoccerNotificationType.MatchResult.ToString(), matchId, recipient.PlayerId,
                    request.OpponentName, recipient.PlayerName, recipient.TeamName, score, subText: null, cancellation);
            }
        }

        /// <remarks>
        /// 팀 입력 화면에서 대회 선택이 사라져(설계 결정 7) 현재 클라이언트는 이 경로를 쓰지 않는다.
        /// 주최측 입력 경로(대회 운영 서비스, Server 공유)가 쓸 자리라 남겨 둔다.
        /// </remarks>
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
