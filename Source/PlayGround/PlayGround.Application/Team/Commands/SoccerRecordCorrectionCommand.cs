using System.Diagnostics;
using PlayGround.Shared.Result;
using PlayGround.Contracts.Team;
using PlayGround.Domain.Soccer;
using PlayGround.Application.Interfaces;

namespace PlayGround.Application.Team.Commands
{
    /// <summary>
    /// 공식 기록 수정 신청 유즈케이스 — **생성·조회·취소만** 한다.
    ///
    /// 심사(Accepted/Rejected 전환·반려 사유 기록)는 주최측 대회 운영 서비스의 몫이고 DB를 공유한다
    /// (설계 결정 6·7). **이 클래스에 승인/반려 메서드를 추가하면 "공식 기록의 주체는 주최측"이라는
    /// 결정이 무너진다** — 필요해 보이면 대회 운영 서비스 쪽에 만들어야 한다.
    /// </summary>
    public class SoccerRecordCorrectionCommand
    {
        /// <summary>요청값·설명 길이 상한 — 컬럼 크기(한글 기준)와 맞춘다.</summary>
        private const int MaxValueLength = 100;
        private const int MaxDescriptionLength = 500;

        private readonly ISoccerTeamRepository mRepository;

        public SoccerRecordCorrectionCommand(ISoccerTeamRepository repository)
        {
            Debug.Assert(repository != null, "repository is required");
            mRepository = repository ?? throw new ArgumentNullException(nameof(repository));
        }

        public async Task<Result<Guid>> ExecuteAsync(
            Guid managerUserId, CreateRecordCorrectionRequest request, CancellationToken cancellation = default)
        {
            if (managerUserId == Guid.Empty)
            {
                return Result<Guid>.Error(ErrorCode.Unauthorized, "managerUserId is empty");
            }

            ArgumentNullException.ThrowIfNull(request);

            if (request.MatchId == Guid.Empty)
            {
                return Result<Guid>.Error(ErrorCode.InvalidInput, "matchId is empty");
            }

            if (!SoccerCorrectionFieldExtensions.TryParse(request.FieldType, out SoccerCorrectionField field))
            {
                return Result<Guid>.Error(ErrorCode.InvalidInput, "unknown field type");
            }

            request.FieldType = field.ToString();
            request.RequestedValue = request.RequestedValue?.Trim() ?? string.Empty;

            if (request.RequestedValue.Length == 0)
            {
                return Result<Guid>.Error(ErrorCode.InvalidInput, "requested value is required");
            }

            if (request.RequestedValue.Length > MaxValueLength)
            {
                return Result<Guid>.Error(ErrorCode.InvalidInput, "requested value is too long");
            }

            request.CurrentValue = Trimmed(request.CurrentValue, MaxValueLength);
            request.Description = Trimmed(request.Description, MaxDescriptionLength);

            Result<Guid?> created = await mRepository.CreateRecordCorrectionAsync(managerUserId, request, cancellation);
            if (created.IsError)
            {
                return Result<Guid>.Error(ErrorCode.DatabaseError);
            }

            // 남의 경기 · 친선경기 · 중복 신청 — 어느 쪽인지 알려주지 않는다
            if (created.Value is null)
            {
                return Result<Guid>.Error(ErrorCode.Forbidden, "correction not permitted for match");
            }

            return Result<Guid>.Success(created.Value.Value);
        }

        public async Task<Result<RecordCorrectionsResponse>> GetAsync(
            Guid managerUserId, CancellationToken cancellation = default)
        {
            if (managerUserId == Guid.Empty)
            {
                return Result<RecordCorrectionsResponse>.Error(ErrorCode.Unauthorized, "managerUserId is empty");
            }

            return await mRepository.GetRecordCorrectionsByManagerAsync(managerUserId, cancellation);
        }

        /// <summary>신청 취소 — 접수(Pending) 상태의 내 신청만. 심사가 끝난 건은 손대지 않는다.</summary>
        public async Task<Result<bool>> CancelAsync(
            Guid managerUserId, Guid correctionId, CancellationToken cancellation = default)
        {
            if (managerUserId == Guid.Empty)
            {
                return Result<bool>.Error(ErrorCode.Unauthorized, "managerUserId is empty");
            }

            if (correctionId == Guid.Empty)
            {
                return Result<bool>.Error(ErrorCode.InvalidInput, "correctionId is empty");
            }

            Result<bool> canceled = await mRepository.CancelRecordCorrectionAsync(managerUserId, correctionId, cancellation);
            if (canceled.IsError)
            {
                return canceled;
            }

            if (!canceled.Value)
            {
                return Result<bool>.Error(ErrorCode.Forbidden, "correction cancel not permitted");
            }

            return Result<bool>.Success(true);
        }

        private static string? Trimmed(string? value, int maxLength)
        {
            string? trimmed = value?.Trim();
            if (string.IsNullOrEmpty(trimmed))
            {
                return null;
            }

            return trimmed.Length > maxLength ? trimmed[..maxLength] : trimmed;
        }
    }
}
