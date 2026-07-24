using Microsoft.Extensions.Options;
using PlayGround.Shared.Result;
using PlayGround.Infrastructure.Database;
using PlayGround.Infrastructure.Database.Base;
using PlayGround.Infrastructure.Logging;
using PlayGround.Contracts.Claim;
using PlayGround.Application.Interfaces;
using PlayGround.Persistence.Database.Generated.Soccer.Entities;
using PlayGround.Persistence.Database.Generated.Soccer.Procedures;

namespace PlayGround.Persistence.Repositories
{
    /// <summary>Claim 플로우(연결 요청) 저장소 (Soccer DB). 거부·무효는 빈 결과 → Success(null).</summary>
    public class SoccerClaimRepository : RepositoryBase, IClaimRepository
    {
        public override DatabaseTypes Database => DatabaseTypes.Soccer;

        public SoccerClaimRepository(IOptions<DatabaseConfiguration> options) : base(options)
        {
        }

        public async Task<Result<ClaimInviteCardResponse?>> GetInviteCardAsync(string code, CancellationToken cancellation = default)
        {
            Logger.InfoWith("Claim invite card requested");

            var procedure = new UspGetSoccerPlayerInviteForClaim(this) { Code = code };
            var queryResult = await procedure.QueryAsync<SoccerClaimInviteCardRecord>(cancellation: cancellation);
            if (queryResult.IsError)
            {
                return Result<ClaimInviteCardResponse?>.Error(ErrorCode.DatabaseError, "GetInviteCard");
            }

            SoccerClaimInviteCardRecord? row = queryResult.Values1.FirstOrDefault();
            if (row is null)
            {
                return Result<ClaimInviteCardResponse?>.Success(null);
            }

            return Result<ClaimInviteCardResponse?>.Success(new ClaimInviteCardResponse
            {
                PlayerId = row.PlayerId,
                Name = row.Name,
                Position = NullIfEmpty(row.Position),
                JerseyNumber = NullIfEmpty(row.JerseyNumber),
                BirthYear = row.BirthDate?.Year,
                AgeGroup = NullIfEmpty(row.AgeGroup),
                TeamName = row.TeamName
            });
        }

        public async Task<Result<ClaimRequestSummaryResponse?>> CreateRequestAsync(
            Guid userId, string requesterName, string code, string relation, CancellationToken cancellation = default)
        {
            Logger.InfoWith("Claim request creation requested", ("UserId", userId), ("Relation", relation));

            var procedure = new UspCreateSoccerPlayerClaimRequest(this)
            {
                UserId = userId,
                RequesterName = requesterName,
                Code = code,
                Relation = relation
            };
            var queryResult = await procedure.QueryAsync<SoccerClaimRequestOwnRecord>(cancellation: cancellation);
            if (queryResult.IsError)
            {
                return Result<ClaimRequestSummaryResponse?>.Error(ErrorCode.DatabaseError, "CreateClaimRequest");
            }

            SoccerClaimRequestOwnRecord? row = queryResult.Values1.FirstOrDefault();
            if (row is not null)
            {
                Logger.InfoWith("Claim request ready", ("RequestId", row.RequestId), ("Status", row.Status));
            }

            return Result<ClaimRequestSummaryResponse?>.Success(row is null ? null : Map(row));
        }

        public async Task<Result<ClaimRequestSummaryResponse?>> GetOwnRequestAsync(Guid userId, CancellationToken cancellation = default)
        {
            Logger.InfoWith("Own claim request requested", ("UserId", userId));

            var procedure = new UspGetSoccerPlayerClaimRequestByUser(this) { UserId = userId };
            var queryResult = await procedure.QueryAsync<SoccerClaimRequestOwnRecord>(cancellation: cancellation);
            if (queryResult.IsError)
            {
                return Result<ClaimRequestSummaryResponse?>.Error(ErrorCode.DatabaseError, "GetOwnClaimRequest");
            }

            SoccerClaimRequestOwnRecord? row = queryResult.Values1.FirstOrDefault();
            return Result<ClaimRequestSummaryResponse?>.Success(row is null ? null : Map(row));
        }

        public async Task<Result<List<PendingChildClaimDto>>> GetPendingChildClaimsAsync(
            Guid userId, CancellationToken cancellation = default)
        {
            Logger.InfoWith("Pending child claims requested", ("UserId", userId));

            var procedure = new UspGetSoccerPendingChildClaimsByUser(this) { UserId = userId };
            var queryResult = await procedure.QueryAsync<SoccerPendingChildClaimRecord>(cancellation: cancellation);
            if (queryResult.IsError)
            {
                return Result<List<PendingChildClaimDto>>.Error(ErrorCode.DatabaseError, "GetPendingChildClaims");
            }

            var list = queryResult.Values1
                .Select(r => new PendingChildClaimDto
                {
                    PlayerId = r.PlayerId,
                    Name = r.Name,
                    AgeGroup = string.IsNullOrEmpty(r.AgeGroup) ? null : r.AgeGroup,
                    TeamName = r.TeamName,
                    RequestedAt = r.CreatedAt
                })
                .ToList();

            return Result<List<PendingChildClaimDto>>.Success(list);
        }

        public async Task<Result<ReviewClaimResponse?>> ReviewAsync(
            Guid managerUserId, Guid requestId, bool approve, CancellationToken cancellation = default)
        {
            Logger.InfoWith("Claim request review requested",
                ("ManagerUserId", managerUserId), ("RequestId", requestId), ("Approve", approve));

            var procedure = new UspReviewSoccerPlayerClaimRequest(this)
            {
                ManagerUserId = managerUserId,
                RequestId = requestId,
                Approve = approve
            };
            var queryResult = await procedure.QueryAsync<SoccerClaimReviewRecord>(cancellation: cancellation);
            if (queryResult.IsError)
            {
                return Result<ReviewClaimResponse?>.Error(ErrorCode.DatabaseError, "ReviewClaimRequest");
            }

            SoccerClaimReviewRecord? row = queryResult.Values1.FirstOrDefault();
            if (row is null)
            {
                Logger.InfoWith("Claim request review denied or not found", ("RequestId", requestId));
                return Result<ReviewClaimResponse?>.Success(null);
            }

            Logger.InfoWith("Claim request reviewed", ("RequestId", row.RequestId), ("Status", row.Status));
            return Result<ReviewClaimResponse?>.Success(new ReviewClaimResponse
            {
                RequestId = row.RequestId,
                Status = row.Status,
                PlayerName = row.Name
            });
        }

        private static ClaimRequestSummaryResponse Map(SoccerClaimRequestOwnRecord row)
        {
            return new ClaimRequestSummaryResponse
            {
                RequestId = row.RequestId,
                Status = row.Status,
                Relation = row.Relation,
                PlayerName = row.Name,
                TeamName = row.TeamName,
                RequestedAt = row.CreatedAt
            };
        }

        private static string? NullIfEmpty(string value)
        {
            return string.IsNullOrEmpty(value) ? null : value;
        }
    }
}
