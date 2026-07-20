-- @entity: SoccerPlayerPortfolioSaveRecord
-- @source: join
-- @join: SoccerPlayerPortfolioVideos AS v (VideoId, IsPrimary)
-- 포트폴리오 영상 저장 (신규·수정 겸용). 관리 주체(UserId 연결 계정) 소유 행만. 권한 없으면 빈 결과.
-- @VideoId 빈 GUID = 신규 등록.
-- 대표 영상은 선수당 1개다 — 대표로 지정하면 나머지를 내리고, 첫 영상은 자동으로 대표가 된다
-- (영상이 있는데 대표가 없는 상태를 만들지 않는다 — 화면이 대표 슬롯을 먼저 그린다).
-- 주의(제너레이터): 파라미터 줄에 꼬리 주석을 달면 그 파라미터가 누락된다.
CREATE PROCEDURE [dbo].[UspSaveSoccerPlayerPortfolioVideo]
    @UserId UNIQUEIDENTIFIER,
    @VideoId UNIQUEIDENTIFIER,
    @Title VARCHAR(300),
    @VideoUrl VARCHAR(2048),
    @ThumbnailUrl VARCHAR(2048) = NULL,
    @Tags VARCHAR(600) = NULL,
    @RecordedOn DATE = NULL,
    @IsPrimary BIT = 0,
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

    IF @PlayerId IS NULL
    BEGIN
        SELECT v.[VideoId], v.[IsPrimary]
        FROM [dbo].[SoccerPlayerPortfolioVideos] v WITH (NOLOCK)
        WHERE 1 = 0;
        RETURN;
    END

    BEGIN TRANSACTION;

    -- 첫 영상이면 대표로 (등록 시점 기준 — 수정 중인 자기 자신은 제외)
    IF NOT EXISTS (
        SELECT 1 FROM [dbo].[SoccerPlayerPortfolioVideos] WITH (UPDLOCK, HOLDLOCK)
        WHERE [PlayerId] = @PlayerId AND [DeletedAt] IS NULL AND [VideoId] <> @VideoId)
    BEGIN
        SET @IsPrimary = 1;
    END

    IF @VideoId = '00000000-0000-0000-0000-000000000000'
    BEGIN
        SET @VideoId = NEWID();

        INSERT INTO [dbo].[SoccerPlayerPortfolioVideos]
            ([VideoId], [PlayerId], [Title], [VideoUrl], [ThumbnailUrl], [Tags], [RecordedOn], [IsPrimary])
        VALUES
            (@VideoId, @PlayerId, @Title, @VideoUrl, @ThumbnailUrl, @Tags, @RecordedOn, @IsPrimary);
    END
    ELSE
    BEGIN
        UPDATE [dbo].[SoccerPlayerPortfolioVideos]
        SET [Title] = @Title,
            [VideoUrl] = @VideoUrl,
            [ThumbnailUrl] = @ThumbnailUrl,
            [Tags] = @Tags,
            [RecordedOn] = @RecordedOn,
            [IsPrimary] = @IsPrimary,
            [UpdatedAt] = GETUTCDATE()
        WHERE [VideoId] = @VideoId AND [PlayerId] = @PlayerId AND [DeletedAt] IS NULL;

        IF @@ROWCOUNT = 0
        BEGIN
            COMMIT TRANSACTION;

            SELECT v.[VideoId], v.[IsPrimary]
            FROM [dbo].[SoccerPlayerPortfolioVideos] v WITH (NOLOCK)
            WHERE 1 = 0;
            RETURN;
        END
    END

    -- 대표는 하나뿐 — 방금 지정한 영상 외에는 내린다
    IF @IsPrimary = 1
    BEGIN
        UPDATE [dbo].[SoccerPlayerPortfolioVideos]
        SET [IsPrimary] = 0, [UpdatedAt] = GETUTCDATE()
        WHERE [PlayerId] = @PlayerId AND [VideoId] <> @VideoId AND [IsPrimary] = 1 AND [DeletedAt] IS NULL;
    END

    COMMIT TRANSACTION;

    SELECT v.[VideoId], v.[IsPrimary]
    FROM [dbo].[SoccerPlayerPortfolioVideos] v WITH (NOLOCK)
    WHERE v.[VideoId] = @VideoId AND v.[DeletedAt] IS NULL;
END
