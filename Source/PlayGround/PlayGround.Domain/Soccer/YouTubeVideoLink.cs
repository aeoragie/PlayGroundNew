using System.Diagnostics;

namespace PlayGround.Domain.Soccer
{
    public static class YouTubeVideoLink
    {
        private const int VideoIdLength = 11;

        private static readonly string[] AllowedHosts =
        {
            "youtube.com", "www.youtube.com", "m.youtube.com", "youtu.be", "www.youtu.be",
        };

        public static string? ParseVideoId(string? url)
        {
            if (string.IsNullOrWhiteSpace(url))
            {
                return null;
            }

            string trimmed = url.Trim();

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

            if (uri.Host.EndsWith("youtu.be", StringComparison.OrdinalIgnoreCase))
            {
                return segments.Length > 0 ? Validate(segments[0]) : null;
            }

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

        public static bool IsValid(string? url) => ParseVideoId(url) is not null;

        public static string? ToCanonicalUrl(string? url)
        {
            string? videoId = ParseVideoId(url);
            return videoId is null ? null : $"https://www.youtube.com/watch?v={videoId}";
        }

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
