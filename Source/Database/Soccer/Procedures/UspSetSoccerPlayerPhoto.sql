-- @entity: SoccerPlayerPhotoRecord
-- @source: join
-- @join: SoccerPlayers AS p (PlayerId, PhotoUrl)
-- 선수 사진 설정·삭제. 미성년자 보호 규칙(Design.ImageUploader)에 따라 주체를 제한한다 —
-- 보호자(FamilyLinks.Role='Guardian' 또는 대리관리 프로필의 관리 계정)와 소속팀 관리자만.
-- 선수 본인 계정(IsGuardianManaged=0)은 제외한다. 권한 없으면 아무것도 바꾸지 않고 빈 결과를 돌려준다
-- (호출부가 NotFound/Forbidden으로 변환 — 존재 여부를 흘리지 않는다).
-- @PhotoUrl NULL = 삭제(이니셜 아바타로 복귀).
CREATE PROCEDURE [dbo].[UspSetSoccerPlayerPhoto]
    @UserId UNIQUEIDENTIFIER,
    @PlayerId UNIQUEIDENTIFIER,
    @PhotoUrl VARCHAR(2048) = NULL
AS
BEGIN
    SET NOCOUNT ON;

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

    -- 보호자 2: 가족 연결 행이 없는 대리관리 프로필 — 관리 계정이 곧 보호자다
    IF @Allowed = 0 AND EXISTS (
        SELECT 1
        FROM [dbo].[SoccerPlayers] WITH (NOLOCK)
        WHERE [PlayerId] = @PlayerId AND [UserId] = @UserId
          AND [IsGuardianManaged] = 1 AND [DeletedAt] IS NULL)
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
    SET [PhotoUrl] = @PhotoUrl, [UpdatedAt] = GETUTCDATE()
    WHERE [PlayerId] = @PlayerId AND [DeletedAt] IS NULL;

    SELECT p.[PlayerId], p.[PhotoUrl]
    FROM [dbo].[SoccerPlayers] p WITH (NOLOCK)
    WHERE p.[PlayerId] = @PlayerId AND p.[DeletedAt] IS NULL;
END
