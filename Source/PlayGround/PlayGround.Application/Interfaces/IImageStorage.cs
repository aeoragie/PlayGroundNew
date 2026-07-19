namespace PlayGround.Application.Interfaces
{
    /// <summary>
    /// 업로드 이미지 저장 포트. 구현은 인프라 몫(로컬 디스크 → 추후 오브젝트 스토리지로 교체).
    /// 반환값은 브라우저가 그대로 쓰는 공개 URL — 저장 위치가 바뀌어도 호출부는 그대로다.
    /// </summary>
    public interface IImageStorage
    {
        /// <summary>
        /// 이미지를 저장하고 공개 URL을 돌려준다.
        /// </summary>
        /// <param name="category">용도 폴더 (예: "team-logo", "team-cover").</param>
        /// <param name="content">이미지 바이트. 리사이즈·회전 보정은 클라이언트에서 이미 끝난 상태다.</param>
        /// <param name="contentType">"image/jpeg" 등 — 허용 형식 검증은 호출 전에 끝난다.</param>
        Task<string> SaveAsync(string category, Stream content, string contentType, CancellationToken cancellation = default);
    }
}
