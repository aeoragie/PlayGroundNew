using PlayGround.Server.Services;
using System.Net;
using System.Text;

namespace PlayGround.Server.Middleware
{
    /// <summary>
    /// 링크 공유 미리보기(OG) — 크롤러 감지 미들웨어 (DECISION.OGMETA 아키텍처 B).
    /// User-Agent가 크롤러(카톡·페북·X·슬랙 등)면 **메타 태그만 있는 최소 HTML**을 반환하고, 사람은 그대로 SPA로 통과.
    /// 자산(.확장자)·API·OG 이미지 경로는 건드리지 않는다 → 크롤러의 og:image(.png) 요청은 컨트롤러가 처리.
    /// </summary>
    public class OgMetaMiddleware
    {
        private static readonly string[] CrawlerAgents =
        {
            "facebookexternalhit", "facebot", "twitterbot", "slackbot", "telegrambot", "discordbot",
            "whatsapp", "linkedinbot", "kakaotalk", "kakaostory", "skypeuripreview", "redditbot",
            "embedly", "pinterest", "applebot", "googlebot", "bingbot", "yeti", "daum", "naver",
        };

        private readonly RequestDelegate mNext;

        public OgMetaMiddleware(RequestDelegate next)
        {
            mNext = next ?? throw new ArgumentNullException(nameof(next));
        }

        public async Task InvokeAsync(HttpContext context)
        {
            if (!HttpMethods.IsGet(context.Request.Method)
                || !IsCrawler(context.Request.Headers.UserAgent.ToString())
                || IsAssetOrApi(context.Request.Path))
            {
                await mNext(context);
                return;
            }

            var service = context.RequestServices.GetRequiredService<OgMetaService>();
            var configuration = context.RequestServices.GetRequiredService<IConfiguration>();

            OgCard card = await service.BuildAsync(context.Request.Path.Value ?? "/", context.RequestAborted);
            string origin = ResolveOrigin(context, configuration);
            string html = RenderHtml(card, origin);

            context.Response.StatusCode = StatusCodes.Status200OK;
            context.Response.ContentType = "text/html; charset=utf-8";
            await context.Response.WriteAsync(html, context.RequestAborted);
        }

        private static bool IsCrawler(string userAgent)
        {
            if (string.IsNullOrEmpty(userAgent))
            {
                return false;
            }

            foreach (string agent in CrawlerAgents)
            {
                if (userAgent.Contains(agent, StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }

            return false;
        }

        // 자산(확장자 있음)·API·OG 이미지 경로는 통과시켜 정상 서빙 (크롤러의 .png 요청 = og:image)
        private static bool IsAssetOrApi(PathString path)
        {
            if (path.StartsWithSegments("/api"))
            {
                return true;
            }

            string? value = path.Value;
            return !string.IsNullOrEmpty(value) && Path.HasExtension(value);
        }

        // 절대 URL 기준 origin — 설정(Og:BaseUrl) 우선, 없으면 요청 scheme+host (프록시 뒤면 X-Forwarded-Proto 반영)
        private static string ResolveOrigin(HttpContext context, IConfiguration configuration)
        {
            string? configured = configuration["Og:BaseUrl"];
            if (!string.IsNullOrWhiteSpace(configured))
            {
                return configured.TrimEnd('/');
            }

            string scheme = context.Request.Headers.TryGetValue("X-Forwarded-Proto", out var proto) && proto.Count > 0
                ? proto[0]!
                : context.Request.Scheme;
            return $"{scheme}://{context.Request.Host}";
        }

        private static string RenderHtml(OgCard card, string origin)
        {
            string title = WebUtility.HtmlEncode(card.Title);
            string description = WebUtility.HtmlEncode(card.Description);
            string image = origin + card.ImagePath;
            string url = origin + card.CanonicalPath;

            var sb = new StringBuilder(1024);
            sb.Append("<!DOCTYPE html><html lang=\"ko\"><head><meta charset=\"utf-8\">");
            sb.Append("<title>").Append(title).Append("</title>");
            Meta(sb, "og:title", title);
            Meta(sb, "og:description", description);
            Meta(sb, "og:image", image);
            Meta(sb, "og:url", url);
            Meta(sb, "og:type", "website");
            Meta(sb, "og:site_name", OgMetaService.SiteName);
            Meta(sb, "og:locale", "ko_KR");
            // 최소 세트 그대로 (DECISION.OGMETA §4) — twitter는 card 한 줄만
            sb.Append("<meta name=\"twitter:card\" content=\"summary_large_image\">");
            sb.Append("</head><body></body></html>");
            return sb.ToString();
        }

        private static void Meta(StringBuilder sb, string property, string content)
        {
            sb.Append("<meta property=\"").Append(property).Append("\" content=\"").Append(content).Append("\">");
        }
    }
}
