using Microsoft.Extensions.Options;
using PlayGround.Shared.Result;
using PlayGround.Infrastructure.Database;
using PlayGround.Infrastructure.Database.Base;
using PlayGround.Infrastructure.Logging;
using PlayGround.Contracts.Agent;
using PlayGround.Application.Interfaces;
using PlayGround.Persistence.Database.Generated.Soccer.Entities;
using PlayGround.Persistence.Database.Generated.Soccer.Procedures;

namespace PlayGround.Persistence.Repositories
{
    /// <summary>에이전트 열람 요청 심사 저장소 (Soccer DB). 다중 결과셋 — MultiQueryReader 소비.</summary>
    public class SoccerAgentApprovalRepository : RepositoryBase, IAgentApprovalRepository
    {
        public override DatabaseTypes Database => DatabaseTypes.Soccer;

        public SoccerAgentApprovalRepository(IOptions<DatabaseConfiguration> options) : base(options)
        {
        }

        public async Task<Result<AgentViewRequestResponse?>> GetRequestAsync(
            Guid guardianUserId, Guid requestId, CancellationToken cancellation = default)
        {
            Logger.InfoWith("Agent view request requested", ("GuardianUserId", guardianUserId), ("RequestId", requestId));

            var procedure = new UspGetSoccerAgentViewRequest(this) { GuardianUserId = guardianUserId, RequestId = requestId };
            Result<MultiQueryReader> opened = await ProcedureMultipleAsync(procedure, cancellation: cancellation);
            if (opened.IsError)
            {
                Logger.ErrorWith("Agent view request query failed", ("DetailCode", opened.ResultData.DetailCode));
                return Result<AgentViewRequestResponse?>.Error(ErrorCode.DatabaseError);
            }

            using MultiQueryReader reader = opened.Value;
            SoccerAgentViewRequestRecord? request = await reader.ReadSingleOrDefaultAsync<SoccerAgentViewRequestRecord>();
            if (request is null)
            {
                Logger.InfoWith("Agent view request not found", ("RequestId", requestId));
                return Result<AgentViewRequestResponse?>.Success(null);
            }

            SoccerAgentProfilesEntity? agent = await reader.ReadSingleOrDefaultAsync<SoccerAgentProfilesEntity>();
            var logs = (await reader.ReadAsync<SoccerAgentViewLogsEntity>()).ToList();

            return Result<AgentViewRequestResponse?>.Success(Map(request, agent, logs));
        }

        public async Task<Result<AgentViewRequestResponse?>> ReviewAsync(
            Guid guardianUserId, Guid requestId, string action, CancellationToken cancellation = default)
        {
            Logger.InfoWith("Agent view request review requested",
                ("GuardianUserId", guardianUserId), ("RequestId", requestId), ("Action", action));

            var procedure = new UspReviewSoccerAgentViewRequest(this)
            {
                GuardianUserId = guardianUserId,
                RequestId = requestId,
                Action = action
            };
            var queryResult = await procedure.QueryAsync<SoccerAgentViewRequestRecord>(cancellation: cancellation);
            if (queryResult.IsError)
            {
                return Result<AgentViewRequestResponse?>.Error(ErrorCode.DatabaseError, "ReviewAgentViewRequest");
            }

            SoccerAgentViewRequestRecord? row = queryResult.Values1.FirstOrDefault();
            if (row is null)
            {
                Logger.InfoWith("Agent view request review denied or not transitionable", ("RequestId", requestId));
                return Result<AgentViewRequestResponse?>.Success(null);
            }

            Logger.InfoWith("Agent view request reviewed", ("RequestId", row.RequestId), ("Status", row.Status));
            return Result<AgentViewRequestResponse?>.Success(Map(row, agent: null, logs: new List<SoccerAgentViewLogsEntity>()));
        }

        public async Task<Result<bool>> BlockAgentAsync(Guid guardianUserId, Guid requestId, CancellationToken cancellation = default)
        {
            Logger.InfoWith("Agent block requested", ("GuardianUserId", guardianUserId), ("RequestId", requestId));

            var procedure = new UspBlockSoccerAgent(this) { GuardianUserId = guardianUserId, RequestId = requestId };
            var queryResult = await procedure.QueryAsync<SoccerAgentBlockRecord>(cancellation: cancellation);
            if (queryResult.IsError)
            {
                return Result<bool>.Error(ErrorCode.DatabaseError, "BlockAgent");
            }

            return Result<bool>.Success(queryResult.Values1.Count > 0);
        }

        private static AgentViewRequestResponse Map(
            SoccerAgentViewRequestRecord request, SoccerAgentProfilesEntity? agent, List<SoccerAgentViewLogsEntity> logs)
        {
            return new AgentViewRequestResponse
            {
                RequestId = request.RequestId,
                Status = request.Status,
                Message = request.Message,
                RequestedAt = request.RequestedAt,
                ExpiresAt = request.ExpiresAt,
                // 만료 판정은 여기 한 곳 — 권한 뷰 접근 차단(후속)도 이 기준을 쓴다
                IsExpired = request.Status == "Approved" && request.ExpiresAt is not null
                            && request.ExpiresAt.Value <= DateTime.UtcNow,
                PlayerId = request.PlayerId,
                PlayerName = request.Name,
                PlayerAgeGroup = NullIfEmpty(request.AgeGroup),
                PlayerPosition = NullIfEmpty(request.Position),
                Agent = agent is null ? new AgentProfileDto() : new AgentProfileDto
                {
                    Name = agent.Name,
                    AgencyName = NullIfEmpty(agent.AgencyName),
                    RegisteredYear = agent.RegisteredYear,
                    IsVerified = agent.IsVerified,
                    BrokerageCount = agent.BrokerageCount,
                    Rating = agent.Rating,
                    ActiveRegions = NullIfEmpty(agent.ActiveRegions)
                },
                Logs = logs
                    .Select(l => new AgentViewLogDto { EventType = l.EventType, CreatedAt = l.CreatedAt })
                    .ToList()
            };
        }

        private static string? NullIfEmpty(string? value)
        {
            return string.IsNullOrEmpty(value) ? null : value;
        }
    }
}
