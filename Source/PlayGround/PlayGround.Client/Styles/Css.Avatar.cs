namespace PlayGround.Client.Styles
{
    public static partial class Css
    {
        /// <summary>
        /// 아바타 — Design.AvatarBadge. 사진 없으면 항상 이니셜(빈 이미지 상태 없음).
        /// 색은 주체 유형별 고정(랜덤 금지) — 팀 네이비 / 개인 teal / 에이전트 violet.
        /// ※ Tailwind content 글롭이 Styles/**/*.cs만 스캔하므로 클래스 문자열은 여기에 둔다.
        /// </summary>
        public static class Avatar
        {
            /// <summary>크기·폰트는 인라인 style — Tailwind JIT가 임의값을 스캔하지 못한다(Skeleton과 같은 이유).</summary>
            public const string Base =
                "rounded-full flex items-center justify-center font-extrabold text-white select-none shrink-0 leading-none";
        }
    }
}
