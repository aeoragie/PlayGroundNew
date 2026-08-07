-- @entity: SoccerPlayerVisibilitySetRecord
-- @source: join
-- @join: SoccerPlayers AS p (PlayerId)
-- 선수 프로필 항목 공개 설정 업서트. 관리 주체(UserId 연결 계정 = 보호자)만 —
-- UserId로 선수를 해석하므로 타인 프로필은 건드릴 수 없다. 선수 미존재 시 빈 결과.
CREATE PROCEDURE [dbo].[UspSetSoccerPlayerFieldVisibility]
    @UserId UNIQUEIDENTIFIER,
    @FieldName VARCHAR(30),
    @IsPublic BIT,
    @TargetPlayerId UNIQUEIDENTIFIER = NULL
AS
BEGIN
    SET NOCOUNT ON;

    -- 시각은 dbo.UfnSystemDate()로만 얻는다. 변수로 한 번 받는 이유는 두 가지다 —
    -- 스칼라 UDF는 인라인되지 않아 WHERE에 직접 쓰면 행마다 호출되고,
    -- 한 프로시저 안의 "지금"이 호출마다 달라지는 것도 막는다.
    DECLARE @Now DATETIME2(7) = dbo.UfnSystemDate();

    -- @TargetPlayerId 없으면 첫 자녀 — 있으면 그 자녀(단, 내가 관리하는 선수여야 한다)
    DECLARE @PlayerId UNIQUEIDENTIFIER = (
        SELECT TOP 1 [PlayerId]
        FROM [dbo].[SoccerPlayers] WITH (NOLOCK)
        WHERE [UserId] = @UserId AND [DeletedAt] IS NULL
          AND (@TargetPlayerId IS NULL OR [PlayerId] = @TargetPlayerId)
        ORDER BY [CreatedAt]);

    IF @PlayerId IS NOT NULL
    BEGIN
        UPDATE [dbo].[SoccerPlayerFieldVisibilities]
        SET [IsPublic] = @IsPublic, [UpdatedAt] = @Now
        WHERE [PlayerId] = @PlayerId AND [FieldName] = @FieldName;

        IF @@ROWCOUNT = 0
        BEGIN
            INSERT INTO [dbo].[SoccerPlayerFieldVisibilities] ([PlayerId], [FieldName], [IsPublic])
            VALUES (@PlayerId, @FieldName, @IsPublic);
        END
    END

    SELECT p.[PlayerId]
    FROM [dbo].[SoccerPlayers] p WITH (NOLOCK)
    WHERE p.[PlayerId] = @PlayerId;
END
