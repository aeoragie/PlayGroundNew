-- 게시판 글 고정 전환 (⋯ 고정/고정 해제). **팀당 고정 최대 2개** — SP가 강제한다.
-- @IsPinned=1인데 이미 다른 2개가 고정돼 있으면 빈 결과(Command가 InvalidInput/Forbidden 변환 → "고정은 2개까지").
-- 소유·미삭제 검증. 반환은 갱신된 글 행(@Applied=1일 때만).
CREATE PROCEDURE [dbo].[UspSetSoccerTeamPostPinned]
    @ManagerUserId UNIQUEIDENTIFIER,
    @PostId        UNIQUEIDENTIFIER,
    @IsPinned      BIT
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @Applied INT = 0;
    DECLARE @TeamId UNIQUEIDENTIFIER = (
        SELECT po.[TeamId]
        FROM [dbo].[SoccerTeamPosts] po WITH (NOLOCK)
        JOIN [dbo].[SoccerTeams] t WITH (NOLOCK)
            ON t.[TeamId] = po.[TeamId] AND t.[ManagerUserId] = @ManagerUserId AND t.[DeletedAt] IS NULL
        WHERE po.[PostId] = @PostId AND po.[DeletedAt] IS NULL);

    IF @TeamId IS NOT NULL
    BEGIN
        DECLARE @OtherPinned INT = (
            SELECT COUNT(*) FROM [dbo].[SoccerTeamPosts]
            WHERE [TeamId] = @TeamId AND [IsPinned] = 1 AND [DeletedAt] IS NULL AND [PostId] <> @PostId);

        -- 고정하려는데 이미 2개가 차 있으면 거부. 해제(@IsPinned=0)는 항상 허용.
        IF @IsPinned = 0 OR @OtherPinned < 2
        BEGIN
            UPDATE [dbo].[SoccerTeamPosts]
            SET [IsPinned] = @IsPinned, [UpdatedAt] = GETUTCDATE()
            WHERE [PostId] = @PostId AND [TeamId] = @TeamId AND [DeletedAt] IS NULL;

            SET @Applied = @@ROWCOUNT;
        END
    END

    SELECT
        po.[PostId], po.[TeamId], po.[Type], po.[Title], po.[Body], po.[IsPinned], po.[IsPublic],
        po.[AuthorId], po.[AuthorName], po.[EditedAt], po.[CreatedAt], po.[UpdatedAt], po.[DeletedAt]
    FROM [dbo].[SoccerTeamPosts] po WITH (NOLOCK)
    WHERE po.[PostId] = @PostId AND @Applied = 1;
END
