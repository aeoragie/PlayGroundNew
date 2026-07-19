namespace PlayGround.Client.Styles
{
    public static partial class Css
    {
        /// <summary>
        /// 에러 페이지(404·403·500) 클래스 — Design.Navigation.
        /// 주 버튼은 네이비(오렌지 CTA 아님), 이모지 금지.
        /// ※ Tailwind content 글롭이 Styles/**/*.cs만 스캔하므로 클래스 문자열은 여기에 둔다.
        /// </summary>
        public static class Error
        {
            public const string Main = "flex-1 flex items-center justify-center px-6 py-12 md:py-[60px]";

            public const string Body =
                "flex flex-col items-center text-center max-w-[320px] md:max-w-[460px]";

            /// <summary>일러스트 120px (모바일 96px).</summary>
            public const string Illustration = "w-24 h-24 md:w-[120px] md:h-[120px]";

            /// <summary>상태 코드 숫자 — 403은 쓰지 않는다.</summary>
            public const string Code =
                "text-[48px] md:text-[60px] font-extrabold text-navy tracking-[-.03em] leading-none mt-4 md:mt-[18px]";

            /// <summary>제목 — 코드가 있으면 그 아래, 없으면(403) 일러스트 바로 아래.</summary>
            public const string Title = "text-[17px] md:text-[19px] font-extrabold text-navy-deep break-keep";

            public const string TitleGapWithCode = "mt-3 md:mt-3.5";

            public const string TitleGapNoCode = "mt-5 md:mt-6";

            public const string Description =
                "text-[13px] md:text-[13.5px] text-text-body leading-[1.65] break-keep mt-[7px] md:mt-2";

            /// <summary>버튼 영역 — 모바일 세로 풀폭, PC 가로.</summary>
            public const string Actions =
                "flex flex-col md:flex-row gap-2.5 md:gap-2.5 mt-6 md:mt-[26px] w-full md:w-auto";

            private const string ButtonBase =
                "h-[46px] md:h-11 px-[22px] rounded-xl md:rounded-[11px] text-sm md:text-[13.5px] " +
                "font-bold whitespace-nowrap flex items-center justify-center transition-colors";

            /// <summary>주 버튼 — 네이비 채움.</summary>
            public const string PrimaryButton =
                ButtonBase + " bg-navy hover:bg-navy-deep text-white hover:text-white border-0 cursor-pointer";

            /// <summary>보조 버튼 — 흰 아웃라인.</summary>
            public const string SecondaryButton =
                ButtonBase + " bg-white border-1.5 border-border hover:border-navy text-text-strong cursor-pointer";

            /// <summary>하단 안내(문의·오류 코드).</summary>
            public const string Footnote = "text-[11px] md:text-[11.5px] text-text-faint break-keep mt-3.5 md:mt-4";

            public const string ErrorCode = "font-bold";
        }
    }
}
