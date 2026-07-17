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
                .ToDictionary(t => t.TeamId, t => t.Slug);
            string? SlugOf(Guid? teamId) => teamId is not null && slugs.TryGetValue(teamId.Value, out string? slug) ? slug : null;

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
                    StartDate = ToDateOnly(tournament.StartDate),
                    EndDate = ToDateOnly(tournament.EndDate),
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
                        VenueName = NullIfEmpty(m.VenueName)
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
                            RecordedOn = ToDateOnly(v.RecordedOn),
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
                        PublishedOn = ToDateOnly(n.PublishedOn)
                    })
                    .ToList()
            };

            Logger.InfoWith("Records tournament detail received", ("TournamentId", tournamentId),
                ("Standings", response.Standings.Count), ("Matches", response.Matches.Count),
                ("Videos", response.Videos.Count), ("News", response.News.Count));

            return Result<RecordsTournamentDetailResponse?>.Success(response);
        }

        private static string? NullIfEmpty(string? value)
        {
            return string.IsNullOrEmpty(value) ? null : value;
        }

        private static DateOnly? ToDateOnly(DateTime? value)
        {
            return value is null ? null : DateOnly.FromDateTime(value.Value);
        }
    }
}
