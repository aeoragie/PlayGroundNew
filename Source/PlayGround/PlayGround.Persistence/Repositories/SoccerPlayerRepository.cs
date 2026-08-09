using Microsoft.Extensions.Options;
using PlayGround.Domain.Soccer;
using PlayGround.Shared.Extensions;
using PlayGround.Application.Interfaces;
using PlayGround.Application.Player.Models;
using PlayGround.Contracts.Player;
using PlayGround.Domain.Soccer;
using PlayGround.Infrastructure.Database;
using PlayGround.Infrastructure.Database.Base;
using PlayGround.Persistence.Database.Generated.Soccer.Entities;
using PlayGround.Persistence.Database.Generated.Soccer.Procedures;
using PlayGround.Shared.Result;
using System.Text.Json;

namespace PlayGround.Persistence.Repositories
{
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
            var procedure = new UspCreatePlayer(this)
            {
                UserId = input.UserId,
                Name = input.Name,
                BirthDate = input.BirthDate,
                AgeGroup = EnumColumn.Write(input.AgeGroup),
                Region = input.Region!
            };

            var queryResult = await procedure.QueryAsync<SoccerCreatePlayerRecord>(cancellation: cancellation);
            if (queryResult.IsError)
            {
                return Result<Guid>.Error(ErrorCode.DatabaseError);
            }

            var row = queryResult.Values1.FirstOrDefault();
            if (row is null)
            {
                return Result<Guid>.Error(ErrorCode.OperationFailed, "no row returned");
            }

            return Result<Guid>.Success(row.PlayerId);
        }

        public async Task<Result<ManagedPlayersResponse>> GetManagedPlayersAsync(Guid userId, CancellationToken cancellation = default)
        {
            var procedure = new UspGetSoccerPlayersByUser(this) { UserId = userId };
            var queryResult = await procedure.QueryAsync<SoccerManagedPlayerRecord>(cancellation: cancellation);
            if (queryResult.IsError)
            {
                return Result<ManagedPlayersResponse>.Error(ErrorCode.DatabaseError);
            }

            var response = new ManagedPlayersResponse
            {
                Players = queryResult.Values1
                    .Select(p => new ManagedPlayerDto
                    {
                        PlayerId = p.PlayerId,
                        Name = p.Name,
                        Slug = NullIfEmpty(p.Slug),
                        AgeGroup = EnumColumn.Read<SoccerAgeGroup>(p.AgeGroup),
                        PhotoUrl = NullIfEmpty(p.PhotoUrl),
                        TeamName = NullIfEmpty(p.TeamName),
                        JerseyNumber = NullIfEmpty(p.JerseyNumber),
                        Position = EnumColumn.Read<SoccerPosition>(p.Position),
                        IsGuardianManaged = p.IsGuardianManaged
                    })
                    .ToList()
            };

            return Result<ManagedPlayersResponse>.Success(response);
        }

        public async Task<Result<PlayerInfoResponse?>> GetInfoByUserAsync(Guid userId, Guid? playerId = null, CancellationToken cancellation = default)
        {
            var procedure = new UspGetSoccerPlayerInfoByUser(this) { UserId = userId, TargetPlayerId = playerId };
            Result<MultiQueryReader> opened = await ProcedureMultipleAsync(procedure, cancellation: cancellation);
            if (opened.IsError)
            {
                return Result<PlayerInfoResponse?>.Error(ErrorCode.DatabaseError);
            }

            using MultiQueryReader reader = opened.Value;
            SoccerPlayerInfoRecord? player = await reader.ReadSingleOrDefaultAsync<SoccerPlayerInfoRecord>();
            if (player is null)
            {
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
                    Slug = NullIfEmpty(player.Slug),
                    PhotoUrl = NullIfEmpty(player.PhotoUrl),
                    AgeGroup = EnumColumn.Read<SoccerAgeGroup>(player.AgeGroup),
                    BirthYear = player.BirthDate?.Year,
                    Grade = EnumColumn.Read<SoccerGrade>(player.Grade),
                    Position = EnumColumn.Read<SoccerPosition>(player.Position),
                    JerseyNumber = NullIfEmpty(player.JerseyNumber),
                    TeamName = NullIfEmpty(player.TeamName),
                    HeightCm = player.HeightCm,
                    WeightKg = player.WeightKg,
                    PreferredFoot = EnumColumn.Read<SoccerPreferredFoot>(player.PreferredFoot),
                    SchoolName = NullIfEmpty(player.SchoolName),
                    GuardianPhoneMasked = MaskPhone(NullIfEmpty(player.GuardianPhone)),
                    IsGuardianManaged = player.IsGuardianManaged,
                    // 사진 편집은 보호자만 — UspSetSoccerPlayerPhoto의 보호자 판정 2갈래와 같은 규칙.
                    // (팀 관리자 갈래는 이 경로에 없다 — 여기 조회 주체는 프로필 관리 계정이다)
                    CanEditPhoto = player.IsGuardianManaged
                                   || family.Any(f => f.UserId == userId && f.Role == GuardianRole),
                    // me/info는 소유자 편집 뷰라 공개 설정과 무관하게 전부 내려준다 (게이팅 없음)
                    StrengthTags = ParseTags(player.StrengthTags)
                },
                Visibilities = Enum.GetValues<SoccerPlayerProfileField>()
                    .Where(field => field != SoccerPlayerProfileField.Unknown)
                    .Select(field => new PlayerFieldVisibilityDto
                    {
                        FieldName = field,
                        IsPublic = visibilities.FirstOrDefault(v => v.FieldName == field.ToString())?.IsPublic
                                   ?? field.DefaultIsPublic()
                    })
                    .ToList(),
                Family = family
                    .Select(f => new PlayerFamilyMemberDto
                    {
                        MemberName = f.MemberName,
                        Role = EnumColumn.Read<SoccerFamilyRole>(f.Role),
                        HasAccount = f.UserId is not null
                    })
                    .ToList()
            };

            return Result<PlayerInfoResponse?>.Success(response);
        }

        public async Task<Result<bool>> SetFieldVisibilityAsync(Guid userId, string fieldName, bool isPublic, Guid? playerId = null, CancellationToken cancellation = default)
        {
            var procedure = new UspSetSoccerPlayerFieldVisibility(this)
            {
                UserId = userId,
                FieldName = fieldName,
                IsPublic = isPublic,
                TargetPlayerId = playerId
            };

            var queryResult = await procedure.QueryAsync<SoccerPlayerVisibilitySetRecord>(cancellation: cancellation);
            if (queryResult.IsError)
            {
                return Result<bool>.Error(ErrorCode.DatabaseError);
            }

            bool applied = queryResult.Values1.Any();
            return Result<bool>.Success(applied);
        }

        public async Task<Result<string?>> UpdateProfileInfoAsync(Guid userId, int? heightCm, int? weightKg, SoccerPreferredFoot preferredFoot, string? schoolName, string? slug, Guid? playerId = null, CancellationToken cancellation = default)
        {
            var procedure = new UspUpdateSoccerPlayerProfileByUser(this)
            {
                UserId = userId,
                HeightCm = heightCm,
                WeightKg = weightKg,
                PreferredFoot = EnumColumn.Write(preferredFoot),
                SchoolName = schoolName,
                Slug = slug,
                TargetPlayerId = playerId
            };

            var queryResult = await procedure.QueryAsync<SoccerPlayerProfileUpdatedRecord>(cancellation: cancellation);
            if (queryResult.IsError)
            {
                return Result<string?>.Error(ErrorCode.DatabaseError);
            }

            // 반환 슬러그(소유 선수 없으면 행 없음 → null = Forbidden). 요청 슬러그와 다르면 Command가 SlugTaken 판정.
            string? resultSlug = queryResult.Values1.FirstOrDefault()?.Slug;
            return Result<string?>.Success(resultSlug);
        }

        public async Task<Result<bool>> SetPhotoAsync(Guid userId, Guid playerId, string? photoUrl, CancellationToken cancellation = default)
        {
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
                return Result<bool>.Error(ErrorCode.DatabaseError);
            }

            bool applied = queryResult.Values1.Any();

            return Result<bool>.Success(applied);
        }

        public async Task<Result<ClaimPlayerInviteResponse?>> ClaimInviteAsync(Guid userId, string code, CancellationToken cancellation = default)
        {
            // 코드 값은 추측 공격 로그가 될 수 있어 남기지 않는다

            var procedure = new UspClaimSoccerPlayerInvite(this) { UserId = userId, Code = code };
            var queryResult = await procedure.QueryAsync<SoccerClaimInviteRecord>(cancellation: cancellation);
            if (queryResult.IsError)
            {
                return Result<ClaimPlayerInviteResponse?>.Error(ErrorCode.DatabaseError);
            }

            var row = queryResult.Values1.FirstOrDefault();
            if (row is null)
            {
                return Result<ClaimPlayerInviteResponse?>.Success(null);
            }

            return Result<ClaimPlayerInviteResponse?>.Success(new ClaimPlayerInviteResponse
            {
                PlayerName = row.Name,
                TeamName = NullIfEmpty(row.TeamName)
            });
        }

        public async Task<Result<PlayerCareerResponse>> GetCareersByUserAsync(Guid userId, Guid? playerId = null, CancellationToken cancellation = default)
        {
            var procedure = new UspGetSoccerPlayerCareersByUser(this) { UserId = userId, TargetPlayerId = playerId };
            var queryResult = await procedure.QueryAsync<SoccerPlayerCareersEntity>(cancellation: cancellation);
            if (queryResult.IsError)
            {
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
                        StartDate = c.StartDate,
                        EndDate = c.EndDate,
                        Role = NullIfEmpty(c.Role),
                        Note = NullIfEmpty(c.Note),
                        IsVerified = c.IsVerified
                    })
                    .ToList()
            };

            return Result<PlayerCareerResponse>.Success(response);
        }

        public async Task<Result<PlayerPortfolioResponse>> GetPortfolioByUserAsync(Guid userId, Guid? playerId = null, CancellationToken cancellation = default)
        {
            var procedure = new UspGetSoccerPlayerPortfolioByUser(this) { UserId = userId, TargetPlayerId = playerId };
            var queryResult = await procedure.QueryAsync<SoccerPlayerPortfolioVideosEntity>(cancellation: cancellation);
            if (queryResult.IsError)
            {
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
                        RecordedOn = v.RecordedOn
                    })
                    .ToList()
            };

            return Result<PlayerPortfolioResponse>.Success(response);
        }

        public async Task<Result<bool>> SaveCareerAsync(Guid userId, SavePlayerCareerRequest request, Guid? playerId = null, CancellationToken cancellation = default)
        {
            var procedure = new UspSaveSoccerPlayerCareer(this)
            {
                UserId = userId,
                CareerId = request.CareerId,
                TeamName = request.TeamName,
                StartDate = request.StartDate,
                EndDate = request.EndDate,
                Role = request.Role!,
                Note = request.Note!,
                BadgeLabel = request.BadgeLabel!,
                TargetPlayerId = playerId
            };

            var queryResult = await procedure.QueryAsync<SoccerPlayerCareerSaveRecord>(cancellation: cancellation);
            if (queryResult.IsError)
            {
                return Result<bool>.Error(ErrorCode.DatabaseError);
            }

            bool applied = queryResult.Values1.Any();
            return Result<bool>.Success(applied);
        }

        public async Task<Result<bool>> DeleteCareerAsync(Guid userId, Guid careerId, bool restore, Guid? playerId = null, CancellationToken cancellation = default)
        {
            var procedure = new UspDeleteSoccerPlayerCareer(this)
            {
                UserId = userId,
                CareerId = careerId,
                Restore = restore,
                TargetPlayerId = playerId
            };

            var queryResult = await procedure.QueryAsync<SoccerPlayerCareerDeleteRecord>(cancellation: cancellation);
            if (queryResult.IsError)
            {
                return Result<bool>.Error(ErrorCode.DatabaseError);
            }

            bool applied = queryResult.Values1.Any();
            return Result<bool>.Success(applied);
        }

        public async Task<Result<bool>> SavePortfolioVideoAsync(Guid userId, SavePlayerPortfolioVideoRequest request, Guid? playerId = null, CancellationToken cancellation = default)
        {
            var procedure = new UspSaveSoccerPlayerPortfolioVideo(this)
            {
                UserId = userId,
                VideoId = request.VideoId,
                Title = request.Title,
                VideoUrl = request.VideoUrl,
                ThumbnailUrl = request.ThumbnailUrl!,
                Tags = request.Tags.Count > 0 ? JsonSerializer.Serialize(request.Tags) : null!,
                RecordedOn = request.RecordedOn,
                IsPrimary = request.IsPrimary,
                TargetPlayerId = playerId
            };

            var queryResult = await procedure.QueryAsync<SoccerPlayerPortfolioSaveRecord>(cancellation: cancellation);
            if (queryResult.IsError)
            {
                return Result<bool>.Error(ErrorCode.DatabaseError);
            }

            bool applied = queryResult.Values1.Any();
            return Result<bool>.Success(applied);
        }

        public async Task<Result<bool>> DeletePortfolioVideoAsync(Guid userId, Guid videoId, bool restore, Guid? playerId = null, CancellationToken cancellation = default)
        {
            var procedure = new UspDeleteSoccerPlayerPortfolioVideo(this)
            {
                UserId = userId,
                VideoId = videoId,
                Restore = restore,
                TargetPlayerId = playerId
            };

            var queryResult = await procedure.QueryAsync<SoccerPlayerPortfolioDeleteRecord>(cancellation: cancellation);
            if (queryResult.IsError)
            {
                return Result<bool>.Error(ErrorCode.DatabaseError);
            }

            bool applied = queryResult.Values1.Any();
            return Result<bool>.Success(applied);
        }

        public async Task<Result<PlayerSeasonStatsResponse>> GetSeasonStatsByUserAsync(Guid userId, int seasonYear, Guid? playerId = null, CancellationToken cancellation = default)
        {
            var procedure = new UspGetSoccerPlayerSeasonStatsByUser(this) { UserId = userId, SeasonYear = seasonYear, TargetPlayerId = playerId };
            Result<MultiQueryReader> opened = await ProcedureMultipleAsync(procedure, cancellation: cancellation);
            if (opened.IsError)
            {
                return Result<PlayerSeasonStatsResponse>.Error(ErrorCode.DatabaseError);
            }

            using MultiQueryReader reader = opened.Value;
            // 결과셋 ⓪ = 프로시저가 실제로 고른 선수 (지정 없으면 첫 자녀)
            Guid? resolvedPlayerId = await reader.ReadSingleOrDefaultAsync<Guid?>();
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
                            CompetitionType = EnumColumn.Read<SoccerCompetitionType>(CompetitionTypeOf(a)),
                            MatchType = EnumColumn.Read<SoccerMatchType>(a.MatchType),
                            OpponentName = isHome ? a.AwayTeamName : a.HomeTeamName,
                            TeamScore = (isHome ? a.HomeScore : a.AwayScore) ?? 0,
                            OpponentScore = (isHome ? a.AwayScore : a.HomeScore) ?? 0,
                            Goals = events.Count(e => e.MatchId == a.MatchId && e.PlayerId == resolvedPlayerId && e.EventType != "OwnGoal"),
                            Assists = events.Count(e => e.MatchId == a.MatchId && e.AssistPlayerId == resolvedPlayerId),
                            MinutesPlayed = a.MinutesPlayed
                        };
                    })
                    .ToList()
            };

            return Result<PlayerSeasonStatsResponse>.Success(response);
        }

        public async Task<Result<PlayerPublicProfileResponse?>> GetPublicProfileBySlugAsync(string slug, int seasonYear, Guid? viewerUserId = null, CancellationToken cancellation = default)
        {
            var procedure = new UspGetSoccerPlayerPublicProfileBySlug(this) { Slug = slug, SeasonYear = seasonYear, ViewerUserId = viewerUserId };
            Result<MultiQueryReader> opened = await ProcedureMultipleAsync(procedure, cancellation: cancellation);
            if (opened.IsError)
            {
                return Result<PlayerPublicProfileResponse?>.Error(ErrorCode.DatabaseError);
            }

            using MultiQueryReader reader = opened.Value;
            SoccerPlayerPublicHeaderRecord? header = await reader.ReadSingleOrDefaultAsync<SoccerPlayerPublicHeaderRecord>();
            var visibilities = (await reader.ReadAsync<SoccerPlayerFieldVisibilitiesEntity>()).ToList();
            var appearances = (await reader.ReadAsync<SoccerPlayerMatchStatRecord>()).ToList();
            var events = (await reader.ReadAsync<SoccerMatchEventsEntity>()).ToList();
            var videos = (await reader.ReadAsync<SoccerPlayerPortfolioVideosEntity>()).ToList();
            var careers = (await reader.ReadAsync<SoccerPlayerCareersEntity>()).ToList();
            SoccerAgentViewRequestsEntity? grant = await reader.ReadSingleOrDefaultAsync<SoccerAgentViewRequestsEntity>();

            // 미존재·프로필 비공개는 프로시저가 빈 결과 — 사유를 구분하지 않는다
            if (header is null)
            {
                return Result<PlayerPublicProfileResponse?>.Success(null);
            }

            // 공개 항목만 값 유지 — 행 없으면 기본값 (키·몸무게·주발 공개, Domain 기본)
            bool IsPublic(SoccerPlayerProfileField field)
            {
                var row = visibilities.FirstOrDefault(v => v.FieldName == field.ToString());
                return row?.IsPublic ?? field.DefaultIsPublic();
            }

            bool isGranted = grant is not null;

            // 시즌 요약 — 항상 공식만 (권한 뷰는 ②에 친선이 섞여 오므로 여기서 거른다).
            var officialAppearances = appearances.Where(a => a.MatchType == "Official").ToList();
            PlayerPublicSeasonDto? season = null;
            if (officialAppearances.Count > 0)
            {
                var withMinutes = officialAppearances.Where(a => a.MinutesPlayed is not null).ToList();
                season = new PlayerPublicSeasonDto
                {
                    SeasonYear = seasonYear,
                    MatchCount = officialAppearances.Count,
                    TotalMinutes = withMinutes.Sum(a => a.MinutesPlayed!.Value),
                    Goals = events.Count(e => e.PlayerId == header.PlayerId && e.EventType != "OwnGoal"
                        && officialAppearances.Any(a => a.MatchId == e.MatchId)),
                    Assists = events.Count(e => e.AssistPlayerId == header.PlayerId
                        && officialAppearances.Any(a => a.MatchId == e.MatchId)),
                    AverageMinutes = withMinutes.Count > 0
                        ? (int)Math.Round((double)withMinutes.Sum(a => a.MinutesPlayed!.Value) / withMinutes.Count)
                        : null
                };
            }

            SoccerPlayerPortfolioVideosEntity? primary = videos.FirstOrDefault();

            var response = new PlayerPublicProfileResponse
            {
                Profile = new PlayerPublicHeaderDto
                {
                    Name = header.Name,
                    PhotoUrl = NullIfEmpty(header.PhotoUrl),
                    IsGuardianManaged = header.IsGuardianManaged,
                    Position = EnumColumn.Read<SoccerPosition>(header.Position),
                    JerseyNumber = NullIfEmpty(header.JerseyNumber),
                    BirthYear = header.BirthDate?.Year,
                    AgeGroup = EnumColumn.Read<SoccerAgeGroup>(header.AgeGroup),
                    TeamName = NullIfEmpty(header.TeamName),
                    TeamSlug = NullIfEmpty(header.Slug),
                    TeamIsVerified = header.IsVerified,
                    IsClaimable = header.UserId is null,
                    HeightCm = IsPublic(SoccerPlayerProfileField.Height) ? header.HeightCm : null,
                    WeightKg = IsPublic(SoccerPlayerProfileField.Weight) ? header.WeightKg : null,
                    PreferredFoot = IsPublic(SoccerPlayerProfileField.PreferredFoot) ? EnumColumn.Read<SoccerPreferredFoot>(header.PreferredFoot) : SoccerPreferredFoot.Unknown,
                    // 학교·학년·보호자명은 권한 뷰(승인된 에이전트)에만 — 공개 뷰는 가시성과 무관하게 항상 null
                    SchoolName = isGranted ? NullIfEmpty(header.SchoolName) : null,
                    Grade = isGranted ? EnumColumn.Read<SoccerGrade>(header.Grade) : SoccerGrade.Unknown,
                    GuardianDisplayName = isGranted ? MaskName(NullIfEmpty(header.MemberName)) : null,
                    // 강점 태그는 공개 설정이 켜진 경우에만 (게이팅은 C# — 프로시저는 원본을 내려준다)
                    StrengthTags = IsPublic(SoccerPlayerProfileField.StrengthTags) ? ParseTags(header.StrengthTags) : new List<string>()
                },
                Season = season,
                Grant = isGranted ? new PlayerPublicGrantDto
                {
                    ApprovedAt = grant!.ReviewedAt!.Value,
                    ExpiresAt = grant.ExpiresAt!.Value
                } : null,
                // 경기별 상세 기록 (권한 뷰 전용) — 친선 포함, 팀 관점 변환·골/도움 매칭은 시즌 통계와 같은 규칙
                Matches = isGranted ? appearances
                    .Select(a =>
                    {
                        bool isHome = a.HomeTeamId == a.TeamId;
                        return new PlayerMatchStatDto
                        {
                            MatchId = a.MatchId,
                            MatchedAt = a.MatchedAt,
                            CompetitionType = EnumColumn.Read<SoccerCompetitionType>(CompetitionTypeOf(a)),
                            MatchType = EnumColumn.Read<SoccerMatchType>(a.MatchType),
                            OpponentName = isHome ? a.AwayTeamName : a.HomeTeamName,
                            TeamScore = (isHome ? a.HomeScore : a.AwayScore) ?? 0,
                            OpponentScore = (isHome ? a.AwayScore : a.HomeScore) ?? 0,
                            Goals = events.Count(e => e.MatchId == a.MatchId && e.PlayerId == header.PlayerId && e.EventType != "OwnGoal"),
                            Assists = events.Count(e => e.MatchId == a.MatchId && e.AssistPlayerId == header.PlayerId),
                            MinutesPlayed = a.MinutesPlayed
                        };
                    })
                    .ToList() : null,
                PrimaryVideo = primary is null ? null : new PlayerPortfolioVideoDto
                {
                    VideoId = primary.VideoId,
                    Title = primary.Title,
                    VideoUrl = primary.VideoUrl,
                    ThumbnailUrl = NullIfEmpty(primary.ThumbnailUrl),
                    DurationSeconds = primary.DurationSeconds,
                    IsPrimary = primary.IsPrimary,
                    Tags = ParseTags(primary.Tags),
                    RecordedOn = primary.RecordedOn
                },
                VideoCount = videos.Count,
                Careers = careers
                    .Select(c => new PlayerCareerEntryDto
                    {
                        CareerId = c.CareerId,
                        TeamName = c.TeamName,
                        IsCurrent = c.IsCurrent,
                        BadgeLabel = NullIfEmpty(c.BadgeLabel),
                        StartDate = c.StartDate,
                        EndDate = c.EndDate,
                        Role = NullIfEmpty(c.Role),
                        Note = NullIfEmpty(c.Note),
                        IsVerified = c.IsVerified
                    })
                    .ToList()
            };

            return Result<PlayerPublicProfileResponse?>.Success(response);
        }

        public async Task<Result<List<StrengthTagPresetDto>>> GetStrengthTagPresetsAsync(CancellationToken cancellation = default)
        {
            var procedure = new UspGetSoccerStrengthTagPresets(this);
            var queryResult = await procedure.QueryAsync<SoccerStrengthTagPresetsEntity>(cancellation: cancellation);
            if (queryResult.IsError)
            {
                return Result<List<StrengthTagPresetDto>>.Error(ErrorCode.DatabaseError);
            }

            var presets = queryResult.Values1
                .Select(p => new StrengthTagPresetDto { Position = EnumColumn.Read<SoccerPosition>(p.Position), Tag = p.Tag })
                .ToList();

            return Result<List<StrengthTagPresetDto>>.Success(presets);
        }

        public async Task<Result<bool>> SaveStrengthTagsAsync(Guid userId, string? tagsJson, Guid? playerId = null, CancellationToken cancellation = default)
        {
            var procedure = new UspSaveSoccerPlayerStrengthTags(this)
            {
                UserId = userId,
                TagsJson = tagsJson!,
                TargetPlayerId = playerId
            };

            var queryResult = await procedure.QueryAsync<SoccerPlayersEntity>(cancellation: cancellation);
            if (queryResult.IsError)
            {
                return Result<bool>.Error(ErrorCode.DatabaseError);
            }

            // 빈 결과 = 관리 주체 소유 선수 없음 (거부) — Command가 Forbidden으로 변환
            bool applied = queryResult.Values1.Any();
            return Result<bool>.Success(applied);
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

        // 보호자 이름 마스킹 — 성만 남김 (김민아 → 김OO). 권한 카드도 실명은 노출하지 않는다
        private static string? MaskName(string? name)
        {
            if (name is null)
            {
                return null;
            }

            return name.Length <= 1 ? name : name[..1] + new string('O', name.Length - 1);
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

            return phone.Length > 3 ? phone[..3] + new string('*', phone.Length - 3) : phone;
        }
    }
}
