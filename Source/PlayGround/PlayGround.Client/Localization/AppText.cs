namespace PlayGround.Client.Localization
{
    /// <summary>타입드 로컬라이제이션 접근자 (도메인별 nested 클래스·Domains는 AppText.g.cs가 생성).
    /// 앱 기동 시 <see cref="Loc"/>에 런타임 로컬라이저를 주입한다. 생성 코드는 커밋 —
    /// 수동 실행(`cd Source/Tools/Generator.Localization &amp;&amp; dotnet run`, wwwroot/i18n/*.ko.json 기준).</summary>
    public static partial class AppText
    {
        internal static ILocalizer Loc { get; set; } = new NullLocalizer();

        // 기동 전(테스트·디자인타임) 안전 폴백 — 키 그대로 반환
        private sealed class NullLocalizer : ILocalizer
        {
            public string Get(string key) => key;

            public string Format(string key, params object[] args) => key;
        }
    }
}
