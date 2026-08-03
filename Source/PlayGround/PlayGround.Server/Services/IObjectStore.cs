namespace PlayGround.Server.Services
{
    /// <summary>
    /// 원격 오브젝트 저장소 — **키로 넣고 꺼내는 것**만 아는 최소 인터페이스.
    ///
    /// 어느 벤더를 쓰는지는 이 뒤에 숨는다. 지금 구현은 AWS S3(<see cref="AwsObjectStore"/>)지만
    /// 업로드 서비스(Remote*)는 그 사실을 모르므로, 다른 저장소로 옮길 때
    /// **어댑터 한 개와 DI 한 줄만** 바뀐다.
    ///
    /// 키 형태(uploads/{category}/{yyyyMM}/{guid}{ext})는 <see cref="UploadPaths"/>가 정한다 —
    /// 여기서는 문자열 키로만 다루고 의미를 해석하지 않는다.
    /// </summary>
    public interface IObjectStore
    {
        /// <summary>키에 저장한다. **스트림 소유권은 호출자에 있다** (구현이 닫지 않는다).</summary>
        Task PutAsync(string key, Stream content, string contentType, CancellationToken cancellation = default);

        /// <summary>
        /// 키의 원본을 연다. 없으면 **오류가 아니라 null** —
        /// 지워진 이미지의 URL이 DB에 남아 있는 것은 정상 시나리오다.
        /// </summary>
        Task<UploadContent?> OpenAsync(string key, CancellationToken cancellation = default);
    }
}
