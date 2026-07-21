namespace PlayGround.Client.Styles
{
    public static partial class Css
    {
        /// <summary>
        /// Claim 플로우 클래스 상수 — Design.ClaimFlow (모바일 우선, PC는 동일 카드 중앙 배치).
        /// (Tailwind content 글롭이 Styles/**/*.cs만 스캔 — 클래스 문자열은 반드시 여기에.)
        /// </summary>
        public static class Claim
        {
            public const string Screen = "min-h-screen bg-flow-bg flex flex-col items-center gap-3.5 px-4 pt-5 pb-[60px]";

            public const string Column = "w-full max-w-[420px] flex flex-col gap-3.5";

            public const string Card = "bg-bg rounded-card shadow-flow overflow-hidden flex flex-col";

            public const string CardTop = "px-5 py-3.5 border-b border-border flex items-center gap-1.5";

            public const string CardTopLabel = "text-[11.5px] text-text-muted whitespace-nowrap";

            //.// 스텝 본문 공통

            public const string Body = "px-[22px] pt-[26px] pb-7 flex flex-col gap-[18px]";

            public const string BodyCenter = "px-[22px] pt-[34px] pb-[30px] flex flex-col items-center gap-4 text-center";

            public const string Title = "m-0 text-[21px] font-extrabold tracking-[-.02em] text-navy-deep break-keep";

            public const string TitleCenter = "m-0 text-xl font-extrabold tracking-[-.02em] text-navy-deep";

            public const string Subtitle = "m-0 text-[13.5px] text-text-body leading-[1.65] break-keep";

            public const string OrangeCta =
                "h-[50px] border-0 rounded-xl bg-orange hover:bg-orange-hover text-[15px] font-bold text-white " +
                "cursor-pointer whitespace-nowrap shadow-cta-orange-sm transition-colors disabled:cursor-default";

            //.// 스텝 ① — 코드 6칸 (실제 입력은 투명 오버레이 input 하나)

            public const string CodeRow = "relative flex gap-2 justify-center";

            public const string CodeInput =
                "absolute inset-0 w-full h-full opacity-0 cursor-text text-[16px]";

            public const string CodeCellBase =
                "w-[46px] h-14 rounded-xl flex items-center justify-center text-[22px] font-extrabold";

            public const string CodeCellFilled = CodeCellBase + " border-1.5 border-solid border-navy bg-surface-icon text-navy-deep";

            public const string CodeCellEmpty = CodeCellBase + " border-1.5 border-solid border-border bg-white text-text-faint";

            public const string InlineError = "text-[12.5px] font-bold text-danger break-keep";

            public const string NoCodeSection = "flex flex-col gap-[9px] border-t border-dashed border-border pt-4";

            public const string NoCodeHead = "text-xs font-extrabold text-text-muted";

            public const string NoCodeBody = "text-[12.5px] text-text-body leading-[1.6] break-keep";

            //.// 스텝 ② — 선수 카드 · 관계 · 안내

            public const string PlayerCard = "border-1.5 border-border rounded-[15px] p-[18px] flex items-center gap-3.5";

            public const string PlayerAvatar =
                "w-[54px] h-[54px] rounded-full bg-navy-deep flex items-center justify-center text-[19px] font-extrabold text-white flex-none";

            public const string UnclaimedBadge = Badge.Neutral; // 상태 뱃지 캡슐 통일 (Design.AvatarBadge)

            public const string RelationButton =
                "flex-1 h-10 rounded-[10px] text-[13px] font-bold cursor-pointer whitespace-nowrap " +
                "border-1.5 border-solid border-border bg-white text-text-body transition-colors";

            public const string RelationButtonActive =
                "flex-1 h-10 rounded-[10px] text-[13px] font-bold cursor-pointer whitespace-nowrap " +
                "border-1.5 border-solid border-navy bg-navy text-white";

            public const string InfoBox = "bg-surface-soft rounded-[11px] px-[15px] py-3 text-xs text-text-body leading-[1.6] break-keep";

            public const string GhostButton = "h-[42px] border-0 bg-transparent text-[13px] font-bold text-text-muted cursor-pointer";

            //.// 스텝 ③·④ — 상태 원 · 요약 카드 · 다음 행동

            public const string PendingCircle = "w-16 h-16 rounded-full bg-surface-orange-badge flex items-center justify-center";

            public const string DoneCircle = "w-16 h-16 rounded-full bg-teal/[.14] flex items-center justify-center";

            public const string SummaryCard = "w-full border-1.5 border-border rounded-[13px] px-4 py-3.5 flex items-center gap-3 text-left";

            public const string SummaryAvatar =
                "w-10 h-10 rounded-full bg-navy-deep flex items-center justify-center text-[15px] font-extrabold text-white flex-none";

            public const string PendingBadge = Badge.Pending;

            public const string NextCard = "flex items-center gap-2.5 border-1.5 border-border rounded-xl px-[15px] py-[13px] text-left w-full bg-white cursor-pointer";
        }
    }
}
