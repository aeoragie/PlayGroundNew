using Microsoft.Extensions.Options;
using PlayGround.Shared.Result;
using PlayGround.Shared.Time;
using PlayGround.Infrastructure.Database;
using PlayGround.Infrastructure.Database.Base;
using PlayGround.Infrastructure.Logging;
using PlayGround.Contracts.Export;
using PlayGround.Application.Interfaces;
using PlayGround.Persistence.Database.Generated.Soccer.Entities;
using PlayGround.Persistence.Database.Generated.Soccer.Procedures;

namespace PlayGround.Persistence.Repositories
{
    /// <summary>데이터 내려받기 요청 저장소 (Soccer DB). 요청 접수·상태 전환·서명 URL 다운로드 게이팅.</summary>
    public class SoccerDataExportRepository : RepositoryBase, ISoccerDataExportRepository
    {
        public override DatabaseTypes Database => DatabaseTypes.Soccer;

        public SoccerDataExportRepository(IOptions<DatabaseConfiguration> options) : base(options)
        {
        }

        public async Task<Result<(string Status, Guid? RequestId)>> CreateAsync(
            Guid userId, bool includeProfile, bool includeRecords, bool includeRequests, CancellationToken cancellation = default)
        {
            Logger.InfoWith("Data export request creation requested", ("UserId", userId));

            var procedure = new UspCreateSoccerDataExportRequest(this)
            {
                UserId = userId,
                IncludeProfile = includeProfile,
                IncludeRecords = includeRecords,
                IncludeRequests = includeRequests
            };
            Result<MultiQueryReader> opened = await ProcedureMultipleAsync(procedure, cancellation: cancellation);
            if (opened.IsError)
            {
                return Result<(string, Guid?)>.Error(ErrorCode.DatabaseError, "CreateDataExport");
            }

            using MultiQueryReader reader = opened.Value;
            string status = await reader.ReadSingleOrDefaultAsync<string>() ?? "Error";
            Guid? requestId = await reader.ReadSingleOrDefaultAsync<Guid?>();

            return Result<(string, Guid?)>.Success((status, requestId is { } id && id != Guid.Empty ? id : null));
        }

        public async Task<Result<DataExportStateDto?>> GetByUserAsync(Guid userId, CancellationToken cancellation = default)
        {
            var procedure = new UspGetSoccerDataExportByUser(this) { UserId = userId };
            var queryResult = await procedure.QueryAsync<SoccerDataExportRequestsEntity>(cancellation: cancellation);
            if (queryResult.IsError)
            {
                return Result<DataExportStateDto?>.Error(ErrorCode.DatabaseError, "GetDataExportByUser");
            }

            SoccerDataExportRequestsEntity? row = queryResult.Values1.FirstOrDefault();
            if (row is null)
            {
                return Result<DataExportStateDto?>.Success(null);
            }

            // 만료된 Ready는 상태 행에서 빼서 "요청" 버튼으로 되돌린다 (판정은 여기 한 곳)
            if (row.Status == "Ready" && row.ExpiresAt is DateTime exp && exp <= SystemTime.Now)
            {
                return Result<DataExportStateDto?>.Success(null);
            }

            return Result<DataExportStateDto?>.Success(MapState(row));
        }

        public async Task<Result<DataExportJob?>> GetByIdAsync(Guid requestId, CancellationToken cancellation = default)
        {
            var procedure = new UspGetSoccerDataExportById(this) { RequestId = requestId };
            var queryResult = await procedure.QueryAsync<SoccerDataExportRequestsEntity>(cancellation: cancellation);
            if (queryResult.IsError)
            {
                return Result<DataExportJob?>.Error(ErrorCode.DatabaseError, "GetDataExportById");
            }

            SoccerDataExportRequestsEntity? row = queryResult.Values1.FirstOrDefault();
            if (row is null)
            {
                return Result<DataExportJob?>.Success(null);
            }

            return Result<DataExportJob?>.Success(new DataExportJob
            {
                RequestId = row.RequestId,
                UserId = row.UserId,
                Status = row.Status,
                IncludeProfile = row.IncludeProfile,
                IncludeRecords = row.IncludeRecords,
                IncludeRequests = row.IncludeRequests
            });
        }

        public async Task<Result<bool>> UpdateStatusAsync(Guid requestId, string status, string? downloadToken, string? storageKey,
            long? sizeBytes, DateTime? expiresAt, CancellationToken cancellation = default)
        {
            Logger.InfoWith("Data export status update requested", ("RequestId", requestId), ("Status", status));

            var procedure = new UspUpdateSoccerDataExportStatus(this)
            {
                RequestId = requestId,
                Status = status,
                DownloadToken = downloadToken!,
                StorageKey = storageKey!,
                SizeBytes = sizeBytes,
                ExpiresAt = expiresAt
            };
            var queryResult = await procedure.QueryAsync<int>(cancellation: cancellation);
            if (queryResult.IsError)
            {
                return Result<bool>.Error(ErrorCode.DatabaseError, "UpdateDataExportStatus");
            }

            return Result<bool>.Success(queryResult.Values1.FirstOrDefault() > 0);
        }

        public async Task<Result<bool>> CancelAsync(Guid userId, Guid requestId, CancellationToken cancellation = default)
        {
            var procedure = new UspCancelSoccerDataExportRequest(this) { UserId = userId, RequestId = requestId };
            var queryResult = await procedure.QueryAsync<int>(cancellation: cancellation);
            if (queryResult.IsError)
            {
                return Result<bool>.Error(ErrorCode.DatabaseError, "CancelDataExport");
            }

            return Result<bool>.Success(queryResult.Values1.FirstOrDefault() > 0);
        }

        public async Task<Result<string?>> ConsumeDownloadAsync(string token, CancellationToken cancellation = default)
        {
            var procedure = new UspConsumeSoccerDataExportDownload(this) { Token = token };
            var queryResult = await procedure.QueryAsync<string>(cancellation: cancellation);
            if (queryResult.IsError)
            {
                return Result<string?>.Error(ErrorCode.DatabaseError, "ConsumeDataExportDownload");
            }

            return Result<string?>.Success(queryResult.Values1.FirstOrDefault());
        }

        public async Task<Result<List<Guid>>> GetPendingIdsAsync(CancellationToken cancellation = default)
        {
            var procedure = new UspGetPendingSoccerDataExports(this);
            var queryResult = await procedure.QueryAsync<Guid>(cancellation: cancellation);
            if (queryResult.IsError)
            {
                return Result<List<Guid>>.Error(ErrorCode.DatabaseError, "GetPendingDataExports");
            }

            return Result<List<Guid>>.Success(queryResult.Values1.ToList());
        }

        private static DataExportStateDto MapState(SoccerDataExportRequestsEntity row)
        {
            bool isReady = row.Status == "Ready";
            return new DataExportStateDto
            {
                RequestId = row.RequestId,
                Status = row.Status,
                SizeBytes = row.SizeBytes ?? 0,
                CreatedAt = row.CreatedAt,
                ExpiresAt = isReady ? row.ExpiresAt : null,
                DownloadToken = isReady ? NullIfEmpty(row.DownloadToken) : null,
                DownloadCount = row.DownloadCount,
                MaxDownloads = row.MaxDownloads
            };
        }

        private static string? NullIfEmpty(string? value) => string.IsNullOrEmpty(value) ? null : value;
    }
}
