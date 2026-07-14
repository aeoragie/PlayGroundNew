-- 관리 주체(UserId) 기준 선수 커리어 목록 조회 (선수 대시보드 커리어 섹션).
-- 정렬: 현재 소속 우선, 이후 시작일 역순 (타임라인 상단 = 최신).
CREATE PROCEDURE [dbo].[UspGetSoccerPlayerCareersByUser]
    @UserId UNIQUEIDENTIFIER
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @PlayerId UNIQUEIDENTIFIER = (
        SELECT TOP 1 [PlayerId]
        FROM [dbo].[SoccerPlayers] WITH (NOLOCK)
        WHERE [UserId] = @UserId AND [DeletedAt] IS NULL
        ORDER BY [CreatedAt] DESC);

    SELECT
        c.[CareerId], c.[PlayerId], c.[TeamName], c.[TeamId], c.[IsCurrent], c.[BadgeLabel],
        c.[StartDate], c.[EndDate], c.[Role], c.[Note], c.[IsVerified],
        c.[CreatedAt], c.[UpdatedAt], c.[DeletedAt]
    FROM [dbo].[SoccerPlayerCareers] c WITH (NOLOCK)
    WHERE c.[PlayerId] = @PlayerId AND c.[DeletedAt] IS NULL
    ORDER BY c.[IsCurrent] DESC, c.[StartDate] DESC;
END
