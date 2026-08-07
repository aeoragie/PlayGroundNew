-- 모집 공고 소프트 삭제·복구 (@Restore = 1 이면 실행취소, B3 규약).
-- 소유 검증 실패·대상 없음은 빈 결과. 복구는 삭제 상태의 행만 되살린다.
CREATE PROCEDURE [dbo].[UspDeleteSoccerTeamRecruitment]
    @ManagerUserId UNIQUEIDENTIFIER,
    @RecruitmentId UNIQUEIDENTIFIER,
    @Restore BIT = 0
AS
BEGIN
    SET NOCOUNT ON;

    -- 시각은 dbo.UfnSystemDate()로만 얻는다. 변수로 한 번 받는 이유는 두 가지다 —
    -- 스칼라 UDF는 인라인되지 않아 WHERE에 직접 쓰면 행마다 호출되고,
    -- 한 프로시저 안의 "지금"이 호출마다 달라지는 것도 막는다.
    DECLARE @Now DATETIME2(7) = dbo.UfnSystemDate();

    UPDATE r
    SET r.[DeletedAt] = CASE WHEN @Restore = 1 THEN NULL ELSE @Now END,
        r.[UpdatedAt] = @Now
    FROM [dbo].[SoccerTeamRecruitments] r
    JOIN [dbo].[SoccerTeams] t
        ON t.[TeamId] = r.[TeamId] AND t.[ManagerUserId] = @ManagerUserId AND t.[DeletedAt] IS NULL
    WHERE r.[RecruitmentId] = @RecruitmentId
      AND ((@Restore = 0 AND r.[DeletedAt] IS NULL) OR (@Restore = 1 AND r.[DeletedAt] IS NOT NULL));

    DECLARE @Applied INT = @@ROWCOUNT;

    SELECT
        r.[RecruitmentId], r.[TeamId], r.[Title], r.[Description], r.[ConditionsJson],
        r.[DeadlineAt], r.[Status], r.[CreatedAt], r.[UpdatedAt], r.[DeletedAt]
    FROM [dbo].[SoccerTeamRecruitments] r WITH (NOLOCK)
    WHERE r.[RecruitmentId] = @RecruitmentId AND @Applied = 1;
END
