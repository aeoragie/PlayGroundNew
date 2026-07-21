using Microsoft.Extensions.Options;
using PlayGround.Shared.Result;
using PlayGround.Infrastructure.Database;
using PlayGround.Infrastructure.Database.Base;
using PlayGround.Infrastructure.Logging;
using PlayGround.Contracts.Settings;
using PlayGround.Domain.Account;
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

        public async Task<Result<AccountSettingsResponse?>> GetSettingsAsync(Guid userId, CancellationToken cancellation = default)
        {
            Logger.InfoWith("Account settings requested", ("UserId", userId));

            var procedure = new UspGetUserSettings(this) { UserId = userId };
            Result<MultiQueryReader> opened = await ProcedureMultipleAsync(procedure, cancellation: cancellation);
            if (opened.IsError)
            {
                Logger.ErrorWith("Account settings query failed", ("DetailCode", opened.ResultData.DetailCode));
                return Result<AccountSettingsResponse?>.Error(ErrorCode.DatabaseError);
            }

            using MultiQueryReader reader = opened.Value;
            UsersEntity? user = await reader.ReadSingleOrDefaultAsync<UsersEntity>();
            if (user is null)
            {
                Logger.InfoWith("Account settings user not found", ("UserId", userId));
                return Result<AccountSettingsResponse?>.Success(null);
            }

            var socials = (await reader.ReadAsync<SocialAccountsEntity>()).ToList();

            var response = new AccountSettingsResponse
            {
                DisplayName = user.DisplayName,
                MaskedEmail = MaskEmail(user.Email),
                AuthProvider = user.AuthProvider,
                SocialLogins = socials
                    .Select(s => new LinkedLoginDto
                    {
                        Provider = s.Provider,
                        LinkedAt = s.CreatedAt
                    })
                    .ToList()
            };
            return Result<AccountSettingsResponse?>.Success(response);
        }

        public async Task<Result<NotificationPreferencesResponse>> GetNotificationPreferencesAsync(Guid userId, CancellationToken cancellation = default)
        {
            Logger.InfoWith("Notification preferences requested", ("UserId", userId));

            var procedure = new UspGetNotificationPreferences(this) { UserId = userId };
            var queryResult = await procedure.QueryAsync<NotificationPreferencesEntity>(cancellation: cancellation);
            if (queryResult.IsError)
            {
                return Result<NotificationPreferencesResponse>.Error(ErrorCode.DatabaseError, "GetNotificationPreferences");
            }

            // 저장 행 위에 기본값 병합 — 항목 6종 전부 내려간다 (행 없으면 enum 기본값)
            Dictionary<string, bool> saved = queryResult.Values1.ToDictionary(p => p.ItemName, p => p.IsEnabled);
            var response = new NotificationPreferencesResponse
            {
                Preferences = Enum.GetValues<NotificationPreferenceItem>()
                    .Select(item => new NotificationPreferenceDto
                    {
                        ItemName = item.ToString(),
                        IsEnabled = saved.TryGetValue(item.ToString(), out bool enabled) ? enabled : item.DefaultIsEnabled()
                    })
                    .ToList()
            };
            return Result<NotificationPreferencesResponse>.Success(response);
        }

        public async Task<Result<bool>> SetNotificationPreferenceAsync(Guid userId, string itemName, bool isEnabled, CancellationToken cancellation = default)
        {
            Logger.InfoWith("Notification preference update requested", ("UserId", userId), ("ItemName", itemName), ("IsEnabled", isEnabled));

            var procedure = new UspSetNotificationPreference(this)
            {
                UserId = userId,
                ItemName = itemName,
                IsEnabled = isEnabled
            };
            var queryResult = await procedure.QueryAsync<NotificationPreferenceSetRecord>(cancellation: cancellation);
            if (queryResult.IsError)
            {
                return Result<bool>.Error(ErrorCode.DatabaseError, "SetNotificationPreference");
            }

            return Result<bool>.Success(queryResult.Values1.Count > 0);
        }

        public async Task<Result<bool>> SoftDeleteAsync(Guid userId, CancellationToken cancellation = default)
        {
            Logger.InfoWith("Account soft delete requested", ("UserId", userId));

            var procedure = new UspDeleteUser(this) { UserId = userId };
            var queryResult = await procedure.QueryAsync<UserRecord>(cancellation: cancellation);
            if (queryResult.IsError)
            {
                return Result<bool>.Error(ErrorCode.DatabaseError, "DeleteUser");
            }

            bool deleted = queryResult.Values1.Count > 0;
            if (deleted)
            {
                Logger.InfoWith("Account soft deleted", ("UserId", userId));
            }

            return Result<bool>.Success(deleted);
        }

        /// <summary>이메일 마스킹 — 로컬파트 앞 3자 + *** (kim***@gmail.com). 3자 미만이면 있는 만큼만.</summary>
        private static string MaskEmail(string email)
        {
            int at = email.IndexOf('@');
            if (at <= 0)
            {
                return "***";
            }

            string local = email[..at];
            string visible = local.Length <= 3 ? local[..1] : local[..3];
            return $"{visible}***{email[at..]}";
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
