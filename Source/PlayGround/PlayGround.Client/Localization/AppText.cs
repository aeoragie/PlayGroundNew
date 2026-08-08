namespace PlayGround.Client.Localization
{
    /// <summary>타입드 로컬라이제이션 접근자 (도메인별 nested 클래스·Domains는 AppText.g.cs가 생성).
    /// 앱 기동 시 <see cref="Loc"/>에 런타임 로컬라이저를 주입한다. 생성 코드는 커밋 —
    /// 수동 실행(`cd Source/Tools/Generator.Localization &amp;&amp; dotnet run`, wwwroot/i18n/*.ko.json 기준).</summary>
    public static partial class AppText
    {
        internal static ILocalizer Loc { get; set; } = new NullLocalizer();

        /// <summary>활성 문화권 코드 — 날짜·숫자 포맷에 CultureInfo가 필요할 때만 쓴다.</summary>
        public static string Culture => Loc.Culture;

        // 문화권 분기(IsKorean 등)는 두지 않는다 — 언어별 문법은 리소스가 소유한다.
        // 한국어 조사는 리소스의 `{0:이/가}` 모디파이어로 표현하고 KoreanParticle이 해석한다.

        private sealed class NullLocalizer : ILocalizer
        {
            public string Culture => "ko";

            public string Get(string key) => key;

            public string Format(string key, params object[] args) => key;
        }
    }
}
