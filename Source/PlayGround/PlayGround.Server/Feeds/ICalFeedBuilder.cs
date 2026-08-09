using PlayGround.Contracts.Soccer;
using PlayGround.Contracts.Team;
using PlayGround.Shared.Time;
using System.Globalization;
using System.Text;

namespace PlayGround.Server.Feeds
{
    /// <summary>팀 일정 구독 캘린더(iCal / RFC 5545) 생성 — 공개 일정만 담는다(Design.Schedule).
    /// StartsAt는 KST(Asia/Seoul) 벽시계로 저장된다 — 한국은 DST가 없어 항상 UTC+9라
    /// UTC 변환(-9h) + 'Z' 접미로 표기하면 어느 캘린더 앱에서든 같은 시각으로 뜬다.</summary>
    public static class ICalFeedBuilder
    {
        private const int KstOffsetHours = 9;

        public static string Build(string slug, IReadOnlyList<ScheduleDto> schedules)
        {
            var sb = new StringBuilder();

            sb.Append("BEGIN:VCALENDAR\r\n");
            sb.Append("VERSION:2.0\r\n");
            sb.Append("PRODID:-//PlayGround Soccer//Schedule//KO\r\n");
            sb.Append("CALSCALE:GREGORIAN\r\n");
            sb.Append("METHOD:PUBLISH\r\n");
            sb.Append("X-WR-CALNAME:").Append(Escape($"PlayGround 일정 · {slug}")).Append("\r\n");
            sb.Append("X-WR-TIMEZONE:Asia/Seoul\r\n");

            string stamp = SystemTime.Now.ToString("yyyyMMdd'T'HHmmss'Z'", CultureInfo.InvariantCulture);

            foreach (ScheduleDto s in schedules)
            {
                string start = s.StartsAt.AddHours(-KstOffsetHours).ToString("yyyyMMdd'T'HHmmss'Z'", CultureInfo.InvariantCulture);

                sb.Append("BEGIN:VEVENT\r\n");
                sb.Append("UID:").Append(s.ScheduleId.ToString("N")).Append("@playground.soccer\r\n");
                sb.Append("DTSTAMP:").Append(stamp).Append("\r\n");
                sb.Append("DTSTART:").Append(start).Append("\r\n");
                sb.Append("SUMMARY:").Append(Escape(Summarize(s))).Append("\r\n");

                if (!string.IsNullOrWhiteSpace(s.Venue))
                {
                    sb.Append("LOCATION:").Append(Escape(s.Venue)).Append("\r\n");
                }

                sb.Append("END:VEVENT\r\n");
            }

            sb.Append("END:VCALENDAR\r\n");
            return sb.ToString();
        }

        // 경기는 상대명에서 제목을 파생("vs {상대}"), 대회·훈련은 제목을 그대로 쓴다.
        private static string Summarize(ScheduleDto s)
        {
            if (s.Type == SoccerScheduleType.Match && !string.IsNullOrWhiteSpace(s.OpponentName))
            {
                return $"vs {s.OpponentName}";
            }

            if (!string.IsNullOrWhiteSpace(s.Title))
            {
                return s.Title!;
            }

            // 폴백 — 제목·상대가 없으면 유형 라벨(방어적).
            return s.Type switch
            {
                SoccerScheduleType.Match => "경기",
                SoccerScheduleType.Tournament => "대회",
                _ => "훈련",
            };
        }

        private static string Escape(string value)
        {
            return value
                .Replace("\\", "\\\\")
                .Replace(";", "\\;")
                .Replace(",", "\\,")
                .Replace("\r\n", "\\n")
                .Replace("\n", "\\n");
        }
    }
}
