namespace PlayGround.Server.Services
{
    /// <summary>
    /// 결제 설정 — 섹션 "PaymentConfiguration". <see cref="Provider"/>가 어댑터를 고른다
    /// (UploadStorageConfiguration과 같은 방식).
    ///
    /// 기본 None이면 결제가 비활성이라 시크릿 없는 환경(운영 초기·CI)에서도 기동한다.
    /// 켜려면 appsettings.Local.json 또는 환경변수: PaymentConfiguration__Provider=Toss
    /// + ClientKey(공개 가능)·SecretKey(서버 전용, 절대 노출 금지).
    /// </summary>
    public sealed class PaymentConfiguration
    {
        public const string Section = "PaymentConfiguration";

        public PaymentProviderKind Provider { get; set; } = PaymentProviderKind.None;

        /// <summary>브라우저 위젯용 공개 키 (토스 test_ck_/live_ck_).</summary>
        public string ClientKey { get; set; } = string.Empty;

        /// <summary>서버 승인용 시크릿 키 (토스 test_sk_/live_sk_). 로그·응답에 싣지 않는다.</summary>
        public string SecretKey { get; set; } = string.Empty;

        public string ApiBaseUrl { get; set; } = "https://api.tosspayments.com";

        public bool IsEnabled => Provider != PaymentProviderKind.None;
    }
}
