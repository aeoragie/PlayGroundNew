using PlayGround.Application.Og;
using SkiaSharp;

namespace PlayGround.Server.Services
{
    /// <summary>OG 카드 서버 렌더 PNG 1200×630 (DECISION.OGMETA §3). 팀·대회는 동적, 랜딩·선수·폴백은 공통 브랜드 카드.
    /// SkiaSharp — Win/macOS 네이티브 기본 포함. 한글 폰트는 시스템에서 매칭(맑은 고딕 등), 실패 시 기본 폰트.</summary>
    public class OgImageRenderer
    {
        private const int Width = 1200;
        private const int Height = 630;
        private static readonly SKColor NavyDeep = SKColor.Parse("#1c2b4a");
        private static readonly SKColor Navy = SKColor.Parse("#23408e");
        private static readonly SKColor Teal = SKColor.Parse("#2EC4B6");

        // 한글 렌더 가능 서체를 한 번만 찾아 캐시 (시스템 설치 폰트 — 맑은 고딕/Noto CJK 등)
        private static readonly SKTypeface KoreanTypeface =
            SKFontManager.Default.MatchCharacter('가') ?? SKTypeface.Default;

        public byte[] RenderBrand()
        {
            using SKSurface surface = CreateSurface(out SKCanvas canvas);
            DrawBackground(canvas);
            DrawCentered(canvas, "PlayGround", 300, 108, SKColors.White, bold: true);
            DrawCentered(canvas, "Soccer", 380, 64, Teal, bold: true);
            DrawCentered(canvas, "유소년 축구 팀·선수·기록을 한곳에서", 448, 34, new SKColor(0xC6, 0xCE, 0xDE));
            return Encode(surface);
        }

        /// <param name="logoBytes">엠블럼 원본 바이트 — 저장 백엔드(디스크/오브젝트 스토리지)를 렌더러가 모르도록
        /// 호출자(OgImageController)가 IUploadReader로 읽어 넘긴다. null이면 이니셜 실드.</param>
        public byte[] RenderTeam(TeamOgCard team, byte[]? logoBytes = null)
        {
            using SKSurface surface = CreateSurface(out SKCanvas canvas);
            DrawBackground(canvas);

            var emblem = new SKRect((Width - 200) / 2f, 120, (Width + 200) / 2f, 320);
            DrawEmblem(canvas, emblem, logoBytes, team.TeamName);

            DrawCentered(canvas, Truncate(team.TeamName, 18), 410, FitSize(team.TeamName, 72, 18), SKColors.White, bold: true);
            DrawWordmark(canvas);
            return Encode(surface);
        }

        public byte[] RenderTournament(TournamentOgCard tournament)
        {
            using SKSurface surface = CreateSurface(out SKCanvas canvas);
            DrawBackground(canvas);
            DrawCentered(canvas, Truncate(tournament.Name, 22), 320, FitSize(tournament.Name, 74, 20), SKColors.White, bold: true);
            if (!string.IsNullOrEmpty(tournament.AgeGroup))
            {
                DrawCentered(canvas, tournament.AgeGroup!, 390, 38, Teal);
            }

            DrawWordmark(canvas);
            return Encode(surface);
        }

        //.// 렌더 헬퍼

        private static SKSurface CreateSurface(out SKCanvas canvas)
        {
            SKSurface surface = SKSurface.Create(new SKImageInfo(Width, Height));
            canvas = surface.Canvas;
            return surface;
        }

        private static void DrawBackground(SKCanvas canvas)
        {
            using var paint = new SKPaint
            {
                Shader = SKShader.CreateLinearGradient(
                    new SKPoint(0, 0), new SKPoint(Width, Height),
                    new[] { NavyDeep, Navy }, null, SKShaderTileMode.Clamp),
                IsAntialias = true
            };
            canvas.DrawRect(0, 0, Width, Height, paint);
        }

        private static void DrawWordmark(SKCanvas canvas)
        {
            using var paint = new SKPaint
            {
                Color = new SKColor(0xC6, 0xCE, 0xDE),
                IsAntialias = true,
                TextSize = 30,
                TextAlign = SKTextAlign.Center,
                Typeface = SKTypeface.FromFamilyName("Plus Jakarta Sans", SKFontStyle.Bold) ?? KoreanTypeface
            };
            canvas.DrawText("PlayGround Soccer", Width / 2f, Height - 56, paint);
        }

        private static void DrawCentered(SKCanvas canvas, string text, float baselineY, float size, SKColor color, bool bold = false)
        {
            using var paint = new SKPaint
            {
                Color = color,
                IsAntialias = true,
                TextSize = size,
                TextAlign = SKTextAlign.Center,
                Typeface = bold
                    ? (SKTypeface.FromFamilyName(KoreanTypeface.FamilyName, SKFontStyle.Bold) ?? KoreanTypeface)
                    : KoreanTypeface
            };
            canvas.DrawText(text, Width / 2f, baselineY, paint);
        }

        // 엠블럼 — 이미지 있으면 라운드 사각형에 커버, 없으면 이니셜 실드
        private void DrawEmblem(SKCanvas canvas, SKRect box, byte[]? logoBytes, string teamName)
        {
            using var clip = new SKPath();
            clip.AddRoundRect(new SKRoundRect(box, 26, 26));

            SKBitmap? bitmap = TryDecodeLogo(logoBytes);
            if (bitmap is not null)
            {
                using (bitmap)
                {
                    canvas.Save();
                    canvas.ClipPath(clip, antialias: true);
                    float scale = Math.Max(box.Width / bitmap.Width, box.Height / bitmap.Height);
                    float w = bitmap.Width * scale, h = bitmap.Height * scale;
                    var dest = new SKRect(box.MidX - w / 2, box.MidY - h / 2, box.MidX + w / 2, box.MidY + h / 2);
                    canvas.DrawBitmap(bitmap, dest);
                    canvas.Restore();
                }
                return;
            }

            using var fill = new SKPaint { Color = new SKColor(0x2E, 0x44, 0x7E), IsAntialias = true };
            canvas.DrawRoundRect(new SKRoundRect(box, 26, 26), fill);
            string initial = string.IsNullOrEmpty(teamName) ? "P" : teamName[..1];
            DrawCentered(canvas, initial, box.MidY + 34, 96, SKColors.White, bold: true);
        }

        private static SKBitmap? TryDecodeLogo(byte[]? logoBytes)
        {
            if (logoBytes is null || logoBytes.Length == 0)
            {
                return null;
            }

            try
            {
                return SKBitmap.Decode(logoBytes);
            }
            catch
            {
                return null; // 디코드 실패 → 이니셜 실드 폴백
            }
        }

        private static byte[] Encode(SKSurface surface)
        {
            using SKImage image = surface.Snapshot();
            using SKData data = image.Encode(SKEncodedImageFormat.Png, 90);
            return data.ToArray();
        }

        // 글자 수에 따라 폰트 크기를 줄여 한 줄에 맞춘다(대략) — 넘치면 Truncate가 함께 자른다
        private static float FitSize(string text, float baseSize, int comfortableLength)
        {
            int length = text?.Length ?? 0;
            if (length <= comfortableLength)
            {
                return baseSize;
            }

            float scaled = baseSize * comfortableLength / length;
            return Math.Max(scaled, baseSize * 0.55f);
        }

        private static string Truncate(string text, int max)
        {
            if (string.IsNullOrEmpty(text) || text.Length <= max)
            {
                return text ?? string.Empty;
            }

            return text[..(max - 1)] + "…";
        }
    }
}
