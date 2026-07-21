namespace PlayGround.Client.Styles
{
    public static partial class Css
    {
        /// <summary>
        /// 스텝퍼 클래스 상수 — Design.BannerStepper §2.
        /// PC = 도트 30px + 레이블 + 연결선(완료 구간만 teal) / 모바일 = 레이블 + "2 / 4" + 진행 바 5px
        /// (도트 나열 금지 — 좁은 폭 레이블 겹침). 2~5스텝 상한 · 완료 스텝만 클릭(뒤로) · 이탈 시 입력 유지.
        /// (Tailwind content 글롭이 Styles/**/*.cs만 스캔 — 클래스 문자열은 반드시 여기에.)
        /// </summary>
        public static class Stepper
        {
            //.// PC — 도트 + 레이블 + 연결선

            public const string Row = "hidden md:flex items-center";

            public const string Item = "flex items-center flex-1 min-w-0";

            public const string DotColumn = "flex flex-col items-center gap-[5px] flex-none bg-transparent border-0 p-0";

            public const string DotBase =
                "w-[30px] h-[30px] rounded-full flex items-center justify-center text-xs font-extrabold flex-none";

            public const string DotDone = DotBase + " bg-teal text-white";

            public const string DotCurrent = DotBase + " bg-navy text-white";

            public const string DotFuture = DotBase + " bg-white border-1.5 border-solid border-switch-track text-text-muted";

            public const string LabelCurrent = "text-[10.5px] whitespace-nowrap font-extrabold text-navy-deep";

            public const string LabelOther = "text-[10.5px] whitespace-nowrap font-bold text-text-muted";

            public const string LineBase = "flex-1 h-[2px] mx-1.5 mb-[18px] rounded-full";

            public const string LineDone = LineBase + " bg-teal";

            public const string LineFuture = LineBase + " bg-border";

            //.// 모바일 — 레이블 + 카운트 + 진행 바

            public const string MobileWrap = "md:hidden flex flex-col gap-[7px]";

            public const string MobileHead = "flex justify-between items-baseline";

            public const string MobileName = "text-xs font-extrabold text-navy-deep whitespace-nowrap";

            public const string MobileCount = "text-[11px] font-bold text-text-muted whitespace-nowrap";

            public const string MobileTrack = "h-[5px] rounded-full bg-surface-segment overflow-hidden";

            public const string MobileBar = "block h-full rounded-full bg-navy transition-[width] duration-[250ms]";
        }
    }
}
