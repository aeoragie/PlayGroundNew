using System.Diagnostics;

namespace PlayGround.Server.Services
{
    /// <summary>
    /// 로컬 디스크 업로드 읽기 (wwwroot/uploads). 브라우저 서빙은 정적 파일 미들웨어가 먼저 하므로
    /// 실제 사용처는 OG 렌더의 엠블럼 로드다. 경로 탈출은 KeyFromUrl + 루트 확인 이중으로 막는다.
    /// </summary>
    public sealed class LocalUploadReader : IUploadReader
    {
        private readonly IWebHostEnvironment mEnvironment;

        public LocalUploadReader(IWebHostEnvironment environment)
        {
            Debug.Assert(environment != null, "environment is required");
            mEnvironment = environment ?? throw new ArgumentNullException(nameof(environment));
        }

        public Task<UploadContent?> OpenAsync(string relativeUrl, CancellationToken cancellation = default)
        {
            string? key = UploadPaths.KeyFromUrl(relativeUrl);
            if (key is null)
            {
                return Task.FromResult<UploadContent?>(null);
            }

            string webRoot = string.IsNullOrEmpty(mEnvironment.WebRootPath)
                ? Path.Combine(mEnvironment.ContentRootPath, "wwwroot")
                : mEnvironment.WebRootPath;

            string uploadsRoot = Path.GetFullPath(Path.Combine(webRoot, UploadPaths.Root));
            string fullPath = Path.GetFullPath(Path.Combine(webRoot, key.Replace('/', Path.DirectorySeparatorChar)));
            if (!fullPath.StartsWith(uploadsRoot, StringComparison.OrdinalIgnoreCase) || !File.Exists(fullPath))
            {
                return Task.FromResult<UploadContent?>(null);
            }

            FileStream stream = File.OpenRead(fullPath);
            return Task.FromResult<UploadContent?>(
                new UploadContent(stream, UploadPaths.ContentTypeOf(key), stream.Length));
        }
    }
}
