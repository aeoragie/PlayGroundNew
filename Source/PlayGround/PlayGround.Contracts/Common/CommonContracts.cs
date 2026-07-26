namespace PlayGround.Contracts.Common
{
    /// <summary>업로드된 이미지의 공개 URL. 저장 위치(로컬·오브젝트 스토리지)는 클라이언트가 알 필요 없다.</summary>
    public class UploadedImageResponse
    {
        public string Url { get; set; } = string.Empty;
    }

    /// <summary>업로드된 문서/파일의 공개 URL + 원본 파일명·크기 (게시판 첨부 등 — 이미지가 아닌 첨부).</summary>
    public class UploadedFileResponse
    {
        public string Url { get; set; } = string.Empty;
        public string FileName { get; set; } = string.Empty;
        public long SizeBytes { get; set; }
    }
}
