namespace PlayGround.Client.Styles
{
    public static partial class Css
    {
        /// <summary>
        /// 세그먼트 — Design.SearchFilter. **데이터 축 전환**에만 쓴다(칩과 역할을 섞지 않는다).
        /// 트랙은 회색, 선택 항목만 흰 카드 + 그림자.
        /// 친선/공식(B5)·자녀 전환이 같은 모양을 쓰므로 여기 한 곳에 둔다.
        /// ※ Tailwind content 글롭이 Styles/**/*.cs만 스캔하므로 클래스 문자열은 여기에 둔다.
        /// </summary>
        public static class Segment
        {
            public const string Track =
                "flex bg-surface-segment rounded-[11px] p-[3px] gap-0.5 max-w-[320px] w-full md:w-auto";

            /// <summary>항목 폭이 이름 길이에 따라 달라져야 하면 wide=false로 min-w를 뗀다.</summary>
            public static string Item(bool isActive, bool wide = true)
            {
                return "flex-1 h-9 md:h-[34px] rounded-[9px] text-xs font-bold whitespace-nowrap " +
                       "border-0 cursor-pointer transition-colors flex items-center justify-center px-3 " +
                       (wide ? "md:min-w-[92px] " : string.Empty) +
                       (isActive
                           ? "bg-white text-navy-deep shadow-[0_1px_4px_rgba(28,43,74,.12)] font-extrabold"
                           : "bg-transparent text-text-muted");
            }
        }
    }
}
