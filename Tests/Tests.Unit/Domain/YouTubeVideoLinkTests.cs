using FluentAssertions;
using Xunit;
using PlayGround.Domain.Soccer;

namespace PlayGround.Tests.Unit.Domain
{
    /// <summary>포트폴리오 영상 링크 해석. 클라이언트 미리보기와 서버 저장이 **같은 규칙**을 써야
    /// 미리보기와 저장 결과가 어긋나지 않으므로, 호스트 화이트리스트·ID 형태 검증을 고정한다.</summary>
    public class YouTubeVideoLinkTests
    {
        private const string Id = "dQw4w9WgXcQ"; // 11자 정상 ID

        //.// 허용 형태

        [Theory]
        [InlineData("https://www.youtube.com/watch?v=dQw4w9WgXcQ")]
        [InlineData("https://youtube.com/watch?v=dQw4w9WgXcQ")]
        [InlineData("https://m.youtube.com/watch?v=dQw4w9WgXcQ")]
        [InlineData("https://youtu.be/dQw4w9WgXcQ")]
        [InlineData("https://www.youtube.com/shorts/dQw4w9WgXcQ")]
        [InlineData("https://www.youtube.com/embed/dQw4w9WgXcQ")]
        [InlineData("https://www.youtube.com/live/dQw4w9WgXcQ")]
        [InlineData("http://youtu.be/dQw4w9WgXcQ")]
        public void ParseVideoId_지원_형태를_모두_해석한다(string url)
        {
            YouTubeVideoLink.ParseVideoId(url).Should().Be(Id);
        }

        [Fact]
        public void ParseVideoId_스킴_없이_붙여넣어도_해석한다()
        {
            // 사용자가 주소창에서 복사하면 스킴이 빠지는 일이 잦다
            YouTubeVideoLink.ParseVideoId("youtu.be/dQw4w9WgXcQ").Should().Be(Id);
            YouTubeVideoLink.ParseVideoId("www.youtube.com/watch?v=dQw4w9WgXcQ").Should().Be(Id);
        }

        [Fact]
        public void ParseVideoId_앞뒤_공백을_무시한다()
        {
            YouTubeVideoLink.ParseVideoId("  https://youtu.be/dQw4w9WgXcQ  ").Should().Be(Id);
        }

        [Fact]
        public void ParseVideoId_watch의_다른_쿼리가_섞여도_v를_찾는다()
        {
            YouTubeVideoLink.ParseVideoId("https://www.youtube.com/watch?list=PL123&v=dQw4w9WgXcQ&t=30s")
                .Should().Be(Id);
        }

        //.// 거부 — 호스트·스킴

        [Theory]
        [InlineData("https://vimeo.com/watch?v=dQw4w9WgXcQ")]      // 다른 서비스
        [InlineData("https://youtube.com.evil.kr/watch?v=dQw4w9WgXcQ")] // 유사 호스트
        [InlineData("https://evil.kr/youtu.be/dQw4w9WgXcQ")]       // 경로에만 유튜브
        [InlineData("javascript:alert(1)")]                        // 스킴 공격
        [InlineData("ftp://youtu.be/dQw4w9WgXcQ")]                 // http(s) 아님
        public void ParseVideoId_허용_호스트와_스킴만_통과시킨다(string url)
        {
            YouTubeVideoLink.ParseVideoId(url).Should().BeNull();
        }

        //.// 거부 — ID 형태

        [Theory]
        [InlineData("https://youtu.be/short")]                     // 11자 미만
        [InlineData("https://youtu.be/dQw4w9WgXcQextra")]          // 11자 초과
        [InlineData("https://youtu.be/dQw4w9WgXc.")]               // 허용 외 문자
        [InlineData("https://youtu.be/../../etc/passwd")]          // 경로 조작
        [InlineData("https://www.youtube.com/watch?v=")]           // 값 없음
        [InlineData("https://www.youtube.com/watch")]              // v 파라미터 없음
        [InlineData("https://www.youtube.com/results?search=x")]   // 영상 경로 아님
        public void ParseVideoId_11자_URL안전_문자만_영상ID로_받는다(string url)
        {
            YouTubeVideoLink.ParseVideoId(url).Should().BeNull();
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        [InlineData("그냥 텍스트")]
        public void ParseVideoId_빈값과_비URL은_null이다(string? url)
        {
            YouTubeVideoLink.ParseVideoId(url).Should().BeNull();
        }

        //.// 파생 — 정규화·썸네일

        [Fact]
        public void ToCanonicalUrl_입력_형태와_무관하게_watch_하나로_모은다()
        {
            const string expected = "https://www.youtube.com/watch?v=" + Id;
            YouTubeVideoLink.ToCanonicalUrl("https://youtu.be/" + Id).Should().Be(expected);
            YouTubeVideoLink.ToCanonicalUrl("https://www.youtube.com/shorts/" + Id).Should().Be(expected);
            YouTubeVideoLink.ToCanonicalUrl(expected).Should().Be(expected);
        }

        [Fact]
        public void ToThumbnailUrl_링크에서_파생한다()
        {
            // 임의 이미지 주소를 저장하지 않기 위해 ID에서 만든다
            YouTubeVideoLink.ToThumbnailUrl("https://youtu.be/" + Id)
                .Should().Be($"https://img.youtube.com/vi/{Id}/hqdefault.jpg");
        }

        [Fact]
        public void 파생_메서드는_유효하지_않은_링크에_null을_준다()
        {
            YouTubeVideoLink.ToCanonicalUrl("https://vimeo.com/123").Should().BeNull();
            YouTubeVideoLink.ToThumbnailUrl("https://vimeo.com/123").Should().BeNull();
            YouTubeVideoLink.IsValid("https://vimeo.com/123").Should().BeFalse();
            YouTubeVideoLink.IsValid("https://youtu.be/" + Id).Should().BeTrue();
        }
    }
}
