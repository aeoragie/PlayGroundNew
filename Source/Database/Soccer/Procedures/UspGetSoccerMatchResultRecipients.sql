-- @entity: SoccerMatchResultRecipientRecord
-- @source: join
-- @join: SoccerPlayers AS p (UserId, PlayerId, Name)
-- @join: SoccerTeams AS t (TeamName)
-- 친선경기 결과 알림 수신자 — 관리자 팀의 Claimed(계정 연결) 선수들. 자녀별 1행.
-- 수신 설정(MatchResult) 필터는 Application이 Account DB에서 확인한다 (DB 간 조인 불가).
CREATE PROCEDURE [dbo].[UspGetSoccerMatchResultRecipients]
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
    WHERE t.[ManagerUserId] = @ManagerUserId AND t.[DeletedAt] IS NULL;
END
