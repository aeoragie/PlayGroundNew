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

            //.// 권한 뷰 (승인된 에이전트 — Design.PlayerPublicProfile 디테일 권한)

            // 상단 승인 배너 — GNB 바로 아래 풀폭 teal 틴트
            public const string GrantBanner = "bg-teal/10 border-b border-teal/30";

            public const string GrantBannerInner =
                "max-w-[1000px] mx-auto px-4 md:px-8 py-2.5 md:py-[11px] flex items-start md:items-center gap-2.5 flex-wrap";

            public const string GrantBannerText = "text-[11.5px] md:text-[12.5px] font-bold text-teal-ink break-keep leading-[1.55]";

            public const string GrantBannerNote = "hidden md:block text-[11.5px] text-text-body whitespace-nowrap";

            // 학교 칩 — 승인 열람 항목이라 공개 칩과 톤을 가른다 (teal 틴트+보더)
            public const string SchoolChip =
                "text-xs md:text-[12.5px] font-bold text-teal-ink bg-teal/[.12] border border-teal/[.35] rounded-full " +
                "px-3 py-1 md:px-3.5 md:py-[5px] whitespace-nowrap";

            // 경기별 상세 기록 카드 — teal 보더로 승인 영역을 시각 구분
            public const string RecordCard =
                "bg-white border-1.5 border-teal/40 rounded-[15px] md:rounded-2xl overflow-hidden";

            public const string RecordHeader =
                "px-4 py-[13px] md:px-[22px] md:py-4 border-b border-surface-icon flex items-center gap-2 md:gap-2.5";

            public const string RecordTitle = "m-0 text-[13.5px] md:text-[15.5px] font-extrabold text-navy-deep";

            public const string RecordRowPc =
                "hidden md:flex items-center gap-3.5 px-[22px] py-3 border-b border-surface-icon last:border-b-0";

            public const string RecordRowMobile =
                "md:hidden flex flex-col gap-1.5 px-4 py-[11px] border-b border-surface-icon last:border-b-0";

            public const string CompPillBase =
                "text-[10px] md:text-[10.5px] font-bold rounded-full px-2 md:px-[9px] py-0.5 whitespace-nowrap shrink-0";

            public const string RecordMatchTitle = "text-xs md:text-[13px] font-bold text-navy-deep break-keep";

            public const string RecordStatChip =
                "text-[11px] md:text-xs font-extrabold text-navy-deep bg-surface-alt rounded-[6px] md:rounded-[7px] px-[9px] md:px-2.5 py-0.5 whitespace-nowrap";

            public const string RecordMinutes = "text-[11px] md:text-xs text-text-body whitespace-nowrap";

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

            //.// 카드 뷰 (공유용 — 네이비 그라디언트, dc 카드 360/모바일 340)

            public const string CardStage =
                "min-h-screen flex flex-col items-center justify-center gap-4 px-5 py-10 " +
                "bg-[linear-gradient(160deg,#eef1f7,rgb(var(--color-surface-alt)))]";

            public const string CardShell =
                "w-full max-w-[340px] md:max-w-[360px] rounded-[20px] md:rounded-[22px] overflow-hidden relative " +
                "text-white bg-gradient-navy shadow-player-card";

            public const string CardDecorLarge =
                "absolute -top-[70px] -right-[70px] w-[210px] h-[210px] md:w-[220px] md:h-[220px] border-1.5 border-white/[.09] rounded-full pointer-events-none";

            public const string CardDecorSmall =
                "absolute top-5 -right-10 w-[140px] h-[140px] md:w-[150px] md:h-[150px] border-1.5 border-teal/25 rounded-full pointer-events-none";

            public const string CardStatChip =
                "flex-1 bg-white/[.08] rounded-[10px] md:rounded-[11px] py-[9px] md:py-2.5 flex flex-col items-center gap-px";

            public const string CardGrantedBlock =
                "mx-[22px] md:mx-6 border-t border-dashed border-white/20 py-3 md:py-3.5 flex flex-col gap-1.5 relative";

            public const string CardFooterBar =
                "bg-[rgba(15,20,30,.35)] px-[22px] md:px-6 py-3 md:py-[13px] flex items-center gap-[9px] relative";

            public const string CardSaveButton =
                "flex-1 md:flex-none flex items-center justify-center gap-[7px] h-[46px] md:h-11 px-0 md:px-[22px] rounded-[11px] " +
                "bg-orange hover:bg-orange-hover text-[13px] md:text-[13.5px] font-bold text-white whitespace-nowrap " +
                "shadow-cta-orange-sm border-0 cursor-pointer transition-colors";

            public const string CardShareButton =
                "flex-1 md:flex-none flex items-center justify-center gap-[7px] h-[46px] md:h-11 px-0 md:px-[22px] rounded-[11px] " +
                "border-1.5 border-border bg-white text-[13px] md:text-[13.5px] font-bold text-text-strong " +
                "hover:border-navy hover:text-navy whitespace-nowrap cursor-pointer transition-colors";

            public const string CardCaption =
                "text-xs md:text-[11.5px] text-text-muted text-center break-keep max-w-[340px] md:max-w-[360px] leading-[1.6]";

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
