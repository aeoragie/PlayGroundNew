-- @entity: SoccerPlayerCareerSaveRecord
-- @source: join
-- @join: SoccerPlayerCareers AS c (CareerId, IsVerified)
-- 선수 커리어 이력 저장 (신규·수정 겸용). 관리 주체(UserId 연결 계정)만 —
-- UserId로 선수를 해석하고, 수정은 그 선수 소유 행만 건드린다. 권한 없으면 빈 결과.
-- @CareerId 빈 GUID = 신규 등록.
-- 주의(설계): 내용을 고치면 팀의 확인이 무효가 되므로 수정 시 IsVerified를 0으로 되돌린다.
--             IsCurrent는 클라이언트 값을 믿지 않고 EndDate로 파생한다(모순 상태 방지).
-- 주의(제너레이터): 파라미터 줄에 꼬리 주석을 달면 그 파라미터가 누락된다.
CREATE PROCEDURE [dbo].[UspSaveSoccerPlayerCareer]
    @UserId UNIQUEIDENTIFIER,
    @CareerId UNIQUEIDENTIFIER,
    @TeamName VARCHAR(300),
    @StartDate DATE,
    @EndDate DATE = NULL,
    @Role VARCHAR(150) = NULL,
    @Note VARCHAR(600) = NULL,
    @BadgeLabel VARCHAR(150) = NULL
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @PlayerId UNIQUEIDENTIFIER = (
        SELECT TOP 1 [PlayerId]
        FROM [dbo].[SoccerPlayers] WITH (NOLOCK)
        WHERE [UserId] = @UserId AND [DeletedAt] IS NULL
        ORDER BY [CreatedAt] DESC);

    IF @PlayerId IS NULL
    BEGIN
        SELECT c.[CareerId], c.[IsVerified]
        FROM [dbo].[SoccerPlayerCareers] c WITH (NOLOCK)
        WHERE 1 = 0;
        RETURN;
    END

    DECLARE @IsCurrent BIT = CASE WHEN @EndDate IS NULL THEN 1 ELSE 0 END;

    IF @CareerId = '00000000-0000-0000-0000-000000000000'
    BEGIN
        SET @CareerId = NEWID();

        INSERT INTO [dbo].[SoccerPlayerCareers]
            ([CareerId], [PlayerId], [TeamName], [IsCurrent], [BadgeLabel], [StartDate], [EndDate], [Role], [Note])
        VALUES
            (@CareerId, @PlayerId, @TeamName, @IsCurrent, @BadgeLabel, @StartDate, @EndDate, @Role, @Note);
    END
    ELSE
    BEGIN
        UPDATE [dbo].[SoccerPlayerCareers]
        SET [TeamName] = @TeamName,
            [IsCurrent] = @IsCurrent,
            [BadgeLabel] = @BadgeLabel,
            [StartDate] = @StartDate,
            [EndDate] = @EndDate,
            [Role] = @Role,
            [Note] = @Note,
            [IsVerified] = 0,
            [UpdatedAt] = GETUTCDATE()
        WHERE [CareerId] = @CareerId AND [PlayerId] = @PlayerId AND [DeletedAt] IS NULL;

        IF @@ROWCOUNT = 0
        BEGIN
            SELECT c.[CareerId], c.[IsVerified]
            FROM [dbo].[SoccerPlayerCareers] c WITH (NOLOCK)
            WHERE 1 = 0;
            RETURN;
        END
    END

    SELECT c.[CareerId], c.[IsVerified]
    FROM [dbo].[SoccerPlayerCareers] c WITH (NOLOCK)
    WHERE c.[CareerId] = @CareerId AND c.[DeletedAt] IS NULL;
END
