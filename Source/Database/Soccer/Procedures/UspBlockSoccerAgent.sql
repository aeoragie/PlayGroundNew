-- @entity: SoccerAgentBlockRecord
-- @source: join
-- @join: SoccerAgentBlocks AS b (BlockId, AgentId)
-- "이 에이전트의 요청 다시 받지 않기" — 차단 행 생성(멱등) + 대기 중이던 그 요청은 거절 처리.
-- 요청 생성 거부는 에이전트 서비스가 이 테이블을 조회해 강제한다. 소유 아님은 빈 결과.
CREATE PROCEDURE [dbo].[UspBlockSoccerAgent]
    @GuardianUserId UNIQUEIDENTIFIER,
    @RequestId UNIQUEIDENTIFIER
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @AgentId UNIQUEIDENTIFIER = (
        SELECT [AgentId] FROM [dbo].[SoccerAgentViewRequests]
        WHERE [RequestId] = @RequestId AND [GuardianUserId] = @GuardianUserId AND [DeletedAt] IS NULL);

    IF @AgentId IS NOT NULL
    BEGIN
        BEGIN TRY
            BEGIN TRANSACTION;

            IF NOT EXISTS (SELECT 1 FROM [dbo].[SoccerAgentBlocks]
                           WHERE [GuardianUserId] = @GuardianUserId AND [AgentId] = @AgentId)
            BEGIN
                INSERT INTO [dbo].[SoccerAgentBlocks] ([GuardianUserId], [AgentId])
                VALUES (@GuardianUserId, @AgentId);
            END

            UPDATE [dbo].[SoccerAgentViewRequests]
            SET [Status] = 'Denied', [ReviewedAt] = GETUTCDATE(), [UpdatedAt] = GETUTCDATE()
            WHERE [RequestId] = @RequestId AND [Status] = 'Pending';

            UPDATE [dbo].[SoccerNotifications]
            SET [IsRead] = 1, [ReadAt] = GETUTCDATE()
            WHERE [RecipientUserId] = @GuardianUserId AND [NotificationType] = 'ViewRequest'
              AND [RefId] = @RequestId AND [IsRead] = 0;

            COMMIT TRANSACTION;
        END TRY
        BEGIN CATCH
            IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
            THROW;
        END CATCH
    END

    SELECT b.[BlockId], b.[AgentId]
    FROM [dbo].[SoccerAgentBlocks] b WITH (NOLOCK)
    WHERE b.[GuardianUserId] = @GuardianUserId AND b.[AgentId] = @AgentId;
END
