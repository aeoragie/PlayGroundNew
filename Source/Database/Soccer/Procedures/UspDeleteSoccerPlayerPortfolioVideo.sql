-- @entity: SoccerPlayerPortfolioDeleteRecord
-- @source: join
-- @join: SoccerPlayerPortfolioVideos AS v (VideoId, DeletedAt)
-- 포트폴리오 영상 소프트 삭제 / 복구(실행취소). 관리 주체(UserId 연결 계정) 소유 행만.
-- @Restore = 1이면 DeletedAt을 지워 되돌린다 — 토스트의 "실행취소"가 이 경로를 쓴다.
-- 대표 영상을 지우면 남은 영상 중 하나를 대표로 올린다(영상이 있는데 대표가 없는 상태 방지).
-- 복구는 대표 자리를 빼앗지 않는다 — 그 사이 올라간 대표를 존중한다.
CREATE PROCEDURE [dbo].[UspDeleteSoccerPlayerPortfolioVideo]
    @UserId UNIQUEIDENTIFIER,
    @VideoId UNIQUEIDENTIFIER,
    @Restore BIT = 0
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @PlayerId UNIQUEIDENTIFIER = (
        SELECT TOP 1 [PlayerId]
        FROM [dbo].[SoccerPlayers] WITH (NOLOCK)
        WHERE [UserId] = @UserId AND [DeletedAt] IS NULL
        ORDER BY [CreatedAt] DESC);

    IF @PlayerId IS NULL
    BEGIN
        SELECT v.[VideoId], v.[DeletedAt]
        FROM [dbo].[SoccerPlayerPortfolioVideos] v WITH (NOLOCK)
        WHERE 1 = 0;
        RETURN;
    END

    BEGIN TRANSACTION;

    DECLARE @WasPrimary BIT = 0;

    IF @Restore = 1
    BEGIN
        UPDATE [dbo].[SoccerPlayerPortfolioVideos]
        SET [DeletedAt] = NULL, [IsPrimary] = 0, [UpdatedAt] = GETUTCDATE()
        WHERE [VideoId] = @VideoId AND [PlayerId] = @PlayerId AND [DeletedAt] IS NOT NULL;
    END
    ELSE
    BEGIN
        SELECT @WasPrimary = [IsPrimary]
        FROM [dbo].[SoccerPlayerPortfolioVideos] WITH (UPDLOCK, HOLDLOCK)
        WHERE [VideoId] = @VideoId AND [PlayerId] = @PlayerId AND [DeletedAt] IS NULL;

        UPDATE [dbo].[SoccerPlayerPortfolioVideos]
        SET [DeletedAt] = GETUTCDATE(), [IsPrimary] = 0, [UpdatedAt] = GETUTCDATE()
        WHERE [VideoId] = @VideoId AND [PlayerId] = @PlayerId AND [DeletedAt] IS NULL;
    END

    IF @@ROWCOUNT = 0
    BEGIN
        COMMIT TRANSACTION;

        SELECT v.[VideoId], v.[DeletedAt]
        FROM [dbo].[SoccerPlayerPortfolioVideos] v WITH (NOLOCK)
        WHERE 1 = 0;
        RETURN;
    END

    -- 대표를 지웠으면 남은 것 중 최신 하나를 대표로
    IF @Restore = 0 AND @WasPrimary = 1
    BEGIN
        UPDATE [dbo].[SoccerPlayerPortfolioVideos]
        SET [IsPrimary] = 1, [UpdatedAt] = GETUTCDATE()
        WHERE [VideoId] = (
            SELECT TOP 1 [VideoId]
            FROM [dbo].[SoccerPlayerPortfolioVideos] WITH (UPDLOCK)
            WHERE [PlayerId] = @PlayerId AND [DeletedAt] IS NULL
            ORDER BY [RecordedOn] DESC, [CreatedAt] DESC);
    END

    -- 복구했는데 대표가 하나도 없으면(마지막 영상을 지웠다 되돌린 경우) 자기가 대표가 된다
    IF @Restore = 1 AND NOT EXISTS (
        SELECT 1 FROM [dbo].[SoccerPlayerPortfolioVideos] WITH (UPDLOCK)
        WHERE [PlayerId] = @PlayerId AND [DeletedAt] IS NULL AND [IsPrimary] = 1)
    BEGIN
        UPDATE [dbo].[SoccerPlayerPortfolioVideos]
        SET [IsPrimary] = 1, [UpdatedAt] = GETUTCDATE()
        WHERE [VideoId] = @VideoId;
    END

    COMMIT TRANSACTION;

    SELECT v.[VideoId], v.[DeletedAt]
    FROM [dbo].[SoccerPlayerPortfolioVideos] v WITH (NOLOCK)
    WHERE v.[VideoId] = @VideoId;
END
