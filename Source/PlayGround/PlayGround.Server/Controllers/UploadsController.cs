using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PlayGround.Server.Services;

namespace PlayGround.Server.Controllers
{
    /// <summary>
    /// 업로드 원본 서빙 — Remote 모드에서 "/uploads/..." URL을 프라이빗 버킷에서 스트리밍한다.
    /// 로컬 모드에서는 정적 파일 미들웨어가 먼저 서빙하므로 여기까지 오면 404가 정상이다.
    /// 파일명이 GUID라 내용 변경 = URL 변경 — immutable 캐시를 걸 수 있다.
    /// </summary>
    [ApiController]
    [AllowAnonymous]
    public class UploadsController : ControllerBase
    {
        private readonly IUploadReader mReader;

        public UploadsController(IUploadReader reader)
        {
            mReader = reader ?? throw new ArgumentNullException(nameof(reader));
        }

        [HttpGet("/uploads/{**path}")]
        public async Task<IActionResult> GetAsync(string path, CancellationToken cancellation)
        {
            UploadContent? content = await mReader.OpenAsync(UploadPaths.UrlPrefix + path, cancellation);
            if (content is null)
            {
                return NotFound();
            }

            Response.Headers.CacheControl = "public, max-age=31536000, immutable";
            return File(content.Stream, content.ContentType);
        }
    }
}
