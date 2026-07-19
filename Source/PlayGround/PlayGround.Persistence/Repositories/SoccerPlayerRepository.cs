using System.Text.Json;
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
        /// <summary>가족 계정 연결의 관리 역할 — SoccerPlayerFamilyLinks.Role 저장 문자열.</summary>
        private const string GuardianRole = "Guardian";

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
                    IsGuardianManaged = player.IsGuardianManaged,
                    // 사진 편집은 보호자만 — UspSetSoccerPlayerPhoto의 보호자 판정 2갈래와 같은 규칙.
                    // (팀 관리자 갈래는 이 경로에 없다 — 여기 조회 주체는 프로필 관리 계정이다)
                    CanEditPhoto = player.IsGuardianManaged
                                   || family.Any(f => f.UserId == userId && f.Role == GuardianRole)
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

        public async Task<Result<bool>> SetPhotoAsync(Guid userId, Guid playerId, string? photoUrl, CancellationToken cancellation = default)
        {
            Logger.InfoWith("Player photo change requested",
                ("UserId", userId), ("PlayerId", playerId), ("IsRemoval", photoUrl is null));

            // 권한 판정(보호자·팀 관리자)은 프로시저 안에 있다 — 거부되면 빈 결과가 돌아온다
            var procedure = new UspSetSoccerPlayerPhoto(this)
            {
                UserId = userId,
                PlayerId = playerId,
                PhotoUrl = photoUrl!
            };

            var queryResult = await procedure.QueryAsync<SoccerPlayerPhotoRecord>(cancellation: cancellation);
            if (queryResult.IsError)
            {
                Logger.ErrorWith("Player photo change failed", ("ResultCode", queryResult.ResultCode));
                return Result<bool>.Error(ErrorCode.DatabaseError);
            }

            bool applied = queryResult.Values1.Any();
            Logger.InfoWith("Player photo change completed",
                ("UserId", userId), ("PlayerId", playerId), ("Applied", applied));

            return Result<bool>.Success(applied);
        }

        public async Task<Result<ClaimPlayerInviteResponse?>> ClaimInviteAsync(Guid userId, string code, CancellationToken cancellation = default)
        {
            // 코드 값은 추측 공격 로그가 될 수 있어 남기지 않는다
            Logger.InfoWith("Player invite claim requested", ("UserId", userId));

            var procedure = new UspClaimSoccerPlayerInvite(this) { UserId = userId, Code = code };
            var queryResult = await procedure.QueryAsync<SoccerClaimInviteRecord>(cancellation: cancellation);
            if (queryResult.IsError)
            {
                Logger.ErrorWith("Player invite claim failed", ("ResultCode", queryResult.ResultCode));
                return Result<ClaimPlayerInviteResponse?>.Error(ErrorCode.DatabaseError);
            }

            var row = queryResult.Values1.FirstOrDefault();
            if (row is null)
            {
                Logger.InfoWith("Player invite claim rejected — invalid or used code", ("UserId", userId));
                return Result<ClaimPlayerInviteResponse?>.Success(null);
            }

            Logger.InfoWith("Player invite claimed", ("UserId", userId), ("PlayerId", row.PlayerId));
            return Result<ClaimPlayerInviteResponse?>.Success(new ClaimPlayerInviteResponse
            {
                PlayerName = row.Name,
                TeamName = NullIfEmpty(row.TeamName)
            });
        }

        public async Task<Result<PlayerCareerResponse>> GetCareersByUserAsync(Guid userId, CancellationToken cancellation = default)
        {
            Logger.InfoWith("Player careers requested", ("UserId", userId));

            var procedure = new UspGetSoccerPlayerCareersByUser(this) { UserId = userId };
            var queryResult = await procedure.QueryAsync<SoccerPlayerCareersEntity>(cancellation: cancellation);
            if (queryResult.IsError)
            {
                Logger.ErrorWith("Player careers query failed", ("ResultCode", queryResult.ResultCode));
                return Result<PlayerCareerResponse>.Error(ErrorCode.DatabaseError);
            }

            var response = new PlayerCareerResponse
            {
                Entries = queryResult.Values1
                    .Select(c => new PlayerCareerEntryDto
                    {
                        CareerId = c.CareerId,
                        TeamName = c.TeamName,
                        IsCurrent = c.IsCurrent,
                        BadgeLabel = NullIfEmpty(c.BadgeLabel),
                        StartDate = DateOnly.FromDateTime(c.StartDate),
                        EndDate = c.EndDate is null ? null : DateOnly.FromDateTime(c.EndDate.Value),
                        Role = NullIfEmpty(c.Role),
                        Note = NullIfEmpty(c.Note),
                        IsVerified = c.IsVerified
                    })
                    .ToList()
            };

            Logger.InfoWith("Player careers received", ("UserId", userId), ("Entries", response.Entries.Count));
            return Result<PlayerCareerResponse>.Success(response);
        }

        public async Task<Result<PlayerPortfolioResponse>> GetPortfolioByUserAsync(Guid userId, CancellationToken cancellation = default)
        {
            Logger.InfoWith("Player portfolio requested", ("UserId", userId));

            var procedure = new UspGetSoccerPlayerPortfolioByUser(this) { UserId = userId };
            var queryResult = await procedure.QueryAsync<SoccerPlayerPortfolioVideosEntity>(cancellation: cancellation);
            if (queryResult.IsError)
            {
                Logger.ErrorWith("Player portfolio query failed", ("ResultCode", queryResult.ResultCode));
                return Result<PlayerPortfolioResponse>.Error(ErrorCode.DatabaseError);
            }

            var response = new PlayerPortfolioResponse
            {
                Videos = queryResult.Values1
                    .Select(v => new PlayerPortfolioVideoDto
                    {
                        VideoId = v.VideoId,
                        Title = v.Title,
                        VideoUrl = v.VideoUrl,
                        ThumbnailUrl = NullIfEmpty(v.ThumbnailUrl),
                        DurationSeconds = v.DurationSeconds,
                        IsPrimary = v.IsPrimary,
                        Tags = ParseTags(v.Tags),
                        RecordedOn = v.RecordedOn is null ? null : DateOnly.FromDateTime(v.RecordedOn.Value)
                    })
                    .ToList()
            };

            Logger.InfoWith("Player portfolio received", ("UserId", userId), ("Videos", response.Videos.Count));
            return Result<PlayerPortfolioResponse>.Success(response);
        }

        public async Task<Result<PlayerSeasonStatsResponse>> GetSeasonStatsByUserAsync(Guid userId, int seasonYear, CancellationToken cancellation = default)
        {
            Logger.InfoWith("Player season stats requested", ("UserId", userId), ("SeasonYear", seasonYear));

            var procedure = new UspGetSoccerPlayerSeasonStatsByUser(this) { UserId = userId, SeasonYear = seasonYear };
            Result<MultiQueryReader> opened = await ProcedureMultipleAsync(procedure, cancellation: cancellation);
            if (opened.IsError)
            {
                Logger.ErrorWith("Player season stats query failed", ("DetailCode", opened.ResultData.DetailCode));
                return Result<PlayerSeasonStatsResponse>.Error(ErrorCode.DatabaseError);
            }

            using MultiQueryReader reader = opened.Value;
            Guid? playerId = await reader.ReadSingleOrDefaultAsync<Guid?>();
            var appearances = (await reader.ReadAsync<SoccerPlayerMatchStatRecord>()).ToList();
            var events = (await reader.ReadAsync<SoccerMatchEventsEntity>()).ToList();
            var seasonYears = (await reader.ReadAsync<int>()).ToList();

            var response = new PlayerSeasonStatsResponse
            {
                SeasonYear = seasonYear,
                SeasonYears = seasonYears,
                Matches = appearances
                    .Select(a =>
                    {
                        bool isHome = a.HomeTeamId == a.TeamId;
                        return new PlayerMatchStatDto
                        {
                            MatchId = a.MatchId,
                            MatchedAt = a.MatchedAt,
                            CompetitionType = CompetitionTypeOf(a),
                            OpponentName = isHome ? a.AwayTeamName : a.HomeTeamName,
                            TeamScore = (isHome ? a.HomeScore : a.AwayScore) ?? 0,
                            OpponentScore = (isHome ? a.AwayScore : a.HomeScore) ?? 0,
                            Goals = events.Count(e => e.MatchId == a.MatchId && e.PlayerId == playerId && e.EventType != "OwnGoal"),
                            Assists = events.Count(e => e.MatchId == a.MatchId && e.AssistPlayerId == playerId),
                            MinutesPlayed = a.MinutesPlayed
                        };
                    })
                    .ToList()
            };

            Logger.InfoWith("Player season stats received", ("UserId", userId),
                ("Matches", response.Matches.Count), ("Years", seasonYears.Count));

            return Result<PlayerSeasonStatsResponse>.Success(response);
        }

        // 친선 = 대회 없음, League 형식 = 리그, 그 외(Cup/Split) = 컵
        private static string CompetitionTypeOf(SoccerPlayerMatchStatRecord appearance)
        {
            if (appearance.TournamentId is null)
            {
                return "Friendly";
            }

            return appearance.Format == "League" ? "League" : "Cup";
        }

        private static string? NullIfEmpty(string? value)
        {
            return string.IsNullOrEmpty(value) ? null : value;
        }

        // 태그 칩 JSON 배열 파싱 — 손상된 값은 빈 목록으로 (조회 실패 사유가 아님)
        private static List<string> ParseTags(string? json)
        {
            if (string.IsNullOrWhiteSpace(json))
            {
                return new List<string>();
            }

            try
            {
                return JsonSerializer.Deserialize<List<string>>(json) ?? new List<string>();
            }
            catch (JsonException)
            {
                return new List<string>();
            }
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
