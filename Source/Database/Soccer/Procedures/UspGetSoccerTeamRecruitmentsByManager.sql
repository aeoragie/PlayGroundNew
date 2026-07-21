-- 팀 대시보드 모집 섹션 — 관리자 소유 팀의 공고 목록 (정렬 규칙은 공개 조회와 동일).
CREATE PROCEDURE [dbo].[UspGetSoccerTeamRecruitmentsByManager]
    @ManagerUserId UNIQUEIDENTIFIER
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        r.[RecruitmentId], r.[TeamId], r.[Title], r.[Description], r.[ConditionsJson],
        r.[DeadlineDate], r.[Status], r.[CreatedAt], r.[UpdatedAt], r.[DeletedAt]
    FROM [dbo].[SoccerTeamRecruitments] r WITH (NOLOCK)
    JOIN [dbo].[SoccerTeams] t WITH (NOLOCK)
        ON t.[TeamId] = r.[TeamId] AND t.[ManagerUserId] = @ManagerUserId AND t.[DeletedAt] IS NULL
    WHERE r.[DeletedAt] IS NULL
    ORDER BY
        CASE WHEN r.[Status] = 'Open'
              AND (r.[DeadlineDate] IS NULL OR r.[DeadlineDate] >= CAST(GETUTCDATE() AS DATE))
             THEN 0 ELSE 1 END,
        r.[CreatedAt] DESC;
END
