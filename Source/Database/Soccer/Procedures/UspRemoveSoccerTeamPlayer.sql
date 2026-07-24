-- 로스터에서 선수 내보내기(소프트 삭제)·복구 겸용 — @Restore = 1 이면 실행취소 (B3 규약).
-- 소속(SoccerTeamPlayers)만 내린다 — 선수 프로필(SoccerPlayers)은 남긴다(가족이 연결돼 있으면 계속 관리).
-- 미처리 초대는 소속과 함께 회수/복원한다(로스터에서 사라진 선수의 코드가 살아 있으면 안 된다).
-- 소유 판정은 팀 ManagerUserId — 거부·대상 없음은 빈 결과(존재 여부 미노출 → Command가 Forbidden).
CREATE PROCEDURE [dbo].[UspRemoveSoccerTeamPlayer]
    @ManagerUserId UNIQUEIDENTIFIER,
    @TeamPlayerId UNIQUEIDENTIFIER,
    @Restore BIT = 0
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @PlayerId UNIQUEIDENTIFIER;
    DECLARE @TeamId UNIQUEIDENTIFIER;

    BEGIN TRY
        BEGIN TRANSACTION;

        UPDATE tp
        SET tp.[DeletedAt] = CASE WHEN @Restore = 1 THEN NULL ELSE GETUTCDATE() END,
            tp.[UpdatedAt] = GETUTCDATE(),
            @PlayerId = tp.[PlayerId],
            @TeamId = tp.[TeamId]
        FROM [dbo].[SoccerTeamPlayers] tp
        JOIN [dbo].[SoccerTeams] t
            ON t.[TeamId] = tp.[TeamId] AND t.[ManagerUserId] = @ManagerUserId AND t.[DeletedAt] IS NULL
        WHERE tp.[TeamPlayerId] = @TeamPlayerId
          AND ((@Restore = 0 AND tp.[DeletedAt] IS NULL) OR (@Restore = 1 AND tp.[DeletedAt] IS NOT NULL));

        DECLARE @Applied INT = @@ROWCOUNT;

        IF @Applied = 1
        BEGIN
            -- 내보내면 미처리 초대를 회수, 복구하면 되살린다(미만료 한정)
            IF @Restore = 0
            BEGIN
                UPDATE [dbo].[SoccerPlayerInvites]
                SET [Status] = 'Revoked'
                WHERE [PlayerId] = @PlayerId AND [TeamId] = @TeamId AND [Status] = 'Pending';
            END
            ELSE
            BEGIN
                UPDATE [dbo].[SoccerPlayerInvites]
                SET [Status] = 'Pending'
                WHERE [PlayerId] = @PlayerId AND [TeamId] = @TeamId AND [Status] = 'Revoked'
                  AND ([ExpiresAt] IS NULL OR [ExpiresAt] > GETUTCDATE());
            END
        END

        COMMIT TRANSACTION;
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
        THROW;
    END CATCH

    -- 적용된 소속 행을 돌려준다(없으면 빈 결과 → Forbidden)
    SELECT tp.[TeamPlayerId], tp.[TeamId], tp.[PlayerId], tp.[JerseyNumber], tp.[Position],
           tp.[Grade], tp.[Status], tp.[CreatedAt], tp.[UpdatedAt], tp.[DeletedAt]
    FROM [dbo].[SoccerTeamPlayers] tp WITH (NOLOCK)
    WHERE tp.[TeamPlayerId] = @TeamPlayerId AND @Applied = 1;
END
