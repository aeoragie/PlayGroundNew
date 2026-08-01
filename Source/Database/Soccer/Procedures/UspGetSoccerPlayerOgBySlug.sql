-- 링크 공유 미리보기(OG) — 선수 프로필 카드용 최소 조회 (DECISION.OGMETA, 7/25 확정).
-- **이름만** 반환한다 — 사진·소속·연령·기록·태그는 절대 내리지 않는다(링크 미리보기로 아이 정보 유통 차단).
-- 공개 판정은 공개 프로필과 동일: FieldName='Profile' 가시성 off(또는 미존재)면 빈 결과 → 랜딩 카드 폴백.
CREATE PROCEDURE [dbo].[UspGetSoccerPlayerOgBySlug]
    @Slug VARCHAR(150)
AS
BEGIN
    SET NOCOUNT ON;

    SELECT p.[Name]
    FROM [dbo].[SoccerPlayers] p WITH (NOLOCK)
    LEFT JOIN [dbo].[SoccerPlayerFieldVisibilities] fv WITH (NOLOCK)
        ON fv.[PlayerId] = p.[PlayerId] AND fv.[FieldName] = 'Profile'
    WHERE p.[Slug] = @Slug AND p.[DeletedAt] IS NULL
      AND COALESCE(fv.[IsPublic], 1) = 1;
END
