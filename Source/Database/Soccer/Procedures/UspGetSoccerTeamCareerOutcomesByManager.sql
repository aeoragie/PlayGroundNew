-- 팀 관리자 기준 진학·진로 사례 목록 (팀 대시보드 팀 정보 섹션의 관리 카드).
CREATE PROCEDURE [dbo].[UspGetSoccerTeamCareerOutcomesByManager]
    @ManagerUserId UNIQUEIDENTIFIER
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @TeamId UNIQUEIDENTIFIER = (
        SELECT TOP 1 [TeamId]
        FROM [dbo].[SoccerTeams] WITH (NOLOCK)
        WHERE [ManagerUserId] = @ManagerUserId AND [DeletedAt] IS NULL
        ORDER BY [CreatedAt] DESC);

    SELECT
        o.[OutcomeId], o.[TeamId], o.[OutcomeYear], o.[OutcomeType], o.[Title], o.[Detail],
        o.[PlayerCount], o.[CreatedAt], o.[UpdatedAt], o.[DeletedAt]
    FROM [dbo].[SoccerTeamCareerOutcomes] o WITH (NOLOCK)
    WHERE o.[TeamId] = @TeamId AND o.[DeletedAt] IS NULL
    ORDER BY o.[OutcomeYear] DESC, o.[CreatedAt] DESC;
END
