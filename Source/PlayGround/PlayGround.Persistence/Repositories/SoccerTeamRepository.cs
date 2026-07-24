using System.Linq;
using System.Text.Json;
using Microsoft.Extensions.Options;
using PlayGround.Shared.Result;
using PlayGround.Infrastructure.Database;
using PlayGround.Infrastructure.Database.Base;
using PlayGround.Infrastructure.Logging;
using PlayGround.Contracts.Team;
using PlayGround.Application.Interfaces;
using PlayGround.Application.Team.Models;
using PlayGround.Persistence.Database.Generated.Soccer.Entities;
using PlayGround.Persistence.Database.Generated.Soccer.Procedures;

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
            Logger.InfoWith("Team creation requested",
                ("ManagerUserId", input.ManagerUserId), ("RosterCount", input.Roster.Count));

            string rosterJson = JsonSerializer.Serialize(input.Roster);

            var procedure = new UspCreateSoccerTeamWithRoster(this)
            {
                ManagerUserId = input.ManagerUserId,
                TeamName = input.TeamName,
                TeamType = input.TeamType!,
                Region = input.Region!,
                Slug = input.Slug,
                RosterJson = rosterJson
            };

            var queryResult = await procedure.QueryAsync<SoccerCreateTeamRecord>(cancellation: cancellation);
            if (queryResult.IsError)
            {
                Logger.ErrorWith("Team creation failed", ("ResultCode", queryResult.ResultCode));
                return Result<string>.Error(ErrorCode.DatabaseError);
            }

            var row = queryResult.Values1.FirstOrDefault();
            if (row is null)
            {
                Logger.ErrorWith("Team creation returned no row");
                return Result<string>.Error(ErrorCode.OperationFailed, "no row returned");
            }

            Logger.InfoWith("Team created", ("TeamId", row.TeamId), ("Slug", row.Slug));
            return Result<string>.Success(row.Slug);
        }

        public async Task<Result<TeamInfoResponse?>> GetTeamInfoByManagerAsync(Guid managerUserId, CancellationToken cancellation = default)
        {
            Logger.InfoWith("Team info requested", ("ManagerUserId", managerUserId));

            var procedure = new UspGetSoccerTeamInfoByManager(this) { ManagerUserId = managerUserId };
            Result<MultiQueryReader> opened = await ProcedureMultipleAsync(procedure, cancellation: cancellation);
            if (opened.IsError)
            {
                Logger.ErrorWith("Team info query failed", ("DetailCode", opened.ResultData.DetailCode));
                return Result<TeamInfoResponse?>.Error(ErrorCode.DatabaseError);
            }

            using MultiQueryReader reader = opened.Value;
            SoccerTeamsEntity? team = await reader.ReadSingleOrDefaultAsync<SoccerTeamsEntity>();
            if (team is null)
            {
                Logger.InfoWith("Team info not found", ("ManagerUserId", managerUserId));
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

            Logger.InfoWith("Team info received", ("TeamId", team.TeamId),
                ("Values", response.Values.Count), ("Coaches", response.Coaches.Count), ("Channels", response.Channels.Count));

            return Result<TeamInfoResponse?>.Success(response);
        }

        public async Task<Result<TeamRosterResponse>> GetTeamRosterByManagerAsync(Guid managerUserId, CancellationToken cancellation = default)
        {
            Logger.InfoWith("Team roster requested", ("ManagerUserId", managerUserId));

            var procedure = new UspGetSoccerTeamRosterByManager(this) { ManagerUserId = managerUserId };
            var queryResult = await procedure.QueryAsync<SoccerTeamRosterRecord>(cancellation: cancellation);
            if (queryResult.IsError)
            {
                Logger.ErrorWith("Team roster query failed", ("ResultCode", queryResult.ResultCode));
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
                        InviteCode = r.UserId is null ? NullIfEmpty(r.Code) : null
                    })
                    .ToList()
            };

            Logger.InfoWith("Team roster received", ("ManagerUserId", managerUserId), ("Players", response.Players.Count));
            return Result<TeamRosterResponse>.Success(response);
        }

        public async Task<Result<TeamRosterPlayerDto?>> AddTeamPlayerByManagerAsync(
            Guid managerUserId, AddTeamPlayerRequest request, CancellationToken cancellation = default)
        {
            Logger.InfoWith("Team roster add requested", ("ManagerUserId", managerUserId));

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
                Logger.ErrorWith("Team roster add failed", ("ResultCode", queryResult.ResultCode));
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
                // 새 선수는 항상 Unclaimed + Pending 초대코드
                ClaimStatus = row.UserId is null ? "Unclaimed" : "Claimed",
                InviteCode = row.UserId is null ? NullIfEmpty(row.Code) : null
            };

            Logger.InfoWith("Team roster add applied", ("ManagerUserId", managerUserId), ("TeamPlayerId", dto.TeamPlayerId));
            return Result<TeamRosterPlayerDto?>.Success(dto);
        }

        public async Task<Result<bool>> RemoveTeamPlayerByManagerAsync(
            Guid managerUserId, Guid teamPlayerId, bool restore, CancellationToken cancellation = default)
        {
            Logger.InfoWith("Team roster remove requested",
                ("ManagerUserId", managerUserId), ("TeamPlayerId", teamPlayerId), ("Restore", restore));

            var procedure = new UspRemoveSoccerTeamPlayer(this)
            {
                ManagerUserId = managerUserId,
                TeamPlayerId = teamPlayerId,
                Restore = restore
            };
            var queryResult = await procedure.QueryAsync<SoccerTeamPlayersEntity>(cancellation: cancellation);
            if (queryResult.IsError)
            {
                Logger.ErrorWith("Team roster remove failed", ("ResultCode", queryResult.ResultCode));
                return Result<bool>.Error(ErrorCode.DatabaseError, "RemoveTeamPlayer");
            }

            // 빈 결과 = 남의 팀이거나 이미 그 상태 — Command가 Forbidden으로 변환
            return Result<bool>.Success(queryResult.Values1.Any());
        }

        public async Task<Result<TeamPublicHomeResponse?>> GetTeamHomeBySlugAsync(string slug, Guid? viewerUserId = null, CancellationToken cancellation = default)
        {
            Logger.InfoWith("Team public home requested", ("Slug", slug));

            var procedure = new UspGetSoccerTeamHomeBySlug(this) { Slug = slug };
            Result<MultiQueryReader> opened = await ProcedureMultipleAsync(procedure, cancellation: cancellation);
            if (opened.IsError)
            {
                Logger.ErrorWith("Team public home query failed", ("DetailCode", opened.ResultData.DetailCode));
                return Result<TeamPublicHomeResponse?>.Error(ErrorCode.DatabaseError);
            }

            using MultiQueryReader reader = opened.Value;
            SoccerTeamsEntity? team = await reader.ReadSingleOrDefaultAsync<SoccerTeamsEntity>();
            if (team is null)
            {
                Logger.InfoWith("Team public home not found", ("Slug", slug));
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
                    // 공개/비공개 규칙 — 회비는 공개 설정일 때만 노출
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
                        Slug = r.UserId is not null ? NullIfEmpty(r.Slug) : null
                    })
                    .ToList()
            };

            Logger.InfoWith("Team public home received", ("Slug", slug), ("TeamId", team.TeamId),
                ("Values", response.Values.Count), ("Coaches", response.Coaches.Count),
                ("Channels", response.Channels.Count), ("Roster", response.Roster.Count));

            return Result<TeamPublicHomeResponse?>.Success(response);
        }

        public async Task<Result<TeamMatchesResponse>> GetTeamMatchesByManagerAsync(Guid managerUserId, int seasonYear, CancellationToken cancellation = default)
        {
            Logger.InfoWith("Team matches requested", ("ManagerUserId", managerUserId), ("SeasonYear", seasonYear));

            var procedure = new UspGetSoccerTeamMatchesByManager(this) { ManagerUserId = managerUserId, SeasonYear = seasonYear };
            Result<MultiQueryReader> opened = await ProcedureMultipleAsync(procedure, cancellation: cancellation);
            if (opened.IsError)
            {
                Logger.ErrorWith("Team matches query failed", ("DetailCode", opened.ResultData.DetailCode));
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

            Logger.InfoWith("Team matches received", ("ManagerUserId", managerUserId),
                ("Matches", response.Matches.Count), ("LeagueRank", leagueRank));

            return Result<TeamMatchesResponse>.Success(response);
        }

        public async Task<Result<TeamSeasonRecordResponse>> GetTeamSeasonRecordBySlugAsync(string slug, int seasonYear, CancellationToken cancellation = default)
        {
            Logger.InfoWith("Team season record requested", ("Slug", slug), ("SeasonYear", seasonYear));

            var procedure = new UspGetSoccerTeamSeasonRecordBySlug(this) { Slug = slug, SeasonYear = seasonYear };
            Result<MultiQueryReader> opened = await ProcedureMultipleAsync(procedure, cancellation: cancellation);
            if (opened.IsError)
            {
                Logger.ErrorWith("Team season record query failed", ("DetailCode", opened.ResultData.DetailCode));
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

            Logger.InfoWith("Team season record received", ("Slug", slug),
                ("Matches", response.Matches.Count), ("Videos", response.Videos.Count));

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

        // 경기 한 건을 우리 팀 관점으로 변환 (이벤트 목록이 비면 칩 없음)
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
                RecordedOn = video.RecordedOn is null ? null : DateOnly.FromDateTime(video.RecordedOn.Value),
                IsMatchLinked = video.MatchId is not null
            };
        }

        public async Task<Result<TeamVideosResponse>> GetTeamVideosByManagerAsync(Guid managerUserId, CancellationToken cancellation = default)
        {
            Logger.InfoWith("Team videos requested", ("ManagerUserId", managerUserId));

            var procedure = new UspGetSoccerTeamVideosByManager(this) { ManagerUserId = managerUserId };
            var queryResult = await procedure.QueryAsync<SoccerMatchVideosEntity>(cancellation: cancellation);
            if (queryResult.IsError)
            {
                Logger.ErrorWith("Team videos query failed", ("ResultCode", queryResult.ResultCode));
                return Result<TeamVideosResponse>.Error(ErrorCode.DatabaseError);
            }

            var response = new TeamVideosResponse
            {
                Videos = queryResult.Values1.Select(MapVideo).ToList()
            };

            Logger.InfoWith("Team videos received", ("ManagerUserId", managerUserId), ("Videos", response.Videos.Count));
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
            Logger.InfoWith("Team info update requested",
                ("ManagerUserId", managerUserId), ("ValueCount", request.Values.Count), ("CoachCount", request.Coaches.Count));

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
                Logger.ErrorWith("Team info update failed", ("ResultCode", queryResult.ResultCode));
                return Result<string?>.Error(ErrorCode.DatabaseError);
            }

            var row = queryResult.Values1.FirstOrDefault();
            if (row is null)
            {
                Logger.WarnWith("Team info update rejected — no team for manager", ("ManagerUserId", managerUserId));
                return Result<string?>.Success(null);
            }

            Logger.InfoWith("Team info updated", ("TeamId", row.TeamId));
            return Result<string?>.Success(NullIfEmpty(row.Slug) ?? string.Empty);
        }

        public async Task<Result<TeamTournamentOptionsResponse>> GetTournamentOptionsByManagerAsync(
            Guid managerUserId, int seasonYear, CancellationToken cancellation = default)
        {
            Logger.InfoWith("Tournament options requested", ("ManagerUserId", managerUserId), ("SeasonYear", seasonYear));

            var procedure = new UspGetSoccerTournamentOptionsByManager(this)
            {
                ManagerUserId = managerUserId,
                SeasonYear = seasonYear
            };

            var queryResult = await procedure.QueryAsync<SoccerTournamentOptionRecord>(cancellation: cancellation);
            if (queryResult.IsError)
            {
                Logger.ErrorWith("Tournament options query failed", ("ResultCode", queryResult.ResultCode));
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
            Logger.InfoWith("Friendly match result save requested",
                ("ManagerUserId", managerUserId), ("IsHome", request.IsHome));

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
                Logger.ErrorWith("Match result save failed", ("ResultCode", queryResult.ResultCode));
                return Result<Guid?>.Error(ErrorCode.DatabaseError);
            }

            var row = queryResult.Values1.FirstOrDefault();
            if (row is null)
            {
                // 팀 없음·없는 대회 — 프로시저가 빈 결과셋으로 알린다
                Logger.WarnWith("Match result rejected — no team or unknown tournament", ("ManagerUserId", managerUserId));
                return Result<Guid?>.Success(null);
            }

            // 친선으로 저장된다 — 순위표(Official만 집계)에는 영향이 없다
            Logger.InfoWith("Friendly match result saved", ("MatchId", row.MatchId));
            return Result<Guid?>.Success(row.MatchId);
        }

        //.// 공식 기록 수정 신청 — 생성·조회·취소만 (심사·반영은 주최측 몫)

        public async Task<Result<Guid?>> CreateRecordCorrectionAsync(
            Guid managerUserId, CreateRecordCorrectionRequest request, CancellationToken cancellation = default)
        {
            Logger.InfoWith("Record correction requested",
                ("ManagerUserId", managerUserId), ("MatchId", request.MatchId), ("FieldType", request.FieldType));

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
                Logger.ErrorWith("Record correction create failed", ("ResultCode", queryResult.ResultCode));
                return Result<Guid?>.Error(ErrorCode.DatabaseError);
            }

            var row = queryResult.Values1.FirstOrDefault();
            if (row is null)
            {
                // 남의 경기 / 친선 / 중복 신청 — 프로시저가 사유를 구분하지 않는다
                Logger.WarnWith("Record correction rejected", ("ManagerUserId", managerUserId), ("MatchId", request.MatchId));
                return Result<Guid?>.Success(null);
            }

            Logger.InfoWith("Record correction created", ("CorrectionId", row.CorrectionId));
            return Result<Guid?>.Success(row.CorrectionId);
        }

        public async Task<Result<PendingInvitesResponse>> GetPendingInvitesByManagerAsync(
            Guid managerUserId, CancellationToken cancellation = default)
        {
            Logger.InfoWith("Pending invites requested", ("ManagerUserId", managerUserId));

            var procedure = new UspGetSoccerPendingInvitesByManager(this) { ManagerUserId = managerUserId };
            var queryResult = await procedure.QueryAsync<SoccerPendingInviteRecord>(cancellation: cancellation);
            if (queryResult.IsError)
            {
                Logger.ErrorWith("Pending invites query failed", ("ResultCode", queryResult.ResultCode));
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

            Logger.InfoWith("Pending invites received",
                ("ManagerUserId", managerUserId), ("Invites", response.Invites.Count));

            return Result<PendingInvitesResponse>.Success(response);
        }

        public async Task<Result<RecordCorrectionsResponse>> GetRecordCorrectionsByManagerAsync(
            Guid managerUserId, CancellationToken cancellation = default)
        {
            Logger.InfoWith("Record corrections requested", ("ManagerUserId", managerUserId));

            var procedure = new UspGetSoccerRecordCorrectionsByManager(this) { ManagerUserId = managerUserId };
            var queryResult = await procedure.QueryAsync<SoccerCorrectionRecord>(cancellation: cancellation);
            if (queryResult.IsError)
            {
                Logger.ErrorWith("Record corrections query failed", ("ResultCode", queryResult.ResultCode));
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
                        // 신청자 관점의 상대 — 우리가 홈이면 원정팀이 상대다
                        OpponentName = c.HomeTeamId == c.TeamId ? c.AwayTeamName : c.HomeTeamName,
                        MatchedAt = c.MatchedAt
                    })
                    .ToList()
            };

            Logger.InfoWith("Record corrections received",
                ("ManagerUserId", managerUserId), ("Corrections", response.Corrections.Count));

            return Result<RecordCorrectionsResponse>.Success(response);
        }

        public async Task<Result<bool>> CancelRecordCorrectionAsync(
            Guid managerUserId, Guid correctionId, CancellationToken cancellation = default)
        {
            Logger.InfoWith("Record correction cancel requested",
                ("ManagerUserId", managerUserId), ("CorrectionId", correctionId));

            var procedure = new UspCancelSoccerRecordCorrection(this)
            {
                ManagerUserId = managerUserId,
                CorrectionId = correctionId
            };

            var queryResult = await procedure.QueryAsync<SoccerCorrectionCancelRecord>(cancellation: cancellation);
            if (queryResult.IsError)
            {
                Logger.ErrorWith("Record correction cancel failed", ("ResultCode", queryResult.ResultCode));
                return Result<bool>.Error(ErrorCode.DatabaseError);
            }

            bool applied = queryResult.Values1.Any();
            Logger.InfoWith("Record correction cancel completed", ("CorrectionId", correctionId), ("Applied", applied));
            return Result<bool>.Success(applied);
        }

        public async Task<Result<TeamExploreResponse>> GetExploreTeamsAsync(CancellationToken cancellation = default)
        {
            Logger.InfoWith("Team explore list requested");

            var procedure = new UspGetSoccerTeamExplore(this);
            Result<MultiQueryReader> opened = await ProcedureMultipleAsync(procedure, cancellation: cancellation);
            if (opened.IsError)
            {
                Logger.ErrorWith("Team explore query failed", ("DetailCode", opened.ResultData.DetailCode));
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
                    .Select(t =>
                    {
                        (int wins, int draws, int losses) = records.GetValueOrDefault(t.TeamId);
                        return new TeamExploreItemDto
                        {
                            TeamName = t.TeamName,
                            Slug = t.Slug,
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

            Logger.InfoWith("Team explore list received", ("Teams", response.Teams.Count));
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
            Logger.InfoWith("Team recruitments requested", ("Slug", slug));

            var procedure = new UspGetSoccerTeamRecruitmentsBySlug(this) { Slug = slug };
            var queryResult = await procedure.QueryAsync<SoccerTeamRecruitmentsEntity>(cancellation: cancellation);
            if (queryResult.IsError)
            {
                return Result<TeamRecruitmentsResponse>.Error(ErrorCode.DatabaseError, "GetRecruitmentsBySlug");
            }

            return Result<TeamRecruitmentsResponse>.Success(new TeamRecruitmentsResponse
            {
                Items = queryResult.Values1.Select(MapRecruitment).ToList()
            });
        }

        public async Task<Result<TeamRecruitmentsResponse>> GetRecruitmentsByManagerAsync(Guid managerUserId, CancellationToken cancellation = default)
        {
            Logger.InfoWith("Team recruitments requested by manager", ("ManagerUserId", managerUserId));

            var procedure = new UspGetSoccerTeamRecruitmentsByManager(this) { ManagerUserId = managerUserId };
            var queryResult = await procedure.QueryAsync<SoccerTeamRecruitmentsEntity>(cancellation: cancellation);
            if (queryResult.IsError)
            {
                return Result<TeamRecruitmentsResponse>.Error(ErrorCode.DatabaseError, "GetRecruitmentsByManager");
            }

            return Result<TeamRecruitmentsResponse>.Success(new TeamRecruitmentsResponse
            {
                Items = queryResult.Values1.Select(MapRecruitment).ToList()
            });
        }

        public async Task<Result<TeamRecruitmentDto?>> SaveRecruitmentByManagerAsync(
            Guid managerUserId, SaveTeamRecruitmentRequest request, CancellationToken cancellation = default)
        {
            Logger.InfoWith("Team recruitment save requested",
                ("ManagerUserId", managerUserId), ("RecruitmentId", request.RecruitmentId));

            var procedure = new UspSaveSoccerTeamRecruitment(this)
            {
                ManagerUserId = managerUserId,
                RecruitmentId = request.RecruitmentId,
                Title = request.Title,
                Description = request.Description,
                ConditionsJson = request.Conditions.Count > 0 ? JsonSerializer.Serialize(request.Conditions) : null,
                DeadlineDate = request.DeadlineDate
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
            Logger.InfoWith("Team recruitment close requested",
                ("ManagerUserId", managerUserId), ("RecruitmentId", recruitmentId));

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
            Logger.InfoWith("Team recruitment delete requested",
                ("ManagerUserId", managerUserId), ("RecruitmentId", recruitmentId), ("Restore", restore));

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

        public async Task<Result<TeamCareerOutcomesResponse>> GetCareerOutcomesBySlugAsync(string slug, CancellationToken cancellation = default)
        {
            Logger.InfoWith("Team career outcomes requested", ("Slug", slug));

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
            Logger.InfoWith("Team career outcomes requested by manager", ("ManagerUserId", managerUserId));

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
            Logger.InfoWith("Team career outcome save requested",
                ("ManagerUserId", managerUserId), ("OutcomeId", request.OutcomeId));

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
            Logger.InfoWith("Team career outcome delete requested",
                ("ManagerUserId", managerUserId), ("OutcomeId", outcomeId), ("Restore", restore));

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
            Logger.InfoWith("Team reviews requested", ("Slug", slug));

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

            Logger.InfoWith("Team reviews received", ("Slug", slug), ("Reviews", response.Items.Count));
            return Result<TeamReviewsResponse>.Success(response);
        }

        public async Task<Result<bool>> SaveReviewAsync(Guid authorUserId, SaveTeamReviewRequest request, CancellationToken cancellation = default)
        {
            Logger.InfoWith("Team review save requested", ("AuthorUserId", authorUserId), ("ReviewId", request.ReviewId));

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
            Logger.InfoWith("Team review delete requested",
                ("AuthorUserId", authorUserId), ("ReviewId", reviewId), ("Restore", restore));

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

            if (row.CreatedAt > DateTime.MinValue)
            {
                int years = Math.Max(1, DateTime.UtcNow.Year - row.CreatedAt.Year + 1);
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
        private static TeamRecruitmentDto MapRecruitment(SoccerTeamRecruitmentsEntity row)
        {
            return new TeamRecruitmentDto
            {
                RecruitmentId = row.RecruitmentId,
                Title = row.Title,
                Description = row.Description,
                Conditions = ParseAchievements(row.ConditionsJson),
                DeadlineDate = row.DeadlineDate,
                Status = row.Status,
                IsOpen = row.Status == "Open"
                         && (row.DeadlineDate is null || row.DeadlineDate.Value.Date >= DateTime.UtcNow.Date)
            };
        }
    }
}
