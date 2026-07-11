using System.Text;
using NLog;
using NLog.LayoutRenderers;

namespace PlayGround.Infrastructure.Logging.Render
{
    /// <summary>
    /// 스레드 ID 렌더러 (4자리 0-패딩)
    /// 예: Thread 5 → "0005"
    /// </summary>
    [LayoutRenderer("paddedthreadid")]
    public class PaddedThreadIdLayoutRenderer : LayoutRenderer
    {
        protected override void Append(StringBuilder builder, LogEventInfo logEvent)
        {
            builder.Append(Environment.CurrentManagedThreadId.ToString().PadLeft(4, '0'));
        }
    }
}
