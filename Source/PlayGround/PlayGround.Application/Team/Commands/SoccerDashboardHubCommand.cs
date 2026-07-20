using System.Diagnostics;
using PlayGround.Shared.Result;
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
        private readonly SoccerActionItemsCommand mActionItems;

        public SoccerDashboardHubCommand(
            ISoccerTeamRepository teamRepository,
            IPlayerRepository playerRepository,
            SoccerActionItemsCommand actionItems)
        {
            Debug.Assert(teamRepository != null, "teamRepository is required");
            Debug.Assert(playerRepository != null, "playerRepository is required");
            Debug.Assert(actionItems != null, "actionItems is required");

            mTeamRepository = teamRepository ?? throw new ArgumentNullException(nameof(teamRepository));
            mPlayerRepository = playerRepository ?? throw new ArgumentNullException(nameof(playerRepository));
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

                response.Teams.Add(new HubTeamDto
                {
                    TeamId = team.Value.Profile.TeamId,
                    TeamName = team.Value.Profile.TeamName,
                    Slug = team.Value.Profile.Slug,
                    IsVerified = team.Value.Profile.IsVerified,
                    PlayerCount = roster.IsError ? 0 : roster.Value.Players.Count,
                    PendingInviteCount = invites.IsError ? 0 : invites.Value.Invites.Count,
                });
            }

            //.// 자녀 — 스탯은 선수 대시보드와 같은 경로로 뽑는다(공식 경기만 — Design.FriendlyMatch)

            Result<ManagedPlayersResponse> children = await mPlayerRepository.GetManagedPlayersAsync(userId, cancellation);
            if (children.IsError)
            {
                return Result<DashboardHubResponse>.Error(ErrorCode.DatabaseError);
            }

            foreach (ManagedPlayerDto child in children.Value.Players)
            {
                var card = new HubChildDto
                {
                    PlayerId = child.PlayerId,
                    Name = child.Name,
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
                }

                response.Children.Add(card);
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
