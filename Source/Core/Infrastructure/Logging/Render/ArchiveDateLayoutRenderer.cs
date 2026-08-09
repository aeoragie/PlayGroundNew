using NLog;
using NLog.LayoutRenderers;
using System.Text;

namespace PlayGround.Infrastructure.Logging.Render
{
    [LayoutRenderer("archivedate")]
    public class ArchiveDateLayoutRenderer : LayoutRenderer
    {
        protected override void Append(StringBuilder builder, LogEventInfo logEvent)
        {
            builder.Append(logEvent.TimeStamp.ToString("yyyy_MM_dd"));
        }
    }
}
