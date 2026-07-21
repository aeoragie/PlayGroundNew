-- @entity: SoccerPlayerPublicHeaderRecord
-- @source: join
-- @join: SoccerPlayers AS p (PlayerId, Name, PhotoUrl, BirthDate, AgeGroup, HeightCm, WeightKg, PreferredFoot, SchoolName, IsGuardianManaged)
-- @join: SoccerTeamPlayers AS tp (JerseyNumber, Position)
-- @join: SoccerTeams AS t (TeamName, IsVerified, Slug)
-- 공개 선수 프로필 조회 (Slug 기준 — Design.PlayerPublicProfile 디테일 공개/권한 뷰).
-- 프로필 공개(FieldName='Profile')를 끈 선수·미존재는 빈 결과 (공개홈 로스터와 같은 기준).
-- 결과셋 7개: ⓪선수+소속팀 헤더 → ①필드 가시성(기본값 병합은 Persistence) → ②시즌 출전 경기
--            → ③선수 이벤트(집계 매칭은 Persistence) → ④대표 영상+총수 → ⑤커리어 → ⑥열람 승인(0~1행).
-- 권한 뷰(@ViewerUserId = 승인된 에이전트): ②에 친선 포함(경기별 상세 기록 — 요약은 Persistence가
-- 공식만 걸러 계산), ⑥에 승인 행. 만료 판정 기준은 ExpiresAt 저장값(AgentViewApproval과 공유).
-- 권한 조회는 SoccerAgentViewLogs에 'ProfileView'를 적재한다 — 열람 기록은 보호자 신뢰의 핵심.
-- SchoolName은 항상 내려가지만 Persistence가 권한 뷰에만 싣는다(공개 뷰 DTO에 필드 없음).
-- TeamSlug는 팀 공개홈이 공개(IsPublicProfile=1)일 때만 — 비공개 팀 홈으로 링크를 걸지 않는다.
CREATE PROCEDURE [dbo].[UspGetSoccerPlayerPublicProfileBySlug]
    @Slug VARCHAR(150),
    @SeasonYear INT,
    @ViewerUserId UNIQUEIDENTIFIER = NULL
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

    --.// 열람 권한 판정 — 뷰어가 이 선수의 승인(미만료) 열람 요청을 가진 에이전트인가
    DECLARE @GrantRequestId UNIQUEIDENTIFIER = (
        SELECT TOP 1 r.[RequestId]
        FROM [dbo].[SoccerAgentViewRequests] r WITH (NOLOCK)
        JOIN [dbo].[SoccerAgentProfiles] a WITH (NOLOCK)
            ON a.[AgentId] = r.[AgentId] AND a.[DeletedAt] IS NULL
        WHERE @ViewerUserId IS NOT NULL AND a.[UserId] = @ViewerUserId
          AND r.[PlayerId] = @PlayerId AND r.[Status] = 'Approved' AND r.[DeletedAt] IS NULL
          AND r.[ExpiresAt] > GETUTCDATE()
        ORDER BY r.[ReviewedAt] DESC);

    -- 권한 뷰 방문 기록 — 조회가 곧 열람이다 ("열람 기록이 보호자에게 표시됩니다")
    IF @GrantRequestId IS NOT NULL
    BEGIN
        INSERT INTO [dbo].[SoccerAgentViewLogs] ([RequestId], [EventType])
        VALUES (@GrantRequestId, 'ProfileView');
    END

    -- Slug = 팀 공개홈 슬러그 (선수 slug는 요청 파라미터와 같아 내리지 않는다). 팀 홈 비공개면 NULL.
    SELECT
        p.[PlayerId], p.[Name], p.[PhotoUrl], p.[BirthDate], p.[AgeGroup],
        p.[HeightCm], p.[WeightKg], p.[PreferredFoot], p.[SchoolName], p.[IsGuardianManaged],
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

    -- 시즌 출전 경기 — 공개 뷰는 공식만 (친선 미노출, Design.FriendlyMatch).
    -- 권한 뷰는 친선 포함(경기별 상세 기록) — 시즌 요약은 Persistence가 공식만 걸러 계산한다.
    SELECT
        a.[MatchId], a.[TeamId], a.[MinutesPlayed],
        m.[HomeTeamId], m.[HomeTeamName], m.[AwayTeamName], m.[HomeScore], m.[AwayScore], m.[TournamentId], m.[MatchedAt], m.[MatchType],
        t.[Format]
    FROM [dbo].[SoccerMatchAppearances] a WITH (NOLOCK)
    JOIN [dbo].[SoccerMatches] m WITH (NOLOCK)
        ON m.[MatchId] = a.[MatchId] AND m.[Status] = 'Completed'
        AND (m.[MatchType] = 'Official' OR @GrantRequestId IS NOT NULL) AND m.[DeletedAt] IS NULL
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

    -- ⑥ 열람 승인 (권한 뷰에만 0~1행 — 승인일·만료일은 상단 배너 표시용)
    SELECT
        r.[RequestId], r.[AgentId], r.[PlayerId], r.[GuardianUserId], r.[Message],
        r.[Status], r.[RequestedAt], r.[ReviewedAt], r.[ExpiresAt],
        r.[CreatedAt], r.[UpdatedAt], r.[DeletedAt]
    FROM [dbo].[SoccerAgentViewRequests] r WITH (NOLOCK)
    WHERE r.[RequestId] = @GrantRequestId;
END
