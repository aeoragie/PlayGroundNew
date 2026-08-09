using Microsoft.Extensions.Options;
using PlayGround.Application.Interfaces;
using PlayGround.Application.Team.Models;
using PlayGround.Contracts.Team;
using PlayGround.Infrastructure.Database;
using PlayGround.Infrastructure.Database.Base;
using PlayGround.Persistence.Database.Generated.Soccer.Entities;
using PlayGround.Persistence.Database.Generated.Soccer.Procedures;
using PlayGround.Shared.Result;
using PlayGround.Shared.Time;
using System.Text.Json;

namespace PlayGround.Persistence.Repositories
{
    /// <summary>팀+로스터 저장 (Soccer DB). 로스터는 JSON으로 넘겨 단일 프로시저가 원자적으로 생성.</summary>
    public class SoccerTeamRepository : RepositoryBase, ISoccerTeamRepository
    {
        public override DatabaseTypes Database => DatabaseTypes.Soccer;

        public SoccerTeamRepository(IOptions<DatabaseConfiguration> options) : base(options)
        {
        }

        public async Task<Result<string>> CreateWithRosterAsync(CreateTeamInput input, CancellationToken cancellation = default)
        {
            string rosterJson = JsonSerializer.Serialize(input.Roster);

            var procedure = new UspCreateSoccerTeamWithRoster(this)
            {
                ManagerUserId = input.ManagerUserId,
                TeamName = input.TeamName,
                TeamType = input.TeamType!,
                Region = input.Region!,
                RosterJson = rosterJson
            };

            var queryResult = await procedure.QueryAsync<SoccerCreateTeamRecord>(cancellation: cancellation);
            if (queryResult.IsError)
            {
                return Result<string>.Error(ErrorCode.DatabaseError);
            }

            var row = queryResult.Values1.FirstOrDefault();
            if (row is null)
            {
                return Result<string>.Error(ErrorCode.OperationFailed, "no row returned");
            }

            // 슬러그는 팀 생성의 반환값이자 공개 홈 주소 — 없으면 후속 이동이 불가능하니 실패로 본다
            if (string.IsNullOrEmpty(row.Slug))
            {
                return Result<string>.Error(ErrorCode.OperationFailed, "slug is empty");
            }

            return Result<string>.Success(row.Slug);
        }

        public async Task<Result<TeamInfoResponse?>> GetTeamInfoByManagerAsync(Guid managerUserId, CancellationToken cancellation = default)
        {
            var procedure = new UspGetSoccerTeamInfoByManager(this) { ManagerUserId = managerUserId };
            Result<MultiQueryReader> opened = await ProcedureMultipleAsync(procedure, cancellation: cancellation);
            if (opened.IsError)
            {
                return Result<TeamInfoResponse?>.Error(ErrorCode.DatabaseError);
            }

            using MultiQueryReader reader = opened.Value;
            SoccerTeamsEntity? team = await reader.ReadSingleOrDefaultAsync<SoccerTeamsEntity>();
            if (team is null)
            {
                return Result<TeamInfoResponse?>.Success(null);
            }

            var values = (await reader.ReadAsync<SoccerTeamValuesEntity>()).ToList();
            var coaches = (await reader.ReadAsync<SoccerTeamCoachesEntity>()).ToList();
            var channels = (await reader.ReadAsync<SoccerTeamChannelsEntity>()).ToList();

            var response = new TeamInfoResponse
            {
                Profile = new TeamProfileDto
                {
                    TeamId = team.TeamId,
                    TeamName = team.TeamName,
                    TeamType = NullIfEmpty(team.TeamType),
                    Region = NullIfEmpty(team.Region),
                    LogoUrl = NullIfEmpty(team.LogoUrl),
                    CoverImageUrl = NullIfEmpty(team.CoverImageUrl),
                    Description = NullIfEmpty(team.Description),
                    Slug = NullIfEmpty(team.Slug),
                    IsVerified = team.IsVerified,
                    FoundedYear = team.FoundedYear,
                    MonthlyFee = team.MonthlyFee,
                    IsMonthlyFeePublic = team.IsMonthlyFeePublic,
                    TrainingDays = NullIfEmpty(team.TrainingDays)
                },
                Values = values
                    .Select(v => new TeamValueDto
                    {
                        TeamValueId = v.TeamValueId,
                        Title = v.Title,
                        Description = v.Description
                    })
                    .ToList(),
                Coaches = coaches
                    .Select(c => new TeamCoachDto
                    {
                        CoachId = c.CoachId,
                        Name = c.Name,
                        Role = c.Role,
                        Career = NullIfEmpty(c.Career),
                        Certification = NullIfEmpty(c.Certification),
                        Quote = NullIfEmpty(c.Quote),
                        Achievements = ParseAchievements(c.Achievements),
                        InstagramUrl = NullIfEmpty(c.InstagramUrl),
                        YoutubeUrl = NullIfEmpty(c.YoutubeUrl)
                    })
                    .ToList(),
                Channels = channels
                    .Select(ch => new TeamChannelDto
                    {
                        ChannelId = ch.ChannelId,
                        ChannelType = ch.ChannelType,
                        Name = ch.Name,
                        Url = ch.Url,
                        Description = NullIfEmpty(ch.Description)
                    })
                    .ToList()
            };

            return Result<TeamInfoResponse?>.Success(response);
        }

        public async Task<Result<TeamRosterResponse>> GetTeamRosterByManagerAsync(Guid managerUserId, CancellationToken cancellation = default)
        {
            var procedure = new UspGetSoccerTeamRosterByManager(this) { ManagerUserId = managerUserId };
            var queryResult = await procedure.QueryAsync<SoccerTeamRosterRecord>(cancellation: cancellation);
            if (queryResult.IsError)
            {
                return Result<TeamRosterResponse>.Error(ErrorCode.DatabaseError);
            }

            var response = new TeamRosterResponse
            {
                Players = queryResult.Values1
                    .Select(r => new TeamRosterPlayerDto
                    {
                        TeamPlayerId = r.TeamPlayerId,
                        PlayerId = r.PlayerId,
                        Name = r.Name,
                        JerseyNumber = NullIfEmpty(r.JerseyNumber),
                        Position = NullIfEmpty(r.Position),
                        Grade = NullIfEmpty(r.Grade),
                        AgeGroup = NullIfEmpty(r.AgeGroup),
                        PhotoUrl = NullIfEmpty(r.PhotoUrl),
                        // Claim 상태는 저장 컬럼이 아니라 파생값 — UserId 연결 = Claimed (Pending은 Claim 플로우 도입 때)
                        ClaimStatus = r.UserId is null ? "Unclaimed" : "Claimed",
                        // 초대코드는 Unclaimed 선수에게만 의미 있다 (Claimed는 코드가 이미 소진된 상태)
                        InviteCode = r.UserId is null ? NullIfEmpty(r.Code) : null,
                        // 공개 설정 게이팅은 SQL에서 끝났다(비공개면 NULL) — 여기서는 파싱만
                        StrengthTags = ParseAchievements(r.StrengthTags)
                    })
                    .ToList()
            };

            return Result<TeamRosterResponse>.Success(response);
        }

        public async Task<Result<TeamRosterPlayerDto?>> AddTeamPlayerByManagerAsync(
            Guid managerUserId, AddTeamPlayerRequest request, CancellationToken cancellation = default)
        {
            var procedure = new UspAddSoccerTeamPlayer(this)
            {
                ManagerUserId = managerUserId,
                Name = request.Name,
                JerseyNumber = request.JerseyNumber,
                Position = request.Position,
                Grade = request.Grade,
                AgeGroup = request.AgeGroup
            };
            var queryResult = await procedure.QueryAsync<SoccerTeamRosterRecord>(cancellation: cancellation);
            if (queryResult.IsError)
            {
                return Result<TeamRosterPlayerDto?>.Error(ErrorCode.DatabaseError, "AddTeamPlayer");
            }

            SoccerTeamRosterRecord? row = queryResult.Values1.FirstOrDefault();
            if (row is null)
            {
                // 빈 결과 = 관리하는 팀이 없다 (거부) — Command가 Forbidden으로 변환
                return Result<TeamRosterPlayerDto?>.Success(null);
            }

            var dto = new TeamRosterPlayerDto
            {
                TeamPlayerId = row.TeamPlayerId,
                PlayerId = row.PlayerId,
                Name = row.Name,
                JerseyNumber = NullIfEmpty(row.JerseyNumber),
                Position = NullIfEmpty(row.Position),
                Grade = NullIfEmpty(row.Grade),
                AgeGroup = NullIfEmpty(row.AgeGroup),
                PhotoUrl = NullIfEmpty(row.PhotoUrl),
                ClaimStatus = row.UserId is null ? "Unclaimed" : "Claimed",
                InviteCode = row.UserId is null ? NullIfEmpty(row.Code) : null
            };

            return Result<TeamRosterPlayerDto?>.Success(dto);
        }

        public async Task<Result<bool>> RemoveTeamPlayerByManagerAsync(
            Guid managerUserId, Guid teamPlayerId, bool restore, CancellationToken cancellation = default)
        {
            var procedure = new UspRemoveSoccerTeamPlayer(this)
            {
                ManagerUserId = managerUserId,
                TeamPlayerId = teamPlayerId,
                Restore = restore
            };
            var queryResult = await procedure.QueryAsync<SoccerTeamPlayersEntity>(cancellation: cancellation);
            if (queryResult.IsError)
            {
                return Result<bool>.Error(ErrorCode.DatabaseError, "RemoveTeamPlayer");
            }

            // 빈 결과 = 남의 팀이거나 이미 그 상태 — Command가 Forbidden으로 변환
            return Result<bool>.Success(queryResult.Values1.Any());
        }

        public async Task<Result<TeamPublicHomeResponse?>> GetTeamHomeBySlugAsync(string slug, Guid? viewerUserId = null, CancellationToken cancellation = default)
        {
            var procedure = new UspGetSoccerTeamHomeBySlug(this) { Slug = slug };
            Result<MultiQueryReader> opened = await ProcedureMultipleAsync(procedure, cancellation: cancellation);
            if (opened.IsError)
            {
                return Result<TeamPublicHomeResponse?>.Error(ErrorCode.DatabaseError);
            }

            using MultiQueryReader reader = opened.Value;
            SoccerTeamsEntity? team = await reader.ReadSingleOrDefaultAsync<SoccerTeamsEntity>();
            if (team is null)
            {
                return Result<TeamPublicHomeResponse?>.Success(null);
            }

            var values = (await reader.ReadAsync<SoccerTeamValuesEntity>()).ToList();
            var coaches = (await reader.ReadAsync<SoccerTeamCoachesEntity>()).ToList();
            var channels = (await reader.ReadAsync<SoccerTeamChannelsEntity>()).ToList();
            var roster = (await reader.ReadAsync<SoccerTeamRosterRecord>()).ToList();

            var response = new TeamPublicHomeResponse
            {
                // 관리자 본인이 자기 팀을 열람 중인지 — ManagerUserId 자체는 계속 비노출, bool만 파생
                IsManager = viewerUserId is not null && team.ManagerUserId == viewerUserId,
                Profile = new TeamPublicProfileDto
                {
                    TeamName = team.TeamName,
                    TeamType = NullIfEmpty(team.TeamType),
                    Region = NullIfEmpty(team.Region),
                    AgeGroup = NullIfEmpty(team.AgeGroup),
                    LogoUrl = NullIfEmpty(team.LogoUrl),
                    CoverImageUrl = NullIfEmpty(team.CoverImageUrl),
                    Description = NullIfEmpty(team.Description),
                    Slug = NullIfEmpty(team.Slug),
                    IsVerified = team.IsVerified,
                    FoundedYear = team.FoundedYear,
                    MonthlyFee = team.IsMonthlyFeePublic ? team.MonthlyFee : null,
                    TrainingDays = NullIfEmpty(team.TrainingDays)
                },
                Values = values
                    .Select(v => new TeamValueDto
                    {
                        TeamValueId = v.TeamValueId,
                        Title = v.Title,
                        Description = v.Description
                    })
                    .ToList(),
                Coaches = coaches
                    .Select(c => new TeamCoachDto
                    {
                        CoachId = c.CoachId,
                        Name = c.Name,
                        Role = c.Role,
                        Career = NullIfEmpty(c.Career),
                        Certification = NullIfEmpty(c.Certification),
                        Quote = NullIfEmpty(c.Quote),
                        Achievements = ParseAchievements(c.Achievements),
                        InstagramUrl = NullIfEmpty(c.InstagramUrl),
                        YoutubeUrl = NullIfEmpty(c.YoutubeUrl)
                    })
                    .ToList(),
                Channels = channels
                    .Select(ch => new TeamChannelDto
                    {
                        ChannelId = ch.ChannelId,
                        ChannelType = ch.ChannelType,
                        Name = ch.Name,
                        Url = ch.Url,
                        Description = NullIfEmpty(ch.Description)
                    })
                    .ToList(),
                Roster = roster
                    .Select(r => new TeamPublicPlayerDto
                    {
                        PlayerId = r.PlayerId,
                        Name = r.Name,
                        JerseyNumber = NullIfEmpty(r.JerseyNumber),
                        Position = NullIfEmpty(r.Position),
                        Grade = NullIfEmpty(r.Grade),
                        AgeGroup = NullIfEmpty(r.AgeGroup),
                        PhotoUrl = NullIfEmpty(r.PhotoUrl),
                        // 공개 규칙: UserId 자체는 내리지 않고 공개 프로필 연결 여부만
                        HasPublicProfile = r.UserId is not null,
                        Slug = r.UserId is not null ? NullIfEmpty(r.Slug) : null,
                        // 공개 설정 게이팅은 SQL에서 끝났다(비공개면 NULL) — 여기서는 파싱만
                        StrengthTags = ParseAchievements(r.StrengthTags)
                    })
                    .ToList()
            };

            return Result<TeamPublicHomeResponse?>.Success(response);
        }

        public async Task<Result<TeamMatchesResponse>> GetTeamMatchesByManagerAsync(Guid managerUserId, int seasonYear, CancellationToken cancellation = default)
        {
            var procedure = new UspGetSoccerTeamMatchesByManager(this) { ManagerUserId = managerUserId, SeasonYear = seasonYear };
            Result<MultiQueryReader> opened = await ProcedureMultipleAsync(procedure, cancellation: cancellation);
            if (opened.IsError)
            {
                return Result<TeamMatchesResponse>.Error(ErrorCode.DatabaseError);
            }

            using MultiQueryReader reader = opened.Value;
            Guid? teamId = await reader.ReadSingleOrDefaultAsync<Guid?>();
            var matches = (await reader.ReadAsync<SoccerTeamMatchRecord>()).ToList();
            var events = (await reader.ReadAsync<SoccerMatchEventsEntity>()).ToList();
            int? leagueRank = await reader.ReadSingleOrDefaultAsync<int?>();

            var response = new TeamMatchesResponse
            {
                SeasonYear = seasonYear,
                LeagueRank = leagueRank,
                Matches = matches.Select(m => MapMatch(m, teamId, events)).ToList()
            };

            return Result<TeamMatchesResponse>.Success(response);
        }

        public async Task<Result<TeamSeasonRecordResponse>> GetTeamSeasonRecordBySlugAsync(string slug, int seasonYear, CancellationToken cancellation = default)
        {
            var procedure = new UspGetSoccerTeamSeasonRecordBySlug(this) { Slug = slug, SeasonYear = seasonYear };
            Result<MultiQueryReader> opened = await ProcedureMultipleAsync(procedure, cancellation: cancellation);
            if (opened.IsError)
            {
                return Result<TeamSeasonRecordResponse>.Error(ErrorCode.DatabaseError);
            }

            using MultiQueryReader reader = opened.Value;
            Guid? teamId = await reader.ReadSingleOrDefaultAsync<Guid?>();
            var matches = (await reader.ReadAsync<SoccerTeamMatchRecord>()).ToList();
            int? leagueRank = await reader.ReadSingleOrDefaultAsync<int?>();
            var videos = (await reader.ReadAsync<SoccerMatchVideosEntity>()).ToList();

            // 공개 뷰는 이벤트 칩이 없다 — 빈 이벤트 목록으로 매핑(승무패 뱃지만 사용).
            var noEvents = new List<SoccerMatchEventsEntity>();
            var response = new TeamSeasonRecordResponse
            {
                TeamName = FindTeamName(matches, teamId),
                SeasonYear = seasonYear,
                LeagueRank = leagueRank,
                Matches = matches.Select(m => MapMatch(m, teamId, noEvents)).ToList(),
                Videos = videos.Select(MapVideo).ToList()
            };

            return Result<TeamSeasonRecordResponse>.Success(response);
        }

        // 경기 목록에서 우리 팀 표시명 파생 (홈/원정 어느 쪽이든 우리 TeamId 쪽 이름). 경기 없으면 빈 문자열.
        private static string FindTeamName(List<SoccerTeamMatchRecord> matches, Guid? teamId)
        {
            SoccerTeamMatchRecord? sample = matches.FirstOrDefault(m => m.HomeTeamId == teamId || m.AwayTeamId == teamId);
            if (sample is null)
            {
                return string.Empty;
            }

            return sample.HomeTeamId == teamId ? sample.HomeTeamName : sample.AwayTeamName;
        }

        private static TeamMatchDto MapMatch(SoccerTeamMatchRecord match, Guid? teamId, List<SoccerMatchEventsEntity> events)
        {
            bool isHome = match.HomeTeamId == teamId;
            return new TeamMatchDto
            {
                MatchId = match.MatchId,
                CompetitionType = CompetitionTypeOf(match),
                MatchType = match.MatchType,
                TournamentName = NullIfEmpty(match.Name),
                MatchedAt = match.MatchedAt,
                VenueName = NullIfEmpty(match.VenueName),
                IsHome = isHome,
                OpponentName = isHome ? match.AwayTeamName : match.HomeTeamName,
                TeamScore = (isHome ? match.HomeScore : match.AwayScore) ?? 0,
                OpponentScore = (isHome ? match.AwayScore : match.HomeScore) ?? 0,
                Events = events
                    .Where(e => e.MatchId == match.MatchId)
                    .Select(e => new TeamMatchEventDto
                    {
                        EventType = e.EventType,
                        PlayerName = NullIfEmpty(e.PlayerName),
                        AssistPlayerName = NullIfEmpty(e.AssistPlayerName)
                    })
                    .ToList()
            };
        }

        private static TeamVideoDto MapVideo(SoccerMatchVideosEntity video)
        {
            return new TeamVideoDto
            {
                VideoId = video.VideoId,
                VideoType = video.VideoType,
                Title = video.Title,
                VideoUrl = video.VideoUrl,
                ThumbnailUrl = NullIfEmpty(video.ThumbnailUrl),
                DurationSeconds = video.DurationSeconds,
                RecordedOn = video.RecordedOn,
                IsMatchLinked = video.MatchId is not null
            };
        }

        public async Task<Result<TeamVideosResponse>> GetTeamVideosByManagerAsync(Guid managerUserId, CancellationToken cancellation = default)
        {
            var procedure = new UspGetSoccerTeamVideosByManager(this) { ManagerUserId = managerUserId };
            var queryResult = await procedure.QueryAsync<SoccerMatchVideosEntity>(cancellation: cancellation);
            if (queryResult.IsError)
            {
                return Result<TeamVideosResponse>.Error(ErrorCode.DatabaseError);
            }

            var response = new TeamVideosResponse
            {
                Videos = queryResult.Values1.Select(MapVideo).ToList()
            };

            return Result<TeamVideosResponse>.Success(response);
        }

        // 친선 = 대회 없음, League 형식 = 리그, 그 외(Cup/Split) = 컵
        private static string CompetitionTypeOf(SoccerTeamMatchRecord match)
        {
            if (match.TournamentId is null)
            {
                return "Friendly";
            }

            return match.Format == "League" ? "League" : "Cup";
        }

        public async Task<Result<string?>> UpdateTeamInfoByManagerAsync(
            Guid managerUserId, UpdateTeamInfoRequest request, CancellationToken cancellation = default)
        {
            // 실적 칩은 DB에 JSON 배열 문자열로 들어간다 — 조회 쪽 ParseAchievements와 짝이다
            var coaches = request.Coaches.Select(c => new
            {
                c.DisplayOrder,
                c.Name,
                c.Role,
                c.Career,
                c.Certification,
                c.Quote,
                Achievements = c.Achievements.Count > 0 ? JsonSerializer.Serialize(c.Achievements) : null,
                c.InstagramUrl,
                c.YoutubeUrl,
            });

            var procedure = new UspUpdateSoccerTeamInfoByManager(this)
            {
                ManagerUserId = managerUserId,
                TeamName = request.TeamName,
                Description = request.Description,
                Region = request.Region,
                FoundedYear = request.FoundedYear,
                LogoUrl = request.LogoUrl,
                CoverImageUrl = request.CoverImageUrl,
                ValuesJson = request.Values.Count > 0 ? JsonSerializer.Serialize(request.Values) : null,
                CoachesJson = request.Coaches.Count > 0 ? JsonSerializer.Serialize(coaches) : null,
            };

            var queryResult = await procedure.QueryAsync<SoccerUpdatedTeamRecord>(cancellation: cancellation);
            if (queryResult.IsError)
            {
                return Result<string?>.Error(ErrorCode.DatabaseError);
            }

            var row = queryResult.Values1.FirstOrDefault();
            if (row is null)
            {
                return Result<string?>.Success(null);
            }

            return Result<string?>.Success(NullIfEmpty(row.Slug) ?? string.Empty);
        }

        public async Task<Result<TeamTournamentOptionsResponse>> GetTournamentOptionsByManagerAsync(
            Guid managerUserId, int seasonYear, CancellationToken cancellation = default)
        {
            var procedure = new UspGetSoccerTournamentOptionsByManager(this)
            {
                ManagerUserId = managerUserId,
                SeasonYear = seasonYear
            };

            var queryResult = await procedure.QueryAsync<SoccerTournamentOptionRecord>(cancellation: cancellation);
            if (queryResult.IsError)
            {
                return Result<TeamTournamentOptionsResponse>.Error(ErrorCode.DatabaseError);
            }

            var response = new TeamTournamentOptionsResponse
            {
                Tournaments = queryResult.Values1.Select(row => new TeamTournamentOptionDto
                {
                    TournamentId = row.TournamentId,
                    Name = row.Name,
                    Format = row.Format,
                    AgeGroup = NullIfEmpty(row.AgeGroup)
                }).ToList()
            };

            return Result<TeamTournamentOptionsResponse>.Success(response);
        }

        public async Task<Result<Guid?>> CreateMatchResultByManagerAsync(
            Guid managerUserId, CreateTeamMatchResultRequest request, CancellationToken cancellation = default)
        {
            // 득점자는 JSON으로 넘겨 프로시저가 한 트랜잭션에 삽입한다 (경기 1행 + 이벤트 N행)
            string? scorers = request.Scorers.Count > 0 ? JsonSerializer.Serialize(request.Scorers) : null;

            var procedure = new UspCreateSoccerTeamMatchResult(this)
            {
                ManagerUserId = managerUserId,
                OpponentName = request.OpponentName,
                IsHome = request.IsHome,
                OurScore = request.OurScore,
                OpponentScore = request.OpponentScore,
                MatchedAt = request.MatchedAt,
                VenueName = request.VenueName,
                Scorers = scorers
            };

            var queryResult = await procedure.QueryAsync<SoccerCreatedMatchRecord>(cancellation: cancellation);
            if (queryResult.IsError)
            {
                return Result<Guid?>.Error(ErrorCode.DatabaseError);
            }

            var row = queryResult.Values1.FirstOrDefault();
            if (row is null)
            {
                return Result<Guid?>.Success(null);
            }

            // 친선으로 저장된다 — 순위표(Official만 집계)에는 영향이 없다
            return Result<Guid?>.Success(row.MatchId);
        }

        //.// 공식 기록 수정 신청 — 생성·조회·취소만 (심사·반영은 주최측 몫)

        public async Task<Result<Guid?>> CreateRecordCorrectionAsync(
            Guid managerUserId, CreateRecordCorrectionRequest request, CancellationToken cancellation = default)
        {
            var procedure = new UspCreateSoccerRecordCorrection(this)
            {
                ManagerUserId = managerUserId,
                MatchId = request.MatchId,
                FieldType = request.FieldType,
                CurrentValue = request.CurrentValue!,
                RequestedValue = request.RequestedValue,
                Description = request.Description!
            };

            var queryResult = await procedure.QueryAsync<SoccerCorrectionCreatedRecord>(cancellation: cancellation);
            if (queryResult.IsError)
            {
                return Result<Guid?>.Error(ErrorCode.DatabaseError);
            }

            var row = queryResult.Values1.FirstOrDefault();
            if (row is null)
            {
                // 남의 경기 / 친선 / 중복 신청 — 프로시저가 사유를 구분하지 않는다
                return Result<Guid?>.Success(null);
            }

            return Result<Guid?>.Success(row.CorrectionId);
        }

        public async Task<Result<Guid?>> CreateGuardianCorrectionAsync(
            Guid userId, Guid targetPlayerId, CreateRecordCorrectionRequest request, CancellationToken cancellation = default)
        {
            var procedure = new UspCreateSoccerRecordCorrectionByGuardian(this)
            {
                UserId = userId,
                TargetPlayerId = targetPlayerId,
                MatchId = request.MatchId,
                FieldType = request.FieldType,
                CurrentValue = request.CurrentValue!,
                RequestedValue = request.RequestedValue,
                Description = request.Description!
            };

            var queryResult = await procedure.QueryAsync<SoccerCorrectionCreatedRecord>(cancellation: cancellation);
            if (queryResult.IsError)
            {
                return Result<Guid?>.Error(ErrorCode.DatabaseError);
            }

            var row = queryResult.Values1.FirstOrDefault();
            if (row is null)
            {
                // 내 자녀 아님 / 출전 기록 없음 / 친선 / 중복 — 프로시저가 사유를 구분하지 않는다
                return Result<Guid?>.Success(null);
            }

            return Result<Guid?>.Success(row.CorrectionId);
        }

        public async Task<Result<PendingInvitesResponse>> GetPendingInvitesByManagerAsync(
            Guid managerUserId, CancellationToken cancellation = default)
        {
            var procedure = new UspGetSoccerPendingInvitesByManager(this) { ManagerUserId = managerUserId };
            var queryResult = await procedure.QueryAsync<SoccerPendingInviteRecord>(cancellation: cancellation);
            if (queryResult.IsError)
            {
                return Result<PendingInvitesResponse>.Error(ErrorCode.DatabaseError);
            }

            var response = new PendingInvitesResponse
            {
                Invites = queryResult.Values1
                    .Select(i => new PendingInviteDto
                    {
                        InviteId = i.InviteId,
                        TeamId = i.TeamId,
                        TeamName = i.TeamName,
                        PlayerId = i.PlayerId,
                        PlayerName = NullIfEmpty(i.Name),
                        CreatedAt = i.CreatedAt
                    })
                    .ToList()
            };

            return Result<PendingInvitesResponse>.Success(response);
        }

        public async Task<Result<RecordCorrectionsResponse>> GetRecordCorrectionsByManagerAsync(
            Guid managerUserId, CancellationToken cancellation = default)
        {
            var procedure = new UspGetSoccerRecordCorrectionsByManager(this) { ManagerUserId = managerUserId };
            var queryResult = await procedure.QueryAsync<SoccerCorrectionRecord>(cancellation: cancellation);
            if (queryResult.IsError)
            {
                return Result<RecordCorrectionsResponse>.Error(ErrorCode.DatabaseError);
            }

            var response = new RecordCorrectionsResponse
            {
                Corrections = queryResult.Values1
                    .Select(c => new RecordCorrectionDto
                    {
                        CorrectionId = c.CorrectionId,
                        MatchId = c.MatchId,
                        FieldType = c.FieldType,
                        CurrentValue = NullIfEmpty(c.CurrentValue),
                        RequestedValue = c.RequestedValue,
                        Description = NullIfEmpty(c.Description),
                        Status = c.Status,
                        RejectReason = NullIfEmpty(c.RejectReason),
                        RequestedAt = c.CreatedAt,
                        ReviewedAt = c.ReviewedAt,
                        TournamentName = NullIfEmpty(c.Name),
                        OpponentName = c.HomeTeamId == c.TeamId ? c.AwayTeamName : c.HomeTeamName,
                        MatchedAt = c.MatchedAt
                    })
                    .ToList()
            };

            return Result<RecordCorrectionsResponse>.Success(response);
        }

        public async Task<Result<bool>> CancelRecordCorrectionAsync(
            Guid managerUserId, Guid correctionId, CancellationToken cancellation = default)
        {
            var procedure = new UspCancelSoccerRecordCorrection(this)
            {
                ManagerUserId = managerUserId,
                CorrectionId = correctionId
            };

            var queryResult = await procedure.QueryAsync<SoccerCorrectionCancelRecord>(cancellation: cancellation);
            if (queryResult.IsError)
            {
                return Result<bool>.Error(ErrorCode.DatabaseError);
            }

            bool applied = queryResult.Values1.Any();
            return Result<bool>.Success(applied);
        }

        public async Task<Result<TeamExploreResponse>> GetExploreTeamsAsync(CancellationToken cancellation = default)
        {
            // "올해 전적"의 올해는 UTC 달력 기준이다 — 시간대별로 하루 걸치는 경기가 있지만
            // 요약 지표라 그 정도 오차는 받아들인다(정확한 시즌 경계는 대회 데이터가 정한다).
            // (SQL이 시간대 산술을 하지 않게 하고, 범위 비교라 인덱스도 탄다).
            (SystemTime seasonStartUtc, SystemTime seasonEndUtc) =
                (new SystemTime(SystemTime.Now.Year, 1, 1), new SystemTime(SystemTime.Now.Year + 1, 1, 1));

            var procedure = new UspGetSoccerTeamExplore(this)
            {
                SeasonStartUtc = seasonStartUtc,
                SeasonEndUtc = seasonEndUtc
            };
            Result<MultiQueryReader> opened = await ProcedureMultipleAsync(procedure, cancellation: cancellation);
            if (opened.IsError)
            {
                return Result<TeamExploreResponse>.Error(ErrorCode.DatabaseError);
            }

            using MultiQueryReader reader = opened.Value;
            var teams = (await reader.ReadAsync<SoccerTeamExploreRecord>()).ToList();
            var values = (await reader.ReadAsync<SoccerTeamValuesEntity>()).ToList();
            var memberships = (await reader.ReadAsync<SoccerTeamPlayersEntity>()).ToList();
            var matches = (await reader.ReadAsync<SoccerMatchesEntity>()).ToList();

            // 팀별 집계 — 핵심가치 상위 2 / 선수단 수 / 올해 종료·공식 경기 전적
            var valuesByTeam = values
                .GroupBy(v => v.TeamId)
                .ToDictionary(g => g.Key, g => g.OrderBy(v => v.DisplayOrder).Take(2).Select(v => v.Title).ToList());
            var playerCounts = memberships
                .GroupBy(m => m.TeamId)
                .ToDictionary(g => g.Key, g => g.Count());

            Dictionary<Guid, (int Wins, int Draws, int Losses)> records = new();
            foreach (SoccerMatchesEntity match in matches)
            {
                if (match.HomeScore is null || match.AwayScore is null)
                {
                    continue;
                }

                Accumulate(records, match.HomeTeamId, match.HomeScore.Value, match.AwayScore.Value);
                Accumulate(records, match.AwayTeamId, match.AwayScore.Value, match.HomeScore.Value);
            }

            var response = new TeamExploreResponse
            {
                Teams = teams
                    // 슬러그가 없으면 팀 홈으로 갈 수 없다 — 탐색 목록에 넣으면 죽은 카드가 된다
                    .Where(t => !string.IsNullOrEmpty(t.Slug))
                    .Select(t =>
                    {
                        (int wins, int draws, int losses) = records.GetValueOrDefault(t.TeamId);
                        return new TeamExploreItemDto
                        {
                            TeamName = t.TeamName,
                            Slug = t.Slug!,
                            TeamType = NullIfEmpty(t.TeamType),
                            Region = NullIfEmpty(t.Region),
                            AgeGroup = NullIfEmpty(t.AgeGroup),
                            LogoUrl = NullIfEmpty(t.LogoUrl),
                            CoverImageUrl = NullIfEmpty(t.CoverImageUrl),
                            IsVerified = t.IsVerified,
                            IsRecruiting = t.IsRecruiting,
                            Values = valuesByTeam.GetValueOrDefault(t.TeamId) ?? new List<string>(),
                            PlayerCount = playerCounts.GetValueOrDefault(t.TeamId),
                            Wins = wins,
                            Draws = draws,
                            Losses = losses
                        };
                    })
                    .ToList()
            };

            return Result<TeamExploreResponse>.Success(response);
        }

        private static void Accumulate(Dictionary<Guid, (int Wins, int Draws, int Losses)> records, Guid? teamId, int scored, int conceded)
        {
            if (teamId is null)
            {
                return;
            }

            (int wins, int draws, int losses) = records.GetValueOrDefault(teamId.Value);
            if (scored > conceded)
            {
                wins++;
            }
            else if (scored == conceded)
            {
                draws++;
            }
            else
            {
                losses++;
            }

            records[teamId.Value] = (wins, draws, losses);
        }

        private static string? NullIfEmpty(string? value)
        {
            return string.IsNullOrEmpty(value) ? null : value;
        }

        // 실적 칩 JSON 배열 파싱 — 손상된 값은 빈 목록으로 (조회 실패 사유가 아님)
        private static List<string> ParseAchievements(string? json)
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

        //.// 모집 공고 (Design.TeamPublicHome ④ 모집)

        public async Task<Result<TeamRecruitmentsResponse>> GetRecruitmentsBySlugAsync(string slug, CancellationToken cancellation = default)
        {
            var procedure = new UspGetSoccerTeamRecruitmentsBySlug(this) { Slug = slug };
            Result<MultiQueryReader> opened = await ProcedureMultipleAsync(procedure, cancellation: cancellation);
            if (opened.IsError)
            {
                return Result<TeamRecruitmentsResponse>.Error(ErrorCode.DatabaseError, "GetRecruitmentsBySlug");
            }

            using MultiQueryReader reader = opened.Value;
            return Result<TeamRecruitmentsResponse>.Success(await MapRecruitmentsAsync(reader));
        }

        public async Task<Result<TeamRecruitmentsResponse>> GetRecruitmentsByManagerAsync(Guid managerUserId, CancellationToken cancellation = default)
        {
            var procedure = new UspGetSoccerTeamRecruitmentsByManager(this) { ManagerUserId = managerUserId };
            Result<MultiQueryReader> opened = await ProcedureMultipleAsync(procedure, cancellation: cancellation);
            if (opened.IsError)
            {
                return Result<TeamRecruitmentsResponse>.Error(ErrorCode.DatabaseError, "GetRecruitmentsByManager");
            }

            using MultiQueryReader reader = opened.Value;
            return Result<TeamRecruitmentsResponse>.Success(await MapRecruitmentsAsync(reader));
        }

        // RS1 = 공고, RS2 = 수락 지원의 공고 Id(공고별 COUNT = AcceptedCount). 두 결과셋을 여기서 합친다.
        private static async Task<TeamRecruitmentsResponse> MapRecruitmentsAsync(MultiQueryReader reader)
        {
            var rows = (await reader.ReadAsync<SoccerTeamRecruitmentsEntity>()).ToList();
            var acceptedIds = (await reader.ReadAsync<Guid>()).ToList();

            var acceptedByRecruitment = acceptedIds
                .GroupBy(id => id)
                .ToDictionary(g => g.Key, g => g.Count());

            return new TeamRecruitmentsResponse
            {
                Items = rows
                    .Select(r => MapRecruitment(r, acceptedByRecruitment.GetValueOrDefault(r.RecruitmentId)))
                    .ToList()
            };
        }

        public async Task<Result<TeamRecruitmentDto?>> SaveRecruitmentByManagerAsync(
            Guid managerUserId, SaveTeamRecruitmentRequest request, CancellationToken cancellation = default)
        {
            var procedure = new UspSaveSoccerTeamRecruitment(this)
            {
                ManagerUserId = managerUserId,
                RecruitmentId = request.RecruitmentId,
                Title = request.Title,
                Description = request.Description,
                ConditionsJson = request.Conditions.Count > 0 ? JsonSerializer.Serialize(request.Conditions) : null,
                // 마감 순간은 클라이언트가 "그 날의 끝"으로 이미 변환해 보낸다 — 서버는 그대로 저장하고
                // 프로시저가 [DeadlineAt] > dbo.UfnSystemDate() 하나로 판정한다(SQL에 시간대가 안 들어간다).
                DeadlineAt = request.DeadlineAt,
                AgeGroup = request.AgeGroup,
                PositionsJson = request.Positions.Count > 0 ? JsonSerializer.Serialize(request.Positions) : null,
                Capacity = request.Capacity
            };
            var queryResult = await procedure.QueryAsync<SoccerTeamRecruitmentsEntity>(cancellation: cancellation);
            if (queryResult.IsError)
            {
                return Result<TeamRecruitmentDto?>.Error(ErrorCode.DatabaseError, "SaveRecruitment");
            }

            SoccerTeamRecruitmentsEntity? row = queryResult.Values1.FirstOrDefault();
            return Result<TeamRecruitmentDto?>.Success(row is null ? null : MapRecruitment(row));
        }

        public async Task<Result<TeamRecruitmentDto?>> CloseRecruitmentByManagerAsync(
            Guid managerUserId, Guid recruitmentId, CancellationToken cancellation = default)
        {
            var procedure = new UspCloseSoccerTeamRecruitment(this)
            {
                ManagerUserId = managerUserId,
                RecruitmentId = recruitmentId
            };
            var queryResult = await procedure.QueryAsync<SoccerTeamRecruitmentsEntity>(cancellation: cancellation);
            if (queryResult.IsError)
            {
                return Result<TeamRecruitmentDto?>.Error(ErrorCode.DatabaseError, "CloseRecruitment");
            }

            SoccerTeamRecruitmentsEntity? row = queryResult.Values1.FirstOrDefault();
            return Result<TeamRecruitmentDto?>.Success(row is null ? null : MapRecruitment(row));
        }

        public async Task<Result<bool>> DeleteRecruitmentByManagerAsync(
            Guid managerUserId, Guid recruitmentId, bool restore, CancellationToken cancellation = default)
        {
            var procedure = new UspDeleteSoccerTeamRecruitment(this)
            {
                ManagerUserId = managerUserId,
                RecruitmentId = recruitmentId,
                Restore = restore
            };
            var queryResult = await procedure.QueryAsync<SoccerTeamRecruitmentsEntity>(cancellation: cancellation);
            if (queryResult.IsError)
            {
                return Result<bool>.Error(ErrorCode.DatabaseError, "DeleteRecruitment");
            }

            return Result<bool>.Success(queryResult.Values1.Count > 0);
        }

        //.// 팀 게시판 (Team Board)

        public async Task<Result<TeamPostsResponse>> GetPostsByManagerAsync(Guid managerUserId, CancellationToken cancellation = default)
        {
            var procedure = new UspGetSoccerTeamPostsByManager(this) { ManagerUserId = managerUserId };
            Result<MultiQueryReader> opened = await ProcedureMultipleAsync(procedure, cancellation: cancellation);
            if (opened.IsError)
            {
                return Result<TeamPostsResponse>.Error(ErrorCode.DatabaseError, "GetPostsByManager");
            }

            using MultiQueryReader reader = opened.Value;
            var posts = (await reader.ReadAsync<SoccerTeamPostsEntity>()).ToList();
            var files = (await reader.ReadAsync<SoccerTeamPostFilesEntity>()).ToList();
            var readIds = (await reader.ReadAsync<Guid>()).ToList();

            Dictionary<Guid, List<TeamPostFileDto>> filesByPost = GroupFiles(files);
            Dictionary<Guid, int> viewByPost = readIds.GroupBy(id => id).ToDictionary(g => g.Key, g => g.Count());

            return Result<TeamPostsResponse>.Success(new TeamPostsResponse
            {
                Posts = posts
                    .Select(p => MapPost(p, filesByPost.GetValueOrDefault(p.PostId) ?? new List<TeamPostFileDto>(),
                        viewByPost.GetValueOrDefault(p.PostId), isRead: false))
                    .ToList()
            });
        }

        public async Task<Result<TeamNewsResponse>> GetNewsBySlugAsync(string slug, CancellationToken cancellation = default)
        {
            var procedure = new UspGetSoccerTeamPostsBySlug(this) { Slug = slug };
            Result<MultiQueryReader> opened = await ProcedureMultipleAsync(procedure, cancellation: cancellation);
            if (opened.IsError)
            {
                return Result<TeamNewsResponse>.Error(ErrorCode.DatabaseError, "GetNewsBySlug");
            }

            using MultiQueryReader reader = opened.Value;
            var posts = (await reader.ReadAsync<SoccerTeamPostsEntity>()).ToList();
            var files = (await reader.ReadAsync<SoccerTeamPostFilesEntity>()).ToList();

            Dictionary<Guid, List<TeamNewsFileDto>> filesByPost = files
                .GroupBy(f => f.PostId)
                .ToDictionary(g => g.Key, g => g.Select(f => new TeamNewsFileDto
                {
                    FileName = f.FileName,
                    SizeBytes = f.SizeBytes
                }).ToList());

            return Result<TeamNewsResponse>.Success(new TeamNewsResponse
            {
                Items = posts.Select(p => new TeamNewsDto
                {
                    PostId = p.PostId,
                    Title = p.Title,
                    Body = p.Body,
                    EditedAt = p.EditedAt,
                    CreatedAt = p.CreatedAt,
                    Files = filesByPost.GetValueOrDefault(p.PostId) ?? new List<TeamNewsFileDto>()
                }).ToList()
            });
        }

        public async Task<Result<GuardianTeamPostsResponse>> GetPostsByGuardianAsync(
            Guid userId, Guid playerId, CancellationToken cancellation = default)
        {
            var procedure = new UspGetSoccerTeamPostsByGuardian(this) { UserId = userId, PlayerId = playerId };
            Result<MultiQueryReader> opened = await ProcedureMultipleAsync(procedure, cancellation: cancellation);
            if (opened.IsError)
            {
                return Result<GuardianTeamPostsResponse>.Error(ErrorCode.DatabaseError, "GetPostsByGuardian");
            }

            using MultiQueryReader reader = opened.Value;
            string teamName = await reader.ReadSingleOrDefaultAsync<string>() ?? string.Empty;
            var posts = (await reader.ReadAsync<SoccerTeamPostsEntity>()).ToList();
            var files = (await reader.ReadAsync<SoccerTeamPostFilesEntity>()).ToList();
            var readIds = (await reader.ReadAsync<Guid>()).ToList();

            Dictionary<Guid, List<TeamPostFileDto>> filesByPost = GroupFiles(files);
            HashSet<Guid> readSet = readIds.ToHashSet();

            return Result<GuardianTeamPostsResponse>.Success(new GuardianTeamPostsResponse
            {
                TeamName = teamName,
                Posts = posts
                    .Select(p => MapPost(p, filesByPost.GetValueOrDefault(p.PostId) ?? new List<TeamPostFileDto>(),
                        viewCount: 0, isRead: readSet.Contains(p.PostId)))
                    .ToList()
            });
        }

        public async Task<Result<TeamPostDto?>> SavePostByManagerAsync(
            Guid managerUserId, SaveTeamPostRequest request, string? authorName, CancellationToken cancellation = default)
        {
            string? filesJson = request.Files.Count > 0
                ? JsonSerializer.Serialize(request.Files.Select((f, i) => new { url = f.Url, name = f.Name, sizeBytes = f.SizeBytes, ord = i }))
                : null;

            var procedure = new UspSaveSoccerTeamPost(this)
            {
                ManagerUserId = managerUserId,
                PostId = request.PostId,
                Type = request.Type,
                Title = request.Title,
                Body = request.Body,
                IsPublic = request.IsPublic,
                AuthorName = authorName!,
                FilesJson = filesJson!
            };

            Result<MultiQueryReader> opened = await ProcedureMultipleAsync(procedure, cancellation: cancellation);
            if (opened.IsError)
            {
                return Result<TeamPostDto?>.Error(ErrorCode.DatabaseError, "SavePost");
            }

            using MultiQueryReader reader = opened.Value;
            SoccerTeamPostsEntity? row = await reader.ReadSingleOrDefaultAsync<SoccerTeamPostsEntity>();
            var files = (await reader.ReadAsync<SoccerTeamPostFilesEntity>()).ToList();

            if (row is null)
            {
                return Result<TeamPostDto?>.Success(null);
            }

            return Result<TeamPostDto?>.Success(MapPost(row, files.Select(MapFile).ToList(), viewCount: 0, isRead: false));
        }

        public async Task<Result<TeamPostDto?>> SetPostPinnedByManagerAsync(
            Guid managerUserId, Guid postId, bool isPinned, CancellationToken cancellation = default)
        {
            var procedure = new UspSetSoccerTeamPostPinned(this)
            {
                ManagerUserId = managerUserId,
                PostId = postId,
                IsPinned = isPinned
            };
            var queryResult = await procedure.QueryAsync<SoccerTeamPostsEntity>(cancellation: cancellation);
            if (queryResult.IsError)
            {
                return Result<TeamPostDto?>.Error(ErrorCode.DatabaseError, "SetPostPinned");
            }

            SoccerTeamPostsEntity? row = queryResult.Values1.FirstOrDefault();
            return Result<TeamPostDto?>.Success(row is null ? null : MapPost(row, new List<TeamPostFileDto>(), 0, false));
        }

        public async Task<Result<TeamPostDto?>> SetPostPublicByManagerAsync(
            Guid managerUserId, Guid postId, bool isPublic, CancellationToken cancellation = default)
        {
            var procedure = new UspSetSoccerTeamPostPublic(this)
            {
                ManagerUserId = managerUserId,
                PostId = postId,
                IsPublic = isPublic
            };
            var queryResult = await procedure.QueryAsync<SoccerTeamPostsEntity>(cancellation: cancellation);
            if (queryResult.IsError)
            {
                return Result<TeamPostDto?>.Error(ErrorCode.DatabaseError, "SetPostPublic");
            }

            SoccerTeamPostsEntity? row = queryResult.Values1.FirstOrDefault();
            return Result<TeamPostDto?>.Success(row is null ? null : MapPost(row, new List<TeamPostFileDto>(), 0, false));
        }

        public async Task<Result<bool>> DeletePostByManagerAsync(
            Guid managerUserId, Guid postId, bool restore, CancellationToken cancellation = default)
        {
            var procedure = new UspDeleteSoccerTeamPost(this)
            {
                ManagerUserId = managerUserId,
                PostId = postId,
                Restore = restore
            };
            var queryResult = await procedure.QueryAsync<SoccerTeamPostsEntity>(cancellation: cancellation);
            if (queryResult.IsError)
            {
                return Result<bool>.Error(ErrorCode.DatabaseError, "DeletePost");
            }

            return Result<bool>.Success(queryResult.Values1.Count > 0);
        }

        public async Task<Result<bool>> MarkPostReadAsync(Guid userId, Guid postId, CancellationToken cancellation = default)
        {
            var procedure = new UspMarkSoccerTeamPostRead(this) { UserId = userId, PostId = postId };
            var queryResult = await procedure.QueryAsync<Guid>(cancellation: cancellation);
            if (queryResult.IsError)
            {
                return Result<bool>.Error(ErrorCode.DatabaseError, "MarkPostRead");
            }

            return Result<bool>.Success(queryResult.Values1.Count > 0);
        }

        public async Task<Result<Dictionary<Guid, int>>> GetPostUnreadCountsByGuardianAsync(Guid userId, CancellationToken cancellation = default)
        {
            var procedure = new UspGetSoccerTeamPostUnreadByGuardian(this) { UserId = userId };
            var queryResult = await procedure.QueryAsync<Guid>(cancellation: cancellation);
            if (queryResult.IsError)
            {
                return Result<Dictionary<Guid, int>>.Error(ErrorCode.DatabaseError, "GetPostUnreadCounts");
            }

            Dictionary<Guid, int> byPlayer = queryResult.Values1
                .GroupBy(id => id)
                .ToDictionary(g => g.Key, g => g.Count());

            return Result<Dictionary<Guid, int>>.Success(byPlayer);
        }

        public async Task<Result<List<NotificationRecipient>>> GetPostRecipientsByManagerAsync(Guid managerUserId, CancellationToken cancellation = default)
        {
            var procedure = new UspGetSoccerTeamPostRecipients(this) { ManagerUserId = managerUserId };
            var queryResult = await procedure.QueryAsync<SoccerTeamPostRecipientRecord>(cancellation: cancellation);
            if (queryResult.IsError)
            {
                return Result<List<NotificationRecipient>>.Error(ErrorCode.DatabaseError, "GetPostRecipients");
            }

            List<NotificationRecipient> recipients = queryResult.Values1
                .Where(r => r.UserId is not null)
                .Select(r => new NotificationRecipient
                {
                    UserId = r.UserId!.Value,
                    PlayerId = r.PlayerId,
                    PlayerName = r.Name,
                    TeamName = r.TeamName
                })
                .ToList();

            return Result<List<NotificationRecipient>>.Success(recipients);
        }

        private static Dictionary<Guid, List<TeamPostFileDto>> GroupFiles(List<SoccerTeamPostFilesEntity> files)
        {
            return files
                .GroupBy(f => f.PostId)
                .ToDictionary(g => g.Key, g => g.Select(MapFile).ToList());
        }

        private static TeamPostFileDto MapFile(SoccerTeamPostFilesEntity f)
        {
            return new TeamPostFileDto
            {
                FileId = f.FileId,
                FileUrl = f.FileUrl,
                FileName = f.FileName,
                SizeBytes = f.SizeBytes
            };
        }

        private static TeamPostDto MapPost(SoccerTeamPostsEntity row, List<TeamPostFileDto> files, int viewCount, bool isRead)
        {
            return new TeamPostDto
            {
                PostId = row.PostId,
                Type = row.Type,
                Title = row.Title,
                Body = row.Body,
                IsPinned = row.IsPinned,
                IsPublic = row.IsPublic,
                AuthorName = NullIfEmpty(row.AuthorName),
                EditedAt = row.EditedAt,
                CreatedAt = row.CreatedAt,
                ViewCount = viewCount,
                IsRead = isRead,
                Files = files
            };
        }

        //.// 팀 일정 (Schedule)

        public async Task<Result<SchedulesResponse>> GetSchedulesByManagerAsync(Guid managerUserId, CancellationToken cancellation = default)
        {
            var procedure = new UspGetSoccerSchedulesByManager(this) { ManagerUserId = managerUserId };
            var queryResult = await procedure.QueryAsync<SoccerSchedulesEntity>(cancellation: cancellation);
            if (queryResult.IsError)
            {
                return Result<SchedulesResponse>.Error(ErrorCode.DatabaseError, "GetSchedulesByManager");
            }

            return Result<SchedulesResponse>.Success(new SchedulesResponse
            {
                Schedules = queryResult.Values1.Select(MapSchedule).ToList()
            });
        }

        public async Task<Result<SchedulesResponse>> GetSchedulesBySlugAsync(string slug, CancellationToken cancellation = default)
        {
            var procedure = new UspGetSoccerSchedulesBySlug(this) { Slug = slug };
            var queryResult = await procedure.QueryAsync<SoccerSchedulesEntity>(cancellation: cancellation);
            if (queryResult.IsError)
            {
                return Result<SchedulesResponse>.Error(ErrorCode.DatabaseError, "GetSchedulesBySlug");
            }

            return Result<SchedulesResponse>.Success(new SchedulesResponse
            {
                Schedules = queryResult.Values1.Select(MapSchedule).ToList()
            });
        }

        public async Task<Result<ScheduleDto?>> SaveScheduleByManagerAsync(
            Guid managerUserId, SaveScheduleRequest request, CancellationToken cancellation = default)
        {
            var procedure = new UspSaveSoccerSchedule(this)
            {
                ManagerUserId = managerUserId,
                ScheduleId = request.ScheduleId,
                Type = request.Type,
                Title = request.Title!,
                OpponentName = request.OpponentName!,
                StartsAt = request.StartsAt,
                Venue = request.Venue,
                IsPublic = request.IsPublic
            };
            var queryResult = await procedure.QueryAsync<SoccerSchedulesEntity>(cancellation: cancellation);
            if (queryResult.IsError)
            {
                return Result<ScheduleDto?>.Error(ErrorCode.DatabaseError, "SaveSchedule");
            }

            SoccerSchedulesEntity? row = queryResult.Values1.FirstOrDefault();
            return Result<ScheduleDto?>.Success(row is null ? null : MapSchedule(row));
        }

        public async Task<Result<bool>> DeleteScheduleByManagerAsync(
            Guid managerUserId, Guid scheduleId, bool restore, CancellationToken cancellation = default)
        {
            var procedure = new UspDeleteSoccerSchedule(this)
            {
                ManagerUserId = managerUserId,
                ScheduleId = scheduleId,
                Restore = restore
            };
            var queryResult = await procedure.QueryAsync<SoccerSchedulesEntity>(cancellation: cancellation);
            if (queryResult.IsError)
            {
                return Result<bool>.Error(ErrorCode.DatabaseError, "DeleteSchedule");
            }

            return Result<bool>.Success(queryResult.Values1.Any());
        }

        // HasResult = 연결된 경기 결과(MatchId) 존재 여부 파생. Title·OpponentName은 빈 문자열이면 null로 내린다.
        private static ScheduleDto MapSchedule(SoccerSchedulesEntity row)
        {
            bool hasResult = row.MatchId is not null && row.MatchId != Guid.Empty;
            return new ScheduleDto
            {
                ScheduleId = row.ScheduleId,
                Type = row.Type,
                Title = NullIfEmpty(row.Title),
                OpponentName = NullIfEmpty(row.OpponentName),
                StartsAt = row.StartsAt,
                Venue = row.Venue,
                IsPublic = row.IsPublic,
                MatchId = hasResult ? row.MatchId : null,
                HasResult = hasResult
            };
        }

        public async Task<Result<TeamCareerOutcomesResponse>> GetCareerOutcomesBySlugAsync(string slug, CancellationToken cancellation = default)
        {
            var procedure = new UspGetSoccerTeamCareerOutcomesBySlug(this) { Slug = slug };
            var queryResult = await procedure.QueryAsync<SoccerTeamCareerOutcomesEntity>(cancellation: cancellation);
            if (queryResult.IsError)
            {
                return Result<TeamCareerOutcomesResponse>.Error(ErrorCode.DatabaseError, "GetCareerOutcomesBySlug");
            }

            return Result<TeamCareerOutcomesResponse>.Success(new TeamCareerOutcomesResponse
            {
                Items = queryResult.Values1.Select(MapCareerOutcome).ToList()
            });
        }

        public async Task<Result<TeamCareerOutcomesResponse>> GetCareerOutcomesByManagerAsync(Guid managerUserId, CancellationToken cancellation = default)
        {
            var procedure = new UspGetSoccerTeamCareerOutcomesByManager(this) { ManagerUserId = managerUserId };
            var queryResult = await procedure.QueryAsync<SoccerTeamCareerOutcomesEntity>(cancellation: cancellation);
            if (queryResult.IsError)
            {
                return Result<TeamCareerOutcomesResponse>.Error(ErrorCode.DatabaseError, "GetCareerOutcomesByManager");
            }

            return Result<TeamCareerOutcomesResponse>.Success(new TeamCareerOutcomesResponse
            {
                Items = queryResult.Values1.Select(MapCareerOutcome).ToList()
            });
        }

        public async Task<Result<TeamCareerOutcomeDto?>> SaveCareerOutcomeByManagerAsync(
            Guid managerUserId, SaveTeamCareerOutcomeRequest request, CancellationToken cancellation = default)
        {
            var procedure = new UspSaveSoccerTeamCareerOutcome(this)
            {
                ManagerUserId = managerUserId,
                OutcomeId = request.OutcomeId,
                OutcomeYear = request.OutcomeYear,
                OutcomeType = request.OutcomeType,
                Title = request.Title,
                Detail = request.Detail!,
                PlayerCount = request.PlayerCount
            };
            var queryResult = await procedure.QueryAsync<SoccerTeamCareerOutcomesEntity>(cancellation: cancellation);
            if (queryResult.IsError)
            {
                return Result<TeamCareerOutcomeDto?>.Error(ErrorCode.DatabaseError, "SaveCareerOutcome");
            }

            SoccerTeamCareerOutcomesEntity? row = queryResult.Values1.FirstOrDefault();
            return Result<TeamCareerOutcomeDto?>.Success(row is null ? null : MapCareerOutcome(row));
        }

        public async Task<Result<bool>> DeleteCareerOutcomeByManagerAsync(
            Guid managerUserId, Guid outcomeId, bool restore, CancellationToken cancellation = default)
        {
            var procedure = new UspDeleteSoccerTeamCareerOutcome(this)
            {
                ManagerUserId = managerUserId,
                OutcomeId = outcomeId,
                Restore = restore
            };
            var queryResult = await procedure.QueryAsync<SoccerTeamCareerOutcomesEntity>(cancellation: cancellation);
            if (queryResult.IsError)
            {
                return Result<bool>.Error(ErrorCode.DatabaseError, "DeleteCareerOutcome");
            }

            return Result<bool>.Success(queryResult.Values1.Any());
        }

        public async Task<Result<TeamReviewsResponse>> GetReviewsBySlugAsync(string slug, Guid? viewerUserId, CancellationToken cancellation = default)
        {
            var procedure = new UspGetSoccerTeamReviewsBySlug(this) { Slug = slug, ViewerUserId = viewerUserId };
            Result<MultiQueryReader> opened = await ProcedureMultipleAsync(procedure, cancellation: cancellation);
            if (opened.IsError)
            {
                return Result<TeamReviewsResponse>.Error(ErrorCode.DatabaseError, "GetReviewsBySlug");
            }

            using MultiQueryReader reader = opened.Value;
            var rows = (await reader.ReadAsync<SoccerTeamReviewRecord>()).ToList();
            var viewer = await reader.ReadSingleOrDefaultAsync<(bool IsResidentGuardian, Guid? MyReviewId)>();

            var response = new TeamReviewsResponse
            {
                Items = rows.Select(MapReview).ToList(),
                IsResidentGuardian = viewer.IsResidentGuardian,
                MyReviewId = viewer.MyReviewId
            };

            return Result<TeamReviewsResponse>.Success(response);
        }

        public async Task<Result<bool>> SaveReviewAsync(Guid authorUserId, SaveTeamReviewRequest request, CancellationToken cancellation = default)
        {
            var procedure = new UspSaveSoccerTeamReview(this)
            {
                AuthorUserId = authorUserId,
                TeamSlug = request.TeamSlug,
                ReviewId = request.ReviewId,
                Rating = request.Rating,
                Body = request.Body
            };
            var queryResult = await procedure.QueryAsync<SoccerTeamReviewsEntity>(cancellation: cancellation);
            if (queryResult.IsError)
            {
                return Result<bool>.Error(ErrorCode.DatabaseError, "SaveReview");
            }

            return Result<bool>.Success(queryResult.Values1.Any());
        }

        public async Task<Result<bool>> DeleteReviewAsync(Guid authorUserId, Guid reviewId, bool restore, CancellationToken cancellation = default)
        {
            var procedure = new UspDeleteSoccerTeamReview(this)
            {
                AuthorUserId = authorUserId,
                ReviewId = reviewId,
                Restore = restore
            };
            var queryResult = await procedure.QueryAsync<SoccerTeamReviewsEntity>(cancellation: cancellation);
            if (queryResult.IsError)
            {
                return Result<bool>.Error(ErrorCode.DatabaseError, "DeleteReview");
            }

            return Result<bool>.Success(queryResult.Values1.Any());
        }

        // "이○○ 학부모 · U15 · 재원 2년차" — 이름은 리뷰에서도 실명을 노출하지 않는다 (dc '○' 마스킹)
        private static TeamReviewDto MapReview(SoccerTeamReviewRecord row)
        {
            List<string> metaParts = new();
            if (!string.IsNullOrEmpty(row.AgeGroup))
            {
                metaParts.Add(row.AgeGroup);
            }

            if (row.CreatedAt > SystemTime.MinValue)
            {
                int years = Math.Max(1, SystemTime.Now.Year - row.CreatedAt.Year + 1);
                metaParts.Add($"재원 {years}년차");
            }

            string masked = string.IsNullOrEmpty(row.MemberName) ? "○○"
                : row.MemberName.Length <= 1 ? row.MemberName
                : row.MemberName[..1] + new string('○', row.MemberName.Length - 1);

            return new TeamReviewDto
            {
                ReviewId = row.ReviewId,
                AuthorDisplayName = $"{masked} 학부모",
                Meta = metaParts.Count > 0 ? string.Join(" · ", metaParts) : null,
                Rating = row.Rating,
                Body = row.Body
            };
        }

        private static TeamCareerOutcomeDto MapCareerOutcome(SoccerTeamCareerOutcomesEntity row)
        {
            return new TeamCareerOutcomeDto
            {
                OutcomeId = row.OutcomeId,
                OutcomeYear = row.OutcomeYear,
                OutcomeType = row.OutcomeType,
                Title = row.Title,
                Detail = NullIfEmpty(row.Detail),
                PlayerCount = row.PlayerCount
            };
        }

        // "모집중" 판정을 여기 한 곳에서 파생 — 팀 탐색(SQL EXISTS)과 같은 기준 (Open + 마감일 미경과)
        // acceptedCount는 수락 지원 수(별도 결과셋에서 공고별로 집계) — 저장 경로는 0으로 온다(조회에서 다시 계산).
        private static TeamRecruitmentDto MapRecruitment(SoccerTeamRecruitmentsEntity row, int acceptedCount = 0)
        {
            return new TeamRecruitmentDto
            {
                RecruitmentId = row.RecruitmentId,
                Title = row.Title,
                Description = row.Description,
                Conditions = ParseAchievements(row.ConditionsJson),
                // 순간 그대로 내려보낸다 — 표시할 날짜는 보는 사람의 시간대로 클라이언트가 만든다
                DeadlineAt = row.DeadlineAt,
                Status = row.Status,
                IsOpen = row.Status == "Open"
                         && (row.DeadlineAt is null || row.DeadlineAt > SystemTime.Now),
                AgeGroup = NullIfEmpty(row.AgeGroup),
                Positions = ParseAchievements(row.PositionsJson),
                Capacity = row.Capacity,
                AcceptedCount = acceptedCount
            };
        }

        //.// 선수 지원(Application)

        public async Task<Result<(string Status, Guid? ApplicationId)>> CreateApplicationAsync(
            Guid guardianUserId, CreateApplicationRequest request, CancellationToken cancellation = default)
        {
            var procedure = new UspCreateSoccerApplication(this)
            {
                GuardianUserId = guardianUserId,
                RecruitmentId = request.RecruitmentId,
                PlayerId = request.PlayerId,
                DesiredPosition = request.DesiredPosition,
                Introduction = request.Introduction
            };

            var queryResult = await procedure.QueryAsync<SoccerApplicationCreateRecord>(cancellation: cancellation);
            if (queryResult.IsError)
            {
                return Result<(string, Guid?)>.Error(ErrorCode.DatabaseError, "CreateApplication");
            }

            SoccerApplicationCreateRecord? row = queryResult.Values1.FirstOrDefault();
            if (row is null)
            {
                return Result<(string, Guid?)>.Error(ErrorCode.OperationFailed, "no status row");
            }

            Guid? id = row.Status == "Ok" && row.ApplicationId != Guid.Empty ? row.ApplicationId : null;
            return Result<(string, Guid?)>.Success((row.Status, id));
        }

        public async Task<Result<TeamApplicationsResponse>> GetApplicationsByManagerAsync(
            Guid managerUserId, CancellationToken cancellation = default)
        {
            var procedure = new UspGetSoccerApplicationsByManager(this) { ManagerUserId = managerUserId };
            Result<MultiQueryReader> opened = await ProcedureMultipleAsync(procedure, cancellation: cancellation);
            if (opened.IsError)
            {
                return Result<TeamApplicationsResponse>.Error(ErrorCode.DatabaseError, "GetApplicationsByManager");
            }

            using MultiQueryReader reader = opened.Value;
            var rows = (await reader.ReadAsync<SoccerApplicationManagerRecord>()).ToList();
            var agents = (await reader.ReadAsync<SoccerAgentProfilesEntity>()).ToList();

            // 추천 에이전트 이름 사전 — AgentRef 지원에만 값이 붙는다 (현재 경로는 전부 Direct)
            var agentNames = agents
                .GroupBy(a => a.AgentId)
                .ToDictionary(g => g.Key, g => g.First().Name);

            var response = new TeamApplicationsResponse
            {
                Applications = rows
                    .Select(r => new ApplicationDto
                    {
                        ApplicationId = r.ApplicationId,
                        RecruitmentId = r.RecruitmentId,
                        RecruitmentTitle = r.Title,
                        PlayerId = r.PlayerId,
                        PlayerName = r.Name,
                        PlayerAgeGroup = NullIfEmpty(r.AgeGroup),
                        PlayerPosition = NullIfEmpty(r.Position),
                        PlayerPhotoUrl = NullIfEmpty(r.PhotoUrl),
                        DesiredPosition = NullIfEmpty(r.DesiredPosition),
                        Introduction = NullIfEmpty(r.Introduction),
                        Status = r.Status,
                        Route = r.Route,
                        RefAgentName = r.RefAgentId is not null ? agentNames.GetValueOrDefault(r.RefAgentId.Value) : null,
                        CreatedAt = r.CreatedAt
                    })
                    .ToList()
            };

            return Result<TeamApplicationsResponse>.Success(response);
        }

        public async Task<Result<MyApplicationsResponse>> GetApplicationsByGuardianAsync(
            Guid guardianUserId, CancellationToken cancellation = default)
        {
            var procedure = new UspGetSoccerApplicationsByGuardian(this) { GuardianUserId = guardianUserId };
            var queryResult = await procedure.QueryAsync<SoccerApplicationGuardianRecord>(cancellation: cancellation);
            if (queryResult.IsError)
            {
                return Result<MyApplicationsResponse>.Error(ErrorCode.DatabaseError, "GetApplicationsByGuardian");
            }

            var response = new MyApplicationsResponse
            {
                Applications = queryResult.Values1
                    .Select(r => new MyApplicationDto
                    {
                        ApplicationId = r.ApplicationId,
                        RecruitmentId = r.RecruitmentId,
                        RecruitmentTitle = r.Title,
                        TeamName = r.TeamName,
                        TeamSlug = NullIfEmpty(r.Slug),
                        PlayerId = r.PlayerId,
                        PlayerName = r.Name,
                        DesiredPosition = NullIfEmpty(r.DesiredPosition),
                        Status = r.Status,
                        Confirmed = r.ConfirmedAt != null,
                        CreatedAt = r.CreatedAt
                    })
                    .ToList()
            };

            return Result<MyApplicationsResponse>.Success(response);
        }

        public async Task<Result<bool>> UpdateApplicationStatusAsync(
            Guid managerUserId, Guid applicationId, string newStatus, CancellationToken cancellation = default)
        {
            var procedure = new UspUpdateSoccerApplicationStatus(this)
            {
                ManagerUserId = managerUserId,
                ApplicationId = applicationId,
                NewStatus = newStatus
            };
            var queryResult = await procedure.QueryAsync<SoccerApplicationCreateRecord>(cancellation: cancellation);
            if (queryResult.IsError)
            {
                return Result<bool>.Error(ErrorCode.DatabaseError, "UpdateApplicationStatus");
            }

            // 빈 결과 = 남의 팀이거나 잘못된 전환 — Command가 Forbidden으로 변환
            return Result<bool>.Success(queryResult.Values1.Any());
        }

        public async Task<Result<bool>> CancelApplicationAsync(
            Guid guardianUserId, Guid applicationId, CancellationToken cancellation = default)
        {
            var procedure = new UspCancelSoccerApplication(this)
            {
                GuardianUserId = guardianUserId,
                ApplicationId = applicationId
            };
            var queryResult = await procedure.QueryAsync<SoccerApplicationCreateRecord>(cancellation: cancellation);
            if (queryResult.IsError)
            {
                return Result<bool>.Error(ErrorCode.DatabaseError, "CancelApplication");
            }

            // 빈 결과 = 내 대기 지원이 아님 — Command가 Forbidden으로 변환
            return Result<bool>.Success(queryResult.Values1.Any());
        }

        public async Task<Result<bool>> ConfirmApplicationInviteAsync(
            Guid guardianUserId, Guid applicationId, CancellationToken cancellation = default)
        {
            var procedure = new UspConfirmSoccerApplicationInvite(this)
            {
                GuardianUserId = guardianUserId,
                ApplicationId = applicationId
            };
            var queryResult = await procedure.QueryAsync<SoccerApplicationCreateRecord>(cancellation: cancellation);
            if (queryResult.IsError)
            {
                return Result<bool>.Error(ErrorCode.DatabaseError, "ConfirmApplicationInvite");
            }

            // 빈 결과 = 내 수락(Accepted) 지원이 아님 — Command가 Forbidden으로 변환
            bool confirmed = queryResult.Values1.Any();
            return Result<bool>.Success(confirmed);
        }
    }
}
