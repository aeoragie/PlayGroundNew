-- 코드 없이(공개 선수 프로필 경유) 연결 요청 생성 + 팀 관리자 알림. InviteId는 NULL.
-- 결과셋은 코드 기반 생성(UspCreateSoccerPlayerClaimRequest)과 같은 모양 → @entity 마커 없이
-- 리포지토리가 SoccerClaimRequestOwnRecord를 재사용한다(중복 생성 방지).
-- 멱등: 같은 계정·선수 Pending 요청이 있으면 그대로 반환. 대상이 연결됐거나 없으면 빈 결과.
CREATE PROCEDURE [dbo].[UspCreateSoccerPlayerClaimRequestByPlayer]
    @UserId UNIQUEIDENTIFIER,
    @RequesterName VARCHAR(300),
    @PlayerId UNIQUEIDENTIFIER,
    @Relation VARCHAR(20)
AS
BEGIN
    SET NOCOUNT ON;

    -- 대상 선수는 미연결(UserId NULL)이어야 하고, 알림 보낼 소속팀이 있어야 한다
    DECLARE @TeamId UNIQUEIDENTIFIER = (
        SELECT TOP 1 tp.[TeamId]
        FROM [dbo].[SoccerTeamPlayers] tp WITH (NOLOCK)
        JOIN [dbo].[SoccerPlayers] p WITH (NOLOCK)
            ON p.[PlayerId] = tp.[PlayerId] AND p.[UserId] IS NULL AND p.[DeletedAt] IS NULL
        WHERE tp.[PlayerId] = @PlayerId AND tp.[Status] = 'Active' AND tp.[DeletedAt] IS NULL);

    DECLARE @RequestId UNIQUEIDENTIFIER;

    IF @TeamId IS NOT NULL
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
                VALUES (@RequestId, NULL, @PlayerId, @TeamId, @UserId, @RequesterName, @Relation);

                -- 팀 관리자에게 액션형 알림 (코드 없이 요청이라 SubText는 경유 표기)
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
                    '프로필 경유 요청', @Relation
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
