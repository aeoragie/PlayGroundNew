using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using PlayGround.Shared.Result;
using PlayGround.Contracts.Team;
using PlayGround.Application.Auth.Models;
using PlayGround.Application.Interfaces;
using PlayGround.Application.Team.Models;

namespace PlayGround.Application.Team.Commands
{
    /// <summary>팀 온보딩 생성 유즈케이스. 검증·슬러그 생성 후 포트로 팀+로스터 저장.</summary>
    public class SoccerTeamCommand
    {
        private static readonly string[] AllowedTeamTypes = ["클럽", "학교", "학원"];

        private readonly ISoccerTeamRepository mRepository;
        private readonly IAccountRepository mAccountRepository;
        private readonly IJwtTokenService mTokenService;

        public SoccerTeamCommand(ISoccerTeamRepository repository, IAccountRepository accountRepository, IJwtTokenService tokenService)
        {
            Debug.Assert(repository != null, "repository is required");
            Debug.Assert(accountRepository != null, "accountRepository is required");
            Debug.Assert(tokenService != null, "tokenService is required");
            mRepository = repository ?? throw new ArgumentNullException(nameof(repository));
            mAccountRepository = accountRepository ?? throw new ArgumentNullException(nameof(accountRepository));
            mTokenService = tokenService ?? throw new ArgumentNullException(nameof(tokenService));
        }

        public async Task<Result<CreateTeamResponse>> ExecuteAsync(
            Guid managerUserId, CreateTeamRequest request, CancellationToken cancellation = default)
        {
            Debug.Assert(request != null, "request is required");
            if (request is null)
            {
                return Result<CreateTeamResponse>.Error(ErrorCode.InvalidInput, "request is null");
            }

            if (managerUserId == Guid.Empty)
            {
                return Result<CreateTeamResponse>.Error(ErrorCode.Unauthorized, "managerUserId is empty");
            }

            if (string.IsNullOrWhiteSpace(request.TeamName))
            {
                return Result<CreateTeamResponse>.Error(ErrorCode.MissingRequired, "TeamName is required");
            }

            string? teamType = NormalizeTeamType(request.TeamType);
            if (request.TeamType is not null && teamType is null)
            {
                return Result<CreateTeamResponse>.Error(ErrorCode.OutOfRange, "TeamType must be 클럽/학교/학원");
            }

            List<RosterEntryDto> roster = (request.Roster ?? new())
                .Where(r => !string.IsNullOrWhiteSpace(r.Name))
                .Select(r => new RosterEntryDto
                {
                    Name = r.Name.Trim(),
                    Position = string.IsNullOrWhiteSpace(r.Position) ? null : r.Position.Trim(),
                    Number = string.IsNullOrWhiteSpace(r.Number) ? null : r.Number.Trim()
                })
                .ToList();

            var input = new CreateTeamInput
            {
                ManagerUserId = managerUserId,
                TeamName = request.TeamName.Trim(),
                TeamType = teamType,
                Region = string.IsNullOrWhiteSpace(request.Region) ? null : request.Region.Trim(),
                Slug = ToSlug(request.TeamName),
                Roster = roster
            };

            Result<string> created = await mRepository.CreateWithRosterAsync(input, cancellation);
            if (created.IsError)
            {
                return Result<CreateTeamResponse>.Failure(created.ResultData);
            }

            // 온보딩 완료 → 역할 승격 + 승격된 역할로 JWT 재발급 (재로그인 없이 /dashboard 분기가 맞도록).
            // 실패해도 팀은 생성됐으므로 비치명적 — 토큰 없이 반환하면 기존 토큰이 유지된다.
            Result<AccountUser> promoted = await mAccountRepository.UpdateRoleAsync(managerUserId, "TeamAdmin", cancellation);

            string? accessToken = null;
            if (promoted.IsSuccess)
            {
                AccountUser user = promoted.Value;
                accessToken = mTokenService.GenerateAccessToken(
                    user.UserId, user.Email, user.DisplayName, user.UserRole, user.ProfileImageUrl);
            }

            return Result<CreateTeamResponse>.Success(new CreateTeamResponse
            {
                Slug = created.Value,
                PlayerCount = roster.Count,
                AccessToken = accessToken
            });
        }

        private static string? NormalizeTeamType(string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return null;
            }

            string trimmed = value.Trim();
            return Array.Exists(AllowedTeamTypes, a => a == trimmed) ? trimmed : null;
        }

        // 공백·URL 위험문자 제거, 한글·영숫자만 유지(영문 소문자화). 중복 처리는 프로시저가 담당.
        private static string ToSlug(string teamName)
        {
            var sb = new StringBuilder();
            foreach (char c in teamName.Trim().ToLowerInvariant())
            {
                if (char.IsLetterOrDigit(c))
                {
                    sb.Append(c);
                }
            }

            string slug = sb.ToString();
            if (slug.Length > 90)
            {
                slug = slug[..90];
            }

            return slug.Length == 0 ? "team" : slug;
        }
    }
}
