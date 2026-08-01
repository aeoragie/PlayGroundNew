-- 링크 공유 미리보기(OG) — 대회 카드용 최소 조회 (DECISION.OGMETA). Records 상세는 공개라 별도 게이팅 없음.
-- 대회명 + 연령 + 기간(StartDate/EndDate) + 참가 팀 수. 미존재는 빈 결과 → 랜딩 카드 폴백.
CREATE PROCEDURE [dbo].[UspGetSoccerTournamentOgById]
    @TournamentId UNIQUEIDENTIFIER
AS
BEGIN
    SET NOCOUNT ON;

    SELECT t.[Name], t.[AgeGroup], t.[StartDate], t.[EndDate], t.[TeamCount]
    FROM [dbo].[SoccerTournaments] t WITH (NOLOCK)
    WHERE t.[TournamentId] = @TournamentId AND t.[DeletedAt] IS NULL;
END
