using PlayGround.Shared.Time;
using PlayGround.Domain.Account;

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

        public DataExportStatus Status { get; set; }

        public long SizeBytes { get; set; }
        public SystemTime CreatedAt { get; set; }

        public SystemTime? ExpiresAt { get; set; }

        public string? DownloadToken { get; set; }

        public int DownloadCount { get; set; }
        public int MaxDownloads { get; set; }
    }

    public class DataExportRequestResult
    {
        public DataExportRequestStatus Status { get; set; }

        public DataExportStateDto? Export { get; set; }
    }
}
