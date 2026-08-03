namespace PlayGround.Server.Services
{
    /// <summary>
    /// 업로드 원본 읽기 — /uploads 프록시 서빙(UploadsController)과 OG 렌더의 엠블럼 로드가 쓴다.
    /// 저장(IImageStorage·IFileStorage)과 같은 백엔드를 바라보도록 DI에서 함께 전환된다.
    /// </summary>
    public interface IUploadReader
    {
        /// <summary>"/uploads/..." 상대 URL의 원본을 연다. 없거나 업로드 URL 형태가 아니면 null.</summary>
        Task<UploadContent?> OpenAsync(string relativeUrl, CancellationToken cancellation = default);
    }

    /// <summary>읽은 원본 — Stream 소유권은 호출자에게 넘어간다(응답으로 흘리면 프레임워크가 dispose).</summary>
    public sealed record UploadContent(Stream Stream, string ContentType, long? Length);
}
