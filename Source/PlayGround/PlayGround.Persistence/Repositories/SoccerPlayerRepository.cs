using Microsoft.Extensions.Options;
using PlayGround.Shared.Result;
using PlayGround.Infrastructure.Database;
using PlayGround.Infrastructure.Database.Base;
using PlayGround.Infrastructure.Logging;
using PlayGround.Contracts.Player;
using PlayGround.Domain.Soccer;
using PlayGround.Application.Interfaces;
using PlayGround.Application.Player.Models;
using PlayGround.Persistence.Database.Generated.Soccer.Entities;
using PlayGround.Persistence.Database.Generated.Soccer.Procedures;

namespace PlayGround.Persistence.Repositories
{
    /// <summary>선수 프로필 저장 (Soccer DB). 생성된 프로시저 객체 + SoccerCreatePlayerRecord 사용.</summary>
    public class SoccerPlayerRepository : RepositoryBase, IPlayerRepository
    {
        public override DatabaseTypes Database => DatabaseTypes.Soccer;

        public SoccerPlayerRepository(IOptions<DatabaseConfiguration> options) : base(options)
        {
        }

        public async Task<Result<Guid>> CreateAsync(CreatePlayerInput input, CancellationToken cancellation = default)
        {
            Logger.InfoWith("Player profile creation requested", ("UserId", input.UserId));

            var procedure = new UspCreatePlayer(this)
            {
                UserId = input.UserId,
                Name = input.Name,
                BirthDate = input.BirthDate?.ToDateTime(TimeOnly.MinValue),
                AgeGroup = input.AgeGroup!,
                Region = input.Region!
            };

            var queryResult = await procedure.QueryAsync<SoccerCreatePlayerRecord>(cancellation: cancellation);
            if (queryResult.IsError)
            {
                Logger.ErrorWith("Player profile creation failed", ("ResultCode", queryResult.ResultCode));
                return Result<Guid>.Error(ErrorCode.DatabaseError);
            }

            var row = queryResult.Values1.FirstOrDefault();
            if (row is null)
            {
                Logger.ErrorWith("Player profile creation returned no row");
                return Result<Guid>.Error(ErrorCode.OperationFailed, "no row returned");
            }

            Logger.InfoWith("Player profile created", ("PlayerId", row.PlayerId));
            return Result<Guid>.Success(row.PlayerId);
        }

        public async Task<Result<PlayerInfoResponse?>> GetInfoByUserAsync(Guid userId, CancellationToken cancellation = default)
        {
            Logger.InfoWith("Player info requested", ("UserId", userId));

            var procedure = new UspGetSoccerPlayerInfoByUser(this) { UserId = userId };
            Result<MultiQueryReader> opened = await ProcedureMultipleAsync(procedure, cancellation: cancellation);
            if (opened.IsError)
            {
                Logger.ErrorWith("Player info query failed", ("DetailCode", opened.ResultData.DetailCode));
                return Result<PlayerInfoResponse?>.Error(ErrorCode.DatabaseError);
            }

            using MultiQueryReader reader = opened.Value;
            SoccerPlayerInfoRecord? player = await reader.ReadSingleOrDefaultAsync<SoccerPlayerInfoRecord>();
            if (player is null)
            {
                Logger.InfoWith("Player info not found", ("UserId", userId));
                return Result<PlayerInfoResponse?>.Success(null);
            }

            var visibilities = (await reader.ReadAsync<SoccerPlayerFieldVisibilitiesEntity>()).ToList();
            var family = (await reader.ReadAsync<SoccerPlayerFamilyLinksEntity>()).ToList();

            var response = new PlayerInfoResponse
            {
                Profile = new PlayerProfileDto
                {
                    PlayerId = player.PlayerId,
                    Name = player.Name,
                    PhotoUrl = NullIfEmpty(player.PhotoUrl),
                    AgeGroup = NullIfEmpty(player.AgeGroup),
                    BirthYear = player.BirthDate?.Year,
                    Grade = NullIfEmpty(player.Grade),
                    Position = NullIfEmpty(player.Position),
                    JerseyNumber = NullIfEmpty(player.JerseyNumber),
                    TeamName = NullIfEmpty(player.TeamName),
                    HeightCm = player.HeightCm,
                    WeightKg = player.WeightKg,
                    PreferredFoot = NullIfEmpty(player.PreferredFoot),
                    SchoolName = NullIfEmpty(player.SchoolName),
                    GuardianPhoneMasked = MaskPhone(NullIfEmpty(player.GuardianPhone)),
                    IsGuardianManaged = player.IsGuardianManaged
                },
                // 저장값이 없는 항목은 기본값으로 채워 5개 항목 전부 내려준다
                Visibilities = Enum.GetValues<SoccerPlayerProfileField>()
                    .Select(field => new PlayerFieldVisibilityDto
                    {
                        FieldName = field.ToString(),
                        IsPublic = visibilities.FirstOrDefault(v => v.FieldName == field.ToString())?.IsPublic
                                   ?? field.DefaultIsPublic()
                    })
                    .ToList(),
                Family = family
                    .Select(f => new PlayerFamilyMemberDto
                    {
                        MemberName = f.MemberName,
                        Role = f.Role,
                        HasAccount = f.UserId is not null
                    })
                    .ToList()
            };

            Logger.InfoWith("Player info received", ("PlayerId", player.PlayerId),
                ("Visibilities", visibilities.Count), ("Family", response.Family.Count));

            return Result<PlayerInfoResponse?>.Success(response);
        }

        public async Task<Result<bool>> SetFieldVisibilityAsync(Guid userId, string fieldName, bool isPublic, CancellationToken cancellation = default)
        {
            Logger.InfoWith("Player field visibility change requested",
                ("UserId", userId), ("FieldName", fieldName), ("IsPublic", isPublic));

            var procedure = new UspSetSoccerPlayerFieldVisibility(this)
            {
                UserId = userId,
                FieldName = fieldName,
                IsPublic = isPublic
            };

            var queryResult = await procedure.QueryAsync<SoccerPlayerVisibilitySetRecord>(cancellation: cancellation);
            if (queryResult.IsError)
            {
                Logger.ErrorWith("Player field visibility change failed", ("ResultCode", queryResult.ResultCode));
                return Result<bool>.Error(ErrorCode.DatabaseError);
            }

            bool applied = queryResult.Values1.Any();
            Logger.InfoWith("Player field visibility change completed", ("UserId", userId), ("Applied", applied));
            return Result<bool>.Success(applied);
        }

        private static string? NullIfEmpty(string? value)
        {
            return string.IsNullOrEmpty(value) ? null : value;
        }

        // 보호자 연락처 마스킹 — 가운데 자리 감춤 (010-1234-5678 → 010-****-5678)
        private static string? MaskPhone(string? phone)
        {
            if (phone is null)
            {
                return null;
            }

            string[] parts = phone.Split('-');
            if (parts.Length == 3)
            {
                return $"{parts[0]}-{new string('*', parts[1].Length)}-{parts[2]}";
            }

            // 하이픈 없는 값은 앞 3자리만 남기고 감춤
            return phone.Length > 3 ? phone[..3] + new string('*', phone.Length - 3) : phone;
        }
    }
}
