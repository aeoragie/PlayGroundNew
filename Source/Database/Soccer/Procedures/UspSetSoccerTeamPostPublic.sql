-- 게시판 글 공개 전환 (⋯ 공개 전환 / 목록 눈 아이콘). 공개(1)면 소개 탭 "팀 소식"에 즉시 노출.
-- 소유·미삭제 검증. 반환은 갱신된 글 행(@Applied=1일 때만 — 거부·미존재는 빈 결과).
CREATE PROCEDURE [dbo].[UspSetSoccerTeamPostPublic]
    @ManagerUserId UNIQUEIDENTIFIER,
    @PostId        UNIQUEIDENTIFIER,
    @IsPublic      BIT
AS
BEGIN
    SET NOCOUNT ON;

    UPDATE po
    SET po.[IsPublic] = @IsPublic, po.[UpdatedAt] = GETUTCDATE()
    FROM [dbo].[SoccerTeamPosts] po
    JOIN [dbo].[SoccerTeams] t
        ON t.[TeamId] = po.[TeamId] AND t.[ManagerUserId] = @ManagerUserId AND t.[DeletedAt] IS NULL
    WHERE po.[PostId] = @PostId AND po.[DeletedAt] IS NULL;

    DECLARE @Applied INT = @@ROWCOUNT;

    SELECT
        po.[PostId], po.[TeamId], po.[Type], po.[Title], po.[Body], po.[IsPinned], po.[IsPublic],
        po.[AuthorId], po.[AuthorName], po.[EditedAt], po.[CreatedAt], po.[UpdatedAt], po.[DeletedAt]
    FROM [dbo].[SoccerTeamPosts] po WITH (NOLOCK)
    WHERE po.[PostId] = @PostId AND @Applied = 1;
END
