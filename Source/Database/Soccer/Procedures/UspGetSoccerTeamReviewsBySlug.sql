-- @entity: SoccerTeamReviewRecord
-- @source: join
-- @join: SoccerTeamReviews AS r (ReviewId, AuthorUserId, Rating, Body)
-- @join: SoccerPlayers AS p (AgeGroup)
-- @join: SoccerPlayerFamilyLinks AS fl (MemberName)
-- @join: SoccerTeamPlayers AS tp (CreatedAt)
-- 공개 팀 홈 리뷰 탭 (Slug 기준). 비공개·미존재 팀은 빈 결과.
-- 결과셋 2개: ①리뷰 목록(작성자 표시명·자녀 연령·재원 시작일 — 마스킹·연차 계산은 Persistence)
--            ②뷰어 상태(CanWrite: 재원 자녀 보유 + 미작성 / MyReviewId: 이미 쓴 리뷰 — 수정 진입용).
-- 평균 별점·개수는 클라이언트가 목록에서 계산한다 (수가 어긋날 수 없다).
CREATE PROCEDURE [dbo].[UspGetSoccerTeamReviewsBySlug]
    @Slug VARCHAR(100),
    @ViewerUserId UNIQUEIDENTIFIER = NULL
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @TeamId UNIQUEIDENTIFIER = (
        SELECT TOP 1 [TeamId]
        FROM [dbo].[SoccerTeams] WITH (NOLOCK)
        WHERE [Slug] = @Slug AND [IsPublicProfile] = 1 AND [DeletedAt] IS NULL
        ORDER BY [CreatedAt] DESC);

    -- CreatedAt = 재원 시작일(SoccerTeamPlayers) — 리뷰 작성일은 dc 카드에 표시가 없어 내리지 않는다
    SELECT
        r.[ReviewId], r.[AuthorUserId], r.[Rating], r.[Body],
        p.[AgeGroup],
        fl.[MemberName],
        tp.[CreatedAt]
    FROM [dbo].[SoccerTeamReviews] r WITH (NOLOCK)
    LEFT JOIN [dbo].[SoccerPlayers] p WITH (NOLOCK)
        ON p.[PlayerId] = r.[PlayerId]
    OUTER APPLY (
        SELECT TOP 1 f.[MemberName]
        FROM [dbo].[SoccerPlayerFamilyLinks] f WITH (NOLOCK)
        WHERE f.[PlayerId] = r.[PlayerId] AND f.[UserId] = r.[AuthorUserId] AND f.[DeletedAt] IS NULL
        ORDER BY f.[DisplayOrder], f.[CreatedAt]) fl
    OUTER APPLY (
        SELECT TOP 1 t.[CreatedAt]
        FROM [dbo].[SoccerTeamPlayers] t WITH (NOLOCK)
        WHERE t.[PlayerId] = r.[PlayerId] AND t.[TeamId] = r.[TeamId] AND t.[DeletedAt] IS NULL
        ORDER BY t.[CreatedAt]) tp
    WHERE r.[TeamId] = @TeamId AND r.[DeletedAt] IS NULL
    ORDER BY r.[CreatedAt] DESC;

    -- 뷰어 상태 — 재원 자녀(보호자 연결 + 이 팀 Active 소속) 보유 여부 + 이미 쓴 리뷰
    SELECT
        CAST(CASE WHEN @ViewerUserId IS NOT NULL AND EXISTS (
            SELECT 1
            FROM [dbo].[SoccerPlayerFamilyLinks] f WITH (NOLOCK)
            JOIN [dbo].[SoccerTeamPlayers] t WITH (NOLOCK)
                ON t.[PlayerId] = f.[PlayerId] AND t.[TeamId] = @TeamId
                AND t.[Status] = 'Active' AND t.[DeletedAt] IS NULL
            WHERE f.[UserId] = @ViewerUserId AND f.[Role] = 'Guardian' AND f.[DeletedAt] IS NULL)
        THEN 1 ELSE 0 END AS BIT) AS [IsResidentGuardian],
        (SELECT TOP 1 r.[ReviewId]
         FROM [dbo].[SoccerTeamReviews] r WITH (NOLOCK)
         WHERE r.[TeamId] = @TeamId AND r.[AuthorUserId] = @ViewerUserId AND r.[DeletedAt] IS NULL) AS [MyReviewId];
END
