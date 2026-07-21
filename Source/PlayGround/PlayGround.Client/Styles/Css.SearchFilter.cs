namespace PlayGround.Client.Styles
{
    public static partial class Css
    {
        /// <summary>
        /// 검색·필터 공용 클래스 상수 — Design.SearchFilter 카탈로그 기준.
        /// 원칙: 즉시 적용(모바일 시트만 지연) · URL 쿼리 동기화 · 결과 카운트 상시 표시.
        /// (Tailwind content 글롭이 Styles/**/*.cs만 스캔 — 클래스 문자열은 반드시 여기에.)
        /// </summary>
        public static class SearchFilter
        {
            //.// 검색 바

            public const string SearchBox =
                "flex items-center gap-2.5 bg-white border-1.5 rounded-xl px-[13px] h-11 md:h-12 transition-colors";

            public const string SearchBoxIdle = "border-border";

            // 포커스 시 네이비 보더 + 글로우
            public const string SearchBoxActive = "border-navy shadow-[0_0_0_3px_rgba(35,64,142,.12)]";

            // 모바일 16px(iOS 줌 방지) / PC 14px
            public const string SearchInput =
                "flex-1 min-w-0 border-0 bg-transparent outline-none text-[16px] md:text-sm text-navy-deep " +
                "placeholder:text-text-muted";

            public const string SearchClear =
                "w-[22px] h-[22px] rounded-full bg-surface-icon flex items-center justify-center border-0 cursor-pointer shrink-0";

            //.// 필터 칩 (선택 = 네이비 채움 + X 포함, "전체"는 상호배타)

            public const string ChipOn =
                "h-8 md:h-[34px] px-3 md:px-3.5 rounded-full bg-navy flex items-center gap-1.5 text-xs md:text-[12.5px] " +
                "font-bold text-white whitespace-nowrap border-0 cursor-pointer shrink-0";

            public const string ChipOff =
                "h-8 md:h-[34px] px-3.5 rounded-full bg-white border-1.5 border-border flex items-center text-xs md:text-[12.5px] " +
                "font-bold text-text-body whitespace-nowrap cursor-pointer shrink-0 hover:border-navy hover:text-navy transition-colors";

            //.// teal 토글 pill (인증팀만·모집중만)

            public const string ToggleOn =
                "h-8 md:h-[34px] px-3 md:px-[13px] rounded-full border-1.5 border-teal/50 bg-teal/[.12] flex items-center gap-[5px] " +
                "text-xs md:text-[12.5px] font-bold text-teal-ink whitespace-nowrap cursor-pointer shrink-0";

            public const string ToggleOff =
                "h-8 md:h-[34px] px-3 md:px-[13px] rounded-full border-1.5 border-border bg-white flex items-center gap-[5px] " +
                "text-xs md:text-[12.5px] font-bold text-text-body whitespace-nowrap cursor-pointer shrink-0 hover:border-teal/50 transition-colors";

            //.// 결과 헤더

            public const string ResultCount = "text-[12.5px] md:text-[13px] font-extrabold text-navy-deep whitespace-nowrap";

            public const string ResultReset =
                "text-[11.5px] md:text-xs font-bold text-text-muted hover:text-navy bg-transparent border-0 cursor-pointer " +
                "whitespace-nowrap transition-colors p-0";

            //.// 모바일 — 필터 버튼(적용 개수 뱃지) + 바텀시트

            public const string FilterButtonOn =
                "h-[42px] px-3.5 rounded-[11px] border-1.5 border-navy bg-navy flex items-center gap-1.5 text-[12.5px] " +
                "font-extrabold text-white whitespace-nowrap cursor-pointer shrink-0";

            public const string FilterButtonOff =
                "h-[42px] px-3.5 rounded-[11px] border-1.5 border-border bg-white flex items-center gap-1.5 text-[12.5px] " +
                "font-bold text-text-body whitespace-nowrap cursor-pointer shrink-0";

            public const string FilterBadge =
                "w-[17px] h-[17px] rounded-full bg-white/25 flex items-center justify-center text-[10px] font-extrabold text-white";

            public const string SheetOverlay = "fixed inset-0 z-[60] bg-navy-deep/45 flex items-end";

            public const string SheetPanel =
                "w-full max-w-[480px] mx-auto bg-white rounded-t-[18px] px-[18px] pt-3 " +
                "pb-[calc(16px+env(safe-area-inset-bottom))] flex flex-col shadow-[0_-10px_40px_rgba(28,43,74,.3)]";

            public const string SheetGrab = "w-9 h-1 rounded-full bg-border mx-auto";

            public const string SheetSectionLabel = "text-[11.5px] font-extrabold text-text-strong mt-3.5";

            // 시트 하단 일괄 적용 버튼 (터치 타겟 48px, 결과 수 실시간 표시)
            public const string SheetApply =
                "h-12 rounded-xl bg-navy hover:bg-navy-deep text-sm font-bold text-white border-0 cursor-pointer " +
                "whitespace-nowrap mt-[18px] transition-colors";

            // 칩 가로 스크롤 행 (스크롤바 숨김)
            public const string ChipScrollRow =
                "flex gap-1.5 overflow-x-auto -mx-4 px-4 [scrollbar-width:none] [&::-webkit-scrollbar]:hidden";
        }
    }
}
