namespace PlayGround.Client.Styles
{
    public static partial class Css
    {
        /// <summary>
        /// 알림 센터 클래스 상수 — Design.ClaimFlow 알림 절. GNB 벨에서 열리는 공통 패널.
        /// 액션형(연결 요청) = 인라인 승인/거절, 이동형 = 셰브론 + 딥링크, 읽음 = opacity .85.
        /// (Tailwind content 글롭이 Styles/**/*.cs만 스캔 — 클래스 문자열은 반드시 여기에.)
        /// </summary>
        public static class Notification
        {
            //.// 벨 (GNB) — 컨테이너 모양은 호출부(네이비 텍스트형 / 박스형 / 밝은 배경)

            public const string BellPlain =
                "relative flex items-center justify-center bg-transparent border-0 cursor-pointer " +
                "text-white/70 hover:text-white transition-colors";

            public const string BellBox =
                "relative w-9 h-9 rounded-[10px] bg-white/[.08] flex items-center justify-center " +
                "text-white/85 border-0 cursor-pointer";

            public const string BellLight =
                "relative w-9 h-9 rounded-[10px] flex items-center justify-center bg-transparent border-0 " +
                "cursor-pointer text-text-muted hover:text-navy transition-colors";

            public const string BellBadge =
                "absolute -top-1.5 -right-2 min-w-[17px] h-[17px] px-1 rounded-full bg-orange text-white " +
                "text-[10.5px] font-extrabold flex items-center justify-center";

            public const string BellBadgeBox =
                "absolute top-0.5 right-0.5 min-w-[15px] h-[15px] px-0.5 rounded-full bg-orange text-white " +
                "text-[9.5px] font-extrabold flex items-center justify-center";

            //.// 패널 — 벨 아래 카드 (PC 우측 고정 420px / 모바일 좌우 12px)

            public const string Overlay = "fixed inset-0 z-[70]";

            public const string Panel =
                "fixed z-[71] top-16 right-3 left-3 md:left-auto md:w-[420px] max-h-[78vh] " +
                "bg-bg rounded-card shadow-flow overflow-hidden flex flex-col";

            public const string Head = "bg-navy-deep px-5 py-3.5 flex items-center gap-2.5 flex-none";

            public const string HeadTitle = "text-[15px] font-extrabold text-white";

            public const string HeadCount = "text-[11px] font-extrabold text-white bg-orange rounded-full px-2 py-px";

            public const string HeadContext = "text-[11.5px] text-white/55 whitespace-nowrap";

            public const string ChipRow = "flex gap-1.5 px-4 py-3 border-b border-surface-icon flex-none overflow-x-auto";

            public const string Chip =
                "h-[30px] flex items-center px-[13px] rounded-full text-[11.5px] font-bold whitespace-nowrap " +
                "border-1.5 border-solid border-border bg-white text-text-body cursor-pointer";

            public const string ChipActive =
                "h-[30px] flex items-center px-[13px] rounded-full text-[11.5px] font-bold whitespace-nowrap " +
                "border-1.5 border-solid border-navy bg-navy text-white cursor-pointer";

            public const string List = "flex-1 overflow-y-auto flex flex-col";

            //.// 액션형 카드 (연결 요청)

            public const string ActionCard = "px-4 py-4 border-b border-surface-icon flex flex-col gap-3";

            public const string ActionCardPending = ActionCard + " bg-surface-action";

            public const string ActionIcon =
                "w-[38px] h-[38px] rounded-full bg-surface-orange-badge flex items-center justify-center flex-none text-orange";

            public const string ActionTitle = "text-[13.5px] font-extrabold text-navy-deep break-keep";

            public const string ActionBody = "text-[12.5px] text-text-strong leading-[1.55] break-keep";

            public const string ActionMeta = "text-[11px] text-text-muted whitespace-nowrap";

            public const string ApproveButton =
                "flex-1 h-[42px] border-0 rounded-[10px] bg-teal hover:bg-teal/90 text-[13px] font-bold text-white " +
                "cursor-pointer whitespace-nowrap transition-colors";

            public const string RejectButton =
                "flex-1 h-[42px] border-1.5 border-solid border-border rounded-[10px] bg-white text-[13px] font-bold " +
                "text-text-body cursor-pointer whitespace-nowrap";

            public const string ActionFoot = "text-[11px] text-text-muted break-keep";

            public const string DoneBox =
                "flex items-center gap-2 bg-teal/10 rounded-[10px] px-3.5 py-[11px] text-[12.5px] font-bold text-teal-ink break-keep";

            public const string RejectedBox =
                "flex items-center gap-2 bg-surface-icon rounded-[10px] px-3.5 py-[11px] text-[12.5px] font-bold text-text-muted break-keep";

            //.// 이동형 행

            public const string MoveRow =
                "w-full px-4 py-[15px] border-0 border-b border-solid border-surface-icon flex items-start gap-[11px] " +
                "bg-transparent text-left cursor-pointer hover:bg-surface-soft transition-colors";

            public const string MoveRowRead = MoveRow + " opacity-[.85]";

            public const string MoveIconTeal = "w-[38px] h-[38px] rounded-full bg-teal/[.12] flex items-center justify-center flex-none text-teal-ink";

            public const string MoveIconNavy = "w-[38px] h-[38px] rounded-full bg-surface-icon flex items-center justify-center flex-none text-navy";

            public const string MoveIconMuted = "w-[38px] h-[38px] rounded-full bg-surface-icon flex items-center justify-center flex-none text-text-muted";

            public const string MoveTitle = "text-[13px] font-extrabold text-navy-deep break-keep";

            public const string MoveBody = "text-[12.5px] text-text-strong leading-[1.55] break-keep";

            public const string MoveTime = "text-[11px] text-text-muted whitespace-nowrap";

            public const string EmptyText = "px-4 py-10 text-center text-[12.5px] text-text-muted break-keep";
        }
    }
}
