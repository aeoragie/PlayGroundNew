namespace PlayGround.Client.Styles
{
    public static partial class Css
    {
        /// <summary>
        /// 토글 스위치 공용 클래스 상수 — Design.ToggleSwitch 카탈로그 기준.
        /// 원칙: 즉시 적용 켬/끔 전용(폼 안 금지) · 44×26 knob 22 · 낙관적 반영 후 실패 롤백.
        /// (Tailwind content 글롭이 Styles/**/*.cs만 스캔 — 클래스 문자열은 반드시 여기에.)
        /// </summary>
        public static class Toggle
        {
            //.// 행 (레이블+캡션 좌 · 스위치/뱃지 우 — 터치 타겟은 행 전체, 모바일 56px)

            public const string Row =
                "w-full flex items-center gap-2.5 py-2.5 min-h-[56px] md:min-h-0 text-left bg-transparent " +
                "border-0 border-b border-solid border-surface-soft last:border-b-0 cursor-pointer " +
                "disabled:cursor-default";

            public const string TextColumn = "flex flex-col gap-px flex-1 min-w-0";

            public const string Label = "text-[13px] font-bold text-navy-deep whitespace-nowrap";

            public const string Caption = "text-[11px] text-text-muted break-keep";

            //.// 트랙 44×26 + knob 22 (켬 teal / 끔 switch-track / 비활성 50%)

            public const string TrackBase =
                "w-11 h-[26px] rounded-full p-[2px] flex flex-none transition-colors duration-200";

            public const string TrackOn = TrackBase + " bg-teal justify-end";

            public const string TrackOff = TrackBase + " bg-switch-track justify-start";

            public const string TrackDisabled = "opacity-50";

            public const string Knob = "w-[22px] h-[22px] rounded-full bg-white shadow-knob";

            //.// 잠금 — 스위치 대신 "항상 켜짐" 뱃지 (승인형 알림)

            public const string LockedBadge = Badge.Neutral; // 상태 뱃지 캡슐 통일 (Design.AvatarBadge)

            //.// 계층 — 상위 끄면 하위 그룹 비활성+dimmed (들여쓰기 16px + 좌측 2px 보더)

            public const string SubGroup =
                "pl-4 ml-[2px] border-0 border-l-2 border-solid border-surface-icon transition-opacity";

            public const string SubGroupDimmed = "opacity-[.45]";
        }
    }
}
