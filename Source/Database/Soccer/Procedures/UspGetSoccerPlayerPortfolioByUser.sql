-- 관리 주체(UserId) 기준 선수 포트폴리오 영상 목록 조회 (선수 대시보드 포트폴리오 섹션).
-- 정렬: 대표 영상 우선, 이후 촬영일 역순.
CREATE PROCEDURE [dbo].[UspGetSoccerPlayerPortfolioByUser]
    @UserId UNIQUEIDENTIFIER,
    @TargetPlayerId UNIQUEIDENTIFIER = NULL
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @PlayerId UNIQUEIDENTIFIER = (
        SELECT TOP 1 [PlayerId]
        FROM [dbo].[SoccerPlayers] WITH (NOLOCK)
        WHERE [UserId] = @UserId AND [DeletedAt] IS NULL
          AND (@TargetPlayerId IS NULL OR [PlayerId] = @TargetPlayerId)
        ORDER BY [CreatedAt]);

    SELECT
        v.[VideoId], v.[PlayerId], v.[Title], v.[VideoUrl], v.[ThumbnailUrl],
        v.[DurationSeconds], v.[IsPrimary], v.[Tags], v.[RecordedOn],
        v.[CreatedAt], v.[UpdatedAt], v.[DeletedAt]
    FROM [dbo].[SoccerPlayerPortfolioVideos] v WITH (NOLOCK)
    WHERE v.[PlayerId] = @PlayerId AND v.[DeletedAt] IS NULL
    ORDER BY v.[IsPrimary] DESC, v.[RecordedOn] DESC, v.[CreatedAt] DESC;
END
