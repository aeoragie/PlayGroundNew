namespace PlayGround.Client.Styles
{
    public static partial class Css
    {
        /// <summary>
        /// 설정 화면 클래스 상수 — Design.Settings dc 기준.
        /// PC = 다크 GNB + 좌 sticky 메뉴 210px / 모바일 = 다크 상단바 + 세그먼트 탭(teal 언더라인).
        /// (Tailwind content 글롭이 Styles/**/*.cs만 스캔 — 클래스 문자열은 반드시 여기에.)
        /// </summary>
        public static class Settings
        {
            //.// PC 사이드 메뉴 (210px sticky)

            public const string SideNav =
                "w-[210px] flex-none bg-white border-1.5 border-border rounded-[15px] p-2.5 " +
                "flex flex-col gap-0.5 sticky top-[84px]";

            public const string SideMenuButton =
                "h-[42px] px-[13px] border-0 rounded-[10px] cursor-pointer text-[13.5px] font-bold " +
                "text-left whitespace-nowrap bg-transparent text-text-body transition-colors";

            public const string SideMenuButtonActive =
                "h-[42px] px-[13px] border-0 rounded-[10px] cursor-pointer text-[13.5px] font-bold " +
                "text-left whitespace-nowrap bg-navy text-white";

            public const string SideLogout =
                "h-10 px-[13px] border-0 rounded-[10px] bg-transparent text-[13px] font-bold text-danger " +
                "cursor-pointer text-left whitespace-nowrap hover:bg-danger/5 transition-colors";

            //.// 모바일 세그먼트 탭 (다크 GNB 하단, teal 언더라인 2.5px)

            public const string MobileTab =
                "flex-1 h-[42px] border-0 bg-transparent cursor-pointer text-[13px] font-bold whitespace-nowrap " +
                "border-b-[2.5px] border-solid border-transparent text-white/55";

            public const string MobileTabActive =
                "flex-1 h-[42px] border-0 bg-transparent cursor-pointer text-[13px] font-bold whitespace-nowrap " +
                "border-b-[2.5px] border-solid border-teal text-white";

            //.// 카드 · 카드 안 구획

            public const string Card =
                "bg-white border-1.5 border-border rounded-[15px] p-[18px] md:p-[22px] flex flex-col";

            public const string CardHeading = "text-[12.5px] md:text-[13px] font-extrabold text-text-strong";

            public const string DividerTop = "border-t border-surface-icon";

            //.// 계정 탭 — 프로필 · 연결된 로그인 · 계정 관리

            public const string ProfileAvatar =
                "w-[50px] h-[50px] md:w-14 md:h-14 rounded-full bg-teal flex items-center justify-center " +
                "text-lg md:text-xl font-extrabold text-white flex-none";

            public const string OutlineButton =
                "h-9 md:h-[38px] px-3.5 md:px-4 border-1.5 border-border rounded-btn bg-white " +
                "text-xs md:text-[12.5px] font-bold text-text-strong cursor-pointer whitespace-nowrap " +
                "hover:border-navy hover:text-navy transition-colors";

            public const string LoginRowLinked =
                "flex items-center gap-2.5 md:gap-[11px] border border-border rounded-[11px] px-[13px] py-[11px] md:px-[15px] md:py-3";

            public const string LoginRowUnlinked =
                "flex items-center gap-2.5 md:gap-[11px] border border-dashed border-border rounded-[11px] px-[13px] py-[11px] md:px-[15px] md:py-3";

            public const string LinkedBadge = Badge.Positive; // 상태 뱃지 캡슐 통일 (Design.AvatarBadge)

            public const string SmallOutlineButton =
                "h-[30px] md:h-8 px-3 md:px-[13px] border-1.5 border-border rounded-lg bg-white " +
                "text-[11px] md:text-[11.5px] font-bold text-text-body cursor-pointer whitespace-nowrap " +
                "hover:border-navy hover:text-navy transition-colors";

            public const string ManageButton =
                "h-[34px] md:h-9 px-3.5 md:px-[15px] border-1.5 border-border rounded-btn bg-white " +
                "text-[11.5px] md:text-xs font-bold text-text-strong cursor-pointer whitespace-nowrap";

            // 계정 삭제 — 레드 보더 #f2d5d0은 danger 25% 근사가 아니라 전용 톤 → danger-muted보다 옅은 danger/25
            public const string DeleteButton =
                "h-[34px] md:h-9 px-3.5 md:px-[15px] border-1.5 border-danger/25 rounded-btn bg-white " +
                "text-[11.5px] md:text-xs font-bold text-danger cursor-pointer whitespace-nowrap";

            public const string MobileLogout =
                "h-[46px] border-1.5 border-border rounded-xl bg-white text-[13px] font-bold text-danger " +
                "cursor-pointer whitespace-nowrap";

            //.// 역할 탭 — 역할 카드

            public const string RoleCard =
                "flex items-center gap-[11px] md:gap-3 border border-border rounded-xl px-3.5 py-[13px] md:px-4 md:py-3.5";

            public const string RoleCardPending =
                "flex items-center gap-[11px] md:gap-3 border border-dashed border-switch-track rounded-xl " +
                "px-3.5 py-[13px] md:px-4 md:py-3.5 opacity-75";

            public const string RoleIconBox =
                "w-[38px] h-[38px] md:w-10 md:h-10 rounded-[10px] md:rounded-[11px] bg-surface-icon " +
                "flex items-center justify-center flex-none";

            public const string RoleIconBoxAgent =
                "w-[38px] h-[38px] md:w-10 md:h-10 rounded-[10px] md:rounded-[11px] bg-agent/[.08] " +
                "flex items-center justify-center flex-none";

            public const string RoleActiveBadge = Badge.Positive;

            public const string RolePendingBadge = Badge.Neutral;

            //.// 알림 탭 — 분류 칩 (승인=orange / 경기·모집·리뷰=navy / 열람=violet)

            public const string ChipBase =
                "text-[9.5px] md:text-[10px] font-bold rounded-full px-2 md:px-[9px] py-[3px] whitespace-nowrap flex-none";

            public const string ChipOrange = ChipBase + " text-orange-ink bg-surface-orange-badge";

            public const string ChipNavy = ChipBase + " text-navy bg-surface-icon";

            public const string ChipViolet = ChipBase + " text-agent bg-agent/10";

            public const string FootCaption = "text-[11px] md:text-[11.5px] text-text-muted pt-2.5 break-keep";
        }
    }
}
