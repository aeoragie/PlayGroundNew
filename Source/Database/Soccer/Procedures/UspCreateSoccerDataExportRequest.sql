-- 데이터 내려받기 요청 생성 (Design.SettingsFlows ③). 동시 1건 + 재요청 쿨다운 24h를 원자적으로 판정.
-- 결과: RS1 상태 스칼라('Ok'|'InProgress'|'Cooldown'), RS2 새 RequestId(생성됐을 때만, 아니면 빈 결과).
--   InProgress = 진행 중(Pending) 요청 존재 / Cooldown = 최근 24h 내 성공/진행 요청 존재(실패는 즉시 재시도 허용).
-- 생성만 한다 — 파일 생성은 백그라운드 잡(요청 API는 접수만 반환, 동기 생성 금지).
CREATE PROCEDURE [dbo].[UspCreateSoccerDataExportRequest]
    @UserId          UNIQUEIDENTIFIER,
    @IncludeProfile  BIT,
    @IncludeRecords  BIT,
    @IncludeRequests BIT
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @Status VARCHAR(20);
    DECLARE @RequestId UNIQUEIDENTIFIER = CAST(0x0 AS UNIQUEIDENTIFIER);

    BEGIN TRY
        BEGIN TRANSACTION;

        IF EXISTS (
            SELECT 1 FROM [dbo].[SoccerDataExportRequests] WITH (UPDLOCK, HOLDLOCK)
            WHERE [UserId] = @UserId AND [Status] = 'Pending' AND [DeletedAt] IS NULL)
        BEGIN
            SET @Status = 'InProgress';
        END
        -- 쿨다운: 최근 24h 내 실패가 아닌 요청(성공·진행)이 있으면 재요청 차단. 실패는 즉시 재시도.
        ELSE IF EXISTS (
            SELECT 1 FROM [dbo].[SoccerDataExportRequests] WITH (UPDLOCK, HOLDLOCK)
            WHERE [UserId] = @UserId AND [Status] <> 'Failed' AND [DeletedAt] IS NULL
              AND [CreatedAt] >= DATEADD(HOUR, -24, GETUTCDATE()))
        BEGIN
            SET @Status = 'Cooldown';
        END
        ELSE
        BEGIN
            SET @RequestId = NEWID();
            INSERT INTO [dbo].[SoccerDataExportRequests]
                ([RequestId], [UserId], [Status], [IncludeProfile], [IncludeRecords], [IncludeRequests])
            VALUES (@RequestId, @UserId, 'Pending', @IncludeProfile, @IncludeRecords, @IncludeRequests);
            SET @Status = 'Ok';
        END

        COMMIT TRANSACTION;
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
        THROW;
    END CATCH

    SELECT @Status AS [Status];
    SELECT [RequestId] FROM [dbo].[SoccerDataExportRequests] WHERE [RequestId] = @RequestId;
END
