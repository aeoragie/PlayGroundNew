namespace PlayGround.Contracts.Common
{
    /// <summary>업로드된 이미지의 공개 URL. 저장 위치(로컬·오브젝트 스토리지)는 클라이언트가 알 필요 없다.</summary>
    public class UploadedImageResponse
    {
        public string Url { get; set; } = string.Empty;
    }
}
