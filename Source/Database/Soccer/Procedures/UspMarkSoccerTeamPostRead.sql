-- 게시판 글 읽음 처리 (보호자가 글을 열 때). 안읽음 점 해제 + 관리자 조회수 산출.
-- 열람 자격: @UserId가 이 글이 속한 팀의 Active 로스터 선수의 보호자여야 한다
--   (SoccerPlayers.UserId 또는 FamilyLinks Guardian). 자격 없으면 아무것도 하지 않고 빈 결과.
-- 이미 읽은 글은 무시(복합 PK — ReadAt 유지). 관리자 대시보드 열람은 이 경로를 타지 않아 조회수를 부풀리지 않는다.
CREATE PROCEDURE [dbo].[UspMarkSoccerTeamPostRead]
    @UserId UNIQUEIDENTIFIER,
    @PostId UNIQUEIDENTIFIER
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @Authorized BIT = CASE WHEN EXISTS (
        SELECT 1
        FROM [dbo].[SoccerTeamPosts] po WITH (NOLOCK)
        JOIN [dbo].[SoccerTeamPlayers] tp WITH (NOLOCK)
            ON tp.[TeamId] = po.[TeamId] AND tp.[Status] = 'Active' AND tp.[DeletedAt] IS NULL
        JOIN [dbo].[SoccerPlayers] p WITH (NOLOCK)
            ON p.[PlayerId] = tp.[PlayerId] AND p.[DeletedAt] IS NULL
        WHERE po.[PostId] = @PostId AND po.[DeletedAt] IS NULL
          AND (
              p.[UserId] = @UserId
              OR EXISTS (
                  SELECT 1 FROM [dbo].[SoccerPlayerFamilyLinks] fl WITH (NOLOCK)
                  WHERE fl.[PlayerId] = p.[PlayerId] AND fl.[UserId] = @UserId
                    AND fl.[Role] = 'Guardian' AND fl.[DeletedAt] IS NULL))
    ) THEN 1 ELSE 0 END;

    DECLARE @Applied INT = 0;

    IF @Authorized = 1 AND NOT EXISTS (
        SELECT 1 FROM [dbo].[SoccerTeamPostReads] WHERE [PostId] = @PostId AND [UserId] = @UserId)
    BEGIN
        INSERT INTO [dbo].[SoccerTeamPostReads] ([PostId], [UserId]) VALUES (@PostId, @UserId);
        SET @Applied = 1;
    END

    -- 자격이 있으면(이미 읽었어도) 성공 행 1개 — Command가 Success(true)로 본다.
    SELECT @PostId AS [PostId] WHERE @Authorized = 1;
END
