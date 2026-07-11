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
            // 콘텐츠 컨테이너 (max-width 1200, 좌우 패딩 PC 32 / 모바일 22)
            public const string Container = "max-w-content mx-auto px-[22px] md:px-8";

            // 헤더 GNB 메뉴 링크
            public const string NavLink = "text-[14.5px] font-semibold hover:text-orange transition-colors";

            // 모바일 드롭다운 메뉴 항목 (터치 44px+)
            public const string MobileMenuItem = "px-[22px] py-3.5 text-[15.5px] font-bold text-navy-deep";
        }
    }
}
