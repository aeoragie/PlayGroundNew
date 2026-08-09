using PlayGround.Shared.Time;

namespace PlayGround.Contracts.Export
{
    public class CreateDataExportRequest
    {
        public bool IncludeProfile { get; set; } = true;
        public bool IncludeRecords { get; set; } = true;
        public bool IncludeRequests { get; set; } = true;
    }

    public class DataExportStateDto
    {
        public Guid RequestId { get; set; }

        public string Status { get; set; } = string.Empty;

        public long SizeBytes { get; set; }
        public SystemTime CreatedAt { get; set; }

        public SystemTime? ExpiresAt { get; set; }

        public string? DownloadToken { get; set; }

        public int DownloadCount { get; set; }
        public int MaxDownloads { get; set; }
    }

    public class DataExportRequestResult
    {
        public string Status { get; set; } = string.Empty;

        public DataExportStateDto? Export { get; set; }
    }
}
