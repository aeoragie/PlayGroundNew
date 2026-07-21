namespace PlayGround.Client.Styles
{
    public static partial class Css
    {
        /// <summary>
        /// 팀 대시보드 전용 클래스 상수 — SPEC.TEAMDASHBOARD.md 기준.
        /// (색상은 토큰, 크기·radius는 arbitrary 값)
        /// </summary>
        public static class Dashboard
        {
            public const string Card =
                "bg-white border-1.5 border-border rounded-[16px]";

            public const string SectionTitle =
                "m-0 text-[23px] font-extrabold tracking-[-.02em] text-navy-deep";

            public const string CardTitle =
                "text-base font-extrabold text-navy-deep";

            // teal 틴트 — "공개 홈페이지 노출중" 등. 상태 뱃지 캡슐 통일 (Design.AvatarBadge)
            public const string BadgeTeal = Badge.Positive;

            public const string BadgeGray =
                "text-[11.5px] font-bold text-text-strong bg-surface-icon " +
                "rounded-full px-2.5 py-[3px] whitespace-nowrap";

            // 화면당 1개 주 액션 (오렌지)
            public const string PrimaryButton =
                "h-[42px] px-5 rounded-[10px] bg-orange hover:bg-orange-hover text-[13.5px] font-bold text-white " +
                "shadow-cta-orange-sm transition-colors whitespace-nowrap";

            // 모바일 주 액션 (높이 40px)
            public const string MobilePrimaryButton =
                "h-10 px-4 rounded-[10px] bg-orange hover:bg-orange-hover text-[13px] font-bold text-white " +
                "shadow-cta-orange-sm transition-colors whitespace-nowrap";

            // 모바일 보조 액션 (아웃라인) — 팀 정보 수정 등
            public const string MobileSecondaryButton =
                "h-10 px-4 rounded-[10px] bg-white border-1.5 border-border hover:border-navy " +
                "text-[13px] font-bold text-text-strong transition-colors whitespace-nowrap cursor-pointer";

            // 필터 칩 pill — 선택 = 네이비 채움 (연령 탭·결과/영상 필터 공용)
            public const string FilterChipOn =
                "h-[38px] px-[18px] rounded-full text-[13.5px] font-bold border-1.5 whitespace-nowrap " +
                "border-navy bg-navy text-white";

            public const string FilterChipOff =
                "h-[38px] px-[18px] rounded-full text-[13.5px] font-bold border-1.5 whitespace-nowrap " +
                "border-border bg-white text-text-body";

            // 화면당 1개인 보조 버튼 (정보 수정 등)
            public const string SecondaryButton =
                "h-[42px] px-5 border-1.5 border-border rounded-[10px] bg-white text-[13.5px] font-bold " +
                "text-text-strong hover:bg-surface-alt transition-colors";

            // 점선 추가 버튼 (＋ 코치 초대 · ＋ 채널 추가)
            public const string DashedButton =
                "h-[38px] px-4 border-1.5 border-dashed border-border rounded-[10px] bg-transparent text-[13px] font-bold " +
                "text-text-muted hover:border-navy hover:text-navy whitespace-nowrap transition-colors";
        }
    }
}
