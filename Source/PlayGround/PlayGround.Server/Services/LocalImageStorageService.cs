using System.Diagnostics;
using PlayGround.Application.Interfaces;

namespace PlayGround.Server.Services
{
    /// <summary>
    /// 로컬 디스크 이미지 저장 (wwwroot/uploads/{category}/{yyyyMM}/{guid}.{ext}).
    /// UseStaticFiles가 그대로 서빙하므로 별도 라우팅이 필요 없다.
    /// 오브젝트 스토리지로 옮길 때는 이 어댑터만 갈아끼운다 — 호출부는 IImageStorage만 안다.
    /// </summary>
    public sealed class LocalImageStorageService : IImageStorage
    {
        /// <summary>정적 서빙 루트 기준 상대 경로 — 공개 URL의 접두사이기도 하다.</summary>
        private const string UploadRoot = "uploads";

        private readonly IWebHostEnvironment mEnvironment;
        private readonly ILogger<LocalImageStorageService> mLogger;

        public LocalImageStorageService(IWebHostEnvironment environment, ILogger<LocalImageStorageService> logger)
        {
            Debug.Assert(environment != null, "environment is required");
            Debug.Assert(logger != null, "logger is required");
            mEnvironment = environment ?? throw new ArgumentNullException(nameof(environment));
            mLogger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<string> SaveAsync(
            string category, Stream content, string contentType, CancellationToken cancellation = default)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(category);
            ArgumentNullException.ThrowIfNull(content);

            string extension = ExtensionOf(contentType);
            string month = DateTime.UtcNow.ToString("yyyyMM");
            string fileName = $"{Guid.NewGuid():N}{extension}";

            // WebRootPath는 게시 형태에 따라 비어 있을 수 있다 — ContentRoot 기준으로 보정
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
            mLogger.LogInformation("Image stored. {{ Category:{Category}, Url:{Url} }}", category, url);
            return url;
        }

        private static string ExtensionOf(string contentType) => contentType switch
        {
            "image/png" => ".png",
            "image/webp" => ".webp",
            _ => ".jpg",
        };
    }
}
