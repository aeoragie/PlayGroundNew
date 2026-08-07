-- @entity: SoccerPlayerCareerDeleteRecord
-- @source: join
-- @join: SoccerPlayerCareers AS c (CareerId, DeletedAt)
-- 선수 커리어 이력 소프트 삭제 / 복구(실행취소). 관리 주체(UserId 연결 계정) 소유 행만.
-- @Restore = 1이면 DeletedAt을 지워 되돌린다 — 토스트의 "실행취소"가 이 경로를 쓴다.
-- 권한 없음·대상 없음은 구분하지 않고 빈 결과로 응답한다.
CREATE PROCEDURE [dbo].[UspDeleteSoccerPlayerCareer]
    @UserId UNIQUEIDENTIFIER,
    @CareerId UNIQUEIDENTIFIER,
    @Restore BIT = 0,
    @TargetPlayerId UNIQUEIDENTIFIER = NULL
AS
BEGIN
    SET NOCOUNT ON;

    -- 시각은 dbo.UfnSystemDate()로만 얻는다. 변수로 한 번 받는 이유는 두 가지다 —
    -- 스칼라 UDF는 인라인되지 않아 WHERE에 직접 쓰면 행마다 호출되고,
    -- 한 프로시저 안의 "지금"이 호출마다 달라지는 것도 막는다.
    DECLARE @Now DATETIME2(7) = dbo.UfnSystemDate();

    DECLARE @PlayerId UNIQUEIDENTIFIER = (
        SELECT TOP 1 [PlayerId]
        FROM [dbo].[SoccerPlayers] WITH (NOLOCK)
        WHERE [UserId] = @UserId AND [DeletedAt] IS NULL
          AND (@TargetPlayerId IS NULL OR [PlayerId] = @TargetPlayerId)
        ORDER BY [CreatedAt]);

    IF @PlayerId IS NOT NULL
    BEGIN
        IF @Restore = 1
        BEGIN
            UPDATE [dbo].[SoccerPlayerCareers]
            SET [DeletedAt] = NULL, [UpdatedAt] = @Now
            WHERE [CareerId] = @CareerId AND [PlayerId] = @PlayerId AND [DeletedAt] IS NOT NULL;
        END
        ELSE
        BEGIN
            UPDATE [dbo].[SoccerPlayerCareers]
            SET [DeletedAt] = @Now, [UpdatedAt] = @Now
            WHERE [CareerId] = @CareerId AND [PlayerId] = @PlayerId AND [DeletedAt] IS NULL;
        END

        IF @@ROWCOUNT > 0
        BEGIN
            SELECT c.[CareerId], c.[DeletedAt]
            FROM [dbo].[SoccerPlayerCareers] c WITH (NOLOCK)
            WHERE c.[CareerId] = @CareerId;
            RETURN;
        END
    END

    SELECT c.[CareerId], c.[DeletedAt]
    FROM [dbo].[SoccerPlayerCareers] c WITH (NOLOCK)
    WHERE 1 = 0;
END
