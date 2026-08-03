namespace PlayGround.Server.Services
{
    /// <summary>
    /// 업로드(이미지·첨부) 저장 백엔드 — 설정값 하나가 어댑터 하나를 고른다
    /// (`DatabaseProvider`와 같은 방식).
    ///
    /// **이 이름이 곧 어댑터 클래스 이름이다** — S3 → <see cref="S3ObjectStore"/>.
    /// 소비자(Remote*)는 <see cref="IObjectStore"/>만 보므로 어느 값이든 코드가 같다.
    /// </summary>
    public enum UploadStorageProvider
    {
        /// <summary>wwwroot/uploads 디스크 + 정적 서빙. 자격 증명이 없는 PC·오프라인 개발용.</summary>
        Local,

        /// <summary>AWS S3. 현재 운영·개발이 쓰는 값.</summary>
        S3,

        /// <summary>Google Cloud Storage. **아직 구현하지 않았다**(껍질만).</summary>
        Google,

        /// <summary>Azure Blob Storage. **아직 구현하지 않았다**(껍질만).</summary>
        Azure,
    }
}
