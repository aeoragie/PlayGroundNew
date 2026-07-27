using PlayGround.Application.Interfaces;

namespace PlayGround.Server.Services
{
    /// <summary>이메일 발송 — 현재는 로그 전용(발송 인프라 미도입). SMTP·SendGrid 등이 생기면 이 어댑터만 교체한다.
    /// 데이터 내려받기 완료 알림의 이메일 채널(Design.SettingsFlows ③) — 알림 센터는 별도로 나간다.
    /// 민감정보(본문에 개인정보 넣지 않음)만 로깅한다.</summary>
    public sealed class LogOnlyEmailSender : IEmailSender
    {
        private readonly ILogger<LogOnlyEmailSender> mLogger;

        public LogOnlyEmailSender(ILogger<LogOnlyEmailSender> logger)
        {
            mLogger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public Task SendAsync(string toEmail, string subject, string body, CancellationToken cancellation = default)
        {
            // 실제 발송 대신 로그 — 수신자 이메일은 마스킹해 남긴다
            mLogger.LogInformation("Email (log-only). {{ To:{To}, Subject:{Subject} }}", Mask(toEmail), subject);
            return Task.CompletedTask;
        }

        private static string Mask(string email)
        {
            int at = email.IndexOf('@');
            if (at <= 0)
            {
                return "***";
            }

            string local = email[..at];
            string visible = local.Length <= 3 ? local[..1] : local[..3];
            return $"{visible}***{email[at..]}";
        }
    }
}
