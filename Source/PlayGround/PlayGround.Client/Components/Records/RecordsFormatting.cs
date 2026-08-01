using PlayGround.Client.Models;
using PlayGround.Contracts.Records;

namespace PlayGround.Client.Components.Records
{
    /// <summary>Records 화면 공용 표시 포맷 (경기 일시·상태 뱃지·PK 스코어).</summary>
    public static class RecordsFormatting
    {
        private const string WeekdayLetters = "일월화수목금토";

        /// <summary>"6/7 (일) 10:00" — 일시 미정이면 "일정 미정".</summary>
        public static string WhenLabel(DateTime? matchedAt)
        {
            if (matchedAt is null)
            {
                return "일정 미정";
            }

            DateTime at = matchedAt.Value;
            return $"{at.Month}/{at.Day} ({WeekdayLetters[(int)at.DayOfWeek]}) {at:HH:mm}";
        }

        public static string MatchStatusLabel(RecordsMatchDto match)
        {
            return match.Status switch
            {
                nameof(SoccerMatchStatus.Completed) => "종료",
                nameof(SoccerMatchStatus.Canceled) => "취소",
                _ => "예정",
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

        /// <summary>토너먼트 승자 판정 — 정규시간 우선, 동점이면 PK.</summary>
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

        /// <summary>이벤트 종류 라벨 — 득점/자책골/경고/퇴장.</summary>
        public static string EventKindLabel(string eventType)
        {
            return eventType switch
            {
                "Goal" or "PenaltyGoal" => "득점",
                "OwnGoal" => "자책골",
                "YellowCard" => "경고",
                "RedCard" => "퇴장",
                _ => string.Empty,
            };
        }

        /// <summary>득점형 이벤트(공 아이콘) 여부 — 카드형과 구분.</summary>
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

        /// <summary>타임라인 상세 — "득점 · 9번" (등번호 있으면).</summary>
        public static string EventDetailText(RecordsMatchEventDto e)
        {
            string kind = EventKindLabel(e.EventType);
            return e.JerseyNumber is int no ? $"{kind} · {no}번" : kind;
        }

        /// <summary>라운드 표시 — 'R1'→'1R', 'R16'→'16강', 'PO'/'QF'/'SF'/'F' 매핑.</summary>
        public static string RoundDisplay(string? roundName)
        {
            switch (roundName)
            {
                case "PO": return "PO";
                case "R16": return "16강";
                case "QF": return "8강";
                case "SF": return "4강";
                case "F": return "결승";
                case null:
                case "": return string.Empty;
            }

            if (roundName.StartsWith('R') && int.TryParse(roundName.AsSpan(1), out int n))
            {
                return $"{n}R";
            }

            return roundName;
        }

        /// <summary>스테이지+라운드 라벨 — "조별 1R", "PO", "리그" 등 (브레드크럼·스코어보드 뱃지).</summary>
        public static string MatchStageLabel(string? stageType, string? roundName)
        {
            string round = RoundDisplay(roundName);
            return stageType switch
            {
                "Group" => string.IsNullOrEmpty(round) ? "조별" : $"조별 {round}",
                "Split1" => string.IsNullOrEmpty(round) ? "1차 풀리그" : $"1차 {round}",
                "Split2" => string.IsNullOrEmpty(round) ? "2차 스플릿" : $"2차 {round}",
                "Knockout" => string.IsNullOrEmpty(round) ? "토너먼트" : round,
                "League" => string.IsNullOrEmpty(round) ? "리그" : round,
                _ => round,
            };
        }
    }
}
