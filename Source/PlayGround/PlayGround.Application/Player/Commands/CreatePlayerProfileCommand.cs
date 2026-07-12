using System.Diagnostics;
using System.Globalization;
using PlayGround.Shared.Result;
using PlayGround.Contracts.Player;
using PlayGround.Application.Interfaces;
using PlayGround.Application.Player.Models;

namespace PlayGround.Application.Player.Commands
{
    /// <summary>선수 온보딩 프로필 생성 유즈케이스. 입력 검증·정규화 후 포트로 저장.</summary>
    public class CreatePlayerProfileCommand
    {
        private static readonly string[] AllowedAgeGroups = ["U12", "U15", "U18"];

        private readonly IPlayerRepository Repository;

        public CreatePlayerProfileCommand(IPlayerRepository repository)
        {
            Debug.Assert(repository != null, "repository is required");
            Repository = repository ?? throw new ArgumentNullException(nameof(repository));
        }

        public async Task<Result<CreatePlayerProfileResponse>> ExecuteAsync(
            Guid userId, CreatePlayerProfileRequest request, CancellationToken cancellation = default)
        {
            Debug.Assert(request != null, "request is required");
            if (request is null)
            {
                return Result<CreatePlayerProfileResponse>.Error(ErrorCode.InvalidInput, "request is null");
            }

            if (userId == Guid.Empty)
            {
                return Result<CreatePlayerProfileResponse>.Error(ErrorCode.Unauthorized, "userId is empty");
            }

            if (string.IsNullOrWhiteSpace(request.Name))
            {
                return Result<CreatePlayerProfileResponse>.Error(ErrorCode.MissingRequired, "Name is required");
            }

            DateOnly? birthDate = null;
            if (!string.IsNullOrWhiteSpace(request.BirthDate))
            {
                if (!DateOnly.TryParseExact(request.BirthDate.Trim(), "yyyy.MM.dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out DateOnly parsed))
                {
                    return Result<CreatePlayerProfileResponse>.Error(ErrorCode.InvalidDateFormat, "BirthDate must be YYYY.MM.DD");
                }
                birthDate = parsed;
            }

            string? ageGroup = NormalizeAgeGroup(request.AgeGroup);
            if (request.AgeGroup is not null && ageGroup is null)
            {
                return Result<CreatePlayerProfileResponse>.Error(ErrorCode.OutOfRange, "AgeGroup must be U12/U15/U18");
            }

            var input = new CreatePlayerInput
            {
                UserId = userId,
                Name = request.Name.Trim(),
                BirthDate = birthDate,
                AgeGroup = ageGroup,
                Region = string.IsNullOrWhiteSpace(request.Region) ? null : request.Region.Trim()
            };

            Result<Guid> created = await Repository.CreateAsync(input, cancellation);
            if (created.IsError)
            {
                return Result<CreatePlayerProfileResponse>.Failure(created.ResultData);
            }

            return Result<CreatePlayerProfileResponse>.Success(new CreatePlayerProfileResponse { PlayerId = created.Value });
        }

        private static string? NormalizeAgeGroup(string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return null;
            }

            string upper = value.Trim().ToUpperInvariant();
            return Array.Exists(AllowedAgeGroups, a => a == upper) ? upper : null;
        }
    }
}
