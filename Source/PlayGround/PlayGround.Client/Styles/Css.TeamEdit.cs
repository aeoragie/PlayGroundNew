namespace PlayGround.Client.Styles
{
    public static partial class Css
    {
        /// <summary>
        /// 팀 정보 수정 폼의 반복 항목(핵심가치·코칭스태프) 클래스.
        /// A1 TextField는 단일 필드용이라, 목록 안의 짧은 입력은 여기 스타일을 직접 쓴다.
        /// ※ Tailwind content 글롭이 Styles/**/*.cs만 스캔하므로 클래스 문자열은 여기에 둔다.
        /// </summary>
        public static class TeamEdit
        {
            public const string ItemCard =
                "border-1.5 border-border rounded-xl p-3 flex flex-col gap-2 bg-surface-soft";

            public const string ItemHeader = "flex items-center justify-between";

            public const string ItemIndex = "text-[11.5px] font-extrabold text-text-muted whitespace-nowrap";

            public const string RemoveButton =
                "text-[11.5px] font-bold text-danger hover:opacity-80 border-0 bg-transparent cursor-pointer whitespace-nowrap";

            /// <summary>모바일 16px — iOS 확대 방지(전역 규칙).</summary>
            public const string Input =
                "w-full h-[46px] md:h-10 px-3 rounded-[10px] border-1.5 border-border bg-white " +
                "text-base md:text-[13px] text-navy-deep placeholder:text-text-faint " +
                "focus:border-navy outline-none transition-colors";

            public const string TextArea =
                "w-full px-3 py-2.5 rounded-[10px] border-1.5 border-border bg-white resize-none " +
                "text-base md:text-[13px] text-navy-deep placeholder:text-text-faint leading-[1.6] " +
                "focus:border-navy outline-none transition-colors";

            public const string Pair = "grid grid-cols-2 gap-2";

            public const string AddButton =
                "h-11 md:h-10 rounded-[10px] border-1.5 border-dashed border-border hover:border-navy " +
                "bg-white text-[12.5px] font-bold text-text-strong whitespace-nowrap cursor-pointer transition-colors";
        }
    }
}
