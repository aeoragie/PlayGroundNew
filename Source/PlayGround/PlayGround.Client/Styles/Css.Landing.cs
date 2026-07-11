namespace PlayGround.Client.Styles
{
    public static partial class Css
    {
        /// <summary>
        /// 랜딩(/soccer) 전용 클래스 상수 — SPEC.LANDING.md 기준
        /// </summary>
        public static class Landing
        {
            // 콘텐츠 컨테이너 (max-width 1200, 좌우 패딩 PC 32 / 모바일 22)
            public const string Container = "max-w-content mx-auto px-[22px] md:px-8";

            // 헤더 GNB 메뉴 링크
            public const string NavLink = "text-[14.5px] font-semibold hover:text-orange transition-colors";

            // 헤더 시작하기 버튼 (오렌지 소형 pill)
            public const string HeaderCta = "text-[13.5px] md:text-[14.5px] font-bold text-white bg-orange hover:bg-orange-hover " +
                "whitespace-nowrap px-4 py-[9px] md:px-5 md:py-2.5 rounded-btn shadow-cta-orange-sm transition-colors";

            // 모바일 드롭다운 메뉴 항목 (터치 44px+)
            public const string MobileMenuItem = "px-[22px] py-3.5 text-[15.5px] font-bold text-navy-deep";

            // 히어로 CTA (오렌지 대형)
            public const string HeroCta = "pointer-events-auto [text-shadow:none] text-[15.5px] md:text-[16.5px] font-bold " +
                "text-white bg-orange hover:bg-orange-hover whitespace-nowrap text-center " +
                "py-[15px] md:py-4 md:px-[34px] rounded-btn-lg shadow-cta-orange transition-colors";

            // 히어로 고스트 버튼 (흰 10% 배경 + 흰 35% 보더)
            public const string HeroGhost = "pointer-events-auto [text-shadow:none] text-[15.5px] md:text-[16.5px] font-bold " +
                "text-white bg-white/10 hover:bg-white/20 border border-white/[.35] backdrop-blur-[4px] whitespace-nowrap text-center " +
                "py-[15px] md:py-4 md:px-[34px] rounded-btn-lg transition-colors";
        }
    }
}
