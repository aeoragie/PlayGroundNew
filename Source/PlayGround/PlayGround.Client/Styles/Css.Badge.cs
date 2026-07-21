namespace PlayGround.Client.Styles
{
    public static partial class Css
    {
        /// <summary>
        /// 상태·카운트 뱃지 클래스 상수 — Design.AvatarBadge (전 서비스 통일).
        /// 상태 캡슐: 10px 700 · radius 99 · 패딩 3×9 · **행당 뱃지 1개**(둘 필요하면 하나는 메타 텍스트로 강등).
        /// 카운트: **오렌지 유일 허용처** · 0 숨김 · 100 이상 "99+".
        /// (Tailwind content 글롭이 Styles/**/*.cs만 스캔 — 클래스 문자열은 반드시 여기에.)
        /// </summary>
        public static class Badge
        {
            public const string Base =
                "inline-flex items-center text-[10px] font-bold rounded-full px-[9px] py-[3px] whitespace-nowrap w-fit";

            /// <summary>긍정·완료 — 연결됨 · ✓ 인증팀 · 승.</summary>
            public const string Positive = Base + " text-teal-ink bg-teal/[.12]";

            /// <summary>대기·주의 — 승인 대기 · 모집중.</summary>
            public const string Pending = Base + " text-orange-ink bg-surface-orange-badge";

            /// <summary>중립·잠금 — 항상 켜짐 · 무 · 미연결 · 마감.</summary>
            public const string Neutral = Base + " text-text-muted bg-surface-icon";

            /// <summary>정보 — 내 팀.</summary>
            public const string Info = Base + " text-navy bg-navy/[.08]";

            /// <summary>에이전트 — 열람 요청 (violet은 에이전트 요소 전용).</summary>
            public const string Agent = Base + " text-agent bg-agent/10";

            /// <summary>패·오류.</summary>
            public const string Negative = Base + " text-danger bg-surface-danger-badge";

            //.// 카운트 뱃지 — 아이콘 우상단 -4px 오프셋 (부모가 relative)

            public const string Count =
                "absolute -top-1 -right-1 min-w-[18px] h-[18px] px-[5px] rounded-full bg-orange " +
                "flex items-center justify-center text-[10px] font-extrabold text-white";
        }
    }
}
