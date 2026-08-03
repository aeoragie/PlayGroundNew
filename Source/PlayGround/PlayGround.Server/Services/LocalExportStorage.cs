using System.Diagnostics;
using PlayGround.Shared.Time;
using PlayGround.Application.Interfaces;

namespace PlayGround.Server.Services
{
    /// <summary>데이터 내려받기 파일 저장 — **정적 서빙(wwwroot) 밖**의 비공개 경로(ContentRoot/App_Data/exports).
    /// UseStaticFiles로 URL 우회가 불가하고, 다운로드는 서명 URL 엔드포인트가 Ready·만료·횟수를 검증한다.
    /// 저장 키는 상대 경로(exports/{yyyyMM}/{requestId}.zip) — 오브젝트 스토리지로 옮기면 이 어댑터만 교체.</summary>
    public sealed class LocalExportStorage : IExportStorage
    {
        private const string RootFolder = "App_Data";
        private const string Category = "exports";

        private readonly IWebHostEnvironment mEnvironment;
        private readonly ILogger<LocalExportStorage> mLogger;

        public LocalExportStorage(IWebHostEnvironment environment, ILogger<LocalExportStorage> logger)
        {
            Debug.Assert(environment != null && logger != null);
            mEnvironment = environment ?? throw new ArgumentNullException(nameof(environment));
            mLogger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<string> SaveAsync(Guid requestId, Stream content, CancellationToken cancellation = default)
        {
            ArgumentNullException.ThrowIfNull(content);

            string month = SystemTime.Now.ToString("yyyyMM");
            string relativeKey = $"{Category}/{month}/{requestId:N}.zip";
            string fullPath = Path.Combine(mEnvironment.ContentRootPath, RootFolder, NormalizeToOs(relativeKey));

            Directory.CreateDirectory(Path.GetDirectoryName(fullPath)!);
            await using (FileStream file = File.Create(fullPath))
            {
                content.Position = 0;
                await content.CopyToAsync(file, cancellation);
            }

            mLogger.LogInformation("Export file stored. {{ Key:{Key} }}", relativeKey);
            return relativeKey;
        }

        public Task<Stream?> OpenReadAsync(string storageKey, CancellationToken cancellation = default)
        {
            if (string.IsNullOrWhiteSpace(storageKey))
            {
                return Task.FromResult<Stream?>(null);
            }

            string fullPath = Path.Combine(mEnvironment.ContentRootPath, RootFolder, NormalizeToOs(storageKey));
            if (!File.Exists(fullPath))
            {
                return Task.FromResult<Stream?>(null);
            }

            Stream stream = File.OpenRead(fullPath);
            return Task.FromResult<Stream?>(stream);
        }

        private static string NormalizeToOs(string relative) =>
            relative.Replace('/', Path.DirectorySeparatorChar);
    }
}
