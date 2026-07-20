using System.Diagnostics;
using PlayGround.Shared.Result;
using PlayGround.Contracts.Team;
using PlayGround.Domain.Soccer;
using PlayGround.Application.Interfaces;

namespace PlayGround.Application.Team.Commands
{
    /// <summary>
    /// "처리가 필요해요" 항목 조립 (Design.DashboardHub §3).
    ///
    /// **알림 테이블을 두지 않고 현재 상태에서 파생한다.** 이 기능들에는 알림을 쓰는 주체(생산자)가
    /// 없어서 — 초대는 코드로 직접 전달하고, 수정 신청의 심사는 주최측이 한다(설계 결정 7) —
    /// 이벤트 로그를 만들면 영원히 비어 있는다. 파생은 항상 정확하고 동기화가 어긋날 일이 없다.
    ///
    /// 대신 **"읽음" 상태가 없다**: 처리하면 목록에서 사라지는 것이 곧 읽음이다.
    /// 진짜 알림 센터(이력·읽음)가 필요해지면 그때 테이블을 만들고 이 파생은 걷어낸다.
    ///
    /// SPEC의 유형 3종 중 "열람"(에이전트)은 축 자체가 미구현이라 여기서 만들 수 없다.
    /// </summary>
    public class SoccerActionItemsCommand
    {
        /// <summary>허브는 상위 3건만 보여준다 — 나머지는 각 화면에서 본다.</summary>
        private const int MaxItems = 3;

        private readonly ISoccerTeamRepository mRepository;

        public SoccerActionItemsCommand(ISoccerTeamRepository repository)
        {
            Debug.Assert(repository != null, "repository is required");
            mRepository = repository ?? throw new ArgumentNullException(nameof(repository));
        }

        public async Task<Result<ActionItemsResponse>> ExecuteAsync(
            Guid userId, CancellationToken cancellation = default)
        {
            if (userId == Guid.Empty)
            {
                return Result<ActionItemsResponse>.Error(ErrorCode.Unauthorized, "userId is empty");
            }

            Result<PendingInvitesResponse> invites = await mRepository.GetPendingInvitesByManagerAsync(userId, cancellation);
            if (invites.IsError)
            {
                return Result<ActionItemsResponse>.Error(ErrorCode.DatabaseError);
            }

            Result<RecordCorrectionsResponse> corrections = await mRepository.GetRecordCorrectionsByManagerAsync(userId, cancellation);
            if (corrections.IsError)
            {
                return Result<ActionItemsResponse>.Error(ErrorCode.DatabaseError);
            }

            var items = new List<ActionItemDto>();
            items.AddRange(BuildInviteItems(invites.Value.Invites));
            items.AddRange(BuildCorrectionItems(corrections.Value.Corrections));

            var response = new ActionItemsResponse
            {
                // 전체 건수는 잘라내기 전 값 — 벨 카운트가 "상위 3건"이 되면 안 된다
                TotalCount = items.Count,
                Items = items
                    .OrderByDescending(i => i.OccurredAt)
                    .Take(MaxItems)
                    .ToList()
            };

            return Result<ActionItemsResponse>.Success(response);
        }

        /// <summary>미처리 초대는 팀 단위로 묶는다 — 선수 10명이면 항목 10개가 아니라 "10건"이다.</summary>
        private static IEnumerable<ActionItemDto> BuildInviteItems(List<PendingInviteDto> invites)
        {
            return invites
                .GroupBy(i => new { i.TeamId, i.TeamName })
                .Select(g => new ActionItemDto
                {
                    Kind = nameof(SoccerActionKind.Invite),
                    Title = $"연결 대기 중인 선수 {g.Count()}명",
                    Description = $"{g.Key.TeamName} · 초대코드를 전달하면 학부모가 직접 기록을 관리해요",
                    TeamId = g.Key.TeamId,
                    OccurredAt = g.Max(i => i.CreatedAt),
                });
        }

        /// <summary>수정 신청은 **심사가 끝난 것만** 액션이다 — 접수 중인 건 기다리는 것 말고 할 일이 없다.</summary>
        private static IEnumerable<ActionItemDto> BuildCorrectionItems(List<RecordCorrectionDto> corrections)
        {
            return corrections
                .Where(c => SoccerCorrectionStatusExtensions.Parse(c.Status) != SoccerCorrectionStatus.Pending)
                .Select(c =>
                {
                    bool rejected = SoccerCorrectionStatusExtensions.Parse(c.Status) == SoccerCorrectionStatus.Rejected;
                    return new ActionItemDto
                    {
                        Kind = nameof(SoccerActionKind.Correction),
                        Title = rejected ? "기록 수정 신청이 반려됐어요" : "기록 수정이 반영됐어요",
                        Description = $"vs {c.OpponentName} · {(rejected ? "사유를 확인하고 다시 신청할 수 있어요" : "경기 결과에 반영됐어요")}",
                        MatchId = c.MatchId,
                        OccurredAt = c.ReviewedAt ?? c.RequestedAt,
                    };
                });
        }
    }
}
