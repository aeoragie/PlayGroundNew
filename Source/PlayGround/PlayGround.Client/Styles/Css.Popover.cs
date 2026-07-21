namespace PlayGround.Client.Styles
{
    public static partial class Css
    {
        /// <summary>인포 팝오버 (Design.TooltipHelp §2) — ⓘ 20px 회색 원 + 흰 카드 radius 12.</summary>
        public static class Popover
        {
            public const string Trigger =
                "w-5 h-5 rounded-full border-0 bg-surface-icon flex items-center justify-center " +
                "cursor-pointer flex-none p-0";

            public const string Card =
                "absolute z-50 w-[300px] max-w-[80vw] bg-white border-1.5 border-border " +
                "rounded-xl px-[15px] py-[13px] shadow-menu flex flex-col gap-1.5 text-left";

            // 기본 = 아래로 (좌측 정렬) / 푸터 등 하단은 위로 + 우측 정렬(화면 오른쪽 잘림 방지)
            public const string PlaceDown = "left-0 top-[26px]";

            public const string PlaceUp = "right-0 bottom-[26px]";

            public const string Title = "text-[12.5px] font-extrabold text-navy-deep break-keep";

            public const string Body = "text-xs text-text-body leading-[1.65] break-keep font-normal whitespace-normal";

            public const string Link = "text-[11.5px] font-bold text-navy w-fit";
        }
    }
}
