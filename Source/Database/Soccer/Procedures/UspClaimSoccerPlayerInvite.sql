-- @entity: SoccerClaimInviteRecord
-- @source: join
-- @join: SoccerPlayers AS p (PlayerId, Name)
-- @join: SoccerTeams AS t (TeamName)
-- 초대코드로 로스터 선수 프로필을 계정에 연결(Claim).
-- 유효 조건: Pending·미만료 코드 + 대상 선수 미연결. 실패 시 빈 결과 (사유 구분은 서버 로그).
--
-- **한 계정이 자녀를 여러 명 관리할 수 있다** (보호자 대리 관리가 P0 시나리오).
-- 다만 "온보딩으로 만든 내 임시 프로필"과 "이미 연결된 다른 자녀"는 구분해야 한다:
--   · 로스터에 없는 프로필 = 온보딩 임시본. 같은 사람의 로스터 행을 Claim한 것이므로 값을 옮기고 소프트 삭제.
--   · 로스터에 있는 프로필 = 이미 연결된 다른 자녀. 건드리지 않는다.
-- 예전에는 무조건 소프트 삭제해서 둘째 자녀를 연결하면 첫째가 사라졌다.
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

            --.// 온보딩 임시 프로필만 병합 — 이미 연결된 다른 자녀는 건드리지 않는다.
            -- 판별: 로스터(SoccerTeamPlayers)에 없는 프로필 = 온보딩으로 만든 임시본.
            -- 연결된 자녀는 팀 로스터에서 온 행이라 반드시 로스터 행이 있다.

            DECLARE @OldPlayerId UNIQUEIDENTIFIER = (
                SELECT TOP 1 p.[PlayerId] FROM [dbo].[SoccerPlayers] p
                WHERE p.[UserId] = @UserId AND p.[DeletedAt] IS NULL AND p.[PlayerId] <> @PlayerId
                  AND NOT EXISTS (
                      SELECT 1 FROM [dbo].[SoccerTeamPlayers] tp WITH (NOLOCK)
                      WHERE tp.[PlayerId] = p.[PlayerId] AND tp.[DeletedAt] IS NULL)
                ORDER BY p.[CreatedAt] DESC);

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
