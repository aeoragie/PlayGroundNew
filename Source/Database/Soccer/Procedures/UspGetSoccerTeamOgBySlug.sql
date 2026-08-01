-- 링크 공유 미리보기(OG) — 팀 카드용 최소 조회 (DECISION.OGMETA). 공개(IsPublicProfile=1) 팀만.
-- 크롤러 경로라 가볍게: RS1 팀 기본 필드(부분 매핑) + RS2 활성 로스터 수(스칼라).
-- 비공개·미존재 팀은 RS1 빈 결과 → 랜딩 카드 폴백.
CREATE PROCEDURE [dbo].[UspGetSoccerTeamOgBySlug]
    @Slug VARCHAR(100)
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @TeamId UNIQUEIDENTIFIER = (
        SELECT TOP 1 [TeamId] FROM [dbo].[SoccerTeams] WITH (NOLOCK)
        WHERE [Slug] = @Slug AND [IsPublicProfile] = 1 AND [DeletedAt] IS NULL
        ORDER BY [CreatedAt] DESC);

    SELECT t.[TeamName], t.[Region], t.[AgeGroup], t.[LogoUrl]
    FROM [dbo].[SoccerTeams] t WITH (NOLOCK)
    WHERE t.[TeamId] = @TeamId;

    SELECT COUNT(*)
    FROM [dbo].[SoccerTeamPlayers] tp WITH (NOLOCK)
    WHERE tp.[TeamId] = @TeamId AND tp.[Status] = 'Active' AND tp.[DeletedAt] IS NULL;
END
