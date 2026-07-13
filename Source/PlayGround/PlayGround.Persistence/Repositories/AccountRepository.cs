using Microsoft.Extensions.Options;
using PlayGround.Shared.Result;
using PlayGround.Infrastructure.Database;
using PlayGround.Infrastructure.Database.Base;
using PlayGround.Infrastructure.Logging;
using PlayGround.Application.Auth.Models;
using PlayGround.Application.Interfaces;
using PlayGround.Persistence.Database.Generated.Account.Entities;
using PlayGround.Persistence.Database.Generated.Account.Procedures;

namespace PlayGround.Persistence.Repositories
{
    /// <summary>계정 조회·생성 (Account DB). 생성된 프로시저 객체 + UserRecord 사용.</summary>
    public class AccountRepository : RepositoryBase, IAccountRepository
    {
        public override DatabaseTypes Database => DatabaseTypes.Account;

        public AccountRepository(IOptions<DatabaseConfiguration> options) : base(options)
        {
        }

        public async Task<Result<AccountUser?>> GetByEmailAsync(string email, CancellationToken cancellation = default)
        {
            Logger.InfoWith("User lookup by email requested");
            var procedure = new UspGetUserByEmail(this) { Email = email };
            return await SingleOrNullAsync(procedure.QueryAsync<UserRecord>(cancellation: cancellation), "GetUserByEmail");
        }

        public async Task<Result<AccountUser?>> GetBySocialAsync(string provider, string providerUserId, CancellationToken cancellation = default)
        {
            Logger.InfoWith("User lookup by social requested", ("Provider", provider));
            var procedure = new UspGetUserBySocial(this) { Provider = provider, ProviderUserId = providerUserId };
            return await SingleOrNullAsync(procedure.QueryAsync<UserRecord>(cancellation: cancellation), "GetUserBySocial");
        }

        public async Task<Result<AccountUser>> CreateByEmailAsync(string email, string passwordHash, string displayName, CancellationToken cancellation = default)
        {
            Logger.InfoWith("User creation by email requested");
            var procedure = new UspCreateUserByEmail(this)
            {
                Email = email,
                PasswordHash = passwordHash,
                DisplayName = displayName
            };
            return await CreatedAsync(procedure.QueryAsync<UserRecord>(cancellation: cancellation), "CreateUserByEmail");
        }

        public async Task<Result<AccountUser>> CreateWithSocialAsync(string email, string displayName, string provider, string providerUserId, string? profileImageUrl, CancellationToken cancellation = default)
        {
            Logger.InfoWith("User creation by social requested", ("Provider", provider));
            var procedure = new UspCreateUserWithSocial(this)
            {
                Email = email,
                DisplayName = displayName,
                Provider = provider,
                ProviderUserId = providerUserId,
                ProfileImageUrl = profileImageUrl!
            };
            return await CreatedAsync(procedure.QueryAsync<UserRecord>(cancellation: cancellation), "CreateUserWithSocial");
        }

        public async Task<Result<AccountUser>> UpdateRoleAsync(Guid userId, string role, CancellationToken cancellation = default)
        {
            Logger.InfoWith("User role update requested", ("UserId", userId), ("Role", role));

            var procedure = new UspUpdateUserRole(this) { UserId = userId, Role = role };
            return await CreatedAsync(procedure.QueryAsync<UserRecord>(cancellation: cancellation), "UpdateUserRole");
        }

        //.// 공통 처리 (빈 결과 = 미존재, DB 오류 = Error)

        private static async Task<Result<AccountUser?>> SingleOrNullAsync(Task<QueryResultList<UserRecord>> task, string operation)
        {
            var queryResult = await task;
            if (queryResult.IsError)
            {
                return Result<AccountUser?>.Error(ErrorCode.DatabaseError, operation);
            }

            var row = queryResult.Values1.FirstOrDefault();
            return Result<AccountUser?>.Success(row is null ? null : Map(row));
        }

        private static async Task<Result<AccountUser>> CreatedAsync(Task<QueryResultList<UserRecord>> task, string operation)
        {
            var queryResult = await task;
            if (queryResult.IsError)
            {
                return Result<AccountUser>.Error(ErrorCode.DatabaseError, operation);
            }

            var row = queryResult.Values1.FirstOrDefault();
            if (row is null)
            {
                return Result<AccountUser>.Error(ErrorCode.OperationFailed, $"{operation}: no row returned");
            }

            return Result<AccountUser>.Success(Map(row));
        }

        private static AccountUser Map(UserRecord r)
        {
            return new AccountUser
            {
                UserId = r.UserId,
                Email = r.Email,
                EmailConfirmed = r.EmailConfirmed,
                PasswordHash = string.IsNullOrEmpty(r.PasswordHash) ? null : r.PasswordHash,
                AuthProvider = r.AuthProvider,
                DisplayName = r.DisplayName,
                ProfileImageUrl = string.IsNullOrEmpty(r.ProfileImageUrl) ? null : r.ProfileImageUrl,
                UserRole = r.UserRole,
                UserStatus = r.UserStatus
            };
        }
    }
}
