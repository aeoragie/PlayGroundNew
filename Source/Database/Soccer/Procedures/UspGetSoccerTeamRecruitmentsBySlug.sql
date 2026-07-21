-- 공개 팀 홈 모집 탭 (Slug 기준, 비로그인 읽기전용). 비공개·미존재 팀은 빈 결과.
-- 정렬: 모집중(Open·마감일 미경과) 먼저 → 최신순. "모집중" 최종 판정은 Persistence(단일 기준 공유).
CREATE PROCEDURE [dbo].[UspGetSoccerTeamRecruitmentsBySlug]
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
        r.[RecruitmentId], r.[TeamId], r.[Title], r.[Description], r.[ConditionsJson],
        r.[DeadlineDate], r.[Status], r.[CreatedAt], r.[UpdatedAt], r.[DeletedAt]
    FROM [dbo].[SoccerTeamRecruitments] r WITH (NOLOCK)
    WHERE r.[TeamId] = @TeamId AND r.[DeletedAt] IS NULL
    ORDER BY
        CASE WHEN r.[Status] = 'Open'
              AND (r.[DeadlineDate] IS NULL OR r.[DeadlineDate] >= CAST(GETUTCDATE() AS DATE))
             THEN 0 ELSE 1 END,
        r.[CreatedAt] DESC;
END
