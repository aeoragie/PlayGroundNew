-- 팀 대시보드 일정 섹션 — 관리자의 팀 전체 일정(공개·비공개 모두). 소유 팀 없으면 빈 결과.
-- 정렬은 Application/Client이 월 단위·미래 우선으로 가공한다(여기선 StartsAt 오름차순).
CREATE PROCEDURE [dbo].[UspGetSoccerSchedulesByManager]
    @ManagerUserId UNIQUEIDENTIFIER
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @TeamId UNIQUEIDENTIFIER = (
        SELECT TOP 1 [TeamId] FROM [dbo].[SoccerTeams] WITH (NOLOCK)
        WHERE [ManagerUserId] = @ManagerUserId AND [DeletedAt] IS NULL
        ORDER BY [CreatedAt] DESC);

    SELECT
        s.[ScheduleId], s.[TeamId], s.[Type], s.[Title], s.[OpponentName], s.[StartsAt],
        s.[Venue], s.[IsPublic], s.[MatchId], s.[CreatedBy], s.[CreatedAt], s.[UpdatedAt], s.[DeletedAt]
    FROM [dbo].[SoccerSchedules] s WITH (NOLOCK)
    WHERE s.[TeamId] = @TeamId AND s.[DeletedAt] IS NULL
    ORDER BY s.[StartsAt];
END
