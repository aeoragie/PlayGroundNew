-- 팀 대시보드 게시판 목록 (관리자 뷰). 소유 팀의 삭제되지 않은 글 전부, 고정 먼저 → 최신순.
-- RS1 = 글(SoccerTeamPostsEntity 그대로), RS2 = 첨부 파일, RS3 = 읽음 행의 PostId(글별 COUNT = 조회수).
-- 조회수는 계산값이라 컬럼이 아니다 — 읽음 행을 그대로 내리고 Persistence가 GROUP BY로 센다(모집 AcceptedCount 패턴).
CREATE PROCEDURE [dbo].[UspGetSoccerTeamPostsByManager]
    @ManagerUserId UNIQUEIDENTIFIER
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @TeamId UNIQUEIDENTIFIER = (
        SELECT TOP 1 [TeamId] FROM [dbo].[SoccerTeams]
        WHERE [ManagerUserId] = @ManagerUserId AND [DeletedAt] IS NULL
        ORDER BY [CreatedAt] DESC);

    -- RS1: 글
    SELECT
        po.[PostId], po.[TeamId], po.[Type], po.[Title], po.[Body], po.[IsPinned], po.[IsPublic],
        po.[AuthorId], po.[AuthorName], po.[EditedAt], po.[CreatedAt], po.[UpdatedAt], po.[DeletedAt]
    FROM [dbo].[SoccerTeamPosts] po WITH (NOLOCK)
    WHERE po.[TeamId] = @TeamId AND po.[DeletedAt] IS NULL
    ORDER BY po.[IsPinned] DESC, po.[CreatedAt] DESC;

    -- RS2: 첨부 파일 (해당 팀 글의 것만)
    SELECT f.[FileId], f.[PostId], f.[FileUrl], f.[FileName], f.[SizeBytes], f.[DisplayOrder], f.[CreatedAt]
    FROM [dbo].[SoccerTeamPostFiles] f WITH (NOLOCK)
    JOIN [dbo].[SoccerTeamPosts] po WITH (NOLOCK)
        ON po.[PostId] = f.[PostId] AND po.[TeamId] = @TeamId AND po.[DeletedAt] IS NULL
    ORDER BY f.[DisplayOrder], f.[CreatedAt];

    -- RS3: 읽음 행의 PostId (글별 COUNT = 조회수)
    SELECT r.[PostId]
    FROM [dbo].[SoccerTeamPostReads] r WITH (NOLOCK)
    JOIN [dbo].[SoccerTeamPosts] po WITH (NOLOCK)
        ON po.[PostId] = r.[PostId] AND po.[TeamId] = @TeamId AND po.[DeletedAt] IS NULL;
END
