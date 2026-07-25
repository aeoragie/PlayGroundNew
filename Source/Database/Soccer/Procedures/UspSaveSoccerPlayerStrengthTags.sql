-- 선수 강점 태그 저장 (Design.StrengthTags) — 순서 보존 JSON 배열 통째 교체.
-- 관리 주체(UserId 연결 계정 = 보호자·선수 본인)만 — UserId로 선수를 해석하므로 타인 프로필은 못 바꾼다.
-- 팀 관리자 편집 경로는 없다(README: 팀 관리자 편집 불가). 개수·길이·금지 패턴 검증은 Application.
-- 빈 배열/NULL이면 태그를 비운다. 선수 미존재 시 빈 결과(Command가 Forbidden).
CREATE PROCEDURE [dbo].[UspSaveSoccerPlayerStrengthTags]
    @UserId         UNIQUEIDENTIFIER,
    @TagsJson       VARCHAR(400) = NULL,
    @TargetPlayerId UNIQUEIDENTIFIER = NULL
AS
BEGIN
    SET NOCOUNT ON;

    IF @TagsJson = '' OR @TagsJson = '[]' SET @TagsJson = NULL;

    DECLARE @PlayerId UNIQUEIDENTIFIER = (
        SELECT TOP 1 [PlayerId]
        FROM [dbo].[SoccerPlayers] WITH (NOLOCK)
        WHERE [UserId] = @UserId AND [DeletedAt] IS NULL
          AND (@TargetPlayerId IS NULL OR [PlayerId] = @TargetPlayerId)
        ORDER BY [CreatedAt]);

    IF @PlayerId IS NOT NULL
    BEGIN
        UPDATE [dbo].[SoccerPlayers]
        SET [StrengthTags] = @TagsJson, [UpdatedAt] = GETUTCDATE()
        WHERE [PlayerId] = @PlayerId;
    END

    SELECT p.[PlayerId]
    FROM [dbo].[SoccerPlayers] p WITH (NOLOCK)
    WHERE p.[PlayerId] = @PlayerId;
END
