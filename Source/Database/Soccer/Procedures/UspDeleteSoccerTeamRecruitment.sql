-- 모집 공고 소프트 삭제·복구 (@Restore = 1 이면 실행취소, B3 규약).
-- 소유 검증 실패·대상 없음은 빈 결과. 복구는 삭제 상태의 행만 되살린다.
CREATE PROCEDURE [dbo].[UspDeleteSoccerTeamRecruitment]
    @ManagerUserId UNIQUEIDENTIFIER,
    @RecruitmentId UNIQUEIDENTIFIER,
    @Restore BIT = 0
AS
BEGIN
    SET NOCOUNT ON;

    UPDATE r
    SET r.[DeletedAt] = CASE WHEN @Restore = 1 THEN NULL ELSE GETUTCDATE() END,
        r.[UpdatedAt] = GETUTCDATE()
    FROM [dbo].[SoccerTeamRecruitments] r
    JOIN [dbo].[SoccerTeams] t
        ON t.[TeamId] = r.[TeamId] AND t.[ManagerUserId] = @ManagerUserId AND t.[DeletedAt] IS NULL
    WHERE r.[RecruitmentId] = @RecruitmentId
      AND ((@Restore = 0 AND r.[DeletedAt] IS NULL) OR (@Restore = 1 AND r.[DeletedAt] IS NOT NULL));

    DECLARE @Applied INT = @@ROWCOUNT;

    SELECT
        r.[RecruitmentId], r.[TeamId], r.[Title], r.[Description], r.[ConditionsJson],
        r.[DeadlineDate], r.[Status], r.[CreatedAt], r.[UpdatedAt], r.[DeletedAt]
    FROM [dbo].[SoccerTeamRecruitments] r WITH (NOLOCK)
    WHERE r.[RecruitmentId] = @RecruitmentId AND @Applied = 1;
END
