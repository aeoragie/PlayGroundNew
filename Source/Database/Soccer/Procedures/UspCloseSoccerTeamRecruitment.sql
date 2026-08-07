-- 모집 공고 마감 — Open → Closed 단방향 (재오픈 없음: 새 모집은 새 공고로).
-- 소유 검증 실패·이미 마감·미존재는 빈 결과.
CREATE PROCEDURE [dbo].[UspCloseSoccerTeamRecruitment]
    @ManagerUserId UNIQUEIDENTIFIER,
    @RecruitmentId UNIQUEIDENTIFIER
AS
BEGIN
    SET NOCOUNT ON;

    -- 시각은 dbo.UfnSystemDate()로만 얻는다. 변수로 한 번 받는 이유는 두 가지다 —
    -- 스칼라 UDF는 인라인되지 않아 WHERE에 직접 쓰면 행마다 호출되고,
    -- 한 프로시저 안의 "지금"이 호출마다 달라지는 것도 막는다.
    DECLARE @Now DATETIME2(7) = dbo.UfnSystemDate();

    UPDATE r
    SET r.[Status] = 'Closed', r.[UpdatedAt] = @Now
    FROM [dbo].[SoccerTeamRecruitments] r
    JOIN [dbo].[SoccerTeams] t
        ON t.[TeamId] = r.[TeamId] AND t.[ManagerUserId] = @ManagerUserId AND t.[DeletedAt] IS NULL
    WHERE r.[RecruitmentId] = @RecruitmentId AND r.[Status] = 'Open' AND r.[DeletedAt] IS NULL;

    DECLARE @Closed INT = @@ROWCOUNT;

    SELECT
        r.[RecruitmentId], r.[TeamId], r.[Title], r.[Description], r.[ConditionsJson],
        r.[DeadlineAt], r.[Status], r.[CreatedAt], r.[UpdatedAt], r.[DeletedAt]
    FROM [dbo].[SoccerTeamRecruitments] r WITH (NOLOCK)
    WHERE r.[RecruitmentId] = @RecruitmentId AND @Closed = 1;
END
