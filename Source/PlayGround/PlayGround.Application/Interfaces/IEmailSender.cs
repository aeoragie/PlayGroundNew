namespace PlayGround.Application.Interfaces
{
    /// <summary>
    /// 이메일 발송 포트. 현재 구현은 로그 전용(LogOnlyEmailSender) — SMTP·SendGrid 등은 어댑터 교체.
    /// 데이터 내려받기 완료 알림이 알림 센터 + 이 채널 두 곳으로 나간다(Design.SettingsFlows ③).
    /// 발송 실패는 부가 작업 실패로 취급한다(주 작업 결과에 영향 없음 — 호출측이 삼킨다).
    /// </summary>
    public interface IEmailSender
    {
        Task SendAsync(string toEmail, string subject, string body, CancellationToken cancellation = default);
    }
}
