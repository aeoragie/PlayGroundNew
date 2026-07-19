using System.Diagnostics;

namespace PlayGround.Domain.Soccer
{
    /// <summary>
    /// 유튜브 링크 해석 — 포트폴리오 영상은 업로드가 아니라 링크 등록이다.
    /// 클라이언트(붙여넣기 시 썸네일 미리보기)와 서버(저장 전 검증·정규화)가 같은 규칙을 써야
    /// 미리보기와 저장 결과가 어긋나지 않으므로 Domain에 둔다.
    /// </summary>
    public static class YouTubeVideoLink
    {
        /// <summary>유튜브 영상 ID 길이 — 11자 고정.</summary>
        private const int VideoIdLength = 11;

        private static readonly string[] AllowedHosts =
        {
            "youtube.com", "www.youtube.com", "m.youtube.com", "youtu.be", "www.youtu.be",
        };

        /// <summary>링크에서 영상 ID를 뽑는다. 유튜브가 아니거나 형태가 다르면 null.</summary>
        public static string? ParseVideoId(string? url)
        {
            if (string.IsNullOrWhiteSpace(url))
            {
                return null;
            }

            string trimmed = url.Trim();

            // 스킴 없이 붙여넣는 경우가 많다 (youtu.be/xxxx)
            if (!trimmed.Contains("://", StringComparison.Ordinal))
            {
                trimmed = "https://" + trimmed;
            }

            if (!Uri.TryCreate(trimmed, UriKind.Absolute, out Uri? uri))
            {
                return null;
            }

            if (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps)
            {
                return null;
            }

            if (!AllowedHosts.Contains(uri.Host, StringComparer.OrdinalIgnoreCase))
            {
                return null;
            }

            string[] segments = uri.AbsolutePath
                .Split('/', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

            // youtu.be/{id}
            if (uri.Host.EndsWith("youtu.be", StringComparison.OrdinalIgnoreCase))
            {
                return segments.Length > 0 ? Validate(segments[0]) : null;
            }

            // youtube.com/watch?v={id}
            if (segments.Length > 0 && segments[0].Equals("watch", StringComparison.OrdinalIgnoreCase))
            {
                return Validate(ReadQueryValue(uri.Query, "v"));
            }

            // youtube.com/shorts/{id} · /embed/{id} · /live/{id}
            if (segments.Length > 1
                && (segments[0].Equals("shorts", StringComparison.OrdinalIgnoreCase)
                    || segments[0].Equals("embed", StringComparison.OrdinalIgnoreCase)
                    || segments[0].Equals("live", StringComparison.OrdinalIgnoreCase)))
            {
                return Validate(segments[1]);
            }

            return null;
        }

        /// <summary>링크가 유튜브 영상으로 해석되는지.</summary>
        public static bool IsValid(string? url) => ParseVideoId(url) is not null;

        /// <summary>저장용 표준 링크 — 입력 형태가 무엇이든 watch 형식 하나로 모은다.</summary>
        public static string? ToCanonicalUrl(string? url)
        {
            string? videoId = ParseVideoId(url);
            return videoId is null ? null : $"https://www.youtube.com/watch?v={videoId}";
        }

        /// <summary>썸네일 주소 — 링크에서 파생한다(임의 이미지 주소를 저장하지 않기 위해).</summary>
        public static string? ToThumbnailUrl(string? url)
        {
            string? videoId = ParseVideoId(url);
            return videoId is null ? null : $"https://img.youtube.com/vi/{videoId}/hqdefault.jpg";
        }

        private static string? ReadQueryValue(string query, string key)
        {
            Debug.Assert(key != null, "key is required");

            foreach (string pair in query.TrimStart('?').Split('&', StringSplitOptions.RemoveEmptyEntries))
            {
                int separator = pair.IndexOf('=');
                if (separator <= 0)
                {
                    continue;
                }

                if (pair[..separator].Equals(key, StringComparison.OrdinalIgnoreCase))
                {
                    return Uri.UnescapeDataString(pair[(separator + 1)..]);
                }
            }

            return null;
        }

        // ID는 11자 + URL 안전 문자만 — 경로 조작이 섞여 들어오지 않게 한다
        private static string? Validate(string? videoId)
        {
            if (videoId is null || videoId.Length != VideoIdLength)
            {
                return null;
            }

            foreach (char c in videoId)
            {
                if (!char.IsAsciiLetterOrDigit(c) && c != '_' && c != '-')
                {
                    return null;
                }
            }

            return videoId;
        }
    }
}
