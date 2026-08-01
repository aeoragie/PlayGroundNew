namespace PlayGround.Client.Localization
{
    /// <summary>타입드 로컬라이제이션 접근자 (도메인별 nested 클래스·Domains는 AppText.g.cs가 생성).
    /// 앱 기동 시 <see cref="Loc"/>에 런타임 로컬라이저를 주입한다. 생성 코드는 커밋 —
    /// 수동 실행(`cd Source/Tools/Generator.Localization &amp;&amp; dotnet run`, wwwroot/i18n/*.ko.json 기준).</summary>
    public static partial class AppText
    {
        internal static ILocalizer Loc { get; set; } = new NullLocalizer();

        /// <summary>활성 문화권 코드. 한국어 조사(이/가) 등 문화권 전용 문법 분기에만 쓴다.</summary>
        public static string Culture => Loc.Culture;

        /// <summary>한국어 문화권 여부 — 조사 삽입 같은 한국어 전용 처리 가드.</summary>
        public static bool IsKorean => Culture == "ko";

        // 기동 전(테스트·디자인타임) 안전 폴백 — 키 그대로 반환
        private sealed class NullLocalizer : ILocalizer
        {
            public string Culture => "ko";

            public string Get(string key) => key;

            public string Format(string key, params object[] args) => key;
        }
    }
}
