-- @entity: SoccerCreateTeamRecord
-- @source: join
-- @join: SoccerTeams AS t (TeamId, Slug)
-- 팀 + 로스터(선수 Unclaimed + 소속 + 초대코드)를 한 트랜잭션으로 생성.
-- 로스터는 JSON 배열: [{"Name":"김민준","Position":"FW","Number":"9"}, ...]
-- 슬러그 중복 시 -N 부여. 반환: 생성된 TeamId, 최종 Slug.
CREATE PROCEDURE [dbo].[UspCreateSoccerTeamWithRoster]
    @ManagerUserId UNIQUEIDENTIFIER,
    @TeamName VARCHAR(300),
    @TeamType VARCHAR(60) = NULL,
    @Region VARCHAR(300) = NULL,
    @Slug VARCHAR(100),
    @RosterJson VARCHAR(MAX) = NULL
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @TeamId UNIQUEIDENTIFIER = NEWID();

    BEGIN TRY
        BEGIN TRANSACTION;

        --.// 슬러그 유일성 확보 (중복 시 -2, -3 …)
        DECLARE @FinalSlug VARCHAR(100) = @Slug;
        DECLARE @n INT = 1;
        WHILE EXISTS (SELECT 1 FROM [dbo].[SoccerTeams] WHERE [Slug] = @FinalSlug AND [DeletedAt] IS NULL)
        BEGIN
            SET @n += 1;
            SET @FinalSlug = LEFT(@Slug, 90) + '-' + CAST(@n AS VARCHAR(10));
        END

        INSERT INTO [dbo].[SoccerTeams]
            ([TeamId], [TeamName], [TeamType], [Region], [Slug], [ManagerUserId])
        VALUES
            (@TeamId, @TeamName, @TeamType, @Region, @FinalSlug, @ManagerUserId);

        --.// 로스터: 유효한(이름 있는) 행마다 PlayerId·Code를 미리 생성해 3개 테이블에 삽입
        DECLARE @roster TABLE (
            Name VARCHAR(150), Position VARCHAR(60), Number VARCHAR(10),
            PlayerId UNIQUEIDENTIFIER, Code VARCHAR(12), PlayerSlug VARCHAR(150));

        IF @RosterJson IS NOT NULL
        BEGIN
            INSERT INTO @roster (Name, Position, Number, PlayerId, Code)
            SELECT
                LTRIM(RTRIM(j.[Name])),
                j.[Position],
                j.[Number],
                NEWID(),
                UPPER(LEFT(REPLACE(CONVERT(VARCHAR(36), NEWID()), '-', ''), 6))
            FROM OPENJSON(@RosterJson)
                WITH ([Name] VARCHAR(150) '$.Name',
                      [Position] VARCHAR(60) '$.Position',
                      [Number] VARCHAR(10) '$.Number') j
            WHERE j.[Name] IS NOT NULL AND LEN(LTRIM(RTRIM(j.[Name]))) > 0;

            --.// 선수 공개 프로필 슬러그 — 세트 내 동명 순번 + 기존 동일 slug 수 (UNIQUE 제약이 최후 방어)
            UPDATE r
            SET r.[PlayerSlug] =
                CASE WHEN d.[Seq] = 1 THEN d.[Base]
                     ELSE LEFT(d.[Base], 140) + '-' + CAST(d.[Seq] AS VARCHAR(10)) END
            FROM @roster r
            JOIN (
                SELECT
                    r2.[PlayerId],
                    REPLACE(r2.[Name], ' ', '-') AS [Base],
                    ROW_NUMBER() OVER (PARTITION BY r2.[Name] ORDER BY r2.[PlayerId])
                        + (SELECT COUNT(*) FROM [dbo].[SoccerPlayers] p
                           WHERE p.[Slug] = REPLACE(r2.[Name], ' ', '-')
                              OR p.[Slug] LIKE REPLACE(r2.[Name], ' ', '-') + '-%') AS [Seq]
                FROM @roster r2
            ) d ON d.[PlayerId] = r.[PlayerId];

            INSERT INTO [dbo].[SoccerPlayers] ([PlayerId], [Name], [Slug])
            SELECT [PlayerId], [Name], [PlayerSlug] FROM @roster;

            INSERT INTO [dbo].[SoccerTeamPlayers] ([TeamId], [PlayerId], [JerseyNumber], [Position])
            SELECT @TeamId, [PlayerId], [Number], [Position] FROM @roster;

            INSERT INTO [dbo].[SoccerPlayerInvites] ([Code], [PlayerId], [TeamId])
            SELECT [Code], [PlayerId], @TeamId FROM @roster;
        END

        COMMIT TRANSACTION;
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
        THROW;
    END CATCH

    SELECT t.[TeamId], t.[Slug]
    FROM [dbo].[SoccerTeams] t WITH (NOLOCK)
    WHERE t.[TeamId] = @TeamId;
END
