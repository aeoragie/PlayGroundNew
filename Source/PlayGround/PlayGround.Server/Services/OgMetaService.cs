using PlayGround.Application.Interfaces;
using PlayGround.Application.Og;
using PlayGround.Shared.Result;
using System.Globalization;

namespace PlayGround.Server.Services
{
    /// <summary>OG 카드 한 장의 조립 결과 — 제목·설명·이미지 경로·정규 경로(전부 상대, 절대화는 미들웨어).</summary>
    public sealed record OgCard(string Title, string Description, string ImagePath, string CanonicalPath);

    /// <summary>
    /// 링크 공유 미리보기(OG) 카드 조립 (DECISION.OGMETA — 아키텍처 B). 화이트리스트 4종(랜딩·팀·선수·대회)만
    /// 라우트별 카드, 그 외는 랜딩 카드 폴백. 비공개·미존재도 폴백(존재 확인 불가). 선수는 **이름 + 고정 문구 +
    /// 공통 브랜드 이미지**만(사진·소속·기록 미포함 — 통제 밖 유통 차단).
    /// </summary>
    public class OgMetaService
    {
        public const string SiteName = "PlayGround Soccer";
        private const string Tagline = "유소년 축구 팀·선수·기록을 한곳에서";
        private const string BrandImage = "/og/brand.png";

        private readonly IOgMetaRepository mRepository;

        public OgMetaService(IOgMetaRepository repository)
        {
            mRepository = repository ?? throw new ArgumentNullException(nameof(repository));
        }

        public async Task<OgCard> BuildAsync(string path, CancellationToken cancellation = default)
        {
            string[] seg = (path ?? "/").Trim('/').Split('/', StringSplitOptions.RemoveEmptyEntries);
            if (seg.Length == 0)
            {
                return Landing("/");
            }

            return seg[0].ToLowerInvariant() switch
            {
                "team" when seg.Length >= 2 => await TeamAsync(seg[1], path!, cancellation),
                "player" when seg.Length >= 2 => await PlayerAsync(seg[1], path!, cancellation),
                "records" when seg.Length >= 2 && Guid.TryParse(seg[1], out Guid id) => await TournamentAsync(id, path!, cancellation),
                _ => Landing(path!),
            };
        }

        private static OgCard Landing(string canonical) => new(SiteName, Tagline, BrandImage, canonical);

        private async Task<OgCard> TeamAsync(string slug, string path, CancellationToken cancellation)
        {
            Result<TeamOgCard?> result = await mRepository.GetTeamOgAsync(slug, cancellation);
            if (result.IsError || result.Value is null)
            {
                return Landing(path); // 비공개·미존재 팀 → 랜딩 폴백
            }

            TeamOgCard t = result.Value;
            string description = Join(t.Region, t.AgeGroup, t.PlayerCount > 0 ? $"선수 {t.PlayerCount}명" : null);
            return new OgCard(t.TeamName, description, $"/og/team/{slug}.png", path);
        }

        private async Task<OgCard> PlayerAsync(string slug, string path, CancellationToken cancellation)
        {
            Result<string?> result = await mRepository.GetPlayerNameOgAsync(slug, cancellation);
            if (result.IsError || string.IsNullOrEmpty(result.Value))
            {
                return Landing(path); // 비공개·검색 노출 끔·미존재 → 태그 미생성(랜딩 폴백)
            }

            // 이름 + 고정 문구 + 공통 브랜드 이미지 (선수별 이미지 생성 안 함)
            return new OgCard(result.Value!, "PlayGround Soccer 선수 프로필", BrandImage, path);
        }

        private async Task<OgCard> TournamentAsync(Guid tournamentId, string path, CancellationToken cancellation)
        {
            Result<TournamentOgCard?> result = await mRepository.GetTournamentOgAsync(tournamentId, cancellation);
            if (result.IsError || result.Value is null)
            {
                return Landing(path);
            }

            TournamentOgCard d = result.Value;
            string description = Join(d.AgeGroup, PeriodText(d.StartDate, d.EndDate), d.TeamCount > 0 ? $"{d.TeamCount}팀" : null);
            return new OgCard(d.Name, description, $"/og/tournament/{tournamentId}.png", path);
        }

        private static string PeriodText(DateOnly? start, DateOnly? end)
        {
            static string M(DateOnly d) => d.ToString("yyyy.M", CultureInfo.InvariantCulture);
            if (start is not null && end is not null)
            {
                return $"{M(start.Value)} ~ {M(end.Value)}";
            }

            return start is not null ? M(start.Value) : (end is not null ? M(end.Value) : string.Empty);
        }

        private static string Join(params string?[] parts) =>
            string.Join(" · ", parts.Where(p => !string.IsNullOrEmpty(p)));
    }
}
