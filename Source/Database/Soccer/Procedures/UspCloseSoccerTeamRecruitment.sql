-- 모집 공고 마감 — Open → Closed 단방향 (재오픈 없음: 새 모집은 새 공고로).
-- 소유 검증 실패·이미 마감·미존재는 빈 결과.
CREATE PROCEDURE [dbo].[UspCloseSoccerTeamRecruitment]
    @ManagerUserId UNIQUEIDENTIFIER,
    @RecruitmentId UNIQUEIDENTIFIER
AS
BEGIN
    SET NOCOUNT ON;

    UPDATE r
    SET r.[Status] = 'Closed', r.[UpdatedAt] = GETUTCDATE()
    FROM [dbo].[SoccerTeamRecruitments] r
    JOIN [dbo].[SoccerTeams] t
        ON t.[TeamId] = r.[TeamId] AND t.[ManagerUserId] = @ManagerUserId AND t.[DeletedAt] IS NULL
    WHERE r.[RecruitmentId] = @RecruitmentId AND r.[Status] = 'Open' AND r.[DeletedAt] IS NULL;

    DECLARE @Closed INT = @@ROWCOUNT;

    SELECT
        r.[RecruitmentId], r.[TeamId], r.[Title], r.[Description], r.[ConditionsJson],
        r.[DeadlineDate], r.[Status], r.[CreatedAt], r.[UpdatedAt], r.[DeletedAt]
    FROM [dbo].[SoccerTeamRecruitments] r WITH (NOLOCK)
    WHERE r.[RecruitmentId] = @RecruitmentId AND @Closed = 1;
END
