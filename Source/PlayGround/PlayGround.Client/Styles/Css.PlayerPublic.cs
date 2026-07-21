namespace PlayGround.Client.Styles
{
    public static partial class Css
    {
        /// <summary>공개 선수 프로필 (/player/{slug}) — Design.PlayerPublicProfile 디테일 공개 뷰.</summary>
        public static class PlayerPublic
        {
            //.// 공통 카드

            public const string Card = "bg-white border-1.5 border-border rounded-[15px] md:rounded-2xl";

            //.// 히어로

            public const string HeroCard =
                "bg-white border-1.5 border-border rounded-2xl md:rounded-[18px] p-4 md:p-7 " +
                "flex flex-col md:flex-row gap-3.5 md:gap-[26px] md:flex-wrap";

            public const string HeroPhoto =
                "w-24 md:w-[150px] aspect-[3/4] rounded-xl md:rounded-[14px] overflow-hidden bg-surface-icon flex-none";

            public const string HeroName =
                "m-0 text-[19px] md:text-[28px] font-extrabold tracking-[-.02em] text-navy-deep whitespace-nowrap";

            public const string HeroMeta = "text-xs md:text-sm text-text-body whitespace-nowrap";

            public const string TeamLink =
                "flex items-center gap-1.5 md:gap-2 text-xs md:text-[13.5px] font-bold text-navy whitespace-nowrap w-fit";

            // 공개 항목 칩 (키·몸무게·주발) — 공개 설정이 켜진 것만 렌더
            public const string FieldChip =
                "text-xs md:text-[12.5px] font-bold text-text-strong bg-surface-icon rounded-full " +
                "px-3 py-1 md:px-3.5 md:py-[5px] whitespace-nowrap";

            //.// CTA (히어로 우측 / 모바일 하단 고정 바)

            public const string RequestButton =
                "flex items-center justify-center h-11 px-6 rounded-[11px] bg-orange hover:bg-orange-hover " +
                "text-[13.5px] font-bold text-white whitespace-nowrap shadow-cta-orange-sm border-0 cursor-pointer transition-colors";

            public const string RequestCaption = "text-[11px] text-text-muted text-center break-keep";

            //.// 시즌 요약

            public const string StatCardNavy =
                "bg-gradient-navy rounded-[13px] md:rounded-[14px] px-[15px] py-[13px] md:px-5 md:py-[18px] " +
                "flex flex-col gap-0.5 md:gap-[3px] text-white";

            public const string StatCard =
                "bg-white border-1.5 border-border rounded-[13px] md:rounded-[14px] px-[15px] py-[13px] md:px-5 md:py-[18px] " +
                "flex flex-col gap-0.5 md:gap-[3px]";

            public const string StatLabel = "text-[10.5px] md:text-[11.5px] font-bold text-text-muted";

            public const string StatLabelOnNavy = "text-[10.5px] md:text-[11.5px] font-bold text-white/60";

            public const string StatValue = "text-base md:text-xl font-extrabold text-navy-deep whitespace-nowrap";

            public const string StatValueOnNavy = "text-base md:text-xl font-extrabold whitespace-nowrap";

            //.// 대표 영상

            public const string VideoBadge =
                "absolute top-[9px] left-[9px] md:top-3 md:left-3 text-[10px] md:text-[11px] font-bold text-white " +
                "bg-orange rounded-full px-[9px] py-0.5 md:px-[11px] md:py-[3px] pointer-events-none";

            public const string VideoTitle = "text-[12.5px] md:text-[14.5px] font-extrabold text-navy-deep break-keep flex-1";

            //.// 커리어 타임라인

            public const string CareerTeamName = "text-[12.5px] md:text-[13.5px] font-extrabold text-navy-deep break-keep";

            public const string CareerMeta = "text-[11px] md:text-[11.5px] text-text-body whitespace-nowrap";

            //.// 잠금 안내 (공개 뷰 전용)

            public const string LockBox =
                "bg-surface-soft border-1.5 border-dashed border-switch-track rounded-[15px] md:rounded-2xl " +
                "p-[18px] md:p-[26px] flex flex-col md:flex-row md:items-center gap-2.5 md:gap-[18px] md:flex-wrap";

            public const string LockIconBox =
                "w-9 h-9 md:w-11 md:h-11 rounded-[10px] md:rounded-xl bg-surface-icon flex items-center justify-center flex-none";

            public const string LockTitle = "text-[13px] md:text-[14.5px] font-extrabold text-navy-deep break-keep leading-[1.5]";

            public const string LockDescription = "text-[11.5px] md:text-[12.5px] text-text-body leading-[1.6] break-keep";

            public const string LockRequestButton =
                "hidden md:flex items-center h-[42px] px-[22px] border-1.5 border-navy rounded-[10px] bg-transparent " +
                "text-[13px] font-bold text-navy hover:bg-navy hover:text-white whitespace-nowrap cursor-pointer transition-colors";

            //.// 모바일 하단 고정 CTA 바

            public const string MobileCtaBar =
                "md:hidden fixed bottom-0 inset-x-0 bg-bg/[.96] backdrop-blur-[10px] border-t border-border " +
                "px-4 pt-2.5 pb-[calc(10px+env(safe-area-inset-bottom))] flex gap-2.5 z-50";

            public const string MobileCtaButton =
                "flex-1 flex items-center justify-center h-12 rounded-xl bg-orange hover:bg-orange-hover " +
                "text-[14.5px] font-bold text-white whitespace-nowrap shadow-authbtn border-0 cursor-pointer transition-colors";
        }
    }
}
