-- 팀 관리자 기준 경기영상 목록 조회 (팀 대시보드 경기영상 섹션).
-- 팀 소유 영상 + 우리 팀 경기에 연결된 영상. 단일 결과셋(테이블 엔티티) — 최신순.
CREATE PROCEDURE [dbo].[UspGetSoccerTeamVideosByManager]
    @ManagerUserId UNIQUEIDENTIFIER
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @TeamId UNIQUEIDENTIFIER = (
        SELECT TOP 1 [TeamId]
        FROM [dbo].[SoccerTeams] WITH (NOLOCK)
        WHERE [ManagerUserId] = @ManagerUserId AND [DeletedAt] IS NULL
        ORDER BY [CreatedAt] DESC);

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
