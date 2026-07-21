-- @entity: SoccerClaimRequestOwnRecord
-- @source: join
-- @join: SoccerPlayerClaimRequests AS r (RequestId, Relation, Status, CreatedAt)
-- @join: SoccerPlayers AS p (Name)
-- @join: SoccerTeams AS t (TeamName)
-- Claim 플로우 스텝 ② → ③: 연결 요청 생성 + 팀 관리자에게 액션형 알림 발송 (한 트랜잭션).
-- 같은 계정의 같은 선수 Pending 요청이 이미 있으면 새로 만들지 않고 그 요청을 반환한다 (멱등 — 재방문·이중 제출).
-- 코드가 무효(만료·소진·선수 연결됨)면 빈 결과.
CREATE PROCEDURE [dbo].[UspCreateSoccerPlayerClaimRequest]
    @UserId UNIQUEIDENTIFIER,
    @RequesterName VARCHAR(300),
    @Code VARCHAR(12),
    @Relation VARCHAR(20)
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @InviteId UNIQUEIDENTIFIER, @PlayerId UNIQUEIDENTIFIER, @TeamId UNIQUEIDENTIFIER;

    SELECT TOP 1 @InviteId = i.[InviteId], @PlayerId = i.[PlayerId], @TeamId = i.[TeamId]
    FROM [dbo].[SoccerPlayerInvites] i
    JOIN [dbo].[SoccerPlayers] p ON p.[PlayerId] = i.[PlayerId] AND p.[UserId] IS NULL AND p.[DeletedAt] IS NULL
    WHERE i.[Code] = UPPER(@Code) AND i.[Status] = 'Pending'
      AND (i.[ExpiresAt] IS NULL OR i.[ExpiresAt] > GETUTCDATE());

    DECLARE @RequestId UNIQUEIDENTIFIER;

    IF @PlayerId IS NOT NULL
    BEGIN
        -- 멱등: 내 Pending 요청이 이미 있으면 그대로 반환
        SELECT @RequestId = [RequestId]
        FROM [dbo].[SoccerPlayerClaimRequests]
        WHERE [RequesterUserId] = @UserId AND [PlayerId] = @PlayerId
          AND [Status] = 'Pending' AND [DeletedAt] IS NULL;

        IF @RequestId IS NULL
        BEGIN
            BEGIN TRY
                BEGIN TRANSACTION;

                SET @RequestId = NEWID();

                INSERT INTO [dbo].[SoccerPlayerClaimRequests]
                    ([RequestId], [InviteId], [PlayerId], [TeamId], [RequesterUserId], [RequesterName], [Relation])
                VALUES (@RequestId, @InviteId, @PlayerId, @TeamId, @UserId, @RequesterName, @Relation);

                -- 팀 관리자에게 액션형 알림 (스냅샷: 요청자·관계·선수·포지션 메타·사용 코드)
                INSERT INTO [dbo].[SoccerNotifications]
                    ([RecipientUserId], [NotificationType], [RefId], [TargetPlayerId],
                     [ActorName], [PlayerName], [TeamName], [MetaText], [SubText], [Relation])
                SELECT
                    t.[ManagerUserId], 'ClaimRequest', @RequestId, @PlayerId,
                    @RequesterName, p.[Name], t.[TeamName],
                    STUFF(CONCAT(
                        CASE WHEN tp.[Position] IS NOT NULL AND tp.[Position] <> '' THEN CONCAT(' · ', tp.[Position]) ELSE '' END,
                        CASE WHEN tp.[JerseyNumber] IS NOT NULL AND tp.[JerseyNumber] <> '' THEN CONCAT(' · #', tp.[JerseyNumber]) ELSE '' END,
                        CASE WHEN p.[AgeGroup] IS NOT NULL AND p.[AgeGroup] <> '' THEN CONCAT(' · ', p.[AgeGroup]) ELSE '' END), 1, 3, ''),
                    UPPER(@Code), @Relation
                FROM [dbo].[SoccerTeams] t
                JOIN [dbo].[SoccerPlayers] p ON p.[PlayerId] = @PlayerId
                LEFT JOIN [dbo].[SoccerTeamPlayers] tp
                    ON tp.[TeamId] = @TeamId AND tp.[PlayerId] = @PlayerId
                   AND tp.[Status] = 'Active' AND tp.[DeletedAt] IS NULL
                WHERE t.[TeamId] = @TeamId AND t.[DeletedAt] IS NULL;

                COMMIT TRANSACTION;
            END TRY
            BEGIN CATCH
                IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
                THROW;
            END CATCH
        END
    END

    SELECT r.[RequestId], r.[Relation], r.[Status], r.[CreatedAt], p.[Name], t.[TeamName]
    FROM [dbo].[SoccerPlayerClaimRequests] r WITH (NOLOCK)
    JOIN [dbo].[SoccerPlayers] p WITH (NOLOCK) ON p.[PlayerId] = r.[PlayerId]
    JOIN [dbo].[SoccerTeams] t WITH (NOLOCK) ON t.[TeamId] = r.[TeamId]
    WHERE r.[RequestId] = @RequestId;
END
