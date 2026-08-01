-- 공식 경기 상세 (Records 내 화면, 공개·읽기 전용). 대회 서비스 SingleIdx 모델 대응 — MatchId로 조회.
-- 결과셋 5개: ①경기 → ②대회 헤더(브레드크럼·경기시간) → ③이벤트(타임라인) → ④출전(라인업)
--            → ⑤등장 선수 공개 슬러그(Claim된 선수만 프로필 링크). MultiQueryReader로 소비.
-- 전반은 저장값, 후반은 (총점-전반)으로 Persistence 계산. 친선(TournamentId NULL)은 ②가 빈 결과.
CREATE PROCEDURE [dbo].[UspGetSoccerMatchDetail]
    @MatchId UNIQUEIDENTIFIER
AS
BEGIN
    SET NOCOUNT ON;

    -- ① 경기
    SELECT
        m.[MatchId], m.[MatchType], m.[TournamentId], m.[StageType], m.[GroupName], m.[RoundName],
        m.[HomeTeamId], m.[HomeTeamName], m.[AwayTeamId], m.[AwayTeamName],
        m.[HomeScore], m.[AwayScore], m.[HomePkScore], m.[AwayPkScore],
        m.[FirstHalfHomeScore], m.[FirstHalfAwayScore], m.[Status], m.[MatchedAt], m.[VenueName],
        m.[RefereeName], m.[MatchSequence], m.[HomeCoachName], m.[AwayCoachName],
        m.[DataSource], m.[ExternalId], m.[SyncStatus], m.[CreatedAt], m.[UpdatedAt], m.[DeletedAt]
    FROM [dbo].[SoccerMatches] m WITH (NOLOCK)
    WHERE m.[MatchId] = @MatchId AND m.[DeletedAt] IS NULL;

    -- ② 대회 헤더 (브레드크럼·경기 시간) — 친선은 행 없음
    SELECT
        t.[TournamentId], t.[SeasonYear], t.[Name], t.[Format], t.[AgeGroup], t.[MatchTimeText]
    FROM [dbo].[SoccerTournaments] t WITH (NOLOCK)
    JOIN [dbo].[SoccerMatches] m WITH (NOLOCK) ON m.[TournamentId] = t.[TournamentId]
    WHERE m.[MatchId] = @MatchId AND t.[DeletedAt] IS NULL;

    -- ③ 이벤트 (타임라인 — 분 오름차순)
    SELECT
        e.[EventId], e.[MatchId], e.[TeamId], e.[TeamName], e.[EventType],
        e.[PlayerId], e.[PlayerName], e.[JerseyNumber], e.[AssistPlayerId], e.[AssistPlayerName],
        e.[MinuteOfPlay], e.[CreatedAt], e.[UpdatedAt], e.[DeletedAt]
    FROM [dbo].[SoccerMatchEvents] e WITH (NOLOCK)
    WHERE e.[MatchId] = @MatchId AND e.[DeletedAt] IS NULL
    ORDER BY e.[MinuteOfPlay], e.[CreatedAt];

    -- ④ 출전 (라인업 — 선발 먼저, 등번호 순)
    SELECT
        a.[AppearanceId], a.[MatchId], a.[TeamId], a.[TeamName], a.[PlayerId], a.[PlayerName],
        a.[JerseyNumber], a.[Position], a.[IsCaptain], a.[MinutesPlayed], a.[IsStarter],
        a.[CreatedAt], a.[UpdatedAt], a.[DeletedAt]
    FROM [dbo].[SoccerMatchAppearances] a WITH (NOLOCK)
    WHERE a.[MatchId] = @MatchId AND a.[DeletedAt] IS NULL
    ORDER BY a.[IsStarter] DESC, a.[JerseyNumber];

    -- ⑤ 등장 선수 공개 슬러그 (Claim된 선수 = UserId 있음 + Slug 있음 → 프로필 링크)
    SELECT p.[PlayerId], p.[Slug]
    FROM [dbo].[SoccerPlayers] p WITH (NOLOCK)
    WHERE p.[DeletedAt] IS NULL AND p.[UserId] IS NOT NULL AND p.[Slug] IS NOT NULL
      AND p.[PlayerId] IN (
        SELECT e.[PlayerId] FROM [dbo].[SoccerMatchEvents] e WITH (NOLOCK)
        WHERE e.[MatchId] = @MatchId AND e.[PlayerId] IS NOT NULL AND e.[DeletedAt] IS NULL
        UNION
        SELECT a.[PlayerId] FROM [dbo].[SoccerMatchAppearances] a WITH (NOLOCK)
        WHERE a.[MatchId] = @MatchId AND a.[PlayerId] IS NOT NULL AND a.[DeletedAt] IS NULL);
END
