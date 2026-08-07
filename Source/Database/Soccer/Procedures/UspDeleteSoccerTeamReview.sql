-- 리뷰 소프트 삭제·복구 (@Restore = 1이면 실행취소, B3 규약). **작성자 본인만** —
-- 팀 관리자 삭제 경로는 만들지 않는다 ("팀은 삭제할 수 없고 답글만" 캡션 규칙).
CREATE PROCEDURE [dbo].[UspDeleteSoccerTeamReview]
    @AuthorUserId UNIQUEIDENTIFIER,
    @ReviewId UNIQUEIDENTIFIER,
    @Restore BIT = 0
AS
BEGIN
    SET NOCOUNT ON;

    -- 시각은 dbo.UfnSystemDate()로만 얻는다. 변수로 한 번 받는 이유는 두 가지다 —
    -- 스칼라 UDF는 인라인되지 않아 WHERE에 직접 쓰면 행마다 호출되고,
    -- 한 프로시저 안의 "지금"이 호출마다 달라지는 것도 막는다.
    DECLARE @Now DATETIME2(7) = dbo.UfnSystemDate();

    UPDATE [dbo].[SoccerTeamReviews]
    SET [DeletedAt] = CASE WHEN @Restore = 1 THEN NULL ELSE @Now END,
        [UpdatedAt] = @Now
    WHERE [ReviewId] = @ReviewId AND [AuthorUserId] = @AuthorUserId
      AND ((@Restore = 0 AND [DeletedAt] IS NULL) OR (@Restore = 1 AND [DeletedAt] IS NOT NULL));

    DECLARE @Applied INT = @@ROWCOUNT;

    SELECT
        r.[ReviewId], r.[TeamId], r.[AuthorUserId], r.[PlayerId], r.[Rating], r.[Body],
        r.[CreatedAt], r.[UpdatedAt], r.[DeletedAt]
    FROM [dbo].[SoccerTeamReviews] r WITH (NOLOCK)
    WHERE r.[ReviewId] = @ReviewId AND @Applied = 1;
END
