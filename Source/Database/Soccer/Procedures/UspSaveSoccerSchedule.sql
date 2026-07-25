-- 팀 일정 저장 — 신규·수정 겸용 (@ScheduleId 빈 GUID = 신규, B3 규약).
-- 소유 판정은 팀 ManagerUserId — 거부·미존재는 빈 결과 (존재 여부 미노출).
-- 상태 컬럼 없음(StartsAt 파생). 유형별 필드 가변(경기·대회=OpponentName, 대회·훈련=Title)은 Application이 검증.
CREATE PROCEDURE [dbo].[UspSaveSoccerSchedule]
    @ManagerUserId UNIQUEIDENTIFIER,
    @ScheduleId    UNIQUEIDENTIFIER,
    @Type          VARCHAR(20),
    @Title         VARCHAR(300) = NULL,
    @OpponentName  VARCHAR(300) = NULL,
    @StartsAt      DATETIME2,
    @Venue         VARCHAR(300),
    @IsPublic      BIT = 1
AS
BEGIN
    SET NOCOUNT ON;

    IF @Title = '' SET @Title = NULL;
    IF @OpponentName = '' SET @OpponentName = NULL;

    DECLARE @Applied INT = 0;
    DECLARE @TeamId UNIQUEIDENTIFIER = (
        SELECT TOP 1 [TeamId] FROM [dbo].[SoccerTeams]
        WHERE [ManagerUserId] = @ManagerUserId AND [DeletedAt] IS NULL
        ORDER BY [CreatedAt] DESC);

    IF @TeamId IS NOT NULL
    BEGIN
        IF @ScheduleId = CAST(0x0 AS UNIQUEIDENTIFIER)
        BEGIN
            SET @ScheduleId = NEWID();

            INSERT INTO [dbo].[SoccerSchedules]
                ([ScheduleId], [TeamId], [Type], [Title], [OpponentName], [StartsAt], [Venue], [IsPublic], [CreatedBy])
            VALUES
                (@ScheduleId, @TeamId, @Type, @Title, @OpponentName, @StartsAt, @Venue, @IsPublic, @ManagerUserId);

            SET @Applied = 1;
        END
        ELSE
        BEGIN
            UPDATE [dbo].[SoccerSchedules]
            SET [Type] = @Type, [Title] = @Title, [OpponentName] = @OpponentName,
                [StartsAt] = @StartsAt, [Venue] = @Venue, [IsPublic] = @IsPublic,
                [UpdatedAt] = GETUTCDATE()
            WHERE [ScheduleId] = @ScheduleId AND [TeamId] = @TeamId AND [DeletedAt] IS NULL;

            SET @Applied = @@ROWCOUNT;
        END
    END

    SELECT
        s.[ScheduleId], s.[TeamId], s.[Type], s.[Title], s.[OpponentName], s.[StartsAt],
        s.[Venue], s.[IsPublic], s.[MatchId], s.[CreatedBy], s.[CreatedAt], s.[UpdatedAt], s.[DeletedAt]
    FROM [dbo].[SoccerSchedules] s WITH (NOLOCK)
    WHERE s.[ScheduleId] = @ScheduleId AND @Applied = 1;
END
