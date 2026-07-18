namespace PlayGround.Client.Styles
{
    public static partial class Css
    {
        /// <summary>
        /// 로딩 스켈레톤(Design.LoadingStates) · 빈 상태(Design.EmptyStates) 클래스.
        /// 두 패턴은 한 쌍 — 로딩 → 스켈레톤 → 데이터 0건 → 빈 상태로 이어진다.
        /// ※ Tailwind content 글롭이 Styles/**/*.cs만 스캔하므로 클래스 문자열은 여기에 둔다.
        /// </summary>
        public static class State
        {
            //.// 스켈레톤

            /// <summary>시머 공통 — 좌→우 1.6s. 폭·높이·radius는 프리미티브가 붙인다.</summary>
            private const string Shimmer = "bg-shimmer bg-[length:200%_100%] animate-shimmer";

            /// <summary>썸네일·커버 — 한 톤 진한 시머.</summary>
            private const string ShimmerDeep = "bg-shimmer-deep bg-[length:200%_100%] animate-shimmer";

            /// <summary>텍스트 바 — radius 6, 높이는 호출부 지정(8~15px).</summary>
            public const string SkeletonBar = Shimmer + " rounded-md";

            /// <summary>아바타·엠블럼 원.</summary>
            public const string SkeletonCircle = Shimmer + " rounded-full shrink-0";

            /// <summary>썸네일·커버 — radius 10~14.</summary>
            public const string SkeletonThumb = ShimmerDeep + " rounded-[12px]";

            /// <summary>칩 — radius 99.</summary>
            public const string SkeletonChip = Shimmer + " rounded-full shrink-0";

            /// <summary>3초 초과 시 스켈레톤 하단 문구.</summary>
            public const string SlowNotice = "text-xs text-text-muted text-center mt-3.5 break-keep";

            /// <summary>로딩 실패 — 스켈레톤 자리를 그대로 대체(Design.Navigation 축약형).</summary>
            public const string ErrorBox =
                "bg-white border-1.5 border-border rounded-[16px] px-6 py-12 " +
                "flex flex-col items-center gap-2.5 text-center";

            public const string ErrorTitle = "text-[15px] font-extrabold text-navy-deep break-keep";

            public const string ErrorRetry =
                "h-10 px-5 rounded-[10px] bg-navy hover:bg-navy-deep text-[12.5px] font-bold text-white " +
                "whitespace-nowrap border-0 cursor-pointer transition-colors mt-1.5";

            //.// 빈 상태 Tier A — 페이지/탭 수준

            public const string EmptyCard =
                "bg-white border-1.5 border-border rounded-[16px] px-6 py-10 md:py-14 " +
                "flex flex-col items-center text-center";

            /// <summary>일러스트 88px (모바일 72px) — 외곽 illustration / 디테일 navy.</summary>
            public const string EmptyIllustration = "w-[72px] h-[72px] md:w-[88px] md:h-[88px]";

            public const string EmptyTitle = "text-sm md:text-[15.5px] font-extrabold text-navy-deep mt-3.5 break-keep";

            public const string EmptyDescription =
                "text-xs md:text-[12.5px] text-text-body leading-[1.6] mt-1.5 break-keep";

            /// <summary>소유자 액션 — 네이비 채움(오렌지 금지: 오렌지는 랜딩 CTA 전용).</summary>
            public const string EmptyCta =
                "h-10 px-5 rounded-[10px] bg-navy hover:bg-navy-deep text-[12.5px] font-bold text-white " +
                "flex items-center whitespace-nowrap border-0 cursor-pointer transition-colors mt-[18px]";

            /// <summary>보조 탐색 — 흰 아웃라인.</summary>
            public const string EmptyCtaGhost =
                "h-10 px-5 rounded-[10px] bg-white border-1.5 border-border hover:border-navy " +
                "text-[12.5px] font-bold text-navy-deep flex items-center whitespace-nowrap " +
                "cursor-pointer transition-colors mt-[18px]";

            //.// 빈 상태 Tier B — 카드/섹션 수준 (일러스트 없음)

            public const string EmptySlot =
                "bg-white border-1.5 border-border rounded-[16px] px-5 py-[22px] flex items-center gap-[13px]";

            /// <summary>아이콘 박스 40px.</summary>
            public const string EmptySlotIcon =
                "w-10 h-10 rounded-[11px] bg-surface-soft flex items-center justify-center shrink-0 text-navy";

            public const string EmptySlotBody = "flex flex-col gap-0.5 min-w-0";

            public const string EmptySlotTitle = "text-[13px] font-bold text-navy-deep break-keep";

            public const string EmptySlotLink = "text-xs font-bold text-navy hover:text-navy-deep whitespace-nowrap";
        }
    }
}
