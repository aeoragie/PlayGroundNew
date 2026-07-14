-- @entity: SoccerClaimInviteRecord
-- @source: join
-- @join: SoccerPlayers AS p (PlayerId, Name)
-- @join: SoccerTeams AS t (TeamName)
-- 초대코드로 로스터 선수 프로필을 계정에 연결(Claim).
-- 유효 조건: Pending·미만료 코드 + 대상 선수 미연결. 실패 시 빈 결과 (사유 구분은 서버 로그).
-- 같은 계정의 기존(온보딩) 프로필 행이 있으면 값을 이전(COALESCE)하고 소프트 삭제 — 계정당 활성 연결 1행 유지.
CREATE PROCEDURE [dbo].[UspClaimSoccerPlayerInvite]
    @UserId UNIQUEIDENTIFIER,
    @Code VARCHAR(12)
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @InviteId UNIQUEIDENTIFIER, @PlayerId UNIQUEIDENTIFIER, @TeamId UNIQUEIDENTIFIER;

    SELECT TOP 1 @InviteId = [InviteId], @PlayerId = [PlayerId], @TeamId = [TeamId]
    FROM [dbo].[SoccerPlayerInvites]
    WHERE [Code] = UPPER(@Code) AND [Status] = 'Pending'
      AND ([ExpiresAt] IS NULL OR [ExpiresAt] > GETUTCDATE());

    -- 대상 선수가 삭제됐거나 이미 다른 계정에 연결돼 있으면 실패
    IF @PlayerId IS NOT NULL AND NOT EXISTS (
        SELECT 1 FROM [dbo].[SoccerPlayers]
        WHERE [PlayerId] = @PlayerId AND [UserId] IS NULL AND [DeletedAt] IS NULL)
    BEGIN
        SET @PlayerId = NULL;
    END

    IF @PlayerId IS NOT NULL
    BEGIN
        BEGIN TRY
            BEGIN TRANSACTION;

            --.// 기존(온보딩) 프로필 병합 — 로스터 행에 없는 값만 이전 후 소프트 삭제

            DECLARE @OldPlayerId UNIQUEIDENTIFIER = (
                SELECT TOP 1 [PlayerId] FROM [dbo].[SoccerPlayers]
                WHERE [UserId] = @UserId AND [DeletedAt] IS NULL AND [PlayerId] <> @PlayerId
                ORDER BY [CreatedAt] DESC);

            IF @OldPlayerId IS NOT NULL
            BEGIN
                UPDATE target
                SET target.[BirthDate] = COALESCE(target.[BirthDate], old.[BirthDate]),
                    target.[Region] = COALESCE(target.[Region], old.[Region]),
                    target.[PhotoUrl] = COALESCE(target.[PhotoUrl], old.[PhotoUrl]),
                    target.[HeightCm] = COALESCE(target.[HeightCm], old.[HeightCm]),
                    target.[WeightKg] = COALESCE(target.[WeightKg], old.[WeightKg]),
                    target.[PreferredFoot] = COALESCE(target.[PreferredFoot], old.[PreferredFoot]),
                    target.[SchoolName] = COALESCE(target.[SchoolName], old.[SchoolName]),
                    target.[GuardianPhone] = COALESCE(target.[GuardianPhone], old.[GuardianPhone])
                FROM [dbo].[SoccerPlayers] target
                JOIN [dbo].[SoccerPlayers] old ON old.[PlayerId] = @OldPlayerId
                WHERE target.[PlayerId] = @PlayerId;

                UPDATE [dbo].[SoccerPlayers]
                SET [DeletedAt] = GETUTCDATE(), [UpdatedAt] = GETUTCDATE()
                WHERE [PlayerId] = @OldPlayerId;
            END

            UPDATE [dbo].[SoccerPlayers]
            SET [UserId] = @UserId, [UpdatedAt] = GETUTCDATE()
            WHERE [PlayerId] = @PlayerId;

            UPDATE [dbo].[SoccerPlayerInvites]
            SET [Status] = 'Claimed', [ClaimedByUserId] = @UserId, [ClaimedAt] = GETUTCDATE()
            WHERE [InviteId] = @InviteId;

            COMMIT TRANSACTION;
        END TRY
        BEGIN CATCH
            IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
            THROW;
        END CATCH
    END

    SELECT p.[PlayerId], p.[Name], t.[TeamName]
    FROM [dbo].[SoccerPlayers] p WITH (NOLOCK)
    LEFT JOIN [dbo].[SoccerTeams] t WITH (NOLOCK) ON t.[TeamId] = @TeamId AND t.[DeletedAt] IS NULL
    WHERE p.[PlayerId] = @PlayerId;
END
