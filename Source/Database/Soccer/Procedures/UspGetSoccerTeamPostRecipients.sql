-- @entity: SoccerTeamPostRecipientRecord
-- @source: join
-- @join: SoccerPlayers AS p (UserId, PlayerId, Name)
-- @join: SoccerTeams AS t (TeamName)
-- 공지 발행 알림 수신자 — 관리자 팀 Active 로스터의 보호자 전원. 자녀별·보호자별 1행(UNION 중복 제거).
-- ① Claimed 선수 본인 계정(SoccerPlayers.UserId) + ② 가족 연결 보호자(FamilyLinks Guardian, 2차 보호자 포함).
-- **설정 필터 없음** — 공지는 "로스터 보호자 전원"에게 간다(README). 자료는 애초에 이 경로를 안 탄다.
-- 같은 보호자가 여러 자녀를 가지면 UserId 중복 → Application이 UserId로 중복 제거(TargetPlayerId는 그중 하나).
-- 소유는 팀 ManagerUserId 조인으로 강제(친선경기 결과 수신자 UspGetSoccerMatchResultRecipients와 같은 형태).
CREATE PROCEDURE [dbo].[UspGetSoccerTeamPostRecipients]
    @ManagerUserId UNIQUEIDENTIFIER
AS
BEGIN
    SET NOCOUNT ON;

    SELECT p.[UserId], p.[PlayerId], p.[Name], t.[TeamName]
    FROM [dbo].[SoccerTeams] t WITH (NOLOCK)
    JOIN [dbo].[SoccerTeamPlayers] tp WITH (NOLOCK)
        ON tp.[TeamId] = t.[TeamId] AND tp.[Status] = 'Active' AND tp.[DeletedAt] IS NULL
    JOIN [dbo].[SoccerPlayers] p WITH (NOLOCK)
        ON p.[PlayerId] = tp.[PlayerId] AND p.[UserId] IS NOT NULL AND p.[DeletedAt] IS NULL
    WHERE t.[ManagerUserId] = @ManagerUserId AND t.[DeletedAt] IS NULL

    UNION

    SELECT fl.[UserId], p.[PlayerId], p.[Name], t.[TeamName]
    FROM [dbo].[SoccerTeams] t WITH (NOLOCK)
    JOIN [dbo].[SoccerTeamPlayers] tp WITH (NOLOCK)
        ON tp.[TeamId] = t.[TeamId] AND tp.[Status] = 'Active' AND tp.[DeletedAt] IS NULL
    JOIN [dbo].[SoccerPlayers] p WITH (NOLOCK)
        ON p.[PlayerId] = tp.[PlayerId] AND p.[DeletedAt] IS NULL
    JOIN [dbo].[SoccerPlayerFamilyLinks] fl WITH (NOLOCK)
        ON fl.[PlayerId] = p.[PlayerId] AND fl.[Role] = 'Guardian'
       AND fl.[UserId] IS NOT NULL AND fl.[DeletedAt] IS NULL
    WHERE t.[ManagerUserId] = @ManagerUserId AND t.[DeletedAt] IS NULL;
END
