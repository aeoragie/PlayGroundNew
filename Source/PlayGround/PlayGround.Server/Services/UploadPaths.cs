namespace PlayGround.Server.Services
{
    /// <summary>
    /// 업로드 경로 규칙의 단일 소스 — 키/URL 형태(uploads/{category}/{yyyyMM}/{guid}{ext})와
    /// 확장자 매핑을 로컬·원격 어댑터가 공유한다.
    /// URL 형태("/uploads/...")는 DB에 저장돼 있고 Application 검증(프로필 사진·게시판 첨부)의
    /// 화이트리스트이기도 하므로 바꾸지 않는다 — 백엔드가 바뀌어도 URL은 그대로다.
    /// </summary>
    internal static class UploadPaths
    {
        public const string Root = "uploads";
        public const string UrlPrefix = "/uploads/";

        /// <summary>문서 첨부 허용 확장자 — 컨트롤러 화이트리스트와 별개의 최후 방어. 밖이면 .bin.</summary>
        private static readonly HashSet<string> DocumentExtensions = new(StringComparer.OrdinalIgnoreCase)
        {
            ".pdf", ".hwp", ".hwpx", ".jpg", ".jpeg", ".png", ".webp",
        };

        public static string ImageExtensionOf(string contentType) => contentType switch
        {
            "image/png" => ".png",
            "image/webp" => ".webp",
            _ => ".jpg",
        };

        public static string SafeDocumentExtension(string? originalFileName)
        {
            string extension = Path.GetExtension(originalFileName ?? string.Empty);
            return DocumentExtensions.Contains(extension) ? extension.ToLowerInvariant() : ".bin";
        }

        /// <summary>새 저장 키 — uploads/{category}/{yyyyMM}/{guid}{ext}. 공개 URL은 "/" + 키.</summary>
        public static string NewKey(string category, string extension)
        {
            string month = DateTime.UtcNow.ToString("yyyyMM");
            return $"{Root}/{category}/{month}/{Guid.NewGuid():N}{extension}";
        }

        /// <summary>"/uploads/..." 상대 URL → 저장 키. 형태가 아니면(외부 URL·경로 탈출 시도) null.</summary>
        public static string? KeyFromUrl(string? relativeUrl)
        {
            if (string.IsNullOrEmpty(relativeUrl) || !relativeUrl.StartsWith(UrlPrefix, StringComparison.Ordinal))
            {
                return null;
            }

            string key = relativeUrl[1..];
            if (key.Contains("..", StringComparison.Ordinal) || key.Contains('\\'))
            {
                return null;
            }

            return key;
        }

        /// <summary>서빙 응답의 Content-Type — 저장 시 확장자를 우리가 정했으므로 역매핑이 안전하다.</summary>
        public static string ContentTypeOf(string key) => Path.GetExtension(key).ToLowerInvariant() switch
        {
            ".png" => "image/png",
            ".webp" => "image/webp",
            ".jpg" or ".jpeg" => "image/jpeg",
            ".pdf" => "application/pdf",
            _ => "application/octet-stream",
        };
    }
}
