-- @entity: SoccerAgentViewRequestRecord
-- @source: join
-- @join: SoccerAgentViewRequests AS r (RequestId, AgentId, Message, Status, RequestedAt, ExpiresAt)
-- @join: SoccerPlayers AS p (PlayerId, Name, AgeGroup)
-- @join: SoccerTeamPlayers AS tp (Position)
-- 열람 요청 심사 — 'Approve'(Pending → Approved, 만료 = +30일, 로그 남김) / 'Deny'(Pending → Denied)
-- / 'Revoke'(Approved → Revoked — 화면은 거절과 동급). 처리하면 해당 열람 요청 알림은 읽음이 된다.
-- 소유 아님·전이 불가 상태는 빈 결과.
CREATE PROCEDURE [dbo].[UspReviewSoccerAgentViewRequest]
    @GuardianUserId UNIQUEIDENTIFIER,
    @RequestId UNIQUEIDENTIFIER,
    @Action VARCHAR(20)
AS
BEGIN
    SET NOCOUNT ON;

    -- 시각은 dbo.UfnSystemDate()로만 얻는다. 변수로 한 번 받는 이유는 두 가지다 —
    -- 스칼라 UDF는 인라인되지 않아 WHERE에 직접 쓰면 행마다 호출되고,
    -- 한 프로시저 안의 "지금"이 호출마다 달라지는 것도 막는다.
    DECLARE @Now DATETIME2(7) = dbo.UfnSystemDate();

    DECLARE @Applied INT = 0;

    BEGIN TRY
        BEGIN TRANSACTION;

        IF @Action = 'Approve'
        BEGIN
            UPDATE [dbo].[SoccerAgentViewRequests]
            SET [Status] = 'Approved', [ReviewedAt] = @Now,
                [ExpiresAt] = DATEADD(DAY, 30, @Now), [UpdatedAt] = @Now
            WHERE [RequestId] = @RequestId AND [GuardianUserId] = @GuardianUserId
              AND [Status] = 'Pending' AND [DeletedAt] IS NULL;

            SET @Applied = @@ROWCOUNT;

            IF @Applied = 1
            BEGIN
                INSERT INTO [dbo].[SoccerAgentViewLogs] ([RequestId], [EventType])
                VALUES (@RequestId, 'Approved');
            END
        END
        ELSE IF @Action = 'Deny'
        BEGIN
            UPDATE [dbo].[SoccerAgentViewRequests]
            SET [Status] = 'Denied', [ReviewedAt] = @Now, [UpdatedAt] = @Now
            WHERE [RequestId] = @RequestId AND [GuardianUserId] = @GuardianUserId
              AND [Status] = 'Pending' AND [DeletedAt] IS NULL;

            SET @Applied = @@ROWCOUNT;
        END
        ELSE IF @Action = 'Revoke'
        BEGIN
            UPDATE [dbo].[SoccerAgentViewRequests]
            SET [Status] = 'Revoked', [ReviewedAt] = @Now, [UpdatedAt] = @Now
            WHERE [RequestId] = @RequestId AND [GuardianUserId] = @GuardianUserId
              AND [Status] = 'Approved' AND [DeletedAt] IS NULL;

            SET @Applied = @@ROWCOUNT;
        END

        -- 처리 = 열람 요청 알림 읽음
        IF @Applied = 1
        BEGIN
            UPDATE [dbo].[SoccerNotifications]
            SET [IsRead] = 1, [ReadAt] = @Now
            WHERE [RecipientUserId] = @GuardianUserId AND [NotificationType] = 'ViewRequest'
              AND [RefId] = @RequestId AND [IsRead] = 0;
        END

        COMMIT TRANSACTION;
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
        THROW;
    END CATCH

    SELECT
        r.[RequestId], r.[AgentId], r.[Message], r.[Status], r.[RequestedAt], r.[ExpiresAt],
        p.[PlayerId], p.[Name], p.[AgeGroup], tp.[Position]
    FROM [dbo].[SoccerAgentViewRequests] r WITH (NOLOCK)
    JOIN [dbo].[SoccerPlayers] p WITH (NOLOCK) ON p.[PlayerId] = r.[PlayerId]
    LEFT JOIN [dbo].[SoccerTeamPlayers] tp WITH (NOLOCK)
        ON tp.[TeamId] = p.[TeamId] AND tp.[PlayerId] = p.[PlayerId]
       AND tp.[Status] = 'Active' AND tp.[DeletedAt] IS NULL
    WHERE r.[RequestId] = @RequestId AND @Applied = 1;
END
