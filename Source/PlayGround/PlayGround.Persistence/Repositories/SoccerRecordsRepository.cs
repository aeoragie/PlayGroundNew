using Microsoft.Extensions.Options;
using PlayGround.Shared.Result;
using PlayGround.Infrastructure.Database;
using PlayGround.Infrastructure.Database.Base;
using PlayGround.Infrastructure.Logging;
using PlayGround.Contracts.Records;
using PlayGround.Application.Interfaces;
using PlayGround.Persistence.Database.Generated.Soccer.Entities;
using PlayGround.Persistence.Database.Generated.Soccer.Procedures;

namespace PlayGround.Persistence.Repositories
{
    /// <summary>공개 경기기록(Records) 조회 (Soccer DB). 다중 결과셋 — MultiQueryReader 소비.</summary>
    public class SoccerRecordsRepository : RepositoryBase, ISoccerRecordsRepository
    {
        public override DatabaseTypes Database => DatabaseTypes.Soccer;

        public SoccerRecordsRepository(IOptions<DatabaseConfiguration> options) : base(options)
        {
        }

        public async Task<Result<RecordsTournamentsResponse>> GetTournamentsBySeasonAsync(int seasonYear, CancellationToken cancellation = default)
        {
            Logger.InfoWith("Records tournaments requested", ("SeasonYear", seasonYear));

            var procedure = new UspGetSoccerTournamentsBySeason(this) { SeasonYear = seasonYear };
            Result<MultiQueryReader> opened = await ProcedureMultipleAsync(procedure, cancellation: cancellation);
            if (opened.IsError)
            {
                Logger.ErrorWith("Records tournaments query failed", ("DetailCode", opened.ResultData.DetailCode));
                return Result<RecordsTournamentsResponse>.Error(ErrorCode.DatabaseError);
            }

            using MultiQueryReader reader = opened.Value;
            var tournaments = (await reader.ReadAsync<SoccerTournamentsEntity>()).ToList();
            var champions = (await reader.ReadAsync<SoccerTournamentAwardsEntity>()).ToList();
            var seasonYears = (await reader.ReadAsync<int>()).ToList();

            var response = new RecordsTournamentsResponse
            {
                SeasonYear = seasonYear,
                SeasonYears = seasonYears,
                Tournaments = tournaments
                    .Select(t => new RecordsTournamentDto
                    {
                        TournamentId = t.TournamentId,
                        Name = t.Name,
                        Format = t.Format,
                        Scope = t.Scope,
                        AgeGroup = t.AgeGroup,
                        RegionGroup = NullIfEmpty(t.RegionGroup),
                        Status = t.Status,
                        TeamCount = t.TeamCount,
                        ChampionTeamName = champions.FirstOrDefault(c => c.TournamentId == t.TournamentId)?.TeamName
                    })
                    .ToList()
            };

            Logger.InfoWith("Records tournaments received",
                ("SeasonYear", seasonYear), ("Tournaments", response.Tournaments.Count), ("Years", seasonYears.Count));

            return Result<RecordsTournamentsResponse>.Success(response);
        }

        public async Task<Result<RecordsTournamentDetailResponse?>> GetTournamentDetailAsync(Guid tournamentId, CancellationToken cancellation = default)
        {
            Logger.InfoWith("Records tournament detail requested", ("TournamentId", tournamentId));

            var procedure = new UspGetSoccerTournamentDetail(this) { TournamentId = tournamentId };
            Result<MultiQueryReader> opened = await ProcedureMultipleAsync(procedure, cancellation: cancellation);
            if (opened.IsError)
            {
                Logger.ErrorWith("Records tournament detail query failed", ("DetailCode", opened.ResultData.DetailCode));
                return Result<RecordsTournamentDetailResponse?>.Error(ErrorCode.DatabaseError);
            }

            using MultiQueryReader reader = opened.Value;
            SoccerTournamentsEntity? tournament = await reader.ReadSingleOrDefaultAsync<SoccerTournamentsEntity>();
            if (tournament is null)
            {
                Logger.InfoWith("Records tournament not found", ("TournamentId", tournamentId));
                return Result<RecordsTournamentDetailResponse?>.Success(null);
            }

            var standings = (await reader.ReadAsync<SoccerTournamentStandingsEntity>()).ToList();
            var matches = (await reader.ReadAsync<SoccerMatchesEntity>()).ToList();
            var awards = (await reader.ReadAsync<SoccerTournamentAwardsEntity>()).ToList();
            var champions = (await reader.ReadAsync<SoccerSeriesChampionRecord>()).ToList();
            var videos = (await reader.ReadAsync<SoccerMatchVideosEntity>()).ToList();
            var news = (await reader.ReadAsync<SoccerTournamentNewsEntity>()).ToList();

            // ⑧ 등장 팀의 공개 슬러그 (TeamId·Slug만 채워진 부분 매핑) — 팀명 → 팀 홈 링크
            Dictionary<Guid, string> slugs = (await reader.ReadAsync<SoccerTeamsEntity>())
                .Where(t => !string.IsNullOrEmpty(t.Slug))
                .ToDictionary(t => t.TeamId, t => t.Slug!);
            string? SlugOf(Guid? teamId) => teamId is not null && slugs.TryGetValue(teamId.Value, out string? slug) ? slug : null;

            // ⑨ 상세 보유 경기 (이벤트/출전 존재) — 행 확장 셰브론 노출 대상
            HashSet<Guid> detailMatchIds = (await reader.ReadAsync<Guid>()).ToHashSet();

            var response = new RecordsTournamentDetailResponse
            {
                Tournament = new RecordsTournamentDetailDto
                {
                    TournamentId = tournament.TournamentId,
                    SeasonYear = tournament.SeasonYear,
                    Name = tournament.Name,
                    Format = tournament.Format,
                    Scope = tournament.Scope,
                    AgeGroup = tournament.AgeGroup,
                    RegionGroup = NullIfEmpty(tournament.RegionGroup),
                    Status = tournament.Status,
                    StartDate = tournament.StartDate,
                    EndDate = tournament.EndDate,
                    TeamCount = tournament.TeamCount,
                    HostName = NullIfEmpty(tournament.HostName),
                    MethodText = NullIfEmpty(tournament.MethodText),
                    MatchTimeText = NullIfEmpty(tournament.MatchTimeText),
                    VenueText = NullIfEmpty(tournament.VenueText),
                    TiebreakText = NullIfEmpty(tournament.TiebreakText),
                    RegulationPdfUrl = NullIfEmpty(tournament.RegulationPdfUrl),
                    SourceName = NullIfEmpty(tournament.SourceName)
                },
                Standings = standings
                    .Select(s => new RecordsStandingDto
                    {
                        StageType = s.StageType,
                        GroupName = NullIfEmpty(s.GroupName),
                        TeamId = s.TeamId,
                        TeamName = s.TeamName,
                        TeamSlug = SlugOf(s.TeamId),
                        TeamRank = s.TeamRank,
                        Played = s.Played,
                        Won = s.Won,
                        Drawn = s.Drawn,
                        Lost = s.Lost,
                        Points = s.Points,
                        GoalsFor = s.GoalsFor,
                        GoalsAgainst = s.GoalsAgainst,
                        IsQualified = s.IsQualified
                    })
                    .ToList(),
                Matches = matches
                    .Select(m => new RecordsMatchDto
                    {
                        MatchId = m.MatchId,
                        StageType = NullIfEmpty(m.StageType),
                        GroupName = NullIfEmpty(m.GroupName),
                        RoundName = NullIfEmpty(m.RoundName),
                        HomeTeamId = m.HomeTeamId,
                        HomeTeamName = m.HomeTeamName,
                        HomeTeamSlug = SlugOf(m.HomeTeamId),
                        AwayTeamId = m.AwayTeamId,
                        AwayTeamName = m.AwayTeamName,
                        AwayTeamSlug = SlugOf(m.AwayTeamId),
                        HomeScore = m.HomeScore,
                        AwayScore = m.AwayScore,
                        HomePkScore = m.HomePkScore,
                        AwayPkScore = m.AwayPkScore,
                        Status = m.Status,
                        MatchedAt = m.MatchedAt,
                        VenueName = NullIfEmpty(m.VenueName),
                        MatchSequence = m.MatchSequence,
                        HasDetail = detailMatchIds.Contains(m.MatchId)
                    })
                    .ToList(),
                Awards = awards
                    .Select(a => new RecordsAwardDto
                    {
                        AwardType = a.AwardType,
                        TeamId = a.TeamId,
                        TeamName = a.TeamName,
                        TeamSlug = SlugOf(a.TeamId)
                    })
                    .ToList(),
                SeriesChampions = champions
                    .Select(c => new RecordsSeriesChampionDto
                    {
                        SeasonYear = c.SeasonYear,
                        TeamId = c.TeamId,
                        TeamName = c.TeamName,
                        TeamSlug = SlugOf(c.TeamId)
                    })
                    .ToList(),
                Videos = videos
                    .Select(v =>
                    {
                        SoccerMatchesEntity? match = v.MatchId is null
                            ? null
                            : matches.FirstOrDefault(m => m.MatchId == v.MatchId);
                        return new RecordsVideoDto
                        {
                            VideoId = v.VideoId,
                            Title = v.Title,
                            VideoUrl = v.VideoUrl,
                            VideoType = v.VideoType,
                            DurationSeconds = v.DurationSeconds,
                            RecordedOn = v.RecordedOn,
                            HomeTeamName = match?.HomeTeamName,
                            AwayTeamName = match?.AwayTeamName,
                            VenueName = NullIfEmpty(match?.VenueName)
                        };
                    })
                    .ToList(),
                News = news
                    .Select(n => new RecordsNewsDto
                    {
                        Title = n.Title,
                        Url = n.Url,
                        PublisherName = NullIfEmpty(n.PublisherName),
                        PublishedOn = n.PublishedOn
                    })
                    .ToList()
            };

            Logger.InfoWith("Records tournament detail received", ("TournamentId", tournamentId),
                ("Standings", response.Standings.Count), ("Matches", response.Matches.Count),
                ("Videos", response.Videos.Count), ("News", response.News.Count));

            return Result<RecordsTournamentDetailResponse?>.Success(response);
        }

        public async Task<Result<RecordsMatchDetailResponse?>> GetMatchDetailAsync(Guid matchId, CancellationToken cancellation = default)
        {
            Logger.InfoWith("Records match detail requested", ("MatchId", matchId));

            var procedure = new UspGetSoccerMatchDetail(this) { MatchId = matchId };
            Result<MultiQueryReader> opened = await ProcedureMultipleAsync(procedure, cancellation: cancellation);
            if (opened.IsError)
            {
                Logger.ErrorWith("Records match detail query failed", ("DetailCode", opened.ResultData.DetailCode));
                return Result<RecordsMatchDetailResponse?>.Error(ErrorCode.DatabaseError);
            }

            using MultiQueryReader reader = opened.Value;
            SoccerMatchesEntity? match = await reader.ReadSingleOrDefaultAsync<SoccerMatchesEntity>();
            if (match is null)
            {
                Logger.InfoWith("Records match not found", ("MatchId", matchId));
                return Result<RecordsMatchDetailResponse?>.Success(null);
            }

            SoccerTournamentsEntity? tournament = await reader.ReadSingleOrDefaultAsync<SoccerTournamentsEntity>();
            var events = (await reader.ReadAsync<SoccerMatchEventsEntity>()).ToList();
            var appearances = (await reader.ReadAsync<SoccerMatchAppearancesEntity>()).ToList();

            // ⑤ 등장 선수 공개 슬러그 (PlayerId·Slug만 부분 매핑) — Claim된 선수만 프로필 링크
            Dictionary<Guid, string> playerSlugs = (await reader.ReadAsync<SoccerPlayersEntity>())
                .Where(p => !string.IsNullOrEmpty(p.Slug))
                .ToDictionary(p => p.PlayerId, p => p.Slug);
            string? PlayerSlugOf(Guid? playerId) =>
                playerId is not null && playerSlugs.TryGetValue(playerId.Value, out string? slug) ? slug : null;

            // 라인업 선수의 득점 마크 — 득점(Goal/PenaltyGoal, 자책 제외) 이벤트를 선수로 매칭
            List<int> GoalMinutesOf(SoccerMatchAppearancesEntity ap) => events
                .Where(e => (e.EventType == "Goal" || e.EventType == "PenaltyGoal")
                    && (ap.PlayerId is not null && e.PlayerId == ap.PlayerId
                        || (ap.PlayerId is null || e.PlayerId is null) && e.PlayerName == ap.PlayerName && e.TeamName == ap.TeamName))
                .Select(e => e.MinuteOfPlay ?? 0)
                .OrderBy(m => m)
                .ToList();

            RecordsLineupPlayerDto MapLineup(SoccerMatchAppearancesEntity ap) => new()
            {
                PlayerName = ap.PlayerName,
                PlayerId = ap.PlayerId,
                PlayerSlug = PlayerSlugOf(ap.PlayerId),
                JerseyNumber = ap.JerseyNumber,
                Position = NullIfEmpty(ap.Position),
                IsCaptain = ap.IsCaptain,
                IsStarter = ap.IsStarter,
                GoalMinutes = GoalMinutesOf(ap)
            };

            var response = new RecordsMatchDetailResponse
            {
                MatchId = match.MatchId,
                MatchType = match.MatchType,
                TournamentId = match.TournamentId,
                TournamentName = tournament?.Name,
                Format = tournament?.Format,
                AgeGroup = tournament?.AgeGroup,
                SeasonYear = tournament?.SeasonYear,
                StageType = NullIfEmpty(match.StageType),
                GroupName = NullIfEmpty(match.GroupName),
                RoundName = NullIfEmpty(match.RoundName),
                MatchSequence = match.MatchSequence,
                Status = match.Status,
                HomeTeamId = match.HomeTeamId,
                HomeTeamName = match.HomeTeamName,
                AwayTeamId = match.AwayTeamId,
                AwayTeamName = match.AwayTeamName,
                HomeCoachName = NullIfEmpty(match.HomeCoachName),
                AwayCoachName = NullIfEmpty(match.AwayCoachName),
                HomeScore = match.HomeScore,
                AwayScore = match.AwayScore,
                HomePkScore = match.HomePkScore,
                AwayPkScore = match.AwayPkScore,
                FirstHalfHomeScore = match.FirstHalfHomeScore,
                FirstHalfAwayScore = match.FirstHalfAwayScore,
                MatchedAt = match.MatchedAt,
                VenueName = NullIfEmpty(match.VenueName),
                RefereeName = NullIfEmpty(match.RefereeName),
                MatchTimeText = NullIfEmpty(tournament?.MatchTimeText),
                Events = events
                    .Select(e => new RecordsMatchEventDto
                    {
                        EventType = e.EventType,
                        MinuteOfPlay = e.MinuteOfPlay,
                        PlayerName = NullIfEmpty(e.PlayerName),
                        PlayerId = e.PlayerId,
                        PlayerSlug = PlayerSlugOf(e.PlayerId),
                        JerseyNumber = e.JerseyNumber,
                        TeamName = e.TeamName,
                        IsHome = e.TeamName == match.HomeTeamName
                    })
                    .ToList(),
                HomeLineup = appearances
                    .Where(a => a.TeamName == match.HomeTeamName)
                    .Select(MapLineup)
                    .ToList(),
                AwayLineup = appearances
                    .Where(a => a.TeamName == match.AwayTeamName)
                    .Select(MapLineup)
                    .ToList()
            };

            Logger.InfoWith("Records match detail received", ("MatchId", matchId),
                ("Events", response.Events.Count), ("HomeLineup", response.HomeLineup.Count), ("AwayLineup", response.AwayLineup.Count));

            return Result<RecordsMatchDetailResponse?>.Success(response);
        }

        private static string? NullIfEmpty(string? value)
        {
            return string.IsNullOrEmpty(value) ? null : value;
        }

    }
}
