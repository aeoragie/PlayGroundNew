-- 게시판 글 공개 전환 (⋯ 공개 전환 / 목록 눈 아이콘). 공개(1)면 소개 탭 "팀 소식"에 즉시 노출.
-- 소유·미삭제 검증. 반환은 갱신된 글 행(@Applied=1일 때만 — 거부·미존재는 빈 결과).
CREATE PROCEDURE [dbo].[UspSetSoccerTeamPostPublic]
    @ManagerUserId UNIQUEIDENTIFIER,
    @PostId        UNIQUEIDENTIFIER,
    @IsPublic      BIT
AS
BEGIN
    SET NOCOUNT ON;

    -- 시각은 dbo.UfnSystemDate()로만 얻는다. 변수로 한 번 받는 이유는 두 가지다 —
    -- 스칼라 UDF는 인라인되지 않아 WHERE에 직접 쓰면 행마다 호출되고,
    -- 한 프로시저 안의 "지금"이 호출마다 달라지는 것도 막는다.
    DECLARE @Now DATETIME2(7) = dbo.UfnSystemDate();

    UPDATE po
    SET po.[IsPublic] = @IsPublic, po.[UpdatedAt] = @Now
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
