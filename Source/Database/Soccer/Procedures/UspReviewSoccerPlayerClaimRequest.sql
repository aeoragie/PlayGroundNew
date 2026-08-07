-- @entity: SoccerClaimReviewRecord
-- @source: join
-- @join: SoccerPlayerClaimRequests AS r (RequestId, Status)
-- @join: SoccerPlayers AS p (Name)
-- 연결 요청 승인/거절 — 팀 관리자 전용. 소유 검증 실패·Pending 아님은 빈 결과 (존재 여부 미노출).
-- 승인: 기존 즉시 연결 프로시저(UspClaimSoccerPlayerInvite)를 재사용해 선수 연결·코드 소진·온보딩
--       임시 프로필 병합을 한 번에 처리하고(단일 진실), 가족 연결(FamilyLink)·대리 관리 플래그·
--       보호자 알림(ClaimApproved)을 같은 트랜잭션으로 얹는다.
-- 거절: 상태만 바꾸고 알림(ClaimRejected). 코드는 소진하지 않는다 — 다시 시도할 수 있다.
-- 처리하면 관리자의 해당 액션형 알림(ClaimRequest)은 읽음이 된다.
CREATE PROCEDURE [dbo].[UspReviewSoccerPlayerClaimRequest]
    @ManagerUserId UNIQUEIDENTIFIER,
    @RequestId UNIQUEIDENTIFIER,
    @Approve BIT
AS
BEGIN
    SET NOCOUNT ON;

    -- 시각은 dbo.UfnSystemDate()로만 얻는다. 변수로 한 번 받는 이유는 두 가지다 —
    -- 스칼라 UDF는 인라인되지 않아 WHERE에 직접 쓰면 행마다 호출되고,
    -- 한 프로시저 안의 "지금"이 호출마다 달라지는 것도 막는다.
    DECLARE @Now DATETIME2(7) = dbo.UfnSystemDate();

    DECLARE @PlayerId UNIQUEIDENTIFIER, @TeamId UNIQUEIDENTIFIER, @InviteId UNIQUEIDENTIFIER,
            @RequesterUserId UNIQUEIDENTIFIER, @RequesterName VARCHAR(300), @Relation VARCHAR(20);

    SELECT @PlayerId = r.[PlayerId], @TeamId = r.[TeamId], @InviteId = r.[InviteId],
           @RequesterUserId = r.[RequesterUserId], @RequesterName = r.[RequesterName], @Relation = r.[Relation]
    FROM [dbo].[SoccerPlayerClaimRequests] r
    JOIN [dbo].[SoccerTeams] t ON t.[TeamId] = r.[TeamId] AND t.[ManagerUserId] = @ManagerUserId AND t.[DeletedAt] IS NULL
    WHERE r.[RequestId] = @RequestId AND r.[Status] = 'Pending' AND r.[DeletedAt] IS NULL;

    IF @PlayerId IS NOT NULL
    BEGIN
        BEGIN TRY
            BEGIN TRANSACTION;

            IF @Approve = 1
            BEGIN
                DECLARE @LinkOk BIT = 0;

                IF @InviteId IS NOT NULL
                BEGIN
                    -- 코드 요청: 기존 연결 프로시저 재사용(선수 연결·코드 소진·온보딩 병합의 단일 진실)
                    DECLARE @Code VARCHAR(12) = (SELECT [Code] FROM [dbo].[SoccerPlayerInvites] WHERE [InviteId] = @InviteId);
                    DECLARE @Linked TABLE ([PlayerId] UNIQUEIDENTIFIER, [Name] VARCHAR(300), [TeamName] VARCHAR(300));

                    INSERT INTO @Linked
                    EXEC [dbo].[UspClaimSoccerPlayerInvite] @UserId = @RequesterUserId, @Code = @Code;

                    IF EXISTS (SELECT 1 FROM @Linked) SET @LinkOk = 1;
                END
                ELSE
                BEGIN
                    -- 프로필 경유 요청(코드 없음): 코드 소진 없이 직접 연결. 여전히 미연결일 때만.
                    -- (온보딩 임시 프로필 병합은 코드 경로에만 있다 — 프로필 경유는 팀 등록 선수를 바로 잡는다.)
                    UPDATE [dbo].[SoccerPlayers]
                    SET [UserId] = @RequesterUserId, [IsGuardianManaged] = 1, [UpdatedAt] = @Now
                    WHERE [PlayerId] = @PlayerId AND [UserId] IS NULL AND [DeletedAt] IS NULL;

                    IF @@ROWCOUNT = 1 SET @LinkOk = 1;
                END

                IF @LinkOk = 0
                BEGIN
                    -- 그사이 선수가 다른 계정에 연결됨(또는 코드 소진) — 전체 롤백, 요청은 Pending 유지
                    ROLLBACK TRANSACTION;
                    SELECT r.[RequestId], r.[Status], p.[Name]
                    FROM [dbo].[SoccerPlayerClaimRequests] r WITH (NOLOCK)
                    JOIN [dbo].[SoccerPlayers] p WITH (NOLOCK) ON p.[PlayerId] = r.[PlayerId]
                    WHERE 1 = 0;
                    RETURN;
                END

                -- 가족 연결 (이미 있으면 관계만 갱신) + 대리 관리 플래그
                IF EXISTS (SELECT 1 FROM [dbo].[SoccerPlayerFamilyLinks]
                           WHERE [PlayerId] = @PlayerId AND [UserId] = @RequesterUserId AND [DeletedAt] IS NULL)
                BEGIN
                    UPDATE [dbo].[SoccerPlayerFamilyLinks]
                    SET [Relation] = @Relation, [UpdatedAt] = @Now
                    WHERE [PlayerId] = @PlayerId AND [UserId] = @RequesterUserId AND [DeletedAt] IS NULL;
                END
                ELSE
                BEGIN
                    INSERT INTO [dbo].[SoccerPlayerFamilyLinks]
                        ([PlayerId], [UserId], [MemberName], [Role], [Relation])
                    VALUES (@PlayerId, @RequesterUserId, @RequesterName, 'Guardian', @Relation);
                END

                UPDATE [dbo].[SoccerPlayers]
                SET [IsGuardianManaged] = 1, [UpdatedAt] = @Now
                WHERE [PlayerId] = @PlayerId;

                UPDATE [dbo].[SoccerPlayerClaimRequests]
                SET [Status] = 'Approved', [ReviewedAt] = @Now, [UpdatedAt] = @Now
                WHERE [RequestId] = @RequestId;

                INSERT INTO [dbo].[SoccerNotifications]
                    ([RecipientUserId], [NotificationType], [RefId], [TargetPlayerId], [PlayerName], [TeamName], [Relation])
                SELECT @RequesterUserId, 'ClaimApproved', @RequestId, @PlayerId, p.[Name], t.[TeamName], @Relation
                FROM [dbo].[SoccerPlayers] p
                JOIN [dbo].[SoccerTeams] t ON t.[TeamId] = @TeamId
                WHERE p.[PlayerId] = @PlayerId;
            END
            ELSE
            BEGIN
                UPDATE [dbo].[SoccerPlayerClaimRequests]
                SET [Status] = 'Rejected', [ReviewedAt] = @Now, [UpdatedAt] = @Now
                WHERE [RequestId] = @RequestId;

                INSERT INTO [dbo].[SoccerNotifications]
                    ([RecipientUserId], [NotificationType], [RefId], [TargetPlayerId], [PlayerName], [TeamName], [Relation])
                SELECT @RequesterUserId, 'ClaimRejected', @RequestId, @PlayerId, p.[Name], t.[TeamName], @Relation
                FROM [dbo].[SoccerPlayers] p
                JOIN [dbo].[SoccerTeams] t ON t.[TeamId] = @TeamId
                WHERE p.[PlayerId] = @PlayerId;
            END

            -- 처리 = 관리자 액션형 알림 읽음
            UPDATE [dbo].[SoccerNotifications]
            SET [IsRead] = 1, [ReadAt] = @Now
            WHERE [RecipientUserId] = @ManagerUserId AND [NotificationType] = 'ClaimRequest'
              AND [RefId] = @RequestId AND [IsRead] = 0;

            COMMIT TRANSACTION;
        END TRY
        BEGIN CATCH
            IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
            THROW;
        END CATCH
    END

    SELECT r.[RequestId], r.[Status], p.[Name]
    FROM [dbo].[SoccerPlayerClaimRequests] r WITH (NOLOCK)
    JOIN [dbo].[SoccerPlayers] p WITH (NOLOCK) ON p.[PlayerId] = r.[PlayerId]
    WHERE r.[RequestId] = @RequestId AND @PlayerId IS NOT NULL;
END
