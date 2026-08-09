namespace PlayGround.Application.Interfaces
{
    /// <summary>
    /// 첨부 문서 저장 포트 (게시판 pdf·hwp·이미지 등). IImageStorage와 달리 **원본 확장자를 보존**한다
    /// (문서는 리사이즈·크롭이 없다). 구현은 인프라 몫(로컬 디스크 → 추후 오브젝트 스토리지 교체).
    /// 반환값은 브라우저가 그대로 쓰는 공개 URL.
    /// </summary>
    public interface IFileStorage
    {
        /// <param name="category">용도 폴더 (예: "team-board") — 임의 경로 생성을 막는 화이트리스트는 호출부가 검사.</param>
        /// <param name="content">파일 바이트.</param>
        /// <param name="originalFileName">원본 파일명 — 확장자를 여기서 딴다(저장 파일명은 GUID + 원본 확장자).</param>
        Task<string> SaveAsync(string category, Stream content, string originalFileName, CancellationToken cancellation = default);
    }
}
