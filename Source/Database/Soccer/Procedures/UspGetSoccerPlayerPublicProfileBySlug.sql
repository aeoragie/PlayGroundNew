-- @entity: SoccerPlayerPublicHeaderRecord
-- @source: join
-- @join: SoccerPlayers AS p (PlayerId, Name, PhotoUrl, BirthDate, AgeGroup, HeightCm, WeightKg, PreferredFoot, IsGuardianManaged)
-- @join: SoccerTeamPlayers AS tp (JerseyNumber, Position)
-- @join: SoccerTeams AS t (TeamName, IsVerified, Slug)
-- 공개 선수 프로필 조회 (Slug 기준, 비로그인 읽기전용 — Design.PlayerPublicProfile 디테일 공개 뷰).
-- 프로필 공개(FieldName='Profile')를 끈 선수·미존재는 빈 결과 (공개홈 로스터와 같은 기준).
-- 결과셋 6개: ⓪선수+소속팀 헤더 → ①필드 가시성(기본값 병합은 Persistence) → ②시즌 출전 경기(공식만)
--            → ③선수 이벤트(집계 매칭은 Persistence — 친선 이벤트는 ②에 경기가 없어 자동 제외)
--            → ④대표 영상+총수 → ⑤커리어.
-- TeamSlug는 팀 공개홈이 공개(IsPublicProfile=1)일 때만 — 비공개 팀 홈으로 링크를 걸지 않는다.
CREATE PROCEDURE [dbo].[UspGetSoccerPlayerPublicProfileBySlug]
    @Slug VARCHAR(150),
    @SeasonYear INT
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @PlayerId UNIQUEIDENTIFIER = (
        SELECT TOP 1 p.[PlayerId]
        FROM [dbo].[SoccerPlayers] p WITH (NOLOCK)
        LEFT JOIN [dbo].[SoccerPlayerFieldVisibilities] fv WITH (NOLOCK)
            ON fv.[PlayerId] = p.[PlayerId] AND fv.[FieldName] = 'Profile'
        WHERE p.[Slug] = @Slug AND p.[DeletedAt] IS NULL
          AND COALESCE(fv.[IsPublic], 1) = 1);

    -- Slug = 팀 공개홈 슬러그 (선수 slug는 요청 파라미터와 같아 내리지 않는다). 팀 홈 비공개면 NULL.
    SELECT
        p.[PlayerId], p.[Name], p.[PhotoUrl], p.[BirthDate], p.[AgeGroup],
        p.[HeightCm], p.[WeightKg], p.[PreferredFoot], p.[IsGuardianManaged],
        tp.[JerseyNumber], tp.[Position],
        t.[TeamName], t.[IsVerified],
        CASE WHEN t.[IsPublicProfile] = 1 THEN t.[Slug] END AS [Slug]
    FROM [dbo].[SoccerPlayers] p WITH (NOLOCK)
    LEFT JOIN [dbo].[SoccerTeamPlayers] tp WITH (NOLOCK)
        ON tp.[PlayerId] = p.[PlayerId] AND tp.[Status] = 'Active' AND tp.[DeletedAt] IS NULL
    LEFT JOIN [dbo].[SoccerTeams] t WITH (NOLOCK)
        ON t.[TeamId] = tp.[TeamId] AND t.[DeletedAt] IS NULL
    WHERE p.[PlayerId] = @PlayerId;

    SELECT
        fv.[VisibilityId], fv.[PlayerId], fv.[FieldName], fv.[IsPublic],
        fv.[CreatedAt], fv.[UpdatedAt]
    FROM [dbo].[SoccerPlayerFieldVisibilities] fv WITH (NOLOCK)
    WHERE fv.[PlayerId] = @PlayerId;

    -- 시즌 출전 경기 — 공식 경기만 (친선은 공개 프로필에 노출하지 않는다, Design.FriendlyMatch)
    SELECT
        a.[MatchId], a.[TeamId], a.[MinutesPlayed],
        m.[HomeTeamId], m.[HomeTeamName], m.[AwayTeamName], m.[HomeScore], m.[AwayScore], m.[TournamentId], m.[MatchedAt], m.[MatchType],
        t.[Format]
    FROM [dbo].[SoccerMatchAppearances] a WITH (NOLOCK)
    JOIN [dbo].[SoccerMatches] m WITH (NOLOCK)
        ON m.[MatchId] = a.[MatchId] AND m.[Status] = 'Completed'
        AND m.[MatchType] = 'Official' AND m.[DeletedAt] IS NULL
    LEFT JOIN [dbo].[SoccerTournaments] t WITH (NOLOCK)
        ON t.[TournamentId] = m.[TournamentId] AND t.[DeletedAt] IS NULL
    WHERE a.[PlayerId] = @PlayerId AND a.[DeletedAt] IS NULL
      AND (t.[SeasonYear] = @SeasonYear OR (m.[TournamentId] IS NULL AND YEAR(m.[MatchedAt]) = @SeasonYear))
    ORDER BY m.[MatchedAt] DESC;

    SELECT
        e.[EventId], e.[MatchId], e.[TeamId], e.[TeamName], e.[EventType],
        e.[PlayerId], e.[PlayerName], e.[AssistPlayerId], e.[AssistPlayerName], e.[MinuteOfPlay],
        e.[CreatedAt], e.[UpdatedAt], e.[DeletedAt]
    FROM [dbo].[SoccerMatchEvents] e WITH (NOLOCK)
    WHERE (e.[PlayerId] = @PlayerId OR e.[AssistPlayerId] = @PlayerId) AND e.[DeletedAt] IS NULL;

    SELECT
        v.[VideoId], v.[PlayerId], v.[Title], v.[VideoUrl], v.[ThumbnailUrl],
        v.[DurationSeconds], v.[IsPrimary], v.[Tags], v.[RecordedOn],
        v.[CreatedAt], v.[UpdatedAt], v.[DeletedAt]
    FROM [dbo].[SoccerPlayerPortfolioVideos] v WITH (NOLOCK)
    WHERE v.[PlayerId] = @PlayerId AND v.[DeletedAt] IS NULL
    ORDER BY v.[IsPrimary] DESC, v.[RecordedOn] DESC, v.[CreatedAt] DESC;

    SELECT
        c.[CareerId], c.[PlayerId], c.[TeamName], c.[TeamId], c.[IsCurrent], c.[BadgeLabel],
        c.[StartDate], c.[EndDate], c.[Role], c.[Note], c.[IsVerified],
        c.[CreatedAt], c.[UpdatedAt], c.[DeletedAt]
    FROM [dbo].[SoccerPlayerCareers] c WITH (NOLOCK)
    WHERE c.[PlayerId] = @PlayerId AND c.[DeletedAt] IS NULL
    ORDER BY c.[IsCurrent] DESC, c.[StartDate] DESC;
END
