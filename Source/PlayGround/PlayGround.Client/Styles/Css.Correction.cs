namespace PlayGround.Client.Styles
{
    public static partial class Css
    {
        /// <summary>
        /// 공식 기록 수정 신청 — Design.RecordCorrection.
        /// 핵심 시각 장치는 **현재 기록(회색 읽기) → 올바른 기록(네이비 입력)** 대비다.
        /// ※ Tailwind content 글롭이 Styles/**/*.cs만 스캔하므로 클래스 문자열은 여기에 둔다.
        /// </summary>
        public static class Correction
        {
            //.// 신청 폼 — 항목 선택 (1건 1항목)

            public const string FieldGrid = "grid grid-cols-2 gap-1.5";

            public static string FieldOption(bool isSelected)
            {
                return "h-[38px] rounded-[9px] border-1.5 text-xs whitespace-nowrap cursor-pointer " +
                       "flex items-center justify-center transition-colors " +
                       (isSelected
                           ? "border-navy bg-navy/[.06] font-extrabold text-navy"
                           : "border-border bg-white font-bold text-text-body hover:border-navy-muted");
            }

            //.// 현재 → 올바른 대비

            public const string CompareRow = "flex gap-2 items-stretch";

            /// <summary>현재 기록 — 읽기 전용. 회색으로 눌러 둬야 입력칸이 눈에 들어온다.</summary>
            public const string CurrentBox =
                "flex-1 bg-surface-soft rounded-[10px] px-3 py-2.5 flex flex-col gap-0.5 min-w-0";

            public const string CurrentLabel = "text-[10px] font-bold text-text-muted whitespace-nowrap";

            public const string CurrentValue =
                "text-sm font-extrabold text-text-muted tabular-nums break-keep";

            public const string CompareArrow = "self-center shrink-0 text-text-muted";

            /// <summary>올바른 기록 — 입력. 네이비 링으로 여기가 할 일임을 알린다.</summary>
            public const string TargetBox =
                "flex-1 border-1.5 border-navy rounded-[10px] px-3 py-2 flex flex-col gap-0.5 min-w-0 " +
                "shadow-[0_0_0_3px_rgba(35,64,142,.12)]";

            public const string TargetLabel = "text-[10px] font-bold text-navy whitespace-nowrap";

            /// <summary>스코어 2칸 — 가운데 콜론.</summary>
            public const string ScoreInputs = "flex items-center gap-1.5";

            public const string ScoreInput =
                "w-full min-w-0 h-8 rounded-lg border border-border px-2 text-sm font-extrabold text-navy-deep " +
                "tabular-nums text-center outline-none focus:border-navy";

            public const string ScoreColon = "text-sm font-extrabold text-text-muted";

            /// <summary>스코어 외 항목 — 자유 입력 한 줄.</summary>
            public const string TargetInput =
                "w-full min-w-0 h-8 rounded-lg border border-border px-2 text-[13px] font-bold text-navy-deep " +
                "outline-none focus:border-navy";

            //.// 내 신청 목록 (TableList 정보형 행)

            public const string ListCard = "flex flex-col gap-2.5";

            public const string Row =
                "flex items-center gap-3 border border-border-soft rounded-xl px-3.5 py-3";

            /// <summary>반려 행은 사유 박스를 품어야 해서 세로 배치.</summary>
            public const string RowRejected =
                "flex flex-col gap-2 border border-border-soft rounded-xl px-3.5 py-3";

            public const string Summary =
                "text-[12.5px] font-bold text-navy-deep whitespace-nowrap overflow-hidden text-ellipsis";

            public const string Meta = "text-[11px] text-text-muted whitespace-nowrap";

            /// <summary>반려 사유 — 반려 행에는 반드시 보인다(왜 거절됐는지가 핵심 정보).</summary>
            public const string ReasonBox =
                "bg-surface-soft rounded-[9px] px-3 py-2.5 text-[11.5px] text-text-body leading-[1.55] break-keep";

            //.// 상태 뱃지 — 접수 오렌지 / 반영 teal / 반려 연레드

            public static string StatusBadge(string status)
            {
                string tone = status switch
                {
                    "Accepted" => "text-teal-ink bg-teal/[.12]",
                    "Rejected" => "text-danger bg-danger/[.08]",
                    _ => "text-orange-ink bg-surface-orange-badge",
                };

                return "text-[10px] font-extrabold rounded-full px-[9px] py-[3px] whitespace-nowrap shrink-0 " + tone;
            }
        }
    }
}
