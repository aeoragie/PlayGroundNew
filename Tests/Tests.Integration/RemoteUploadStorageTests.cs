using System.Text.RegularExpressions;
using Amazon.S3;
using Amazon.S3.Model;
using FluentAssertions;
using Moq;
using PlayGround.Server.Services;
using Xunit;

namespace PlayGround.Tests.Integration
{
    /// <summary>
    /// 원격 업로드 어댑터 계약 — 키/URL 형태가 로컬 어댑터와 동일해야 한다
    /// (URL "/uploads/..."는 DB 저장값이자 Application 검증 화이트리스트라 백엔드 교체로 바뀌면 안 된다).
    ///
    /// 저장소 벤더는 <see cref="AwsObjectStore"/> 뒤에 있으므로 여기서만 SDK를 안다.
    /// 실제 S3는 부르지 않는다 — IAmazonS3 목으로 요청 형태만 검증한다.
    /// </summary>
    public class RemoteUploadStorageTests
    {
        private const string Bucket = "test-bucket";

        private static UploadStorageConfiguration.RemoteSettings Settings => new() { BucketName = Bucket };

        private static IObjectStore StoreOf(Mock<IAmazonS3> client) => new AwsObjectStore(client.Object, Settings);

        //.// RemoteImageStorageService

        [Fact]
        public async Task ImageSave_UsesUploadsKeyShape_AndReturnsMatchingUrl()
        {
            var client = new Mock<IAmazonS3>(MockBehavior.Strict);
            PutObjectRequest? captured = null;
            client.Setup(s => s.PutObjectAsync(It.IsAny<PutObjectRequest>(), It.IsAny<CancellationToken>()))
                .Callback<PutObjectRequest, CancellationToken>((r, _) => captured = r)
                .ReturnsAsync(new PutObjectResponse());

            var service = new RemoteImageStorageService(
                StoreOf(client), Mock.Of<Microsoft.Extensions.Logging.ILogger<RemoteImageStorageService>>());
            using var content = new MemoryStream(new byte[] { 1, 2, 3 });

            string url = await service.SaveAsync("team-logo", content, "image/png");

            captured.Should().NotBeNull();
            captured!.BucketName.Should().Be(Bucket);
            captured.ContentType.Should().Be("image/png");
            captured.Key.Should().MatchRegex(@"^uploads/team-logo/\d{6}/[0-9a-f]{32}\.png$");
            url.Should().Be("/" + captured.Key);
        }

        [Fact]
        public async Task ImageSave_DoesNotCloseCallerStream()
        {
            var client = new Mock<IAmazonS3>();
            client.Setup(s => s.PutObjectAsync(It.IsAny<PutObjectRequest>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new PutObjectResponse());

            var service = new RemoteImageStorageService(
                StoreOf(client), Mock.Of<Microsoft.Extensions.Logging.ILogger<RemoteImageStorageService>>());
            using var content = new MemoryStream(new byte[] { 1 });

            await service.SaveAsync("player-photo", content, "image/webp");

            // 스트림 소유권은 호출자(컨트롤러의 await using) — 어댑터가 닫으면 안 된다
            content.CanRead.Should().BeTrue();
        }

        //.// RemoteFileStorageService

        [Theory]
        [InlineData("규정.pdf", @"\.pdf$", "application/pdf")]
        [InlineData("훈련안내.hwp", @"\.hwp$", "application/octet-stream")]
        [InlineData("malware.exe", @"\.bin$", "application/octet-stream")]
        public async Task FileSave_PreservesAllowedExtension_AndDowngradesUnknown(
            string originalFileName, string keyPattern, string expectedContentType)
        {
            var client = new Mock<IAmazonS3>(MockBehavior.Strict);
            PutObjectRequest? captured = null;
            client.Setup(s => s.PutObjectAsync(It.IsAny<PutObjectRequest>(), It.IsAny<CancellationToken>()))
                .Callback<PutObjectRequest, CancellationToken>((r, _) => captured = r)
                .ReturnsAsync(new PutObjectResponse());

            var service = new RemoteFileStorageService(
                StoreOf(client), Mock.Of<Microsoft.Extensions.Logging.ILogger<RemoteFileStorageService>>());
            using var content = new MemoryStream(new byte[] { 1 });

            string url = await service.SaveAsync("team-board", content, originalFileName);

            captured.Should().NotBeNull();
            Regex.IsMatch(captured!.Key, keyPattern).Should().BeTrue($"key '{captured.Key}'는 {keyPattern}이어야 한다");
            captured.ContentType.Should().Be(expectedContentType);
            url.Should().Be("/" + captured.Key);
        }

        //.// RemoteUploadReader — URL 해석은 저장소를 건드리기 전에 끝난다

        [Theory]
        [InlineData("/not-uploads/a.png")]                  // 업로드 URL 형태가 아님
        [InlineData("https://evil.example.com/a.png")]      // 외부 URL
        [InlineData("/uploads/../appsettings.json")]        // 경로 탈출 시도
        [InlineData("/uploads/a\\b.png")]                   // 백슬래시 경로
        public async Task Reader_RejectsNonUploadUrls_WithoutTouchingStore(string url)
        {
            // 저장소를 한 번이라도 부르면 Strict 목이 예외를 던진다
            var store = new Mock<IObjectStore>(MockBehavior.Strict);
            var reader = new RemoteUploadReader(store.Object);

            UploadContent? content = await reader.OpenAsync(url);

            content.Should().BeNull();
        }

        [Fact]
        public async Task Reader_ReturnsNull_WhenObjectMissing()
        {
            var client = new Mock<IAmazonS3>();
            client.Setup(s => s.GetObjectAsync(Bucket, It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new AmazonS3Exception("missing") { StatusCode = System.Net.HttpStatusCode.NotFound });

            var reader = new RemoteUploadReader(StoreOf(client));

            UploadContent? content = await reader.OpenAsync("/uploads/team-logo/202608/deadbeef.png");

            content.Should().BeNull();
        }

        [Fact]
        public async Task Reader_StreamsObject_WithContentType()
        {
            byte[] payload = { 9, 8, 7 };
            var response = new GetObjectResponse { ResponseStream = new MemoryStream(payload) };
            response.Headers.ContentType = "image/png";

            var client = new Mock<IAmazonS3>();
            client.Setup(s => s.GetObjectAsync(Bucket, "uploads/team-logo/202608/deadbeef.png", It.IsAny<CancellationToken>()))
                .ReturnsAsync(response);

            var reader = new RemoteUploadReader(StoreOf(client));

            UploadContent? content = await reader.OpenAsync("/uploads/team-logo/202608/deadbeef.png");

            content.Should().NotBeNull();
            content!.ContentType.Should().Be("image/png");
            using var buffer = new MemoryStream();
            await content.Stream.CopyToAsync(buffer);
            buffer.ToArray().Should().Equal(payload);
        }
    }
}
