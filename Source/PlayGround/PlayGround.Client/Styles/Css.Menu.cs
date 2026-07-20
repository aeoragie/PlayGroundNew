namespace PlayGround.Client.Styles
{
    public static partial class Css
    {
        /// <summary>
        /// 액션 메뉴 — Design.DropdownMenu. 오버플로(⋯)는 PC 팝오버 / 모바일 바텀시트.
        /// 값 선택(셀렉트)·조건 좁히기(칩)는 여기가 아니다 — 이동·수정·삭제만.
        /// ※ Tailwind content 글롭이 Styles/**/*.cs만 스캔하므로 클래스 문자열은 여기에 둔다.
        /// </summary>
        public static class Menu
        {
            //.// 트리거

            /// <summary>⋯ 버튼 32px. 주 액션 1개는 행에 직접 노출하고 나머지만 여기로.</summary>
            public const string Trigger =
                "w-8 h-8 rounded-lg border-0 bg-transparent cursor-pointer flex items-center justify-center shrink-0 " +
                "text-base font-extrabold text-text-muted tracking-[1px] leading-none " +
                "hover:bg-surface-alt transition-colors";

            //.// PC 팝오버

            /// <summary>바깥 클릭 감지용 투명 배경 — JS 없이 닫기를 처리한다.</summary>
            public const string Backdrop = "fixed inset-0 z-[115] cursor-default";

            public const string Popover =
                "hidden md:block absolute right-0 top-full mt-1.5 z-[120] w-[200px] bg-white " +
                "border-1.5 border-border rounded-[13px] shadow-picker overflow-hidden";

            public const string Group = "p-1.5 flex flex-col";

            /// <summary>파괴 그룹 — 항상 맨 아래 + 구분선.</summary>
            public const string GroupDestructive = "p-1.5 flex flex-col border-t border-surface-icon";

            public const string Item =
                "flex items-center gap-2.5 px-2.5 py-[9px] rounded-lg text-[12.5px] font-bold text-navy-deep " +
                "bg-transparent border-0 w-full text-left cursor-pointer whitespace-nowrap " +
                "hover:bg-surface-alt transition-colors";

            /// <summary>비활성 항목 — 감추는 대신 왜 못 하는지를 남긴다("신청 처리 중").</summary>
            public const string ItemDisabled =
                "flex items-center gap-2.5 px-2.5 py-[9px] rounded-lg text-[12.5px] font-bold text-text-faint " +
                "whitespace-nowrap cursor-default select-none";

            public const string ItemDestructive =
                "flex items-center gap-2.5 px-2.5 py-[9px] rounded-lg text-[12.5px] font-bold text-danger " +
                "bg-transparent border-0 w-full text-left cursor-pointer whitespace-nowrap " +
                "hover:bg-danger/5 transition-colors";

            //.// 모바일 바텀시트 (앵커 팝업 금지 — 엄지 도달권)

            public const string SheetOverlay = "md:hidden fixed inset-0 z-[120] bg-navy-deep/45 flex items-end";

            public const string SheetCard =
                "w-full bg-white rounded-t-[18px] px-[18px] pt-3 pb-4 flex flex-col shadow-toast";

            public const string SheetGrabBar = "w-9 h-1 rounded-full bg-border mx-auto";

            /// <summary>대상 이름 헤더 — 어떤 행의 메뉴인지 시트에서는 안 보이므로 명시한다.</summary>
            public const string SheetTitle = "text-xs font-extrabold text-text-muted mt-3 px-1 whitespace-nowrap truncate";

            /// <summary>행 48px — 터치 타겟.</summary>
            public const string SheetItem =
                "flex items-center min-h-12 px-1 text-sm font-bold text-navy-deep bg-transparent border-0 " +
                "border-b border-surface-soft w-full text-left cursor-pointer whitespace-nowrap";

            public const string SheetItemDisabled =
                "flex items-center min-h-12 px-1 text-sm font-bold text-text-faint " +
                "border-b border-surface-soft cursor-default select-none";

            public const string SheetItemDestructive =
                "flex items-center min-h-12 px-1 text-sm font-bold text-danger bg-transparent border-0 " +
                "w-full text-left cursor-pointer whitespace-nowrap";

            public const string SheetCancel =
                "h-[46px] rounded-xl border-0 bg-surface-icon text-[13.5px] font-bold text-text-body " +
                "cursor-pointer whitespace-nowrap mt-2";
        }
    }
}
