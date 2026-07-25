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
    @RosterJson VARCHAR(MAX) = NULL
AS
BEGIN
    SET NOCOUNT ON;

    -- 온보딩 중복 방지: 이 관리자에게 이미 팀이 있으면 새로 만들지 않고 그 팀을 반환한다.
    -- (모든 팀 프로시저가 관리자당 1팀을 전제하므로 2번째 팀은 고아가 된다 — 재진입·재제출 대비 멱등.)
    DECLARE @ExistingTeamId UNIQUEIDENTIFIER = (
        SELECT TOP 1 [TeamId] FROM [dbo].[SoccerTeams] WITH (NOLOCK)
        WHERE [ManagerUserId] = @ManagerUserId AND [DeletedAt] IS NULL
        ORDER BY [CreatedAt]);

    IF @ExistingTeamId IS NOT NULL
    BEGIN
        SELECT t.[TeamId], t.[Slug]
        FROM [dbo].[SoccerTeams] t WITH (NOLOCK)
        WHERE t.[TeamId] = @ExistingTeamId;
        RETURN;
    END

    DECLARE @TeamId UNIQUEIDENTIFIER = NEWID();

    BEGIN TRY
        BEGIN TRANSACTION;

        --.// 슬러그 — 팀명 로마자(ASCII) 파생(선수·마이그레이션과 같은 함수 = 단일 진실), 중복 시 -2, -3 …
        DECLARE @Base VARCHAR(100) = dbo.UfnRomanizeKoreanSlug(@TeamName);
        IF @Base = '' SET @Base = 'team';
        DECLARE @FinalSlug VARCHAR(100) = @Base;
        DECLARE @n INT = 1;
        WHILE EXISTS (SELECT 1 FROM [dbo].[SoccerTeams] WHERE [Slug] = @FinalSlug AND [DeletedAt] IS NULL)
        BEGIN
            SET @n += 1;
            SET @FinalSlug = LEFT(@Base, 90) + '-' + CAST(@n AS VARCHAR(10));
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

            --.// 선수 공개 프로필 슬러그 — 이름 로마자(ASCII) base + 세트 내 동일 base 순번 + 기존 동일 slug 수
            --    (UNIQUE 제약이 최후 방어. 서로 다른 한글 이름이 같은 로마자로 겹쳐도 -N으로 갈라진다.)
            UPDATE r
            SET r.[PlayerSlug] =
                CASE WHEN d.[Seq] = 1 THEN d.[Base]
                     ELSE LEFT(d.[Base], 140) + '-' + CAST(d.[Seq] AS VARCHAR(10)) END
            FROM @roster r
            JOIN (
                SELECT
                    r2.[PlayerId],
                    b.[Base],
                    ROW_NUMBER() OVER (PARTITION BY b.[Base] ORDER BY r2.[PlayerId])
                        + (SELECT COUNT(*) FROM [dbo].[SoccerPlayers] p
                           WHERE p.[Slug] = b.[Base]
                              OR p.[Slug] LIKE b.[Base] + '-%') AS [Seq]
                FROM @roster r2
                CROSS APPLY (SELECT dbo.UfnRomanizeKoreanSlug(r2.[Name]) AS [Raw]) rr
                CROSS APPLY (SELECT CASE WHEN rr.[Raw] = '' THEN 'player' ELSE rr.[Raw] END AS [Base]) b
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
