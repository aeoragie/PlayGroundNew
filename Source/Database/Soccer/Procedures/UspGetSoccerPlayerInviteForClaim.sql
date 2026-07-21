-- @entity: SoccerClaimInviteCardRecord
-- @source: join
-- @join: SoccerPlayers AS p (PlayerId, Name, BirthDate, AgeGroup)
-- @join: SoccerTeamPlayers AS tp (Position, JerseyNumber)
-- @join: SoccerTeams AS t (TeamId, TeamName)
-- Claim 플로우 스텝 ① → ②: 코드로 선수 카드를 조회만 한다 (코드 소진 없음 — 소진은 승인 시점).
-- 유효 조건: Pending·미만료 코드 + 대상 선수 미연결·미삭제. 실패는 전부 빈 결과 (사유 미구분 — 코드 추측 대비).
CREATE PROCEDURE [dbo].[UspGetSoccerPlayerInviteForClaim]
    @Code VARCHAR(12)
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        p.[PlayerId], p.[Name], p.[BirthDate], p.[AgeGroup],
        tp.[Position], tp.[JerseyNumber],
        t.[TeamId], t.[TeamName]
    FROM [dbo].[SoccerPlayerInvites] i WITH (NOLOCK)
    JOIN [dbo].[SoccerPlayers] p WITH (NOLOCK)
        ON p.[PlayerId] = i.[PlayerId] AND p.[UserId] IS NULL AND p.[DeletedAt] IS NULL
    JOIN [dbo].[SoccerTeams] t WITH (NOLOCK)
        ON t.[TeamId] = i.[TeamId] AND t.[DeletedAt] IS NULL
    LEFT JOIN [dbo].[SoccerTeamPlayers] tp WITH (NOLOCK)
        ON tp.[TeamId] = i.[TeamId] AND tp.[PlayerId] = i.[PlayerId]
       AND tp.[Status] = 'Active' AND tp.[DeletedAt] IS NULL
    WHERE i.[Code] = UPPER(@Code) AND i.[Status] = 'Pending'
      AND (i.[ExpiresAt] IS NULL OR i.[ExpiresAt] > GETUTCDATE());
END
