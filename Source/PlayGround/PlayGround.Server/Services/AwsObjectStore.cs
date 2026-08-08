using System.Diagnostics;
using System.Net;
using Amazon;
using Amazon.S3;
using Amazon.S3.Model;

namespace PlayGround.Server.Services
{
    /// <summary>
    /// <see cref="IObjectStore"/>의 AWS S3 구현 (`Provider=Aws`).
    /// **AWS SDK를 아는 유일한 클래스다** — 소비자(Remote*)는 인터페이스만 본다.
    ///
    /// 자격 증명은 SDK 기본 체인(EC2 인스턴스 역할 · 개발 PC의 aws 프로필)이 정한다 —
    /// **서버에 액세스 키를 두지 않는다.**
    /// 버킷은 퍼블릭 차단 상태이고, 브라우저 서빙은 UploadsController(`/uploads` 프록시)가 맡는다.
    /// </summary>
    public sealed class AwsObjectStore : IObjectStore
    {
        private readonly IAmazonS3 mClient;
        private readonly string mBucketName;

        /// <summary>운영·개발 경로 — 설정만 주면 클라이언트는 스스로 만든다(호출자가 SDK를 몰라도 된다).</summary>
        public AwsObjectStore(UploadStorageConfiguration settings)
            : this(CreateClient(settings), settings)
        {
        }

        public AwsObjectStore(IAmazonS3 client, UploadStorageConfiguration settings)
        {
            Debug.Assert(client != null, "client is required");
            Debug.Assert(settings != null, "settings is required");
            mClient = client ?? throw new ArgumentNullException(nameof(client));
            mBucketName = settings?.BucketName ?? throw new ArgumentNullException(nameof(settings));
        }

        public async Task PutAsync(
            string key, Stream content, string contentType, CancellationToken cancellation = default)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(key);
            ArgumentNullException.ThrowIfNull(content);

            var request = new PutObjectRequest
            {
                BucketName = mBucketName,
                Key = key,
                InputStream = content,
                ContentType = contentType,
                AutoCloseStream = false, // 스트림 소유권은 호출자(컨트롤러의 await using)에 있다
            };
            await mClient.PutObjectAsync(request, cancellation);
        }

        public async Task<UploadContent?> OpenAsync(string key, CancellationToken cancellation = default)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(key);

            try
            {
                GetObjectResponse response = await mClient.GetObjectAsync(mBucketName, key, cancellation);

                // 헤더가 비어 오는 경우가 있어 확장자로 보정한다 — 저장 시 확장자를 우리가 정했으므로 안전하다
                string contentType = string.IsNullOrEmpty(response.Headers.ContentType)
                    ? UploadPaths.ContentTypeOf(key)
                    : response.Headers.ContentType;
                return new UploadContent(response.ResponseStream, contentType, response.ContentLength);
            }
            catch (AmazonS3Exception ex) when (ex.StatusCode == HttpStatusCode.NotFound)
            {
                return null;
            }
        }

        private static IAmazonS3 CreateClient(UploadStorageConfiguration settings)
        {
            ArgumentNullException.ThrowIfNull(settings);

            // Region이 비면 SDK 기본 체인(EC2 IMDS·로컬 프로필)이 정하게 둔다
            return string.IsNullOrEmpty(settings.Region)
                ? new AmazonS3Client()
                : new AmazonS3Client(RegionEndpoint.GetBySystemName(settings.Region));
        }
    }
}
