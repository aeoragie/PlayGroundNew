namespace PlayGround.Client.Styles
{
    public static partial class Css
    {
        /// <summary>
        /// 경기 결과 입력 다이얼로그 — PC 중앙 모달 / 모바일 전체 시트.
        /// ※ Tailwind content 글롭이 Styles/**/*.cs만 스캔하므로 클래스 문자열은 여기에 둔다.
        /// </summary>
        public static class MatchForm
        {
            public const string Overlay =
                "fixed inset-0 z-[115] bg-navy-deep/45 flex items-end md:items-center md:justify-center";

            /// <summary>모바일은 화면 대부분을 쓰는 시트, PC는 520px 카드. 내용이 길어 본문만 스크롤한다.</summary>
            public const string Card =
                "relative w-full md:w-[520px] max-h-[92vh] md:max-h-[86vh] bg-white " +
                "rounded-t-[18px] md:rounded-[18px] flex flex-col shadow-modal";

            public const string GrabBar = "w-9 h-1 rounded-full bg-border mx-auto mt-3 md:hidden";

            public const string Header =
                "flex items-center justify-between px-5 md:px-6 pt-4 md:pt-5 pb-3 border-b border-surface-icon";

            public const string Title = "m-0 text-[15.5px] md:text-base font-extrabold text-navy-deep";

            public const string CloseButton =
                "w-8 h-8 rounded-lg flex items-center justify-center text-text-muted hover:text-navy-deep " +
                "border-0 bg-transparent cursor-pointer transition-colors";

            public const string Body =
                "flex-1 overflow-y-auto px-5 md:px-6 py-4 flex flex-col gap-[18px]";

            /// <summary>스코어 2칸 — 가운데 콜론.</summary>
            public const string ScoreRow = "flex items-start gap-2.5";

            public const string ScoreColon = "text-lg font-extrabold text-text-muted pt-[34px]";

            public const string DateTimeRow = "grid grid-cols-2 gap-2.5";

            public const string PickerField = "flex flex-col gap-1.5";

            public const string ScorerBlock = "flex flex-col gap-1.5";

            public const string ScorerChips = "flex flex-wrap gap-1.5 mt-1";

            public static string ScorerChip(bool isPicked)
            {
                return isPicked
                    ? "h-9 px-3 rounded-full bg-navy text-[12.5px] font-bold text-white whitespace-nowrap " +
                      "border-0 cursor-pointer flex items-center gap-1.5 transition-colors"
                    : "h-9 px-3 rounded-full bg-white border-1.5 border-border hover:border-navy " +
                      "text-[12.5px] font-bold text-text-strong whitespace-nowrap cursor-pointer " +
                      "flex items-center gap-1.5 transition-colors";
            }

            /// <summary>같은 선수를 여러 번 누르면 득점 수가 올라간다.</summary>
            public const string ScorerCount =
                "min-w-[18px] h-[18px] px-1 rounded-full bg-white/25 text-[11px] font-extrabold " +
                "flex items-center justify-center";

            public const string ScorerReset =
                "self-start text-xs font-bold text-text-muted hover:text-danger border-0 bg-transparent cursor-pointer mt-0.5";

            public const string Footer =
                "flex gap-2.5 px-5 md:px-6 py-4 border-t border-surface-icon " +
                "pb-[calc(16px+env(safe-area-inset-bottom))] md:pb-4";

            public const string CancelButton =
                "h-[46px] md:h-11 px-5 rounded-xl md:rounded-[11px] bg-white border-1.5 border-border " +
                "text-sm md:text-[13px] font-bold text-text-strong whitespace-nowrap cursor-pointer transition-colors";

            public const string SubmitWidth = "flex-1";
        }
    }
}
