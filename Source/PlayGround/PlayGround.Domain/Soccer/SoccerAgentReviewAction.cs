namespace PlayGround.Domain.Soccer
{
    /// <summary>에이전트 열람 요청 심사 액션. 멤버 이름 = 프로시저 @Action 문자열.
    /// Approve: Pending → Approved(+30일) / Deny: Pending → Denied / Revoke: Approved → Revoked(거절 동급).</summary>
    public enum SoccerAgentReviewAction
    {
        Approve,
        Deny,
        Revoke,
    }
}
