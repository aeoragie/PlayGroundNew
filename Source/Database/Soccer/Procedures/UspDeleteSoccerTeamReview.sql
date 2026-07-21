-- 리뷰 소프트 삭제·복구 (@Restore = 1이면 실행취소, B3 규약). **작성자 본인만** —
-- 팀 관리자 삭제 경로는 만들지 않는다 ("팀은 삭제할 수 없고 답글만" 캡션 규칙).
CREATE PROCEDURE [dbo].[UspDeleteSoccerTeamReview]
    @AuthorUserId UNIQUEIDENTIFIER,
    @ReviewId UNIQUEIDENTIFIER,
    @Restore BIT = 0
AS
BEGIN
    SET NOCOUNT ON;

    UPDATE [dbo].[SoccerTeamReviews]
    SET [DeletedAt] = CASE WHEN @Restore = 1 THEN NULL ELSE GETUTCDATE() END,
        [UpdatedAt] = GETUTCDATE()
    WHERE [ReviewId] = @ReviewId AND [AuthorUserId] = @AuthorUserId
      AND ((@Restore = 0 AND [DeletedAt] IS NULL) OR (@Restore = 1 AND [DeletedAt] IS NOT NULL));

    DECLARE @Applied INT = @@ROWCOUNT;

    SELECT
        r.[ReviewId], r.[TeamId], r.[AuthorUserId], r.[PlayerId], r.[Rating], r.[Body],
        r.[CreatedAt], r.[UpdatedAt], r.[DeletedAt]
    FROM [dbo].[SoccerTeamReviews] r WITH (NOLOCK)
    WHERE r.[ReviewId] = @ReviewId AND @Applied = 1;
END
