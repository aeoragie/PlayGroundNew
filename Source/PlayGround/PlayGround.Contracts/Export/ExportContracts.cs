using PlayGround.Shared.Time;
using System;

namespace PlayGround.Contracts.Export
{
    /// <summary>데이터 내려받기 요청 (Design.SettingsFlows ③). 포함 항목 3종 체크.</summary>
    public class CreateDataExportRequest
    {
        public bool IncludeProfile { get; set; } = true;
        public bool IncludeRecords { get; set; } = true;
        public bool IncludeRequests { get; set; } = true;
    }

    /// <summary>현재 데이터 내려받기 상태 (설정 계정 관리 절 상태 행). 없으면 null → "요청" 버튼.</summary>
    public class DataExportStateDto
    {
        public Guid RequestId { get; set; }

        /// <summary>'Pending'(준비 중) | 'Ready'(준비 완료) | 'Failed'(실패).</summary>
        public string Status { get; set; } = string.Empty;

        public long SizeBytes { get; set; }
        public SystemTime CreatedAt { get; set; }

        /// <summary>Ready일 때만 값 — 만료일(요청+7일). 만료되면 서버가 이 행을 상태에서 뺀다.</summary>
        public SystemTime? ExpiresAt { get; set; }

        /// <summary>Ready일 때만 값 — 다운로드 서명 URL 토큰. 클라가 다운로드 링크를 조립한다.</summary>
        public string? DownloadToken { get; set; }

        public int DownloadCount { get; set; }
        public int MaxDownloads { get; set; }
    }

    /// <summary>요청 생성 결과. Status로 접수/거부 사유를 가른다 — 진행 중·쿨다운은 인라인 안내.</summary>
    public class DataExportRequestResult
    {
        /// <summary>'Ok'(접수됨) | 'InProgress'(진행 중 1건 존재) | 'Cooldown'(24h 이내 재요청).</summary>
        public string Status { get; set; } = string.Empty;

        public DataExportStateDto? Export { get; set; }
    }
}
