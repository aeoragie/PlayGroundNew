using Microsoft.Extensions.Logging;
using PlayGround.Application.Interfaces;
using PlayGround.Contracts.Claim;
using PlayGround.Domain.Soccer;
using PlayGround.Shared.Logging;
using PlayGround.Shared.Result;
using System.Diagnostics;

namespace PlayGround.Application.Claim.Commands
{
    /// <summary>보호자 Claim 4스텝 유즈케이스 — 코드 조회(①→②) · 요청 생성(②→③) · 재방문 복원.
    /// 무효 코드는 사유를 구분하지 않고 NotFound (코드 추측 대비 — 기존 Claim 규약).</summary>
    public class SoccerClaimFlowCommand
    {
        private readonly IClaimRepository mRepository;
        private readonly ILogger<SoccerClaimFlowCommand> mLogger;

        public SoccerClaimFlowCommand(IClaimRepository repository, ILogger<SoccerClaimFlowCommand> logger)
        {
            Debug.Assert(repository != null, "repository is required");
            mRepository = repository ?? throw new ArgumentNullException(nameof(repository));
            mLogger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<Result<ClaimInviteCardResponse>> LookupAsync(string code, CancellationToken cancellation = default) =>
            (await LookupCoreAsync(code, cancellation)).LogWith(mLogger, "Lookup", ("Code", code));

        private async Task<Result<ClaimInviteCardResponse>> LookupCoreAsync(string code, CancellationToken cancellation = default)
        {
            string? normalized = NormalizeCode(code);
            if (normalized is null)
            {
                return Result<ClaimInviteCardResponse>.Error(ErrorCode.InvalidInput, "invalid code format");
            }

            Result<ClaimInviteCardResponse?> card = await mRepository.GetInviteCardAsync(normalized, cancellation);
            if (card.IsError)
            {
                return Result<ClaimInviteCardResponse>.Failure(card.ResultData);
            }

            if (card.Value is null)
            {
                return Result<ClaimInviteCardResponse>.Error(ErrorCode.NotFound, "invite code is not valid");
            }

            return Result<ClaimInviteCardResponse>.Success(card.Value);
        }

        public async Task<Result<ClaimRequestSummaryResponse>> CreateAsync(
            Guid userId, string requesterName, CreateClaimRequestRequest request, CancellationToken cancellation = default) =>
            (await CreateCoreAsync(userId, requesterName, request, cancellation)).LogWith(mLogger, "Create", ("UserId", userId));

        private async Task<Result<ClaimRequestSummaryResponse>> CreateCoreAsync(
            Guid userId, string requesterName, CreateClaimRequestRequest request, CancellationToken cancellation = default)
        {
            if (userId == Guid.Empty || request is null)
            {
                return Result<ClaimRequestSummaryResponse>.Error(ErrorCode.Unauthorized, "userId/request required");
            }

            string? normalized = NormalizeCode(request.Code);
            if (normalized is null)
            {
                return Result<ClaimRequestSummaryResponse>.Error(ErrorCode.InvalidInput, "invalid code format");
            }

            if (request.Relation == SoccerClaimRelation.Unknown)
            {
                return Result<ClaimRequestSummaryResponse>.Error(ErrorCode.InvalidInput, "unknown relation");
            }

            string name = string.IsNullOrWhiteSpace(requesterName) ? "보호자" : requesterName.Trim();
            Result<ClaimRequestSummaryResponse?> created =
                await mRepository.CreateRequestAsync(userId, name, normalized, request.Relation.ToString(), cancellation);
            if (created.IsError)
            {
                return Result<ClaimRequestSummaryResponse>.Failure(created.ResultData);
            }

            if (created.Value is null)
            {
                return Result<ClaimRequestSummaryResponse>.Error(ErrorCode.NotFound, "invite code is not valid");
            }

            return Result<ClaimRequestSummaryResponse>.Success(created.Value);
        }

        public async Task<Result<ClaimInviteCardResponse>> LookupBySlugAsync(string slug, CancellationToken cancellation = default) =>
            (await LookupBySlugCoreAsync(slug, cancellation)).LogWith(mLogger, "LookupBySlug", ("Slug", slug));

        private async Task<Result<ClaimInviteCardResponse>> LookupBySlugCoreAsync(string slug, CancellationToken cancellation = default)
        {
            if (string.IsNullOrWhiteSpace(slug))
            {
                return Result<ClaimInviteCardResponse>.Error(ErrorCode.InvalidInput, "slug is required");
            }

            Result<ClaimInviteCardResponse?> card = await mRepository.GetClaimCardBySlugAsync(slug.Trim(), cancellation);
            if (card.IsError)
            {
                return Result<ClaimInviteCardResponse>.Failure(card.ResultData);
            }

            if (card.Value is null)
            {
                // 미연결 선수가 아니거나(이미 연결됨) 없음 — 사유 구분 없이 NotFound
                return Result<ClaimInviteCardResponse>.Error(ErrorCode.NotFound, "player not claimable");
            }

            return Result<ClaimInviteCardResponse>.Success(card.Value);
        }

        /// <summary>공개 선수 프로필 경유(코드 없음): PlayerId + 관계로 연결 요청 생성.</summary>
        public async Task<Result<ClaimRequestSummaryResponse>> CreateByPlayerAsync(
            Guid userId, string requesterName, Guid playerId, SoccerClaimRelation relation, CancellationToken cancellation = default) =>
            (await CreateByPlayerCoreAsync(userId, requesterName, playerId, relation, cancellation)).LogWith(mLogger, "CreateByPlayer", ("UserId", userId));

        private async Task<Result<ClaimRequestSummaryResponse>> CreateByPlayerCoreAsync(
            Guid userId, string requesterName, Guid playerId, SoccerClaimRelation relation, CancellationToken cancellation = default)
        {
            if (userId == Guid.Empty || playerId == Guid.Empty)
            {
                return Result<ClaimRequestSummaryResponse>.Error(ErrorCode.Unauthorized, "userId/playerId required");
            }

            if (relation == SoccerClaimRelation.Unknown)
            {
                return Result<ClaimRequestSummaryResponse>.Error(ErrorCode.InvalidInput, "unknown relation");
            }

            string name = string.IsNullOrWhiteSpace(requesterName) ? "보호자" : requesterName.Trim();
            Result<ClaimRequestSummaryResponse?> created =
                await mRepository.CreateRequestByPlayerAsync(userId, name, playerId, relation.ToString(), cancellation);
            if (created.IsError)
            {
                return Result<ClaimRequestSummaryResponse>.Failure(created.ResultData);
            }

            if (created.Value is null)
            {
                return Result<ClaimRequestSummaryResponse>.Error(ErrorCode.NotFound, "player not claimable");
            }

            return Result<ClaimRequestSummaryResponse>.Success(created.Value);
        }

        /// <summary>연결 요청 취소 — 본인의 Pending 요청만. 대기 화면에서 철회(Design.ClaimFlow P1).</summary>
        public async Task<Result<bool>> CancelAsync(Guid userId, Guid requestId, CancellationToken cancellation = default) =>
            (await CancelCoreAsync(userId, requestId, cancellation)).LogWith(mLogger, "Cancel", ("UserId", userId));

        private async Task<Result<bool>> CancelCoreAsync(Guid userId, Guid requestId, CancellationToken cancellation = default)
        {
            if (userId == Guid.Empty || requestId == Guid.Empty)
            {
                return Result<bool>.Error(ErrorCode.InvalidInput, "userId/requestId required");
            }

            Result<bool> canceled = await mRepository.CancelRequestAsync(userId, requestId, cancellation);
            if (canceled.IsError)
            {
                return canceled;
            }

            mLogger.Info("Claim request cancelled", ("UserId", userId));

            if (!canceled.Value)
            {
                return Result<bool>.Error(ErrorCode.Forbidden, "claim request cancel not permitted");
            }

            return Result<bool>.Success(true);
        }

        /// <summary>재방문 복원 — 요청이 없으면 NotFound (클라이언트는 스텝 ①부터).</summary>
        public async Task<Result<ClaimRequestSummaryResponse>> GetMineAsync(Guid userId, CancellationToken cancellation = default) =>
            (await GetMineCoreAsync(userId, cancellation)).LogWith(mLogger, "GetMine", ("UserId", userId));

        private async Task<Result<ClaimRequestSummaryResponse>> GetMineCoreAsync(Guid userId, CancellationToken cancellation = default)
        {
            if (userId == Guid.Empty)
            {
                return Result<ClaimRequestSummaryResponse>.Error(ErrorCode.Unauthorized, "userId is empty");
            }

            Result<ClaimRequestSummaryResponse?> own = await mRepository.GetOwnRequestAsync(userId, cancellation);
            if (own.IsError)
            {
                return Result<ClaimRequestSummaryResponse>.Failure(own.ResultData);
            }

            if (own.Value is null)
            {
                return Result<ClaimRequestSummaryResponse>.Error(ErrorCode.NotFound, "no claim request");
            }

            return Result<ClaimRequestSummaryResponse>.Success(own.Value);
        }

        private static string? NormalizeCode(string? code)
        {
            string normalized = code?.Trim().ToUpperInvariant() ?? string.Empty;
            return normalized.Length is < 4 or > 12 ? null : normalized;
        }
    }
}
