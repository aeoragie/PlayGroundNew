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
    }
}
