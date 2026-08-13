namespace PlayGround.Server.Services
{
    /// <summary>결제 PG 어댑터 선택지. None은 결제 비활성(시크릿 없는 환경에서도 기동 가능).</summary>
    public enum PaymentProviderKind
    {
        None = 0,
        Toss,
    }
}
