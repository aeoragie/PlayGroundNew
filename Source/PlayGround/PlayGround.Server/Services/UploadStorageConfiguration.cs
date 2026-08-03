namespace PlayGround.Server.Services
{
    /// <summary>
    /// 업로드(이미지·첨부) 저장 설정 — 섹션 "UploadStorageConfiguration".
    /// `DatabaseConfiguration`과 같은 형태로, <see cref="Provider"/>가 어댑터를 고르고
    /// 나머지 값이 그 옆에 붙는다.
    ///
    /// 운영 전환은 환경변수: UploadStorageConfiguration__Provider=S3
    /// + UploadStorageConfiguration__BucketName=&lt;버킷&gt;.
    /// URL 형태("/uploads/...")는 어느 Provider든 동일하다 — DB 저장값이 바뀌지 않는다.
    /// </summary>
    public sealed class UploadStorageConfiguration
    {
        public const string Section = "UploadStorageConfiguration";

        public UploadStorageProvider Provider { get; set; } = UploadStorageProvider.Local;

        /// <summary>
        /// 버킷(컨테이너) 이름 — Local 외에는 필수다.
        /// 비어 있으면 기동을 실패시킨다(설정 누락을 조용히 로컬 폴백하지 않는다).
        /// </summary>
        public string BucketName { get; set; } = string.Empty;

        /// <summary>리전(예: ap-northeast-2). 비우면 SDK 기본 체인(EC2 IMDS·로컬 프로필)이 정한다.</summary>
        public string Region { get; set; } = string.Empty;

        /// <summary>디스크에 쓰는가 — Local만 오브젝트 저장소를 쓰지 않는다.</summary>
        public bool UsesLocalDisk => Provider == UploadStorageProvider.Local;
    }
}
