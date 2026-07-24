-- 코드 없이(공개 선수 프로필 경유) 연결할 때의 선수 카드 조회 — 슬러그 기준.
-- 결과셋은 코드 조회(UspGetSoccerPlayerInviteForClaim)와 같은 모양 → @entity 마커를 두지 않고
-- 리포지토리가 SoccerClaimInviteCardRecord를 재사용한다(중복 생성 방지).
-- 유효 조건: 미연결(UserId NULL)·미삭제 선수 + 소속팀 존재. 연결됐거나 없으면 빈 결과(사유 미노출).
CREATE PROCEDURE [dbo].[UspGetSoccerClaimCardBySlug]
    @Slug VARCHAR(150)
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        p.[PlayerId], p.[Name], p.[BirthDate], p.[AgeGroup],
        tp.[Position], tp.[JerseyNumber],
        t.[TeamId], t.[TeamName]
    FROM [dbo].[SoccerPlayers] p WITH (NOLOCK)
    JOIN [dbo].[SoccerTeamPlayers] tp WITH (NOLOCK)
        ON tp.[PlayerId] = p.[PlayerId] AND tp.[Status] = 'Active' AND tp.[DeletedAt] IS NULL
    JOIN [dbo].[SoccerTeams] t WITH (NOLOCK)
        ON t.[TeamId] = tp.[TeamId] AND t.[DeletedAt] IS NULL
    WHERE p.[Slug] = @Slug AND p.[UserId] IS NULL AND p.[DeletedAt] IS NULL;
END
