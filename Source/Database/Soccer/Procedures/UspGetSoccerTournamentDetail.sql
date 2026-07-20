-- @entity: SoccerSeriesChampionRecord
-- @source: join
-- @join: SoccerTournaments AS st (SeasonYear)
-- @join: SoccerTournamentAwards AS sa (TeamId, TeamName)
-- 대회 상세 묶음 조회 (Records 상세 화면, 공개).
-- 결과셋 8개: ①대회 → ②순위표 → ③경기 → ④수상 → ⑤역대 우승(같은 SeriesSlug의 타 연도 Champion)
--            → ⑥영상 → ⑦뉴스 → ⑧등장 팀의 공개 슬러그(팀명→팀 홈 링크용, 공개 팀만). MultiQueryReader로 소비.
-- 통계 바(총 경기·득점)와 영상 VS 팀명(⑥.MatchId → ③ 매핑)은 Persistence/클라이언트 계산.
-- ⑧은 TeamId·Slug 두 컬럼만 SELECT — SoccerTeamsEntity에 부분 매핑(나머지 프로퍼티 기본값).
CREATE PROCEDURE [dbo].[UspGetSoccerTournamentDetail]
    @TournamentId UNIQUEIDENTIFIER
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        t.[TournamentId], t.[SeasonYear], t.[Name], t.[SeriesSlug], t.[Format], t.[Scope],
        t.[AgeGroup], t.[RegionGroup], t.[Status], t.[StartDate], t.[EndDate], t.[TeamCount],
        t.[HostName], t.[MethodText], t.[MatchTimeText], t.[VenueText], t.[TiebreakText],
        t.[RegulationPdfUrl], t.[SourceName], t.[SourceUrl], t.[OrganizerUserId], t.[OrganizerType],
        t.[DataSource], t.[ExternalId], t.[SyncStatus], t.[CreatedAt], t.[UpdatedAt], t.[DeletedAt]
    FROM [dbo].[SoccerTournaments] t WITH (NOLOCK)
    WHERE t.[TournamentId] = @TournamentId AND t.[DeletedAt] IS NULL;

    SELECT
        s.[StandingId], s.[TournamentId], s.[StageType], s.[GroupName], s.[TeamId], s.[TeamName],
        s.[TeamRank], s.[Played], s.[Won], s.[Drawn], s.[Lost], s.[Points], s.[GoalsFor], s.[GoalsAgainst],
        s.[IsQualified], s.[DataSource], s.[ExternalId], s.[SyncStatus], s.[CreatedAt], s.[UpdatedAt], s.[DeletedAt]
    FROM [dbo].[SoccerTournamentStandings] s WITH (NOLOCK)
    WHERE s.[TournamentId] = @TournamentId AND s.[DeletedAt] IS NULL
    ORDER BY s.[StageType], s.[GroupName], s.[TeamRank];

    SELECT
        m.[MatchId], m.[TournamentId], m.[StageType], m.[GroupName], m.[RoundName],
        m.[HomeTeamId], m.[HomeTeamName], m.[AwayTeamId], m.[AwayTeamName],
        m.[HomeScore], m.[AwayScore], m.[HomePkScore], m.[AwayPkScore],
        m.[Status], m.[MatchedAt], m.[VenueName], m.[MatchType], m.[DataSource], m.[ExternalId], m.[SyncStatus],
        m.[CreatedAt], m.[UpdatedAt], m.[DeletedAt]
    FROM [dbo].[SoccerMatches] m WITH (NOLOCK)
    WHERE m.[TournamentId] = @TournamentId AND m.[DeletedAt] IS NULL
    ORDER BY m.[MatchedAt];

    SELECT
        a.[AwardId], a.[TournamentId], a.[AwardType], a.[TeamId], a.[TeamName], a.[DisplayOrder],
        a.[CreatedAt], a.[UpdatedAt], a.[DeletedAt]
    FROM [dbo].[SoccerTournamentAwards] a WITH (NOLOCK)
    WHERE a.[TournamentId] = @TournamentId AND a.[DeletedAt] IS NULL
    ORDER BY a.[DisplayOrder];

    SELECT
        st.[SeasonYear], sa.[TeamId], sa.[TeamName]
    FROM [dbo].[SoccerTournaments] this WITH (NOLOCK)
    JOIN [dbo].[SoccerTournaments] st WITH (NOLOCK)
        ON st.[SeriesSlug] = this.[SeriesSlug] AND st.[TournamentId] <> this.[TournamentId] AND st.[DeletedAt] IS NULL
    JOIN [dbo].[SoccerTournamentAwards] sa WITH (NOLOCK)
        ON sa.[TournamentId] = st.[TournamentId] AND sa.[AwardType] = 'Champion' AND sa.[DeletedAt] IS NULL
    WHERE this.[TournamentId] = @TournamentId AND this.[SeriesSlug] IS NOT NULL
    ORDER BY st.[SeasonYear] DESC;

    SELECT
        v.[VideoId], v.[TournamentId], v.[MatchId], v.[TeamId], v.[Title], v.[VideoUrl], v.[ThumbnailUrl],
        v.[VideoType], v.[DurationSeconds], v.[RecordedOn], v.[CreatedAt], v.[UpdatedAt], v.[DeletedAt]
    FROM [dbo].[SoccerMatchVideos] v WITH (NOLOCK)
    WHERE v.[TournamentId] = @TournamentId AND v.[DeletedAt] IS NULL
    ORDER BY v.[RecordedOn] DESC, v.[CreatedAt] DESC;

    SELECT
        n.[NewsId], n.[TournamentId], n.[Title], n.[Url], n.[PublisherName], n.[PublishedOn],
        n.[CreatedAt], n.[UpdatedAt], n.[DeletedAt]
    FROM [dbo].[SoccerTournamentNews] n WITH (NOLOCK)
    WHERE n.[TournamentId] = @TournamentId AND n.[DeletedAt] IS NULL
    ORDER BY n.[PublishedOn] DESC, n.[CreatedAt] DESC;

    SELECT tm.[TeamId], tm.[Slug]
    FROM [dbo].[SoccerTeams] tm WITH (NOLOCK)
    WHERE tm.[DeletedAt] IS NULL AND tm.[IsPublicProfile] = 1 AND tm.[Slug] IS NOT NULL
      AND tm.[TeamId] IN (
        SELECT m.[HomeTeamId] FROM [dbo].[SoccerMatches] m WITH (NOLOCK)
        WHERE m.[TournamentId] = @TournamentId AND m.[HomeTeamId] IS NOT NULL AND m.[DeletedAt] IS NULL
        UNION
        SELECT m.[AwayTeamId] FROM [dbo].[SoccerMatches] m WITH (NOLOCK)
        WHERE m.[TournamentId] = @TournamentId AND m.[AwayTeamId] IS NOT NULL AND m.[DeletedAt] IS NULL
        UNION
        SELECT s.[TeamId] FROM [dbo].[SoccerTournamentStandings] s WITH (NOLOCK)
        WHERE s.[TournamentId] = @TournamentId AND s.[TeamId] IS NOT NULL AND s.[DeletedAt] IS NULL
        UNION
        SELECT a.[TeamId] FROM [dbo].[SoccerTournamentAwards] a WITH (NOLOCK)
        WHERE a.[TournamentId] = @TournamentId AND a.[TeamId] IS NOT NULL AND a.[DeletedAt] IS NULL);
END
