namespace PlayGround.Client.Styles
{
    public static partial class Css
    {
        /// <summary>대시보드 허브 (Design.DashboardHub). 읽기 전용 현관 화면.</summary>
        public static class Hub
        {
            public const string Page = "min-h-screen bg-surface-alt flex flex-col text-navy-deep";
            public const string Container = "w-full max-w-[1100px] mx-auto px-5 md:px-8 py-6 md:py-9 flex flex-col gap-7 md:gap-9";

            //.// 섹션 공통

            public const string SectionTitle = "m-0 text-[15px] md:text-[16.5px] font-extrabold tracking-[-.01em] text-navy-deep whitespace-nowrap";
            public const string RoleLabel = "text-[11.5px] font-bold text-text-muted bg-surface-icon rounded-full px-[9px] py-[3px] whitespace-nowrap";
            public const string Card = "bg-white border-1.5 border-border rounded-card-xs";

            //.// 처리가 필요해요 — 허브에서 유일하게 오렌지 테두리가 허용되는 카드

            public const string ActionCard = "bg-white border-1.5 border-orange/35 rounded-card-xs p-4 md:p-5 flex flex-col gap-3.5";
            public const string ActionCount = "min-w-[19px] h-[19px] px-1.5 rounded-full bg-orange text-white text-[11.5px] font-extrabold flex items-center justify-center";
            public const string ActionGrid = "grid grid-cols-1 md:grid-cols-[repeat(auto-fill,minmax(300px,1fr))] gap-2.5";
            public const string ActionItem = "flex items-center gap-3 text-left bg-surface-soft border-0 rounded-xl px-3.5 py-3 cursor-pointer hover:bg-surface-icon transition-colors no-underline";
            public const string ActionTitle = "m-0 text-[12.5px] font-extrabold text-navy-deep leading-snug break-keep";
            public const string ActionDesc = "m-0 mt-0.5 text-[11.5px] text-text-muted leading-snug break-keep";

            /// <summary>유형 칩 — 연결=오렌지톤 / 열람=violet / 결과=네이비톤.</summary>
            public static string ActionChip(string kind)
            {
                const string Base = "w-8 h-8 md:w-9 md:h-9 rounded-full shrink-0 flex items-center justify-center text-[10.5px] font-extrabold ";
                return kind switch
                {
                    "Invite" => Base + "bg-surface-orange-badge text-orange-ink",
                    "View" => Base + "bg-agent/10 text-agent",
                    _ => Base + "bg-surface-icon text-navy",
                };
            }

            //.// 내 팀 — 네이비 그라디언트 대형 카드

            public const string TeamCard = "relative overflow-hidden rounded-card-sm p-5 md:p-6 text-white bg-[linear-gradient(160deg,rgb(var(--color-navy-deep)),rgb(var(--color-navy)))]";
            public const string TeamDecor = "absolute -right-10 -top-14 w-[180px] h-[180px] rounded-full bg-white/[.06] pointer-events-none";
            public const string TeamIconBox = "w-12 h-12 md:w-[58px] md:h-[58px] rounded-[14px] bg-white/10 flex items-center justify-center shrink-0";
            public const string TeamName = "m-0 text-[15.5px] md:text-[18px] font-extrabold tracking-[-.01em] whitespace-nowrap";
            // 다크 네이비 카드 위라 캡슐 틴트가 묻힌다 — 밝은 teal 텍스트 유지 (Records 히어로와 같은 예외)
            public const string TeamVerified = "text-[11px] font-bold text-teal whitespace-nowrap";
            public const string TeamSummary = "m-0 mt-1 text-[12.5px] text-white/70 leading-relaxed break-keep";
            public const string TeamPrimary = "h-[42px] px-5 rounded-btn bg-orange hover:bg-orange-hover text-white text-[13.5px] font-extrabold flex items-center justify-center whitespace-nowrap no-underline transition-colors";
            public const string TeamGhost = "h-[42px] px-5 rounded-btn bg-white/10 hover:bg-white/[.18] border-1.5 border-white/[.22] text-white text-[13.5px] font-bold flex items-center justify-center whitespace-nowrap no-underline transition-colors";

            //.// 내 자녀

            public const string ChildGrid = "grid grid-cols-1 md:grid-cols-[repeat(auto-fill,minmax(340px,1fr))] gap-3.5";
            public const string ChildCard = Card + " p-4 md:p-5 flex flex-col gap-3.5";
            public const string ChildName = "m-0 text-[14.5px] font-extrabold text-navy-deep whitespace-nowrap";
            public const string ChildMeta = "m-0 mt-0.5 text-[12px] text-text-muted break-keep";
            public const string StatGrid = "grid grid-cols-3 gap-1.5";
            public const string StatCell = "bg-surface-soft rounded-[10px] py-2.5 flex flex-col items-center gap-0.5";
            public const string StatValue = "text-[17px] font-extrabold text-navy-deep leading-none";
            public const string StatKey = "text-[11px] font-bold text-text-muted whitespace-nowrap";
            public const string ChildPrimary = "flex-1 h-10 rounded-btn bg-navy hover:bg-navy-deep text-white text-[13px] font-extrabold flex items-center justify-center whitespace-nowrap no-underline transition-colors";

            //.// 바로가기

            public const string ShortcutGrid = "grid grid-cols-2 md:grid-cols-3 gap-2.5 md:gap-3.5";
            public const string Shortcut = Card + " p-4 flex md:flex-row flex-col md:items-center items-start gap-3 no-underline hover:border-navy-muted transition-colors";
            public const string ShortcutTitle = "m-0 text-[13.5px] font-extrabold text-navy-deep whitespace-nowrap";
            public const string ShortcutSub = "m-0 mt-0.5 text-[11.5px] text-text-muted break-keep";
        }
    }
}
