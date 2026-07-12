-- @entity: SoccerCreatePlayerRecord
-- @source: join
-- @join: SoccerPlayers AS p (PlayerId)
-- 선수 프로필 생성. 생성 후 새 PlayerId 반환.
-- (IsGuardianManaged/TeamId/DataSource는 테이블 기본값 사용 — Phase A 미수집)
CREATE PROCEDURE [dbo].[UspCreatePlayer]
    @UserId UNIQUEIDENTIFIER,
    @Name NVARCHAR(50),
    @BirthDate DATE = NULL,
    @AgeGroup VARCHAR(20) = NULL,
    @Region NVARCHAR(100) = NULL
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @PlayerId UNIQUEIDENTIFIER = NEWID();

    INSERT INTO [dbo].[SoccerPlayers]
        ([PlayerId], [UserId], [Name], [BirthDate], [AgeGroup], [Region])
    VALUES
        (@PlayerId, @UserId, @Name, @BirthDate, @AgeGroup, @Region);

    SELECT @PlayerId AS [PlayerId];
END
