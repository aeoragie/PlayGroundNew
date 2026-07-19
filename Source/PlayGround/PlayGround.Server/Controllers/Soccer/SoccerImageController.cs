using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PlayGround.Shared.Http;
using PlayGround.Shared.Result;
using PlayGround.Application.Interfaces;
using PlayGround.Contracts.Common;

namespace PlayGround.Server.Controllers.Soccer
{
    /// <summary>
    /// 이미지 업로드. 클라이언트가 리사이즈·EXIF 보정을 끝낸 파일만 올라오지만,
    /// 서버는 그걸 신뢰하지 않고 형식·용량을 다시 검사한다(우회 요청 대비).
    /// </summary>
    [ApiController]
    [Route("api/soccer/images")]
    [Authorize]
    public class SoccerImageController : ControllerBase
    {
        /// <summary>10MB — 클라이언트 리사이즈를 거치면 보통 1MB 안쪽이다.</summary>
        private const long MaxBytes = 10 * 1024 * 1024;

        private static readonly HashSet<string> AllowedContentTypes = new(StringComparer.OrdinalIgnoreCase)
        {
            "image/jpeg", "image/png", "image/webp",
        };

        /// <summary>업로드를 허용하는 용도 — 임의 경로 생성을 막는다.</summary>
        private static readonly HashSet<string> AllowedCategories = new(StringComparer.OrdinalIgnoreCase)
        {
            "team-logo", "team-cover", "player-photo",
        };

        private readonly IImageStorage mStorage;

        public SoccerImageController(IImageStorage storage)
        {
            mStorage = storage;
        }

        [HttpPost("{category}")]
        [RequestSizeLimit(MaxBytes + (512 * 1024))]
        public async Task<Envelope<UploadedImageResponse>> UploadAsync(
            string category, IFormFile file, CancellationToken cancellation)
        {
            if (!AllowedCategories.Contains(category))
            {
                return Result<UploadedImageResponse>.Error(ErrorCode.InvalidInput, "unknown category").ToEnvelope();
            }

            if (file is null || file.Length == 0)
            {
                return Result<UploadedImageResponse>.Error(ErrorCode.InvalidInput, "file is empty").ToEnvelope();
            }

            if (file.Length > MaxBytes)
            {
                return Result<UploadedImageResponse>.Error(ErrorCode.InvalidInput, "file is too large").ToEnvelope();
            }

            if (!AllowedContentTypes.Contains(file.ContentType))
            {
                return Result<UploadedImageResponse>.Error(ErrorCode.InvalidInput, "unsupported image type").ToEnvelope();
            }

            await using Stream content = file.OpenReadStream();
            string url = await mStorage.SaveAsync(category.ToLowerInvariant(), content, file.ContentType, cancellation);

            return Result<UploadedImageResponse>.Success(new UploadedImageResponse { Url = url }).ToEnvelope();
        }
    }
}
