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
    /// S3 업로드 어댑터 계약 — 키/URL 형태가 로컬 어댑터와 동일해야 한다
    /// (URL "/uploads/..."는 DB 저장값이자 Application 검증 화이트리스트라 백엔드 교체로 바뀌면 안 된다).
    /// 실제 S3는 부르지 않는다 — IAmazonS3 목으로 요청 형태만 검증한다.
    /// </summary>
    public class S3UploadStorageTests
    {
        private const string Bucket = "test-bucket";

        private static UploadStorageConfiguration.S3Settings Settings => new() { BucketName = Bucket };

        //.// S3ImageStorageService

        [Fact]
        public async Task ImageSave_UsesUploadsKeyShape_AndReturnsMatchingUrl()
        {
            var s3 = new Mock<IAmazonS3>(MockBehavior.Strict);
            PutObjectRequest? captured = null;
            s3.Setup(s => s.PutObjectAsync(It.IsAny<PutObjectRequest>(), It.IsAny<CancellationToken>()))
                .Callback<PutObjectRequest, CancellationToken>((r, _) => captured = r)
                .ReturnsAsync(new PutObjectResponse());

            var service = new S3ImageStorageService(
                s3.Object, Settings, Mock.Of<Microsoft.Extensions.Logging.ILogger<S3ImageStorageService>>());
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
            var s3 = new Mock<IAmazonS3>();
            s3.Setup(s => s.PutObjectAsync(It.IsAny<PutObjectRequest>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new PutObjectResponse());

            var service = new S3ImageStorageService(
                s3.Object, Settings, Mock.Of<Microsoft.Extensions.Logging.ILogger<S3ImageStorageService>>());
            using var content = new MemoryStream(new byte[] { 1 });

            await service.SaveAsync("player-photo", content, "image/webp");

            // 스트림 소유권은 호출자(컨트롤러의 await using) — 어댑터가 닫으면 안 된다
            content.CanRead.Should().BeTrue();
        }

        //.// S3FileStorageService

        [Theory]
        [InlineData("규정.pdf", @"\.pdf$", "application/pdf")]
        [InlineData("훈련안내.hwp", @"\.hwp$", "application/octet-stream")]
        [InlineData("malware.exe", @"\.bin$", "application/octet-stream")]
        public async Task FileSave_PreservesAllowedExtension_AndDowngradesUnknown(
            string originalFileName, string keyPattern, string expectedContentType)
        {
            var s3 = new Mock<IAmazonS3>(MockBehavior.Strict);
            PutObjectRequest? captured = null;
            s3.Setup(s => s.PutObjectAsync(It.IsAny<PutObjectRequest>(), It.IsAny<CancellationToken>()))
                .Callback<PutObjectRequest, CancellationToken>((r, _) => captured = r)
                .ReturnsAsync(new PutObjectResponse());

            var service = new S3FileStorageService(
                s3.Object, Settings, Mock.Of<Microsoft.Extensions.Logging.ILogger<S3FileStorageService>>());
            using var content = new MemoryStream(new byte[] { 1 });

            string url = await service.SaveAsync("team-board", content, originalFileName);

            captured.Should().NotBeNull();
            Regex.IsMatch(captured!.Key, keyPattern).Should().BeTrue($"key '{captured.Key}'는 {keyPattern}이어야 한다");
            captured.ContentType.Should().Be(expectedContentType);
            url.Should().Be("/" + captured.Key);
        }

        //.// S3UploadReader

        [Theory]
        [InlineData("/not-uploads/a.png")]                  // 업로드 URL 형태가 아님
        [InlineData("https://evil.example.com/a.png")]      // 외부 URL
        [InlineData("/uploads/../appsettings.json")]        // 경로 탈출 시도
        [InlineData("/uploads/a\\b.png")]                   // 백슬래시 경로
        public async Task Reader_RejectsNonUploadUrls_WithoutCallingS3(string url)
        {
            var s3 = new Mock<IAmazonS3>(MockBehavior.Strict); // 어떤 호출도 없어야 한다 — 있으면 예외
            var reader = new S3UploadReader(s3.Object, Settings);

            UploadContent? content = await reader.OpenAsync(url);

            content.Should().BeNull();
        }

        [Fact]
        public async Task Reader_ReturnsNull_WhenObjectMissing()
        {
            var s3 = new Mock<IAmazonS3>();
            s3.Setup(s => s.GetObjectAsync(Bucket, It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new AmazonS3Exception("missing") { StatusCode = System.Net.HttpStatusCode.NotFound });

            var reader = new S3UploadReader(s3.Object, Settings);

            UploadContent? content = await reader.OpenAsync("/uploads/team-logo/202608/deadbeef.png");

            content.Should().BeNull();
        }

        [Fact]
        public async Task Reader_StreamsObject_WithContentType()
        {
            byte[] payload = { 9, 8, 7 };
            var response = new GetObjectResponse { ResponseStream = new MemoryStream(payload) };
            response.Headers.ContentType = "image/png";

            var s3 = new Mock<IAmazonS3>();
            s3.Setup(s => s.GetObjectAsync(Bucket, "uploads/team-logo/202608/deadbeef.png", It.IsAny<CancellationToken>()))
                .ReturnsAsync(response);

            var reader = new S3UploadReader(s3.Object, Settings);

            UploadContent? content = await reader.OpenAsync("/uploads/team-logo/202608/deadbeef.png");

            content.Should().NotBeNull();
            content!.ContentType.Should().Be("image/png");
            using var buffer = new MemoryStream();
            await content.Stream.CopyToAsync(buffer);
            buffer.ToArray().Should().Equal(payload);
        }
    }
}
