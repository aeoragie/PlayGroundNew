using Microsoft.Extensions.Options;
using PlayGround.Domain.Soccer;
using PlayGround.Shared.Extensions;
using PlayGround.Application.Interfaces;
using PlayGround.Contracts.Agent;
using PlayGround.Infrastructure.Database;
using PlayGround.Infrastructure.Database.Base;
using PlayGround.Persistence.Database.Generated.Soccer.Entities;
using PlayGround.Persistence.Database.Generated.Soccer.Procedures;
using PlayGround.Shared.Result;
using PlayGround.Shared.Time;

namespace PlayGround.Persistence.Repositories
{
    public class SoccerAgentApprovalRepository : RepositoryBase, IAgentApprovalRepository
    {
        public override DatabaseTypes Database => DatabaseTypes.Soccer;

        public SoccerAgentApprovalRepository(IOptions<DatabaseConfiguration> options) : base(options)
        {
        }

        public async Task<Result<AgentViewRequestResponse?>> GetRequestAsync(
            Guid guardianUserId, Guid requestId, CancellationToken cancellation = default)
        {
            var procedure = new UspGetSoccerAgentViewRequest(this) { GuardianUserId = guardianUserId, RequestId = requestId };
            Result<MultiQueryReader> opened = await ProcedureMultipleAsync(procedure, cancellation: cancellation);
            if (opened.IsError)
            {
                return Result<AgentViewRequestResponse?>.Error(ErrorCode.DatabaseError);
            }

            using MultiQueryReader reader = opened.Value;
            SoccerAgentViewRequestRecord? request = await reader.ReadSingleOrDefaultAsync<SoccerAgentViewRequestRecord>();
            if (request is null)
            {
                return Result<AgentViewRequestResponse?>.Success(null);
            }

            SoccerAgentProfilesEntity? agent = await reader.ReadSingleOrDefaultAsync<SoccerAgentProfilesEntity>();
            var logs = (await reader.ReadAsync<SoccerAgentViewLogsEntity>()).ToList();

            return Result<AgentViewRequestResponse?>.Success(Map(request, agent, logs));
        }

        public async Task<Result<AgentViewRequestResponse?>> ReviewAsync(
            Guid guardianUserId, Guid requestId, string action, CancellationToken cancellation = default)
        {
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
                return Result<AgentViewRequestResponse?>.Success(null);
            }

            return Result<AgentViewRequestResponse?>.Success(Map(row, agent: null, logs: new List<SoccerAgentViewLogsEntity>()));
        }

        public async Task<Result<bool>> BlockAgentAsync(Guid guardianUserId, Guid requestId, CancellationToken cancellation = default)
        {
            var procedure = new UspBlockSoccerAgent(this) { GuardianUserId = guardianUserId, RequestId = requestId };
            var queryResult = await procedure.QueryAsync<SoccerAgentBlockRecord>(cancellation: cancellation);
            if (queryResult.IsError)
            {
                return Result<bool>.Error(ErrorCode.DatabaseError, "BlockAgent");
            }

            return Result<bool>.Success(queryResult.Values1.Count > 0);
        }

        public async Task<Result<AgentRequestEligibilityResponse>> GetEligibilityAsync(
            Guid requesterUserId, Guid playerId, Guid guardianUserId, CancellationToken cancellation = default)
        {
            var procedure = new UspGetSoccerAgentRequestEligibility(this)
            {
                RequesterUserId = requesterUserId,
                PlayerId = playerId,
                GuardianUserId = guardianUserId
            };
            Result<MultiQueryReader> opened = await ProcedureMultipleAsync(procedure, cancellation: cancellation);
            if (opened.IsError)
            {
                return Result<AgentRequestEligibilityResponse>.Error(ErrorCode.DatabaseError, "GetAgentEligibility");
            }

            using MultiQueryReader reader = opened.Value;
            string status = await reader.ReadSingleOrDefaultAsync<string>() ?? "NotAgent";
            SystemTime? cooldownUntil = await reader.ReadSingleOrDefaultAsync<SystemTime?>();

            return Result<AgentRequestEligibilityResponse>.Success(new AgentRequestEligibilityResponse
            {
                Status = EnumColumn.Read<SoccerAgentEligibility>(status),
                CooldownUntil = cooldownUntil
            });
        }

        private static AgentViewRequestResponse Map(
            SoccerAgentViewRequestRecord request, SoccerAgentProfilesEntity? agent, List<SoccerAgentViewLogsEntity> logs)
        {
            return new AgentViewRequestResponse
            {
                RequestId = request.RequestId,
                Status = EnumColumn.Read<SoccerAgentRequestStatus>(request.Status),
                Message = request.Message,
                RequestedAt = request.RequestedAt,
                ExpiresAt = request.ExpiresAt,
                // 만료 판정은 여기 한 곳 — 권한 뷰 접근 차단(후속)도 이 기준을 쓴다
                IsExpired = request.Status == "Approved" && request.ExpiresAt is not null
                            && request.ExpiresAt.Value <= SystemTime.Now,
                PlayerId = request.PlayerId,
                PlayerName = request.Name,
                PlayerAgeGroup = EnumColumn.Read<SoccerAgeGroup>(request.AgeGroup),
                PlayerPosition = EnumColumn.Read<SoccerPosition>(request.Position),
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
                    .Select(l => new AgentViewLogDto { EventType = EnumColumn.Read<SoccerAgentViewEvent>(l.EventType), CreatedAt = l.CreatedAt })
                    .ToList()
            };
        }

        private static string? NullIfEmpty(string? value)
        {
            return string.IsNullOrEmpty(value) ? null : value;
        }
    }
}
