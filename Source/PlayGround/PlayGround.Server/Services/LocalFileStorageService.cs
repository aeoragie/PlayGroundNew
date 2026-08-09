using PlayGround.Application.Interfaces;
using System.Diagnostics;

namespace PlayGround.Server.Services
{
    /// <summary>
    /// 로컬 디스크 첨부 저장 (wwwroot/uploads/{category}/{yyyyMM}/{guid}{ext}).
    /// LocalImageStorageService와 같은 구조지만 **원본 확장자를 보존**한다(문서는 리사이즈·크롭이 없다).
    /// 키/URL 형태·허용 확장자는 UploadPaths가 단일 소스 — 원격 어댑터(RemoteFileStorageService)와 동일하다.
    /// </summary>
    public sealed class LocalFileStorageService : IFileStorage
    {
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

            string key = UploadPaths.NewKey(category, UploadPaths.SafeDocumentExtension(originalFileName));

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
            mLogger.LogInformation("File stored. {{ Category:{Category}, Url:{Url} }}", category, url);
            return url;
        }
    }
}
