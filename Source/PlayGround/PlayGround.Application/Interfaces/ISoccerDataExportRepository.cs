using PlayGround.Contracts.Common;
using PlayGround.Contracts.Export;
using PlayGround.Shared.Result;
using PlayGround.Shared.Time;

namespace PlayGround.Application.Interfaces
{
    /// <summary>백그라운드 잡이 처리할 내보내기 요청 상세 (Soccer DB).</summary>
    public class DataExportJob
    {
        public Guid RequestId { get; set; }
        public Guid UserId { get; set; }
        public DataExportStatus Status { get; set; }
        public bool IncludeProfile { get; set; }
        public bool IncludeRecords { get; set; }
        public bool IncludeRequests { get; set; }
    }

    /// <summary>데이터 내려받기 요청 저장·조회 포트 (Soccer DB). 생성만 접수하고 파일 생성은 백그라운드 잡.</summary>
    public interface ISoccerDataExportRepository
    {
        /// <summary>요청 생성 — 동시 1건·쿨다운 24h를 SP가 판정. Ok일 때만 RequestId가 채워진다.</summary>
        Task<Result<(DataExportRequestStatus Status, Guid? RequestId)>> CreateAsync(
            Guid userId, bool includeProfile, bool includeRecords, bool includeRequests, CancellationToken cancellation = default);

        /// <summary>현재 상태 행 (최신 비삭제). 만료된 Ready는 Success(null)로 걸러 "요청" 버튼으로 되돌린다.</summary>
        Task<Result<DataExportStateDto?>> GetByUserAsync(Guid userId, CancellationToken cancellation = default);

        /// <summary>잡 처리용 단건 상세 (UserId·포함 항목). 취소·미존재는 Success(null).</summary>
        Task<Result<DataExportJob?>> GetByIdAsync(Guid requestId, CancellationToken cancellation = default);

        /// <summary>잡 완료 전환 — Ready(토큰·키·크기·만료) 또는 Failed. Pending일 때만 반영.</summary>
        Task<Result<bool>> UpdateStatusAsync(Guid requestId, DataExportStatus status, string? downloadToken, string? storageKey,
            long? sizeBytes, SystemTime? expiresAt, CancellationToken cancellation = default);

        /// <summary>요청 취소 — Pending + 소유자만 소프트 삭제.</summary>
        Task<Result<bool>> CancelAsync(Guid userId, Guid requestId, CancellationToken cancellation = default);

        /// <summary>서명 URL 다운로드 소비 — Ready·미만료·횟수<상한이면 count++ 후 StorageKey, 아니면 null.</summary>
        Task<Result<string?>> ConsumeDownloadAsync(string token, CancellationToken cancellation = default);

        /// <summary>미완료(Pending) 요청 Id 목록 — 서버 재기동 시 워커가 재개용으로 읽는다.</summary>
        Task<Result<List<Guid>>> GetPendingIdsAsync(CancellationToken cancellation = default);
    }
}
