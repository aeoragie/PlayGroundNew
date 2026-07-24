-- 로스터에 선수 1명 추가 — 대시보드 선수단 "＋ 선수 추가" (SPEC.TEAMDASHBOARD §2).
-- 팀 생성 로스터(UspCreateSoccerTeamWithRoster)와 같은 방식: Unclaimed 선수 + 소속 + Pending 6자 초대코드를
-- 한 트랜잭션으로 만든다. 반환 컬럼은 로스터 조회(SoccerTeamRosterRecord)와 동일 — 새 행을 그대로 화면에 꽂는다.
-- 소유 판정은 팀 ManagerUserId — 팀이 없으면(거부) 빈 결과(존재 여부 미노출 → Command가 Forbidden).
CREATE PROCEDURE [dbo].[UspAddSoccerTeamPlayer]
    @ManagerUserId UNIQUEIDENTIFIER,
    @Name VARCHAR(150),
    @JerseyNumber VARCHAR(10) = NULL,
    @Position VARCHAR(60) = NULL,
    @Grade VARCHAR(60) = NULL,
    @AgeGroup VARCHAR(20) = NULL
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @TeamId UNIQUEIDENTIFIER = (
        SELECT TOP 1 [TeamId] FROM [dbo].[SoccerTeams] WITH (NOLOCK)
        WHERE [ManagerUserId] = @ManagerUserId AND [DeletedAt] IS NULL
        ORDER BY [CreatedAt] DESC);

    IF @TeamId IS NULL
    BEGIN
        -- 관리하는 팀이 없다 = 거부. 로스터와 같은 모양의 빈 결과를 돌려준다(로직 분기 없이 Forbidden).
        SELECT
            tp.[TeamPlayerId], tp.[JerseyNumber], tp.[Position], tp.[Grade],
            p.[PlayerId], p.[Name], p.[Slug], p.[PhotoUrl], p.[AgeGroup], p.[UserId],
            CAST(NULL AS VARCHAR(12)) AS [Code]
        FROM [dbo].[SoccerTeamPlayers] tp WITH (NOLOCK)
        JOIN [dbo].[SoccerPlayers] p WITH (NOLOCK) ON p.[PlayerId] = tp.[PlayerId]
        WHERE 1 = 0;
        RETURN;
    END

    DECLARE @PlayerId UNIQUEIDENTIFIER = NEWID();
    DECLARE @Code VARCHAR(12) = UPPER(LEFT(REPLACE(CONVERT(VARCHAR(36), NEWID()), '-', ''), 6));

    --.// 공개 프로필 슬러그 — 이름 기반, 중복 시 -N (UNIQUE 제약이 최후 방어)
    DECLARE @Base VARCHAR(150) = REPLACE(LTRIM(RTRIM(@Name)), ' ', '-');
    DECLARE @Slug VARCHAR(150) = @Base;
    DECLARE @n INT = 1;
    WHILE EXISTS (SELECT 1 FROM [dbo].[SoccerPlayers] WITH (NOLOCK) WHERE [Slug] = @Slug)
    BEGIN
        SET @n += 1;
        SET @Slug = LEFT(@Base, 140) + '-' + CAST(@n AS VARCHAR(10));
    END

    BEGIN TRY
        BEGIN TRANSACTION;

        INSERT INTO [dbo].[SoccerPlayers] ([PlayerId], [Name], [Slug], [AgeGroup], [TeamId])
        VALUES (@PlayerId, LTRIM(RTRIM(@Name)), @Slug, @AgeGroup, @TeamId);

        INSERT INTO [dbo].[SoccerTeamPlayers] ([TeamId], [PlayerId], [JerseyNumber], [Position], [Grade])
        VALUES (@TeamId, @PlayerId, @JerseyNumber, @Position, @Grade);

        INSERT INTO [dbo].[SoccerPlayerInvites] ([Code], [PlayerId], [TeamId])
        VALUES (@Code, @PlayerId, @TeamId);

        COMMIT TRANSACTION;
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
        THROW;
    END CATCH

    --.// 새 행을 로스터 조회와 같은 모양으로 돌려준다
    SELECT
        tp.[TeamPlayerId], tp.[JerseyNumber], tp.[Position], tp.[Grade],
        p.[PlayerId], p.[Name], p.[Slug], p.[PhotoUrl], p.[AgeGroup], p.[UserId],
        i.[Code]
    FROM [dbo].[SoccerTeamPlayers] tp WITH (NOLOCK)
    JOIN [dbo].[SoccerPlayers] p WITH (NOLOCK) ON p.[PlayerId] = tp.[PlayerId]
    LEFT JOIN [dbo].[SoccerPlayerInvites] i WITH (NOLOCK)
        ON i.[PlayerId] = tp.[PlayerId] AND i.[TeamId] = tp.[TeamId] AND i.[Status] = 'Pending'
    WHERE tp.[PlayerId] = @PlayerId;
END
