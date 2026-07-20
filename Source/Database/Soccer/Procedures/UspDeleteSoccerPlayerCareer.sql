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
            SET [DeletedAt] = NULL, [UpdatedAt] = GETUTCDATE()
            WHERE [CareerId] = @CareerId AND [PlayerId] = @PlayerId AND [DeletedAt] IS NOT NULL;
        END
        ELSE
        BEGIN
            UPDATE [dbo].[SoccerPlayerCareers]
            SET [DeletedAt] = GETUTCDATE(), [UpdatedAt] = GETUTCDATE()
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
