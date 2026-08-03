namespace PlayGround.Server.Services
{
    /// <summary>
    /// 업로드(이미지·첨부) 저장 백엔드 선택 — 섹션 "UploadStorageConfiguration".
    ///
    /// Local  = wwwroot/uploads 디스크 + 정적 서빙 (자격 증명이 없는 PC·오프라인 개발용)
    /// Remote = 오브젝트 스토리지 + `/uploads` 프록시 서빙 (운영·개발 기본)
    ///
    /// **"Remote"가 어느 벤더인지는 설정이 정하지 않는다** — 어댑터(<see cref="AwsObjectStore"/>)가
    /// 안다. 그래서 저장소를 바꿔도 이 설정 이름과 환경변수는 그대로다.
    /// 운영 전환은 환경변수: UploadStorageConfiguration__Provider=Remote
    /// + UploadStorageConfiguration__Remote__BucketName=&lt;버킷&gt;.
    /// </summary>
    public sealed class UploadStorageConfiguration
    {
        public const string Section = "UploadStorageConfiguration";

        /// <summary>"Local" 또는 "Remote".</summary>
        public string Provider { get; set; } = "Local";

        public RemoteSettings Remote { get; set; } = new();

        public bool UsesRemote => string.Equals(Provider, "Remote", StringComparison.OrdinalIgnoreCase);

        public sealed class RemoteSettings
        {
            /// <summary>업로드 버킷 이름 — Provider=Remote인데 비어 있으면 기동을 실패시킨다(설정 누락을 조용히 넘기지 않는다).</summary>
            public string BucketName { get; set; } = string.Empty;

            /// <summary>리전(예: ap-northeast-2). 비우면 SDK 기본 체인(EC2 IMDS·로컬 프로필)이 정한다.</summary>
            public string Region { get; set; } = string.Empty;
        }
    }
}
