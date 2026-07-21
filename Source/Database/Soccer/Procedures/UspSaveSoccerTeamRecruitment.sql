-- 모집 공고 저장 — 신규·수정 겸용 (@RecruitmentId 빈 GUID = 신규, B3 규약).
-- 소유 판정은 팀 ManagerUserId — 거부·미존재는 빈 결과 (존재 여부 미노출). 마감된 공고는 수정할 수 없다.
CREATE PROCEDURE [dbo].[UspSaveSoccerTeamRecruitment]
    @ManagerUserId UNIQUEIDENTIFIER,
    @RecruitmentId UNIQUEIDENTIFIER,
    @Title VARCHAR(300),
    @Description VARCHAR(1500),
    @ConditionsJson VARCHAR(600) = NULL,
    @DeadlineDate DATE = NULL
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @Applied INT = 0;
    DECLARE @TeamId UNIQUEIDENTIFIER = (
        SELECT TOP 1 [TeamId] FROM [dbo].[SoccerTeams]
        WHERE [ManagerUserId] = @ManagerUserId AND [DeletedAt] IS NULL
        ORDER BY [CreatedAt] DESC);

    IF @TeamId IS NOT NULL
    BEGIN
        IF @RecruitmentId = CAST(0x0 AS UNIQUEIDENTIFIER)
        BEGIN
            SET @RecruitmentId = NEWID();

            INSERT INTO [dbo].[SoccerTeamRecruitments]
                ([RecruitmentId], [TeamId], [Title], [Description], [ConditionsJson], [DeadlineDate])
            VALUES (@RecruitmentId, @TeamId, @Title, @Description, @ConditionsJson, @DeadlineDate);

            SET @Applied = 1;
        END
        ELSE
        BEGIN
            UPDATE [dbo].[SoccerTeamRecruitments]
            SET [Title] = @Title, [Description] = @Description,
                [ConditionsJson] = @ConditionsJson, [DeadlineDate] = @DeadlineDate,
                [UpdatedAt] = GETUTCDATE()
            WHERE [RecruitmentId] = @RecruitmentId AND [TeamId] = @TeamId
              AND [Status] = 'Open' AND [DeletedAt] IS NULL;

            SET @Applied = @@ROWCOUNT;
        END
    END

    SELECT
        r.[RecruitmentId], r.[TeamId], r.[Title], r.[Description], r.[ConditionsJson],
        r.[DeadlineDate], r.[Status], r.[CreatedAt], r.[UpdatedAt], r.[DeletedAt]
    FROM [dbo].[SoccerTeamRecruitments] r WITH (NOLOCK)
    WHERE r.[RecruitmentId] = @RecruitmentId AND @Applied = 1;
END
