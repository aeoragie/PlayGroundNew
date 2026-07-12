namespace PlayGround.Client.Styles
{
    public static partial class Css
    {
        /// <summary>
        /// 랜딩(/soccer) 전용 클래스 상수 — SPEC.LANDING.md 기준.
        /// 버튼/로고/카드 타이틀·본문은 Components/Shared 컴포넌트로 이관됨.
        /// </summary>
        public static class Landing
        {
            public const string Container = "max-w-content mx-auto px-[22px] md:px-8";
            public const string NavLink = "text-[14.5px] font-semibold hover:text-orange transition-colors";
            public const string MobileMenuItem = "px-[22px] py-3.5 text-[15.5px] font-bold text-navy-deep";
        }
    }
}
