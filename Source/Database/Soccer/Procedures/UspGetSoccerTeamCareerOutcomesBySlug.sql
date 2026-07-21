-- 공개 팀 홈 진학·진로 탭 (Slug 기준, 비로그인 읽기전용). 비공개·미존재 팀은 빈 결과.
-- 요약 3카드 집계(유형별 PlayerCount 합)는 Persistence — 타임라인과 같은 행에서 파생해 어긋날 수 없다.
-- 정렬: 연도 역순 → 최신 등록순 (dc 타임라인).
CREATE PROCEDURE [dbo].[UspGetSoccerTeamCareerOutcomesBySlug]
    @Slug VARCHAR(100)
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @TeamId UNIQUEIDENTIFIER = (
        SELECT TOP 1 [TeamId]
        FROM [dbo].[SoccerTeams] WITH (NOLOCK)
        WHERE [Slug] = @Slug AND [IsPublicProfile] = 1 AND [DeletedAt] IS NULL
        ORDER BY [CreatedAt] DESC);

    SELECT
        o.[OutcomeId], o.[TeamId], o.[OutcomeYear], o.[OutcomeType], o.[Title], o.[Detail],
        o.[PlayerCount], o.[CreatedAt], o.[UpdatedAt], o.[DeletedAt]
    FROM [dbo].[SoccerTeamCareerOutcomes] o WITH (NOLOCK)
    WHERE o.[TeamId] = @TeamId AND o.[DeletedAt] IS NULL
    ORDER BY o.[OutcomeYear] DESC, o.[CreatedAt] DESC;
END
