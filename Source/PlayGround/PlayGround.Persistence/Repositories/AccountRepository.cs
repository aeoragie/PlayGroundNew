using Microsoft.Extensions.Options;
using PlayGround.Application.Auth.Models;
using PlayGround.Application.Interfaces;
using PlayGround.Contracts.Settings;
using PlayGround.Domain.Account;
using PlayGround.Infrastructure.Database;
using PlayGround.Infrastructure.Database.Base;
using PlayGround.Persistence.Database;
using PlayGround.Persistence.Database.Generated.Account.Entities;
using PlayGround.Persistence.Database.Generated.Account.Procedures;
using PlayGround.Shared.Result;
using PlayGround.Shared.Time;

namespace PlayGround.Persistence.Repositories
{
    public class AccountRepository : RepositoryBase, IAccountRepository
    {
        public override DatabaseTypes Database => DatabaseTypes.Account;

        public AccountRepository(IOptions<DatabaseConfiguration> options) : base(options)
        {
        }

        public async Task<Result<AccountUser?>> GetByEmailAsync(string email, CancellationToken cancellation = default)
        {
            var procedure = new UspGetUserByEmail(this) { Email = email };
            return await SingleOrNullAsync(procedure.QueryAsync<UserRecord>(cancellation: cancellation), "GetUserByEmail");
        }

        public async Task<Result<AccountUser?>> GetByIdAsync(Guid userId, CancellationToken cancellation = default)
        {
            // UspGetUserSettings의 첫 결과셋(사용자 행)만 쓴다 — 원본 이메일 포함(내보내기 이메일용)
            var procedure = new UspGetUserSettings(this) { UserId = userId };
            Result<MultiQueryReader> opened = await ProcedureMultipleAsync(procedure, cancellation: cancellation);
            if (opened.IsError)
            {
                return Result<AccountUser?>.Error(ErrorCode.DatabaseError, "GetUserById");
            }

            using MultiQueryReader reader = opened.Value;
            UsersEntity? user = await reader.ReadSingleOrDefaultAsync<UsersEntity>();
            if (user is null)
            {
                return Result<AccountUser?>.Success(null);
            }

            return Result<AccountUser?>.Success(new AccountUser
            {
                UserId = user.UserId,
                Email = user.Email,
                EmailConfirmed = user.EmailConfirmed,
                PasswordHash = string.IsNullOrEmpty(user.PasswordHash) ? null : user.PasswordHash,
                AuthProvider = EnumColumn.Read<AccountAuthProvider>(user.AuthProvider),
                DisplayName = user.DisplayName,
                ProfileImageUrl = string.IsNullOrEmpty(user.ProfileImageUrl) ? null : user.ProfileImageUrl,
                UserRole = EnumColumn.Read<AccountRole>(user.UserRole),
                UserStatus = user.UserStatus
            });
        }

        public async Task<Result<AccountUser?>> GetBySocialAsync(string provider, string providerUserId, CancellationToken cancellation = default)
        {
            var procedure = new UspGetUserBySocial(this) { Provider = provider, ProviderUserId = providerUserId };
            return await SingleOrNullAsync(procedure.QueryAsync<UserRecord>(cancellation: cancellation), "GetUserBySocial");
        }

        public async Task<Result<AccountUser>> CreateByEmailAsync(string email, string passwordHash, string displayName, CancellationToken cancellation = default)
        {
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
            var procedure = new UspUpdateUserRole(this) { UserId = userId, Role = role };
            return await CreatedAsync(procedure.QueryAsync<UserRecord>(cancellation: cancellation), "UpdateUserRole");
        }

        public async Task<Result<AccountUser?>> UpdateDisplayNameAsync(Guid userId, string newName, CancellationToken cancellation = default)
        {
            var procedure = new UspUpdateUserDisplayName(this) { UserId = userId, NewName = newName };
            // 빈 결과 = 제한 초과·미변경·미존재 (프로시저가 원자 판정) → Success(null)
            return await SingleOrNullAsync(procedure.QueryAsync<UserRecord>(cancellation: cancellation), "UpdateDisplayName");
        }

        public async Task<Result<string>> LinkSocialAsync(Guid userId, string provider, string providerUserId, string? email, CancellationToken cancellation = default)
        {
            var procedure = new UspLinkSocialAccount(this)
            {
                UserId = userId,
                Provider = provider,
                ProviderUserId = providerUserId,
                Email = email!
            };
            var queryResult = await procedure.QueryAsync<string>(cancellation: cancellation);
            if (queryResult.IsError)
            {
                return Result<string>.Error(ErrorCode.DatabaseError, "LinkSocial");
            }

            return Result<string>.Success(queryResult.Values1.FirstOrDefault() ?? "Error");
        }

        public async Task<Result<string>> UnlinkSocialAsync(Guid userId, string provider, CancellationToken cancellation = default)
        {
            var procedure = new UspUnlinkSocialAccount(this) { UserId = userId, Provider = provider };
            var queryResult = await procedure.QueryAsync<string>(cancellation: cancellation);
            if (queryResult.IsError)
            {
                return Result<string>.Error(ErrorCode.DatabaseError, "UnlinkSocial");
            }

            return Result<string>.Success(queryResult.Values1.FirstOrDefault() ?? "Error");
        }

        public async Task<Result<AccountSettingsResponse?>> GetSettingsAsync(Guid userId, CancellationToken cancellation = default)
        {
            var procedure = new UspGetUserSettings(this) { UserId = userId };
            Result<MultiQueryReader> opened = await ProcedureMultipleAsync(procedure, cancellation: cancellation);
            if (opened.IsError)
            {
                return Result<AccountSettingsResponse?>.Error(ErrorCode.DatabaseError);
            }

            using MultiQueryReader reader = opened.Value;
            UsersEntity? user = await reader.ReadSingleOrDefaultAsync<UsersEntity>();
            if (user is null)
            {
                return Result<AccountSettingsResponse?>.Success(null);
            }

            var socials = (await reader.ReadAsync<SocialAccountsEntity>()).ToList();
            int recentNameChanges = await reader.ReadSingleOrDefaultAsync<int>();
            SystemTime? earliestNameChange = await reader.ReadSingleOrDefaultAsync<SystemTime?>();

            bool hasPassword = !string.IsNullOrEmpty(user.PasswordHash);
            int remaining = Math.Max(0, 2 - recentNameChanges);

            var response = new AccountSettingsResponse
            {
                DisplayName = user.DisplayName,
                MaskedEmail = MaskEmail(user.Email),
                AuthProvider = EnumColumn.Read<AccountAuthProvider>(user.AuthProvider),
                SocialLogins = socials
                    .Select(s => new LinkedLoginDto
                    {
                        Provider = EnumColumn.Read<AccountAuthProvider>(s.Provider),
                        LinkedAt = s.CreatedAt,
                        MaskedEmail = string.IsNullOrEmpty(s.Email) ? null : MaskEmail(s.Email)
                    })
                    .ToList(),
                NameChangeRemaining = remaining,
                // 제한 초과일 때만 "다음 변경 가능" — 가장 오래된 최근 변경 + 30일
                NameChangeAvailableAt = remaining == 0 && earliestNameChange is SystemTime d
                    ? d.AddDays(30)
                    : null,
                LoginMeansCount = socials.Count + (hasPassword ? 1 : 0)
            };
            return Result<AccountSettingsResponse?>.Success(response);
        }

        public async Task<Result<NotificationPreferencesResponse>> GetNotificationPreferencesAsync(Guid userId, CancellationToken cancellation = default)
        {
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
                    .Where(item => item != NotificationPreferenceItem.Unknown)
                    .Select(item => new NotificationPreferenceDto
                    {
                        ItemName = item,
                        IsEnabled = saved.TryGetValue(item.ToString(), out bool enabled) ? enabled : item.DefaultIsEnabled()
                    })
                    .ToList()
            };
            return Result<NotificationPreferencesResponse>.Success(response);
        }

        public async Task<Result<bool>> SetNotificationPreferenceAsync(Guid userId, string itemName, bool isEnabled, CancellationToken cancellation = default)
        {
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
            var procedure = new UspDeleteUser(this) { UserId = userId };
            var queryResult = await procedure.QueryAsync<UserRecord>(cancellation: cancellation);
            if (queryResult.IsError)
            {
                return Result<bool>.Error(ErrorCode.DatabaseError, "DeleteUser");
            }

            return Result<bool>.Success(queryResult.Values1.Count > 0);
        }

        public async Task<Result<Dictionary<Guid, bool>>> GetNotificationStatesAsync(
            IReadOnlyCollection<Guid> userIds, string itemName, CancellationToken cancellation = default)
        {
            if (userIds.Count == 0)
            {
                return Result<Dictionary<Guid, bool>>.Success(new Dictionary<Guid, bool>());
            }

            var procedure = new UspGetNotificationPreferenceStatesByUsers(this)
            {
                UserIdsJson = System.Text.Json.JsonSerializer.Serialize(userIds),
                ItemName = itemName
            };
            var queryResult = await procedure.QueryAsync<NotificationPreferenceStateRecord>(cancellation: cancellation);
            if (queryResult.IsError)
            {
                return Result<Dictionary<Guid, bool>>.Error(ErrorCode.DatabaseError, "GetNotificationStates");
            }

            return Result<Dictionary<Guid, bool>>.Success(
                queryResult.Values1.ToDictionary(p => p.UserId, p => p.IsEnabled));
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
                AuthProvider = EnumColumn.Read<AccountAuthProvider>(r.AuthProvider),
                DisplayName = r.DisplayName,
                ProfileImageUrl = string.IsNullOrEmpty(r.ProfileImageUrl) ? null : r.ProfileImageUrl,
                UserRole = EnumColumn.Read<AccountRole>(r.UserRole),
                UserStatus = r.UserStatus
            };
        }
    }
}
