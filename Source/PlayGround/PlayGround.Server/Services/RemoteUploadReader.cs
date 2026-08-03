using System.Diagnostics;

namespace PlayGround.Server.Services
{
    /// <summary>
    /// 오브젝트 저장소 업로드 읽기 — `/uploads` 프록시 서빙과 OG 렌더가 원본을 가져올 때 쓴다.
    ///
    /// 여기가 하는 일은 **URL → 키 해석**뿐이고(경로 탈출·외부 URL 차단 포함),
    /// 실제 가져오기는 <see cref="IObjectStore"/> 뒤에 있다.
    /// 미존재는 오류가 아니라 null — 지워진 이미지 URL이 남아 있는 정상 시나리오다.
    /// </summary>
    public sealed class RemoteUploadReader : IUploadReader
    {
        private readonly IObjectStore mStore;

        public RemoteUploadReader(IObjectStore store)
        {
            Debug.Assert(store != null, "store is required");
            mStore = store ?? throw new ArgumentNullException(nameof(store));
        }

        public async Task<UploadContent?> OpenAsync(string relativeUrl, CancellationToken cancellation = default)
        {
            string? key = UploadPaths.KeyFromUrl(relativeUrl);
            if (key is null)
            {
                return null;
            }

            return await mStore.OpenAsync(key, cancellation);
        }
    }
}
