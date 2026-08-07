-- @entity: SoccerPlayerProfileUpdatedRecord
-- @source: join
-- @join: SoccerPlayers AS p (PlayerId, Slug)
-- 선수 프로필 수치·주소 편집 (키·몸무게·주발·학교 + 공개 프로필 슬러그). 관리 주체(UserId 연결 계정
-- = 보호자)만 — UserId로 선수를 해석하므로 타인 프로필은 건드릴 수 없다. 선수 미존재 시 빈 결과.
-- 폼이 전체 상태를 보내므로 비운 수치는 NULL로 덮어쓴다(항목 지우기).
-- @Slug: NULL = 주소 변경 안 함. 값이 있고 현재와 다르면 유일성 검사 후 반영 —
--        다른 선수가 이미 쓰면 슬러그는 바꾸지 않는다(수치는 이미 반영).
-- 반환: PlayerId + 반영 후 Slug. Command가 요청 Slug와 반환 Slug를 비교해 SlugTaken을 판정한다
--       (계산 컬럼 대신 실제 값으로 신호 — 제너레이터가 실컬럼만 매핑). 빈 결과 = 소유 아님(Forbidden).
CREATE PROCEDURE [dbo].[UspUpdateSoccerPlayerProfileByUser]
    @UserId UNIQUEIDENTIFIER,
    @HeightCm INT = NULL,
    @WeightKg INT = NULL,
    @PreferredFoot VARCHAR(20) = NULL,
    @SchoolName VARCHAR(300) = NULL,
    @Slug VARCHAR(150) = NULL,
    @TargetPlayerId UNIQUEIDENTIFIER = NULL
AS
BEGIN
    SET NOCOUNT ON;

    -- 시각은 dbo.UfnSystemDate()로만 얻는다. 변수로 한 번 받는 이유는 두 가지다 —
    -- 스칼라 UDF는 인라인되지 않아 WHERE에 직접 쓰면 행마다 호출되고,
    -- 한 프로시저 안의 "지금"이 호출마다 달라지는 것도 막는다.
    DECLARE @Now DATETIME2(7) = dbo.UfnSystemDate();

    IF @PreferredFoot = '' SET @PreferredFoot = NULL;
    IF @SchoolName = '' SET @SchoolName = NULL;
    IF @Slug = '' SET @Slug = NULL;

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
            [UpdatedAt] = @Now
        WHERE [PlayerId] = @PlayerId;

        -- 슬러그(선택) — 현재와 다르고 남이 안 쓸 때만 반영. 반영 실패는 반환 Slug로 드러난다.
        IF @Slug IS NOT NULL
           AND @Slug <> (SELECT [Slug] FROM [dbo].[SoccerPlayers] WHERE [PlayerId] = @PlayerId)
           AND NOT EXISTS (SELECT 1 FROM [dbo].[SoccerPlayers] WHERE [Slug] = @Slug AND [PlayerId] <> @PlayerId)
        BEGIN
            UPDATE [dbo].[SoccerPlayers]
            SET [Slug] = @Slug, [UpdatedAt] = @Now
            WHERE [PlayerId] = @PlayerId;
        END
    END

    SELECT p.[PlayerId], p.[Slug]
    FROM [dbo].[SoccerPlayers] p WITH (NOLOCK)
    WHERE p.[PlayerId] = @PlayerId;
END
