using PlayGround.Client.Localization;
using PlayGround.Client.Models;
using PlayGround.Client.Services;
using PlayGround.Contracts.Records;
using PlayGround.Shared.Time;

namespace PlayGround.Client.Components.Records
{
    /// <summary>Records 화면 공용 표시 포맷 (경기 일시·상태 뱃지·PK 스코어).</summary>
    public static class RecordsFormatting
    {
        private static string WeekdayLetters => AppText.Records.WeekdayLetters;

        public static string WhenLabel(SystemTime? matchedAt)
        {
            if (matchedAt is null)
            {
                return AppText.Records.ScheduleTbd;
            }

            // 서버가 주는 값은 UTC 순간 — 날짜·요일·시각은 반드시 현지로 되돌린 뒤 만든다
            DateTime at = matchedAt.Value.ToWallClock();
            return $"{at.Month}/{at.Day} ({WeekdayLetters[(int)at.DayOfWeek]}) {at:HH:mm}";
        }

        public static string MatchStatusLabel(RecordsMatchDto match) => MatchStatusLabel(match.Status);

        public static string MatchStatusLabel(string status)
        {
            return status switch
            {
                nameof(SoccerMatchStatus.Completed) => AppText.Records.StatusCompleted,
                nameof(SoccerMatchStatus.Canceled) => AppText.Records.StatusCanceled,
                _ => AppText.Records.StatusScheduled,
            };
        }

        public static string MatchStatusBadgeClass(RecordsMatchDto match)
        {
            string baseClass = "text-[11px] font-bold rounded-full px-[11px] py-[3px] whitespace-nowrap shrink-0 ";
            return baseClass + (match.Status == nameof(SoccerMatchStatus.Completed)
                ? "text-text-muted bg-surface-icon"
                : "text-navy bg-surface-icon");
        }

        /// <summary>토너먼트 홈 스코어 — PK는 괄호 표기 ("1 (4)"). 미종료는 "-".</summary>
        public static string HomeScoreLabel(RecordsMatchDto match)
        {
            if (match.HomeScore is null)
            {
                return "-";
            }

            return match.HomePkScore is null ? match.HomeScore.ToString()! : $"{match.HomeScore} ({match.HomePkScore})";
        }

        /// <summary>토너먼트 원정 스코어 — PK는 괄호 표기 ("(3) 1"). 미종료는 "-".</summary>
        public static string AwayScoreLabel(RecordsMatchDto match)
        {
            if (match.AwayScore is null)
            {
                return "-";
            }

            return match.AwayPkScore is null ? match.AwayScore.ToString()! : $"({match.AwayPkScore}) {match.AwayScore}";
        }

        public static bool IsHomeWinner(RecordsMatchDto match)
        {
            if (match.HomeScore is null || match.AwayScore is null)
            {
                return false;
            }

            if (match.HomeScore != match.AwayScore)
            {
                return match.HomeScore > match.AwayScore;
            }

            return match.HomePkScore > match.AwayPkScore;
        }

        public static bool IsAwayWinner(RecordsMatchDto match)
        {
            if (match.HomeScore is null || match.AwayScore is null)
            {
                return false;
            }

            return !IsHomeWinner(match) && (match.HomeScore != match.AwayScore || match.HomePkScore != match.AwayPkScore);
        }

        //.// 공식 경기 상세 — 이벤트·스테이지 라벨

        public static string EventKindLabel(string eventType)
        {
            return eventType switch
            {
                "Goal" or "PenaltyGoal" => AppText.Records.EventGoal,
                "OwnGoal" => AppText.Records.EventOwnGoal,
                "YellowCard" => AppText.Records.EventYellow,
                "RedCard" => AppText.Records.EventRed,
                _ => string.Empty,
            };
        }

        public static bool IsGoalEvent(string eventType)
        {
            return eventType is "Goal" or "PenaltyGoal" or "OwnGoal";
        }

        /// <summary>주요 로그 문구 — "득점 김유한" (선수명 없으면 종류만).</summary>
        public static string EventLogText(RecordsMatchEventDto e)
        {
            string kind = EventKindLabel(e.EventType);
            return string.IsNullOrEmpty(e.PlayerName) ? kind : $"{kind} {e.PlayerName}";
        }

        public static string EventDetailText(RecordsMatchEventDto e)
        {
            string kind = EventKindLabel(e.EventType);
            return e.JerseyNumber is int no ? AppText.Records.EventDetailWithNumber(kind, no) : kind;
        }

        /// <summary>라운드 표시 — 'R1'→'1R', 'R16'→'16강', 'PO'/'QF'/'SF'/'F' 매핑.</summary>
        public static string RoundDisplay(string? roundName)
        {
            switch (roundName)
            {
                case "PO":
                    return AppText.Records.RoundPo;
                case "R16":
                    return AppText.Records.RoundR16;
                case "QF":
                    return AppText.Records.RoundQf;
                case "SF":
                    return AppText.Records.RoundSf;
                case "F":
                    return AppText.Records.RoundF;
                case null:
                case "":
                    return string.Empty;
            }

            if (roundName.StartsWith('R') && int.TryParse(roundName.AsSpan(1), out int n))
            {
                return AppText.Records.RoundGroup(n);
            }

            return roundName;
        }

        /// <summary>스테이지+라운드 라벨 — "조별 1R", "PO", "리그" 등 (브레드크럼·스코어보드 뱃지).</summary>
        public static string MatchStageLabel(string? stageType, string? roundName)
        {
            string round = RoundDisplay(roundName);
            return stageType switch
            {
                "Group" => string.IsNullOrEmpty(round) ? AppText.Records.StageGroup : $"{AppText.Records.StageGroup} {round}",
                "Split1" => string.IsNullOrEmpty(round) ? AppText.Records.StageSplit1 : $"{AppText.Records.StageSplit1} {round}",
                "Split2" => string.IsNullOrEmpty(round) ? AppText.Records.StageSplit2 : $"{AppText.Records.StageSplit2} {round}",
                "Knockout" => string.IsNullOrEmpty(round) ? AppText.Records.StageKnockout : round,
                "League" => string.IsNullOrEmpty(round) ? AppText.Records.StageLeague : round,
                _ => round,
            };
        }
    }
}
