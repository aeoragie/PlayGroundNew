-- 진학·진로 사례 소프트 삭제·복구 (@Restore = 1이면 실행취소, B3 규약).
-- 소유 판정은 팀 ManagerUserId — 거부·미존재는 빈 결과.
CREATE PROCEDURE [dbo].[UspDeleteSoccerTeamCareerOutcome]
    @ManagerUserId UNIQUEIDENTIFIER,
    @OutcomeId UNIQUEIDENTIFIER,
    @Restore BIT = 0
AS
BEGIN
    SET NOCOUNT ON;

    UPDATE o
    SET o.[DeletedAt] = CASE WHEN @Restore = 1 THEN NULL ELSE GETUTCDATE() END,
        o.[UpdatedAt] = GETUTCDATE()
    FROM [dbo].[SoccerTeamCareerOutcomes] o
    JOIN [dbo].[SoccerTeams] t
        ON t.[TeamId] = o.[TeamId] AND t.[ManagerUserId] = @ManagerUserId AND t.[DeletedAt] IS NULL
    WHERE o.[OutcomeId] = @OutcomeId
      AND ((@Restore = 0 AND o.[DeletedAt] IS NULL) OR (@Restore = 1 AND o.[DeletedAt] IS NOT NULL));

    DECLARE @Applied INT = @@ROWCOUNT;

    SELECT
        o.[OutcomeId], o.[TeamId], o.[OutcomeYear], o.[OutcomeType], o.[Title], o.[Detail],
        o.[PlayerCount], o.[CreatedAt], o.[UpdatedAt], o.[DeletedAt]
    FROM [dbo].[SoccerTeamCareerOutcomes] o WITH (NOLOCK)
    WHERE o.[OutcomeId] = @OutcomeId AND @Applied = 1;
END
