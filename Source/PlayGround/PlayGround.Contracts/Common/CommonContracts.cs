namespace PlayGround.Contracts.Common
{
    public class UploadedImageResponse
    {
        public string Url { get; set; } = string.Empty;
    }

    public class UploadedFileResponse
    {
        public string Url { get; set; } = string.Empty;
        public string FileName { get; set; } = string.Empty;
        public long SizeBytes { get; set; }
    }
}
