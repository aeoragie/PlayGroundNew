using System.Diagnostics;
using PlayGround.Shared.Result;
using PlayGround.Contracts.Claim;
using PlayGround.Domain.Soccer;
using PlayGround.Application.Interfaces;

namespace PlayGround.Application.Claim.Commands
{
    /// <summary>보호자 Claim 4스텝 유즈케이스 — 코드 조회(①→②) · 요청 생성(②→③) · 재방문 복원.
    /// 무효 코드는 사유를 구분하지 않고 NotFound (코드 추측 대비 — 기존 Claim 규약).</summary>
    public class SoccerClaimFlowCommand
    {
        private readonly IClaimRepository mRepository;

        public SoccerClaimFlowCommand(IClaimRepository repository)
        {
            Debug.Assert(repository != null, "repository is required");
            mRepository = repository ?? throw new ArgumentNullException(nameof(repository));
        }

        public async Task<Result<ClaimInviteCardResponse>> LookupAsync(string code, CancellationToken cancellation = default)
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

            // 관계는 enum 화이트리스트 (이름 형태만 — 숫자 문자열 거부)
            if (string.IsNullOrWhiteSpace(request.Relation)
                || char.IsAsciiDigit(request.Relation[0])
                || !Enum.TryParse(request.Relation, out SoccerClaimRelation relation))
            {
                return Result<ClaimRequestSummaryResponse>.Error(ErrorCode.InvalidInput, "unknown relation");
            }

            string name = string.IsNullOrWhiteSpace(requesterName) ? "보호자" : requesterName.Trim();
            Result<ClaimRequestSummaryResponse?> created =
                await mRepository.CreateRequestAsync(userId, name, normalized, relation.ToString(), cancellation);
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

        /// <summary>재방문 복원 — 요청이 없으면 NotFound (클라이언트는 스텝 ①부터).</summary>
        public async Task<Result<ClaimRequestSummaryResponse>> GetMineAsync(Guid userId, CancellationToken cancellation = default)
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
