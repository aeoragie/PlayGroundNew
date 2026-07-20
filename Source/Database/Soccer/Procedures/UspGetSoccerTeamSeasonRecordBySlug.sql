-- 공개 팀 홈 시즌성적 탭 조회 (Slug 기준, 비로그인 읽기전용). 비공개·미존재 팀은 빈 결과.
-- 결과셋 4개: ⓪TeamId(IsHome 판별·존재 확인) → ①최근 종료 경기(TOP 8, 대회명·형식 — 친선 NULL)
--            → ②리그 순위 → ③경기영상(팀 소유+팀 경기 연결). 이벤트 없음(공개는 승무패 뱃지만).
-- 팀 관점 변환·승무패·요약 집계는 Persistence/클라이언트 몫. 관리 정보(UserId 등) 미노출.
-- SoccerTeamMatchRecord·SoccerMatchVideosEntity 엔티티 재사용(마커 불필요).
CREATE PROCEDURE [dbo].[UspGetSoccerTeamSeasonRecordBySlug]
    @Slug VARCHAR(100),
    @SeasonYear INT
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @TeamId UNIQUEIDENTIFIER = (
        SELECT TOP 1 [TeamId]
        FROM [dbo].[SoccerTeams] WITH (NOLOCK)
        WHERE [Slug] = @Slug AND [IsPublicProfile] = 1 AND [DeletedAt] IS NULL
        ORDER BY [CreatedAt] DESC);

    SELECT @TeamId AS [TeamId];

    SELECT TOP 8
        m.[MatchId], m.[TournamentId], m.[HomeTeamId], m.[HomeTeamName], m.[AwayTeamId], m.[AwayTeamName],
        m.[HomeScore], m.[AwayScore], m.[HomePkScore], m.[AwayPkScore], m.[Status], m.[MatchedAt], m.[VenueName], m.[MatchType],
        t.[Name], t.[Format]
    FROM [dbo].[SoccerMatches] m WITH (NOLOCK)
    LEFT JOIN [dbo].[SoccerTournaments] t WITH (NOLOCK)
        ON t.[TournamentId] = m.[TournamentId] AND t.[DeletedAt] IS NULL
    WHERE (m.[HomeTeamId] = @TeamId OR m.[AwayTeamId] = @TeamId)
      AND m.[Status] = 'Completed' AND m.[DeletedAt] IS NULL
      AND (t.[SeasonYear] = @SeasonYear OR (m.[TournamentId] IS NULL AND YEAR(m.[MatchedAt]) = @SeasonYear))
    ORDER BY m.[MatchedAt] DESC;

    SELECT TOP 1 s.[TeamRank]
    FROM [dbo].[SoccerTournamentStandings] s WITH (NOLOCK)
    JOIN [dbo].[SoccerTournaments] t WITH (NOLOCK)
        ON t.[TournamentId] = s.[TournamentId] AND t.[SeasonYear] = @SeasonYear AND t.[DeletedAt] IS NULL
    WHERE s.[TeamId] = @TeamId AND s.[StageType] = 'League' AND s.[DeletedAt] IS NULL
    ORDER BY s.[UpdatedAt] DESC;

    SELECT
        v.[VideoId], v.[TournamentId], v.[MatchId], v.[TeamId], v.[Title], v.[VideoUrl], v.[ThumbnailUrl],
        v.[VideoType], v.[DurationSeconds], v.[RecordedOn], v.[CreatedAt], v.[UpdatedAt], v.[DeletedAt]
    FROM [dbo].[SoccerMatchVideos] v WITH (NOLOCK)
    WHERE v.[DeletedAt] IS NULL
      AND (v.[TeamId] = @TeamId
           OR v.[MatchId] IN (
               SELECT m.[MatchId] FROM [dbo].[SoccerMatches] m WITH (NOLOCK)
               WHERE (m.[HomeTeamId] = @TeamId OR m.[AwayTeamId] = @TeamId) AND m.[DeletedAt] IS NULL))
    ORDER BY v.[RecordedOn] DESC, v.[CreatedAt] DESC;
END
