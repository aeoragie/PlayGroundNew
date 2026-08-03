using System.Diagnostics;
using PlayGround.Application.Interfaces;

namespace PlayGround.Server.Services
{
    /// <summary>
    /// 로컬 디스크 이미지 저장 (wwwroot/uploads/{category}/{yyyyMM}/{guid}.{ext}).
    /// UseStaticFiles가 그대로 서빙하므로 별도 라우팅이 필요 없다.
    /// 키/URL 형태는 UploadPaths가 단일 소스 — 원격 어댑터(RemoteImageStorageService)와 동일하다.
    /// </summary>
    public sealed class LocalImageStorageService : IImageStorage
    {
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

            string key = UploadPaths.NewKey(category, UploadPaths.ImageExtensionOf(contentType));

            // WebRootPath는 게시 형태에 따라 비어 있을 수 있다 — ContentRoot 기준으로 보정
            string webRoot = string.IsNullOrEmpty(mEnvironment.WebRootPath)
                ? Path.Combine(mEnvironment.ContentRootPath, "wwwroot")
                : mEnvironment.WebRootPath;

            string fullPath = Path.Combine(webRoot, key.Replace('/', Path.DirectorySeparatorChar));
            Directory.CreateDirectory(Path.GetDirectoryName(fullPath)!);

            await using (FileStream file = File.Create(fullPath))
            {
                await content.CopyToAsync(file, cancellation);
            }

            string url = "/" + key;
            mLogger.LogInformation("Image stored. {{ Category:{Category}, Url:{Url} }}", category, url);
            return url;
        }
    }
}
