using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PlayGround.Application.Interfaces;
using PlayGround.Contracts.Common;
using PlayGround.Domain.Account;
using PlayGround.Shared.Http;
using PlayGround.Shared.Result;

namespace PlayGround.Server.Controllers.Soccer
{
    /// <summary>
    /// 첨부 문서 업로드 (게시판 pdf·hwp·이미지 등). 이미지 업로드(SoccerImageController)와 달리
    /// 리사이즈·크롭이 없고 원본 확장자를 보존한다. 형식·용량은 서버가 다시 검사한다(우회 요청 대비).
    /// </summary>
    [ApiController]
    [Route("api/soccer/files")]
    [Authorize]
    public class SoccerFileController : ControllerBase
    {
        private const long MaxBytes = 10 * 1024 * 1024;

        private static readonly HashSet<string> AllowedContentTypes = new(StringComparer.OrdinalIgnoreCase)
        {
            "application/pdf",
            "image/jpeg", "image/png", "image/webp",
            // 한글(HWP)은 브라우저별 MIME이 제각각이라(빈 문자열·octet-stream 포함) 확장자로 보강 판정한다.
            "application/x-hwp", "application/haansofthwp", "application/vnd.hancom.hwp",
            "application/octet-stream", "",
        };

        private static readonly HashSet<string> AllowedExtensions = new(StringComparer.OrdinalIgnoreCase)
        {
            ".pdf", ".hwp", ".hwpx", ".jpg", ".jpeg", ".png", ".webp",
        };

        /// <summary>업로드를 허용하는 용도 — 임의 경로 생성을 막는다.</summary>
        private static readonly HashSet<string> AllowedCategories = new(StringComparer.OrdinalIgnoreCase)
        {
            "team-board",
        };

        private readonly IFileStorage mStorage;

        public SoccerFileController(IFileStorage storage)
        {
            mStorage = storage;
        }

        [HttpPost("{category}")]
        [RequestSizeLimit(MaxBytes + (512 * 1024))]
        public async Task<Envelope<UploadedFileResponse>> UploadAsync(
            string category, IFormFile file, CancellationToken cancellation)
        {
            if (!AllowedCategories.Contains(category))
            {
                return Result<UploadedFileResponse>.Error(ErrorCode.InvalidInput, "unknown category").ToEnvelope();
            }

            if (file is null || file.Length == 0)
            {
                return Result<UploadedFileResponse>.Error(ErrorCode.InvalidInput, "file is empty").ToEnvelope();
            }

            if (file.Length > MaxBytes)
            {
                return Result<UploadedFileResponse>.Error(ErrorCode.InvalidInput, "file is too large").ToEnvelope();
            }

            string extension = Path.GetExtension(file.FileName ?? string.Empty);
            // 형식은 확장자를 1차 기준으로 — HWP MIME이 불안정해서 콘텐츠 타입만으로는 못 막는다.
            if (!AllowedExtensions.Contains(extension) || !AllowedContentTypes.Contains(file.ContentType ?? string.Empty))
            {
                return Result<UploadedFileResponse>.Error(ErrorCode.InvalidInput, "unsupported file type").ToEnvelope();
            }

            await using Stream content = file.OpenReadStream();
            string url = await mStorage.SaveAsync(category.ToLowerInvariant(), content, file.FileName ?? "file", cancellation);

            return Result<UploadedFileResponse>.Success(new UploadedFileResponse
            {
                Url = url,
                FileName = Path.GetFileName(file.FileName ?? "file"),
                SizeBytes = file.Length
            }).ToEnvelope();
        }
    }
}
