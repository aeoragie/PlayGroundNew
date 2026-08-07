-- 팀 일정 소프트 삭제·복구 (@Restore = 1 이면 실행취소, B3 규약).
-- 소유 검증 실패·대상 없음은 빈 결과. 반환된 행으로 Application이 팀원 알림을 보낸다(삭제 시).
CREATE PROCEDURE [dbo].[UspDeleteSoccerSchedule]
    @ManagerUserId UNIQUEIDENTIFIER,
    @ScheduleId    UNIQUEIDENTIFIER,
    @Restore       BIT = 0
AS
BEGIN
    SET NOCOUNT ON;

    -- 시각은 dbo.UfnSystemDate()로만 얻는다. 변수로 한 번 받는 이유는 두 가지다 —
    -- 스칼라 UDF는 인라인되지 않아 WHERE에 직접 쓰면 행마다 호출되고,
    -- 한 프로시저 안의 "지금"이 호출마다 달라지는 것도 막는다.
    DECLARE @Now DATETIME2(7) = dbo.UfnSystemDate();

    UPDATE s
    SET s.[DeletedAt] = CASE WHEN @Restore = 1 THEN NULL ELSE @Now END,
        s.[UpdatedAt] = @Now
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
