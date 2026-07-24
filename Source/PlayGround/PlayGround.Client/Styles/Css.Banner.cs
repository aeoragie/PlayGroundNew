using PlayGround.Client.Services.Feedback;

namespace PlayGround.Client.Styles
{
    public static partial class Css
    {
        /// <summary>시스템·운영 배너 (Design.BannerStepper §1) — 3톤, 콘텐츠 최상단 풀폭, 동시 1개.</summary>
        public static class Banner
        {
            /// <summary>바깥 래퍼 — 풀폭. 안쪽 카드가 콘텐츠 폭에 맞춰 정렬된다.</summary>
            public const string Layer = "w-full px-4 md:px-8 pt-3 md:pt-4";

            /// <summary>톤별 배경·보더. 정보는 기존 토큰 재사용, 주의·오류만 신설 토큰.</summary>
            public static string Card(BannerSeverity severity)
            {
                const string Base = "max-w-[1100px] mx-auto flex items-start gap-3 rounded-[11px] border-1.5 px-3.5 md:px-4 py-2.5 md:py-3 ";
                return severity switch
                {
                    BannerSeverity.Warning => Base + "bg-banner-warn border-orange/35",
                    BannerSeverity.Error => Base + "bg-banner-error border-banner-error",
                    _ => Base + "bg-surface-soft border-border",
                };
            }

            /// <summary>상태 원 20px — 톤별 색.</summary>
            public static string Dot(BannerSeverity severity)
            {
                const string Base = "w-5 h-5 shrink-0 mt-px rounded-full flex items-center justify-center text-[12px] font-extrabold text-white ";
                return severity switch
                {
                    BannerSeverity.Warning => Base + "bg-orange",
                    BannerSeverity.Error => Base + "bg-danger",
                    _ => Base + "bg-text-muted",
                };
            }

            public const string Body = "flex-1 min-w-0 text-[12.5px] leading-relaxed text-text-strong break-keep";
            public const string Prefix = "font-extrabold";

            /// <summary>해결/자세히 링크 — 톤별 강조색(본문에 이어 붙는 단일 링크).</summary>
            public static string Link(BannerSeverity severity)
            {
                const string Base = "ml-1 font-bold whitespace-nowrap underline-offset-2 hover:underline ";
                return severity switch
                {
                    BannerSeverity.Warning => Base + "text-orange-ink",
                    BannerSeverity.Error => Base + "text-danger",
                    _ => Base + "text-navy",
                };
            }

            /// <summary>정보 배너 전용 닫기 — 주의·오류에는 렌더하지 않는다.</summary>
            public const string Close = "shrink-0 mt-px w-5 h-5 flex items-center justify-center text-text-muted hover:text-text-strong bg-transparent border-0 cursor-pointer";
        }
    }
}
