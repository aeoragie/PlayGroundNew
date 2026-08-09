using NLog;
using NLog.LayoutRenderers;
using System.Text;

namespace PlayGround.Infrastructure.Logging.Render
{
    [LayoutRenderer("paddedthreadid")]
    public class PaddedThreadIdLayoutRenderer : LayoutRenderer
    {
        protected override void Append(StringBuilder builder, LogEventInfo logEvent)
        {
            builder.Append(Environment.CurrentManagedThreadId.ToString().PadLeft(4, '0'));
        }
    }
}
