-- 팀 일정 소프트 삭제·복구 (@Restore = 1 이면 실행취소, B3 규약).
-- 소유 검증 실패·대상 없음은 빈 결과. 반환된 행으로 Application이 팀원 알림을 보낸다(삭제 시).
CREATE PROCEDURE [dbo].[UspDeleteSoccerSchedule]
    @ManagerUserId UNIQUEIDENTIFIER,
    @ScheduleId    UNIQUEIDENTIFIER,
    @Restore       BIT = 0
AS
BEGIN
    SET NOCOUNT ON;

    UPDATE s
    SET s.[DeletedAt] = CASE WHEN @Restore = 1 THEN NULL ELSE GETUTCDATE() END,
        s.[UpdatedAt] = GETUTCDATE()
    FROM [dbo].[SoccerSchedules] s
    JOIN [dbo].[SoccerTeams] t
        ON t.[TeamId] = s.[TeamId] AND t.[ManagerUserId] = @ManagerUserId AND t.[DeletedAt] IS NULL
    WHERE s.[ScheduleId] = @ScheduleId
      AND ((@Restore = 0 AND s.[DeletedAt] IS NULL) OR (@Restore = 1 AND s.[DeletedAt] IS NOT NULL));

    DECLARE @Applied INT = @@ROWCOUNT;

    SELECT
        s.[ScheduleId], s.[TeamId], s.[Type], s.[Title], s.[OpponentName], s.[StartsAt],
        s.[Venue], s.[IsPublic], s.[MatchId], s.[CreatedBy], s.[CreatedAt], s.[UpdatedAt], s.[DeletedAt]
    FROM [dbo].[SoccerSchedules] s WITH (NOLOCK)
    WHERE s.[ScheduleId] = @ScheduleId AND @Applied = 1;
END
