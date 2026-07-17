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

        public async Task<Result<TeamPublicHomeResponse?>> GetTeamHomeBySlugAsync(string slug, CancellationToken cancellation = default)
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
                        HasPublicProfile = r.UserId is not null
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
                Matches = matches
                    .Select(m =>
                    {
                        bool isHome = m.HomeTeamId == teamId;
                        return new TeamMatchDto
                        {
                            MatchId = m.MatchId,
                            CompetitionType = CompetitionTypeOf(m),
                            TournamentName = NullIfEmpty(m.Name),
                            MatchedAt = m.MatchedAt,
                            VenueName = NullIfEmpty(m.VenueName),
                            IsHome = isHome,
                            OpponentName = isHome ? m.AwayTeamName : m.HomeTeamName,
                            TeamScore = (isHome ? m.HomeScore : m.AwayScore) ?? 0,
                            OpponentScore = (isHome ? m.AwayScore : m.HomeScore) ?? 0,
                            Events = events
                                .Where(e => e.MatchId == m.MatchId)
                                .Select(e => new TeamMatchEventDto
                                {
                                    EventType = e.EventType,
                                    PlayerName = NullIfEmpty(e.PlayerName),
                                    AssistPlayerName = NullIfEmpty(e.AssistPlayerName)
                                })
                                .ToList()
                        };
                    })
                    .ToList()
            };

            Logger.InfoWith("Team matches received", ("ManagerUserId", managerUserId),
                ("Matches", response.Matches.Count), ("LeagueRank", leagueRank));

            return Result<TeamMatchesResponse>.Success(response);
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
                Videos = queryResult.Values1
                    .Select(v => new TeamVideoDto
                    {
                        VideoId = v.VideoId,
                        VideoType = v.VideoType,
                        Title = v.Title,
                        VideoUrl = v.VideoUrl,
                        ThumbnailUrl = NullIfEmpty(v.ThumbnailUrl),
                        DurationSeconds = v.DurationSeconds,
                        RecordedOn = v.RecordedOn is null ? null : DateOnly.FromDateTime(v.RecordedOn.Value),
                        IsMatchLinked = v.MatchId is not null
                    })
                    .ToList()
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
    }
}
