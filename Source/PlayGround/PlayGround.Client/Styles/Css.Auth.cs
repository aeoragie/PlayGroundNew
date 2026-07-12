namespace PlayGround.Client.Styles
{
    public static partial class Css
    {
        /// <summary>
        /// 인증·온보딩 전용 클래스 상수 — SPEC.AUTHONBOARDING.md 기준.
        /// (색상은 토큰, 크기·radius·shadow는 arbitrary 값)
        /// </summary>
        public static class Auth
        {
            // 흰 카드 (보더 + radius + 그림자)
            public const string Card =
                "bg-white border-1.5 border-border rounded-[18px] shadow-authcard";

            // 입력 필드 (h-48, 포커스 시 네이비 아웃라인)
            public const string Input =
                "h-12 border-1.5 border-border rounded-[11px] px-[15px] text-[14.5px] font-medium text-navy-deep " +
                "placeholder:text-text-faint focus:outline-none focus:ring-2 focus:ring-navy focus:border-navy";

            // 주 버튼 (오렌지, h-50)
            public const string PrimaryButton =
                "h-[50px] rounded-[11px] bg-orange hover:bg-orange-hover text-white text-[15px] font-bold " +
                "shadow-authbtn transition-colors disabled:opacity-60";

            // 보조 버튼 (흰 배경 + 보더)
            public const string SecondaryButton =
                "h-[50px] px-[22px] rounded-[11px] bg-white border-1.5 border-border hover:bg-surface-alt " +
                "text-[14.5px] font-bold text-text-strong transition-colors";

            // 네이비 버튼 (이메일로 계속하기)
            public const string NavyButton =
                "h-[50px] rounded-[11px] bg-navy hover:bg-navy-deep text-white text-[15px] font-bold transition-colors";

            // 텍스트 링크 (하단 스킵/보조)
            public const string TextLink =
                "border-0 bg-transparent text-[13.5px] text-text-muted hover:text-navy underline cursor-pointer transition-colors";
        }
    }
}
