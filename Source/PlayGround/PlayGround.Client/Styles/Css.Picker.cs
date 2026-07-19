namespace PlayGround.Client.Styles
{
    public static partial class Css
    {
        /// <summary>
        /// 날짜·시간 선택 클래스 — Design.DatePicker.
        /// 네이티브 input[type=date]는 쓰지 않는다(브라우저별 편차).
        /// ※ Tailwind content 글롭이 Styles/**/*.cs만 스캔하므로 클래스 문자열은 여기에 둔다.
        /// </summary>
        public static class Picker
        {
            //.// 트리거 필드 (날짜·시간 공통)

            private const string TriggerBase =
                "w-full h-[46px] md:h-11 px-[13px] rounded-[11px] border-1.5 bg-white " +
                "flex items-center justify-between gap-2 text-base md:text-[13px] text-navy-deep " +
                "cursor-pointer transition-colors";

            public static string Trigger(bool isOpen, bool hasError)
            {
                if (hasError)
                {
                    return $"{TriggerBase} border-danger";
                }

                return isOpen
                    ? $"{TriggerBase} border-navy ring-[3px] ring-navy/[.12]"
                    : $"{TriggerBase} border-border hover:border-navy-muted";
            }

            public const string TriggerPlaceholder = "text-text-muted";

            //.// 캘린더 (PC 팝오버 / 모바일 바텀시트)

            /// <summary>PC 팝오버 — 트리거 아래 떠 있는 300px 카드.</summary>
            public const string Popover =
                "absolute z-50 top-[52px] left-0 w-[300px] bg-white border-1.5 border-border rounded-[14px] " +
                "p-4 shadow-picker";

            /// <summary>모바일 바텀시트 — 오버레이 + 하단 시트.</summary>
            public const string SheetOverlay =
                "fixed inset-0 z-[120] bg-navy-deep/45 flex items-end md:hidden";

            public const string Sheet =
                "w-full bg-white rounded-t-[18px] px-[18px] pt-3 pb-[calc(18px+env(safe-area-inset-bottom))] flex flex-col";

            public const string GrabBar = "w-9 h-1 rounded-full bg-border mx-auto";

            //.// 월 헤더

            public const string MonthRow = "flex items-center justify-between mb-3";

            public const string MonthLabel = "text-sm md:text-[13.5px] font-extrabold text-navy-deep whitespace-nowrap";

            public const string MonthNav =
                "w-[30px] h-[30px] md:w-7 md:h-7 rounded-lg bg-surface-icon hover:bg-border-soft " +
                "flex items-center justify-center text-text-body border-0 cursor-pointer transition-colors " +
                "disabled:opacity-40 disabled:cursor-not-allowed";

            //.// 요일 헤더 · 셀 그리드

            public const string Grid = "grid grid-cols-7 gap-0.5";

            public const string WeekdayRow = "grid grid-cols-7 gap-0.5 mb-1";

            /// <summary>요일 라벨 — 일요일 레드톤 / 토요일 네이비톤 / 평일 회색.</summary>
            public static string Weekday(int dayOfWeek)
            {
                string color = dayOfWeek switch
                {
                    0 => "text-weekend-sun",
                    6 => "text-weekend-sat",
                    _ => "text-text-muted",
                };

                return $"text-[10.5px] font-extrabold text-center py-1 {color}";
            }

            private const string CellBase =
                "relative flex items-center justify-center rounded-full text-xs tabular-nums " +
                "h-11 md:h-8 border-0 bg-transparent transition-colors";

            /// <summary>셀 상태 4종: 선택(네이비 원) · 오늘(teal 링) · 비활성 · 기본(요일 색).</summary>
            public static string Cell(int dayOfWeek, bool isSelected, bool isToday, bool isDisabled)
            {
                if (isSelected)
                {
                    return $"{CellBase} font-extrabold text-white bg-navy cursor-pointer";
                }

                if (isDisabled)
                {
                    return $"{CellBase} font-semibold text-picker-disabled cursor-not-allowed";
                }

                string color = dayOfWeek switch
                {
                    0 => "text-weekend-sun",
                    6 => "text-weekend-sat",
                    _ => "text-navy-deep",
                };

                string today = isToday ? " ring-[1.5px] ring-inset ring-teal font-extrabold" : " font-bold";
                return $"{CellBase} {color}{today} hover:bg-surface-alt cursor-pointer";
            }

            /// <summary>경기 있는 날 — 셀 하단 teal 도트 4px.</summary>
            public const string MatchDot =
                "absolute bottom-[3px] left-1/2 -translate-x-1/2 w-1 h-1 rounded-full bg-teal";

            //.// 퀵버튼 · 확정 버튼

            public const string QuickRow = "flex gap-1.5 mt-3 pt-3 border-t border-surface-icon";

            public const string QuickButton =
                "h-[30px] px-3 rounded-lg border-1.5 border-border hover:border-navy bg-white " +
                "text-[11.5px] font-bold text-text-strong whitespace-nowrap cursor-pointer transition-colors";

            /// <summary>모바일 확정 버튼 — 선택 날짜를 라벨에 담는다(지연 적용).</summary>
            public const string SheetConfirm =
                "h-[46px] mt-3.5 rounded-xl bg-navy hover:bg-navy-deep text-[13.5px] font-bold text-white " +
                "whitespace-nowrap border-0 cursor-pointer transition-colors disabled:bg-navy-muted disabled:cursor-not-allowed";

            //.// 시간 리스트

            /// <summary>15분 단위 리스트 — PC 팝오버 / 모바일 시트 안 스크롤 영역.</summary>
            public const string TimeList =
                "absolute z-50 top-[52px] left-0 w-[160px] max-h-[220px] overflow-y-auto " +
                "bg-white border-1.5 border-border rounded-[11px] p-1.5 flex flex-col gap-px shadow-picker";

            public static string TimeOption(bool isSelected)
            {
                return isSelected
                    ? "px-[11px] py-2 rounded-lg bg-navy text-[12.5px] font-extrabold text-white text-left border-0 cursor-pointer whitespace-nowrap"
                    : "px-[11px] py-2 rounded-lg hover:bg-surface-alt text-[12.5px] font-bold text-text-body text-left border-0 bg-transparent cursor-pointer whitespace-nowrap";
            }

            /// <summary>직접 입력 — 리스트 상단 고정.</summary>
            public const string TimeInput =
                "w-full h-9 px-[11px] mb-1 rounded-lg border-1.5 border-border focus:border-navy outline-none " +
                "text-[12.5px] font-bold text-navy-deep tabular-nums";
        }
    }
}
