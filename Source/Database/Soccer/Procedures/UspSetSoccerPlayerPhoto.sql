-- @entity: SoccerPlayerPhotoRecord
-- @source: join
-- @join: SoccerPlayers AS p (PlayerId, PhotoUrl)
-- 선수 사진 설정·삭제. 주체 = 관리 주체(선수 본인 계정 포함) + 소속팀 관리자 —
-- 팀 소속 여부와 무관하다 (2026-08-09 규칙 변경: 팀 이탈 선수도 스카우터 어필용 사진이
-- 필요하다. 소유 검증이 곧 통제라 미성년자 보호는 유지된다).
-- 권한 없으면 아무것도 바꾸지 않고 빈 결과를 돌려준다
-- (호출부가 NotFound/Forbidden으로 변환 — 존재 여부를 흘리지 않는다).
-- @PhotoUrl NULL = 삭제(이니셜 아바타로 복귀).
CREATE PROCEDURE [dbo].[UspSetSoccerPlayerPhoto]
    @UserId UNIQUEIDENTIFIER,
    @PlayerId UNIQUEIDENTIFIER,
    @PhotoUrl VARCHAR(2048) = NULL
AS
BEGIN
    SET NOCOUNT ON;

    -- 시각은 dbo.UfnSystemDate()로만 얻는다. 변수로 한 번 받는 이유는 두 가지다 —
    -- 스칼라 UDF는 인라인되지 않아 WHERE에 직접 쓰면 행마다 호출되고,
    -- 한 프로시저 안의 "지금"이 호출마다 달라지는 것도 막는다.
    DECLARE @Now DATETIME2(7) = dbo.UfnSystemDate();

    DECLARE @Allowed BIT = 0;

    -- 보호자 1: 가족 계정 연결에 Guardian 행이 있다
    IF EXISTS (
        SELECT 1
        FROM [dbo].[SoccerPlayerFamilyLinks] WITH (NOLOCK)
        WHERE [PlayerId] = @PlayerId AND [UserId] = @UserId
          AND [Role] = 'Guardian' AND [DeletedAt] IS NULL)
    BEGIN
        SET @Allowed = 1;
    END

    -- 관리 계정: 프로필의 소유 계정 — 대리관리(보호자)든 선수 본인이든 관리 주체가 곧 권한이다
    IF @Allowed = 0 AND EXISTS (
        SELECT 1
        FROM [dbo].[SoccerPlayers] WITH (NOLOCK)
        WHERE [PlayerId] = @PlayerId AND [UserId] = @UserId
          AND [DeletedAt] IS NULL)
    BEGIN
        SET @Allowed = 1;
    END

    -- 팀 관리자: 그 선수가 현재 소속된 팀의 관리 계정
    IF @Allowed = 0 AND EXISTS (
        SELECT 1
        FROM [dbo].[SoccerTeamPlayers] tp WITH (NOLOCK)
        INNER JOIN [dbo].[SoccerTeams] t WITH (NOLOCK) ON t.[TeamId] = tp.[TeamId]
        WHERE tp.[PlayerId] = @PlayerId AND tp.[Status] = 'Active' AND tp.[DeletedAt] IS NULL
          AND t.[ManagerUserId] = @UserId AND t.[DeletedAt] IS NULL)
    BEGIN
        SET @Allowed = 1;
    END

    IF @Allowed = 0
    BEGIN
        SELECT p.[PlayerId], p.[PhotoUrl]
        FROM [dbo].[SoccerPlayers] p WITH (NOLOCK)
        WHERE 1 = 0;
        RETURN;
    END

    UPDATE [dbo].[SoccerPlayers]
    SET [PhotoUrl] = @PhotoUrl, [UpdatedAt] = @Now
    WHERE [PlayerId] = @PlayerId AND [DeletedAt] IS NULL;

    SELECT p.[PlayerId], p.[PhotoUrl]
    FROM [dbo].[SoccerPlayers] p WITH (NOLOCK)
    WHERE p.[PlayerId] = @PlayerId AND p.[DeletedAt] IS NULL;
END
