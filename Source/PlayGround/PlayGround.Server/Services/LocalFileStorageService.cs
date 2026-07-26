using System.Diagnostics;
using PlayGround.Application.Interfaces;

namespace PlayGround.Server.Services
{
    /// <summary>
    /// 로컬 디스크 첨부 저장 (wwwroot/uploads/{category}/{yyyyMM}/{guid}{ext}).
    /// LocalImageStorageService와 같은 구조지만 **원본 확장자를 보존**한다(문서는 리사이즈·크롭이 없다).
    /// 오브젝트 스토리지로 옮길 때는 이 어댑터만 갈아끼운다 — 호출부는 IFileStorage만 안다.
    /// </summary>
    public sealed class LocalFileStorageService : IFileStorage
    {
        private const string UploadRoot = "uploads";

        /// <summary>저장 허용 확장자 — 컨트롤러 화이트리스트와 별개의 최후 방어. 알 수 없으면 .bin.</summary>
        private static readonly HashSet<string> AllowedExtensions = new(StringComparer.OrdinalIgnoreCase)
        {
            ".pdf", ".hwp", ".hwpx", ".jpg", ".jpeg", ".png", ".webp",
        };

        private readonly IWebHostEnvironment mEnvironment;
        private readonly ILogger<LocalFileStorageService> mLogger;

        public LocalFileStorageService(IWebHostEnvironment environment, ILogger<LocalFileStorageService> logger)
        {
            Debug.Assert(environment != null, "environment is required");
            Debug.Assert(logger != null, "logger is required");
            mEnvironment = environment ?? throw new ArgumentNullException(nameof(environment));
            mLogger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<string> SaveAsync(
            string category, Stream content, string originalFileName, CancellationToken cancellation = default)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(category);
            ArgumentNullException.ThrowIfNull(content);

            string extension = Path.GetExtension(originalFileName ?? string.Empty);
            if (!AllowedExtensions.Contains(extension))
            {
                extension = ".bin";
            }

            string month = DateTime.UtcNow.ToString("yyyyMM");
            string fileName = $"{Guid.NewGuid():N}{extension.ToLowerInvariant()}";

            string webRoot = string.IsNullOrEmpty(mEnvironment.WebRootPath)
                ? Path.Combine(mEnvironment.ContentRootPath, "wwwroot")
                : mEnvironment.WebRootPath;

            string directory = Path.Combine(webRoot, UploadRoot, category, month);
            Directory.CreateDirectory(directory);

            string fullPath = Path.Combine(directory, fileName);
            await using (FileStream file = File.Create(fullPath))
            {
                await content.CopyToAsync(file, cancellation);
            }

            string url = $"/{UploadRoot}/{category}/{month}/{fileName}";
            mLogger.LogInformation("File stored. {{ Category:{Category}, Url:{Url} }}", category, url);
            return url;
        }
    }
}
