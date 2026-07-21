-- @entity: SoccerCreatePlayerRecord
-- @source: join
-- @join: SoccerPlayers AS p (PlayerId)
-- 선수 프로필 생성. 생성 후 새 PlayerId 반환.
-- (IsGuardianManaged/TeamId/DataSource는 테이블 기본값 사용 — Phase A 미수집)
CREATE PROCEDURE [dbo].[UspCreatePlayer]
    @UserId UNIQUEIDENTIFIER,
    @Name VARCHAR(150),
    @BirthDate DATE = NULL,
    @AgeGroup VARCHAR(20) = NULL,
    @Region VARCHAR(300) = NULL
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @PlayerId UNIQUEIDENTIFIER = NEWID();

    --.// 공개 프로필 슬러그 — 이름 기반, 중복 시 -2, -3 … (팀 슬러그와 같은 정책)
    DECLARE @Base VARCHAR(150) = REPLACE(LTRIM(RTRIM(@Name)), ' ', '-');
    DECLARE @Slug VARCHAR(150) = @Base;
    DECLARE @n INT = 1;
    WHILE EXISTS (SELECT 1 FROM [dbo].[SoccerPlayers] WHERE [Slug] = @Slug)
    BEGIN
        SET @n += 1;
        SET @Slug = LEFT(@Base, 140) + '-' + CAST(@n AS VARCHAR(10));
    END

    INSERT INTO [dbo].[SoccerPlayers]
        ([PlayerId], [UserId], [Name], [Slug], [BirthDate], [AgeGroup], [Region])
    VALUES
        (@PlayerId, @UserId, @Name, @Slug, @BirthDate, @AgeGroup, @Region);

    SELECT @PlayerId AS [PlayerId];
END
