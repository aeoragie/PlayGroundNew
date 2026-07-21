-- 진학·진로 사례 저장 — 신규·수정 겸용 (@OutcomeId 빈 GUID = 신규, B3 규약).
-- 소유 판정은 팀 ManagerUserId — 거부·미존재는 빈 결과 (존재 여부 미노출).
CREATE PROCEDURE [dbo].[UspSaveSoccerTeamCareerOutcome]
    @ManagerUserId UNIQUEIDENTIFIER,
    @OutcomeId UNIQUEIDENTIFIER,
    @OutcomeYear INT,
    @OutcomeType VARCHAR(20),
    @Title VARCHAR(300),
    @Detail VARCHAR(600) = NULL,
    @PlayerCount INT = 1
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
        IF @OutcomeId = CAST(0x0 AS UNIQUEIDENTIFIER)
        BEGIN
            SET @OutcomeId = NEWID();

            INSERT INTO [dbo].[SoccerTeamCareerOutcomes]
                ([OutcomeId], [TeamId], [OutcomeYear], [OutcomeType], [Title], [Detail], [PlayerCount])
            VALUES (@OutcomeId, @TeamId, @OutcomeYear, @OutcomeType, @Title, @Detail, @PlayerCount);

            SET @Applied = 1;
        END
        ELSE
        BEGIN
            UPDATE [dbo].[SoccerTeamCareerOutcomes]
            SET [OutcomeYear] = @OutcomeYear, [OutcomeType] = @OutcomeType,
                [Title] = @Title, [Detail] = @Detail, [PlayerCount] = @PlayerCount,
                [UpdatedAt] = GETUTCDATE()
            WHERE [OutcomeId] = @OutcomeId AND [TeamId] = @TeamId AND [DeletedAt] IS NULL;

            SET @Applied = @@ROWCOUNT;
        END
    END

    SELECT
        o.[OutcomeId], o.[TeamId], o.[OutcomeYear], o.[OutcomeType], o.[Title], o.[Detail],
        o.[PlayerCount], o.[CreatedAt], o.[UpdatedAt], o.[DeletedAt]
    FROM [dbo].[SoccerTeamCareerOutcomes] o WITH (NOLOCK)
    WHERE o.[OutcomeId] = @OutcomeId AND @Applied = 1;
END
