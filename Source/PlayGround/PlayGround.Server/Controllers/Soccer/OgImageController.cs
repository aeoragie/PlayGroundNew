using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using PlayGround.Shared.Result;
using PlayGround.Application.Og;
using PlayGround.Application.Interfaces;
using PlayGround.Server.Services;

namespace PlayGround.Server.Controllers.Soccer
{
    /// <summary>OG 카드 이미지 (DECISION.OGMETA §3) — 서버 렌더 PNG 1200×630 + 24시간 캐시.
    /// 팀·대회는 동적, 랜딩·선수·폴백은 공통 브랜드 카드. 미존재·비공개·렌더 실패는 브랜드 카드 폴백(빈 이미지 금지).</summary>
    [ApiController]
    [Route("og")]
    [AllowAnonymous]
    public class OgImageController : ControllerBase
    {
        private static readonly TimeSpan CacheTtl = TimeSpan.FromHours(24);

        private readonly IMemoryCache mCache;
        private readonly IOgMetaRepository mRepository;
        private readonly OgImageRenderer mRenderer;
        private readonly IUploadReader mUploadReader;

        public OgImageController(
            IMemoryCache cache, IOgMetaRepository repository, OgImageRenderer renderer, IUploadReader uploadReader)
        {
            mCache = cache;
            mRepository = repository;
            mRenderer = renderer;
            mUploadReader = uploadReader;
        }

        [HttpGet("brand.png")]
        public IActionResult Brand()
        {
            byte[] png = mCache.GetOrCreate("og:brand", entry =>
            {
                entry.AbsoluteExpirationRelativeToNow = CacheTtl;
                return mRenderer.RenderBrand();
            })!;
            return Png(png);
        }

        [HttpGet("team/{slug}.png")]
        public async Task<IActionResult> Team(string slug, CancellationToken cancellation)
        {
            string key = $"og:team:{slug}";
            if (mCache.TryGetValue(key, out byte[]? cached) && cached is not null)
            {
                return Png(cached);
            }

            Result<TeamOgCard?> result = await mRepository.GetTeamOgAsync(slug, cancellation);
            byte[] png;
            if (result.IsError || result.Value is null)
            {
                png = mRenderer.RenderBrand();          // 비공개·미존재 → 브랜드 폴백
            }
            else
            {
                // 엠블럼 원본은 저장 백엔드(디스크/S3)에서 읽어 렌더러에 바이트로 넘긴다 — 실패 시 이니셜 실드
                byte[]? logo = await TryReadLogoAsync(result.Value.LogoUrl, cancellation);
                png = mRenderer.RenderTeam(result.Value, logo);
            }

            mCache.Set(key, png, CacheTtl);
            return Png(png);
        }

        [HttpGet("tournament/{tournamentId:guid}.png")]
        public async Task<IActionResult> Tournament(Guid tournamentId, CancellationToken cancellation)
        {
            string key = $"og:tournament:{tournamentId}";
            if (mCache.TryGetValue(key, out byte[]? cached) && cached is not null)
            {
                return Png(cached);
            }

            Result<TournamentOgCard?> result = await mRepository.GetTournamentOgAsync(tournamentId, cancellation);
            byte[] png = result.IsError || result.Value is null
                ? mRenderer.RenderBrand()
                : mRenderer.RenderTournament(result.Value);
            mCache.Set(key, png, CacheTtl);
            return Png(png);
        }

        private async Task<byte[]?> TryReadLogoAsync(string? logoUrl, CancellationToken cancellation)
        {
            if (string.IsNullOrEmpty(logoUrl))
            {
                return null;
            }

            try
            {
                UploadContent? content = await mUploadReader.OpenAsync(logoUrl, cancellation);
                if (content is null)
                {
                    return null;
                }

                await using Stream stream = content.Stream;
                using var buffer = new MemoryStream();
                await stream.CopyToAsync(buffer, cancellation);
                return buffer.ToArray();
            }
            catch
            {
                return null; // 읽기 실패 → 이니셜 실드 폴백 (OG 카드는 빈 이미지 금지)
            }
        }

        private IActionResult Png(byte[] bytes)
        {
            Response.Headers.CacheControl = "public, max-age=86400";
            return File(bytes, "image/png");
        }
    }
}
