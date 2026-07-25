-- 강점 태그 프리셋 전체 조회 (Design.StrengthTags) — 포지션별 추천 칩. 클라이언트가 포지션으로 거른다.
-- 32행뿐이라 전체를 한 번에 내린다(포지션 전환 시 재조회 불필요).
CREATE PROCEDURE [dbo].[UspGetSoccerStrengthTagPresets]
AS
BEGIN
    SET NOCOUNT ON;

    SELECT p.[PresetId], p.[Position], p.[Tag], p.[SortOrder]
    FROM [dbo].[SoccerStrengthTagPresets] p WITH (NOLOCK)
    ORDER BY p.[Position], p.[SortOrder];
END
