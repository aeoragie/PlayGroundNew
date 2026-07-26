using System.Diagnostics;
using PlayGround.Shared.Result;
using PlayGround.Contracts.Claim;
using PlayGround.Contracts.Player;
using PlayGround.Contracts.Team;

using PlayGround.Application.Interfaces;

namespace PlayGround.Application.Team.Commands
{
    /// <summary>
    /// 대시보드 허브 묶음 조립 (Design.DashboardHub).
    ///
    /// **라우팅 3분기의 근거를 함께 만든다** — SPEC은 "역할 2개 이상 또는 자녀 2명 이상"이라고 하지만
    /// 역할은 단일 컬럼이라 2개를 표현할 수 없다. 그래서 역할 enum이 아니라 **실제로 관리하는 대상의 수**
    /// (팀 + 자녀)로 판단한다 — 규칙의 의도("여러 역할을 한눈에 모아 분기")를 있는 모델로 구현한 것.
    ///
    /// 화면 조각이 여러 곳에서 오므로 서버에서 한 번에 모은다(허브는 로그인 직후 한 번 뜨는 화면이다).
    /// 자녀 스탯은 선수 대시보드와 **같은 집계 경로**를 쓴다 — 두 화면의 숫자가 어긋나면 안 된다.
    /// </summary>
    public class SoccerDashboardHubCommand
    {
        private readonly ISoccerTeamRepository mTeamRepository;
        private readonly IPlayerRepository mPlayerRepository;
        private readonly IClaimRepository mClaimRepository;
        private readonly SoccerActionItemsCommand mActionItems;

        public SoccerDashboardHubCommand(
            ISoccerTeamRepository teamRepository,
            IPlayerRepository playerRepository,
            IClaimRepository claimRepository,
            SoccerActionItemsCommand actionItems)
        {
            Debug.Assert(teamRepository != null, "teamRepository is required");
            Debug.Assert(playerRepository != null, "playerRepository is required");
            Debug.Assert(claimRepository != null, "claimRepository is required");
            Debug.Assert(actionItems != null, "actionItems is required");

            mTeamRepository = teamRepository ?? throw new ArgumentNullException(nameof(teamRepository));
            mPlayerRepository = playerRepository ?? throw new ArgumentNullException(nameof(playerRepository));
            mClaimRepository = claimRepository ?? throw new ArgumentNullException(nameof(claimRepository));
            mActionItems = actionItems ?? throw new ArgumentNullException(nameof(actionItems));
        }

        public async Task<Result<DashboardHubResponse>> ExecuteAsync(
            Guid userId, string displayName, int seasonYear, CancellationToken cancellation = default)
        {
            if (userId == Guid.Empty)
            {
                return Result<DashboardHubResponse>.Error(ErrorCode.Unauthorized, "userId is empty");
            }

            var response = new DashboardHubResponse { DisplayName = displayName };

            //.// 팀 — 현재 앱은 관리자당 1팀을 전제로 동작한다(모든 팀 프로시저가 TOP 1).
            // 복수 팀은 그 전제를 먼저 풀어야 해서 여기서도 1팀까지만 담는다.

            Result<TeamInfoResponse?> team = await mTeamRepository.GetTeamInfoByManagerAsync(userId, cancellation);
            if (team.IsError)
            {
                return Result<DashboardHubResponse>.Error(ErrorCode.DatabaseError);
            }

            if (team.Value is not null)
            {
                Result<TeamRosterResponse> roster = await mTeamRepository.GetTeamRosterByManagerAsync(userId, cancellation);
                Result<PendingInvitesResponse> invites = await mTeamRepository.GetPendingInvitesByManagerAsync(userId, cancellation);

                var teamCard = new HubTeamDto
                {
                    TeamId = team.Value.Profile.TeamId,
                    TeamName = team.Value.Profile.TeamName,
                    Slug = team.Value.Profile.Slug,
                    IsVerified = team.Value.Profile.IsVerified,
                    PlayerCount = roster.IsError ? 0 : roster.Value.Players.Count,
                    PendingInviteCount = invites.IsError ? 0 : invites.Value.Invites.Count,
                };

                //.// 다음 경기 — 경기·대회 중 가장 가까운 미래 1건(훈련 제외). 없으면 null(카드에서 줄 생략).
                Result<SchedulesResponse> schedules = await mTeamRepository.GetSchedulesByManagerAsync(userId, cancellation);
                if (!schedules.IsError)
                {
                    ScheduleDto? next = schedules.Value.Schedules
                        .Where(s => s.StartsAt > DateTime.Now && (s.Type == "Match" || s.Type == "Tournament"))
                        .OrderBy(s => s.StartsAt)
                        .FirstOrDefault();

                    if (next is not null)
                    {
                        teamCard.NextMatchStartsAt = next.StartsAt;
                        teamCard.NextMatchOpponent = next.OpponentName;
                    }
                }

                response.Teams.Add(teamCard);
            }

            //.// 자녀 — 스탯은 선수 대시보드와 같은 경로로 뽑는다(공식 경기만 — Design.FriendlyMatch)

            Result<ManagedPlayersResponse> children = await mPlayerRepository.GetManagedPlayersAsync(userId, cancellation);
            if (children.IsError)
            {
                return Result<DashboardHubResponse>.Error(ErrorCode.DatabaseError);
            }

            // 미처리(접수) 기록 수정 신청 MatchId 집합 — 허브 자녀 카드 요약용(RecordCorrection).
            // 전체 목록은 선수 대시보드 시즌 통계에 있고, 여기선 자녀별 개수만 얹는다.
            // 조회는 RequestedByUserId 스코프라 보호자가 그대로 쓴다(같은 프로시저 재사용).
            HashSet<Guid> pendingCorrectionMatchIds = new();

            // 자녀별 진행 중 지원(내가 올린 지원 중 종료·편입 완료가 아닌 것) — 허브 자녀 카드 요약용(Application §5).
            // 전체 현황은 선수 대시보드 "내 지원 현황"에 있고, 여기선 자녀별 개수·확인 필요 여부만 얹는다.
            List<MyApplicationDto> guardianApplications = new();

            // 자녀별 안읽은 게시판 글 수(Design.TeamBoard) — 허브 자녀 카드 요약용. 한 번에 뽑아 카드에 얹는다.
            Dictionary<Guid, int> unreadPostsByChild = new();
            if (children.Value.Players.Count > 0)
            {
                Result<Dictionary<Guid, int>> unread =
                    await mTeamRepository.GetPostUnreadCountsByGuardianAsync(userId, cancellation);
                if (!unread.IsError)
                {
                    unreadPostsByChild = unread.Value;
                }
            }

            if (children.Value.Players.Count > 0)
            {
                Result<RecordCorrectionsResponse> corrections =
                    await mTeamRepository.GetRecordCorrectionsByManagerAsync(userId, cancellation);
                if (!corrections.IsError)
                {
                    pendingCorrectionMatchIds = corrections.Value.Corrections
                        .Where(c => c.Status == "Pending")
                        .Select(c => c.MatchId)
                        .ToHashSet();
                }

                Result<MyApplicationsResponse> applications =
                    await mTeamRepository.GetApplicationsByGuardianAsync(userId, cancellation);
                if (!applications.IsError)
                {
                    // 진행 중 = 종료(Rejected) 아님 + 편입 완료(Confirmed) 아님.
                    guardianApplications = applications.Value.Applications
                        .Where(a => a.Status != "Rejected" && !a.Confirmed)
                        .ToList();
                }
            }

            foreach (ManagedPlayerDto child in children.Value.Players)
            {
                var card = new HubChildDto
                {
                    PlayerId = child.PlayerId,
                    Name = child.Name,
                    Slug = child.Slug,
                    AgeGroup = child.AgeGroup,
                    TeamName = child.TeamName,
                    Position = child.Position,
                    JerseyNumber = child.JerseyNumber,
                };

                Result<PlayerSeasonStatsResponse> stats =
                    await mPlayerRepository.GetSeasonStatsByUserAsync(userId, seasonYear, child.PlayerId, cancellation);

                if (!stats.IsError)
                {
                    // MatchType enum은 Client에만 있어(Application이 참조할 수 없다) 문자열로 비교한다.
                    // DB 저장값 = enum 멤버 이름이라 'Friendly'가 곧 규칙이다.
                    List<PlayerMatchStatDto> official = stats.Value.Matches
                        .Where(m => m.MatchType != "Friendly")
                        .ToList();

                    card.Appearances = official.Count;
                    card.Goals = official.Sum(m => m.Goals);
                    card.Assists = official.Sum(m => m.Assists);

                    // 이 자녀 경기 중 미처리 신청 수 — 보호자 대시보드 목록과 같은 MatchId 기준
                    // (같은 경기에 미처리 1건 규칙이라 경기 수 = 신청 수).
                    card.CorrectionPendingCount = stats.Value.Matches
                        .Count(m => pendingCorrectionMatchIds.Contains(m.MatchId));
                }

                // 이 자녀의 진행 중 지원 — PlayerId로 그룹핑(이름 매칭이 아니라 정확히).
                List<MyApplicationDto> childApplications = guardianApplications
                    .Where(a => a.PlayerId == child.PlayerId)
                    .ToList();
                card.ApplicationPendingCount = childApplications.Count;
                card.ApplicationActionNeeded = childApplications.Any(a => a.Status == "Accepted");

                card.TeamNewsUnreadCount = unreadPostsByChild.GetValueOrDefault(child.PlayerId);

                response.Children.Add(card);
            }

            //.// 승인 대기(Pending) 자녀 — 아직 연결되지 않은 내 연결 요청. 스탯은 없고 "요청 상태 보기"만.
            // 이미 연결된 선수와 겹치지 않게(프로시저가 소유 선수를 제외) 뒤에 덧붙인다.

            Result<List<PendingChildClaimDto>> pending = await mClaimRepository.GetPendingChildClaimsAsync(userId, cancellation);
            if (!pending.IsError)
            {
                HashSet<Guid> linked = response.Children.Select(c => c.PlayerId).ToHashSet();
                foreach (PendingChildClaimDto claim in pending.Value)
                {
                    if (linked.Contains(claim.PlayerId))
                    {
                        continue;
                    }

                    response.Children.Add(new HubChildDto
                    {
                        PlayerId = claim.PlayerId,
                        Name = claim.Name,
                        AgeGroup = claim.AgeGroup,
                        TeamName = claim.TeamName,
                        ClaimStatus = "Pending",
                        RequestedAt = claim.RequestedAt,
                    });
                }
            }

            //.// 처리가 필요해요 — 알림 테이블이 아니라 현재 상태에서 파생

            Result<ActionItemsResponse> actions = await mActionItems.ExecuteAsync(userId, cancellation);
            if (!actions.IsError)
            {
                response.Actions = actions.Value;
            }

            return Result<DashboardHubResponse>.Success(response);
        }
    }
}
