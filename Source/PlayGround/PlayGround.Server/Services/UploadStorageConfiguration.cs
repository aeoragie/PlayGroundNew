namespace PlayGround.Server.Services
{
    /// <summary>
    /// 업로드(이미지·첨부) 저장 백엔드 선택 — 섹션 "UploadStorageConfiguration".
    /// Local = wwwroot/uploads 디스크(개발 기본), S3 = 프라이빗 버킷 + /uploads 프록시 서빙(운영).
    /// 운영 전환은 환경변수: UploadStorageConfiguration__Provider=S3 + __S3__BucketName=&lt;버킷&gt;.
    /// </summary>
    public sealed class UploadStorageConfiguration
    {
        public const string Section = "UploadStorageConfiguration";

        /// <summary>"Local" 또는 "S3".</summary>
        public string Provider { get; set; } = "Local";

        public S3Settings S3 { get; set; } = new();

        public bool UsesS3 => string.Equals(Provider, "S3", StringComparison.OrdinalIgnoreCase);

        public sealed class S3Settings
        {
            /// <summary>업로드 버킷 이름 — Provider=S3인데 비어 있으면 기동을 실패시킨다(설정 누락을 조용히 넘기지 않는다).</summary>
            public string BucketName { get; set; } = string.Empty;

            /// <summary>리전(예: ap-northeast-2). 비우면 SDK 기본 체인(EC2 IMDS·로컬 프로필)이 정한다.</summary>
            public string Region { get; set; } = string.Empty;
        }
    }
}
