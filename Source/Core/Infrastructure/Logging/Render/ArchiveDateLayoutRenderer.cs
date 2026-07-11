using System.Text;
using NLog;
using NLog.LayoutRenderers;

namespace PlayGround.Infrastructure.Logging.Render
{
    /// <summary>
    /// 아카이브 날짜 포맷 렌더러 (yyyy_MM_dd)
    /// 로그 파일 일별 아카이브 경로에 사용
    /// </summary>
    [LayoutRenderer("archivedate")]
    public class ArchiveDateLayoutRenderer : LayoutRenderer
    {
        protected override void Append(StringBuilder builder, LogEventInfo logEvent)
        {
            builder.Append(logEvent.TimeStamp.ToString("yyyy_MM_dd"));
        }
    }
}
