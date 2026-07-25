-- 공개 홈 Schedule 탭 + iCal 피드 — 슬러그로 팀의 **공개(IsPublic=1) 일정만**. 비공개·삭제 제외.
-- 공개 홈이 없는(비공개) 팀도 슬러그로 조회되지 않게 IsPublicProfile 조건은 컨트롤러/Application에서 판단하지 않고
-- 여기서는 팀 슬러그 매칭만 한다(공개 홈 접근 자체는 AllowAnonymous — 팀 홈이 이미 공개 전제).
CREATE PROCEDURE [dbo].[UspGetSoccerSchedulesBySlug]
    @Slug VARCHAR(200)
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @TeamId UNIQUEIDENTIFIER = (
        SELECT TOP 1 [TeamId] FROM [dbo].[SoccerTeams] WITH (NOLOCK)
        WHERE [Slug] = @Slug AND [DeletedAt] IS NULL);

    SELECT
        s.[ScheduleId], s.[TeamId], s.[Type], s.[Title], s.[OpponentName], s.[StartsAt],
        s.[Venue], s.[IsPublic], s.[MatchId], s.[CreatedBy], s.[CreatedAt], s.[UpdatedAt], s.[DeletedAt]
    FROM [dbo].[SoccerSchedules] s WITH (NOLOCK)
    WHERE s.[TeamId] = @TeamId AND s.[IsPublic] = 1 AND s.[DeletedAt] IS NULL
    ORDER BY s.[StartsAt];
END
