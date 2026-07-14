-- @entity: SoccerPlayerVisibilitySetRecord
-- @source: join
-- @join: SoccerPlayers AS p (PlayerId)
-- 선수 프로필 항목 공개 설정 업서트. 관리 주체(UserId 연결 계정 = 보호자)만 —
-- UserId로 선수를 해석하므로 타인 프로필은 건드릴 수 없다. 선수 미존재 시 빈 결과.
CREATE PROCEDURE [dbo].[UspSetSoccerPlayerFieldVisibility]
    @UserId UNIQUEIDENTIFIER,
    @FieldName VARCHAR(30),
    @IsPublic BIT
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @PlayerId UNIQUEIDENTIFIER = (
        SELECT TOP 1 [PlayerId]
        FROM [dbo].[SoccerPlayers] WITH (NOLOCK)
        WHERE [UserId] = @UserId AND [DeletedAt] IS NULL
        ORDER BY [CreatedAt] DESC);

    IF @PlayerId IS NOT NULL
    BEGIN
        UPDATE [dbo].[SoccerPlayerFieldVisibilities]
        SET [IsPublic] = @IsPublic, [UpdatedAt] = GETUTCDATE()
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
