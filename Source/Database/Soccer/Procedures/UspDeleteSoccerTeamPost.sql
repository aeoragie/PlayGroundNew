-- 게시판 글 소프트 삭제·복구 (@Restore = 1 이면 실행취소, B3 규약). 소프트 삭제 30일 보관.
-- 공개 글 삭제는 공개홈에서도 즉시 사라진다(공개 조회 SP가 DeletedAt IS NULL을 요구).
-- 첨부는 물리 삭제하지 않는다(복구 대비) — 조회가 글의 DeletedAt로 함께 가려진다.
-- 소유 검증 실패·대상 없음은 빈 결과. 복구는 삭제 상태 행만 되살린다.
CREATE PROCEDURE [dbo].[UspDeleteSoccerTeamPost]
    @ManagerUserId UNIQUEIDENTIFIER,
    @PostId        UNIQUEIDENTIFIER,
    @Restore       BIT = 0
AS
BEGIN
    SET NOCOUNT ON;

    -- 시각은 dbo.UfnSystemDate()로만 얻는다. 변수로 한 번 받는 이유는 두 가지다 —
    -- 스칼라 UDF는 인라인되지 않아 WHERE에 직접 쓰면 행마다 호출되고,
    -- 한 프로시저 안의 "지금"이 호출마다 달라지는 것도 막는다.
    DECLARE @Now DATETIME2(7) = dbo.UfnSystemDate();

    UPDATE po
    SET po.[DeletedAt] = CASE WHEN @Restore = 1 THEN NULL ELSE @Now END,
        po.[UpdatedAt] = @Now
    FROM [dbo].[SoccerTeamPosts] po
    JOIN [dbo].[SoccerTeams] t
        ON t.[TeamId] = po.[TeamId] AND t.[ManagerUserId] = @ManagerUserId AND t.[DeletedAt] IS NULL
    WHERE po.[PostId] = @PostId
      AND ((@Restore = 0 AND po.[DeletedAt] IS NULL) OR (@Restore = 1 AND po.[DeletedAt] IS NOT NULL));

    DECLARE @Applied INT = @@ROWCOUNT;

    SELECT po.[PostId], po.[TeamId], po.[DeletedAt]
    FROM [dbo].[SoccerTeamPosts] po WITH (NOLOCK)
    WHERE po.[PostId] = @PostId AND @Applied = 1;
END
