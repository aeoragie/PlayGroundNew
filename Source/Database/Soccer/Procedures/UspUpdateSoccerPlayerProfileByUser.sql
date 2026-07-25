-- @entity: SoccerPlayerProfileUpdatedRecord
-- @source: join
-- @join: SoccerPlayers AS p (PlayerId)
-- 선수 프로필 수치 편집 (키·몸무게·주발·학교). 관리 주체(UserId 연결 계정 = 보호자)만 —
-- UserId로 선수를 해석하므로 타인 프로필은 건드릴 수 없다. 선수 미존재 시 빈 결과(Command가 Forbidden).
-- 폼이 전체 상태를 보내므로 비운 값은 NULL로 덮어쓴다(항목 지우기 = 빈 값 저장).
CREATE PROCEDURE [dbo].[UspUpdateSoccerPlayerProfileByUser]
    @UserId UNIQUEIDENTIFIER,
    @HeightCm INT = NULL,
    @WeightKg INT = NULL,
    @PreferredFoot VARCHAR(20) = NULL,
    @SchoolName VARCHAR(300) = NULL,
    @TargetPlayerId UNIQUEIDENTIFIER = NULL
AS
BEGIN
    SET NOCOUNT ON;

    IF @PreferredFoot = '' SET @PreferredFoot = NULL;
    IF @SchoolName = '' SET @SchoolName = NULL;

    -- @TargetPlayerId 없으면 첫 자녀 — 있으면 그 자녀(단, 내가 관리하는 선수여야 한다)
    DECLARE @PlayerId UNIQUEIDENTIFIER = (
        SELECT TOP 1 [PlayerId]
        FROM [dbo].[SoccerPlayers] WITH (NOLOCK)
        WHERE [UserId] = @UserId AND [DeletedAt] IS NULL
          AND (@TargetPlayerId IS NULL OR [PlayerId] = @TargetPlayerId)
        ORDER BY [CreatedAt]);

    IF @PlayerId IS NOT NULL
    BEGIN
        UPDATE [dbo].[SoccerPlayers]
        SET [HeightCm] = @HeightCm,
            [WeightKg] = @WeightKg,
            [PreferredFoot] = @PreferredFoot,
            [SchoolName] = @SchoolName,
            [UpdatedAt] = GETUTCDATE()
        WHERE [PlayerId] = @PlayerId;
    END

    SELECT p.[PlayerId]
    FROM [dbo].[SoccerPlayers] p WITH (NOLOCK)
    WHERE p.[PlayerId] = @PlayerId;
END
