-- 허브 자녀 카드 "팀 소식 안읽음" 요약 — 보호자의 각 Active 자녀별 안읽음 글 수 산출용.
-- 안읽은 글 하나당 그 자녀의 PlayerId 한 행을 내리고, Persistence가 PlayerId로 GROUP BY COUNT 한다
--   (모집 AcceptedCount·조회수와 같은 raw 행 패턴 — 계산 컬럼/전용 Record가 필요 없다).
-- 자녀 판정: SoccerPlayers.UserId 또는 FamilyLinks Guardian. 팀 글 전부 대상(공지·자료 모두 안읽음 점에 포함).
CREATE PROCEDURE [dbo].[UspGetSoccerTeamPostUnreadByGuardian]
    @UserId UNIQUEIDENTIFIER
AS
BEGIN
    SET NOCOUNT ON;

    SELECT c.[PlayerId]
    FROM (
        SELECT DISTINCT p.[PlayerId], tp.[TeamId]
        FROM [dbo].[SoccerPlayers] p WITH (NOLOCK)
        JOIN [dbo].[SoccerTeamPlayers] tp WITH (NOLOCK)
            ON tp.[PlayerId] = p.[PlayerId] AND tp.[Status] = 'Active' AND tp.[DeletedAt] IS NULL
        WHERE p.[DeletedAt] IS NULL
          AND (
              p.[UserId] = @UserId
              OR EXISTS (
                  SELECT 1 FROM [dbo].[SoccerPlayerFamilyLinks] fl WITH (NOLOCK)
                  WHERE fl.[PlayerId] = p.[PlayerId] AND fl.[UserId] = @UserId
                    AND fl.[Role] = 'Guardian' AND fl.[DeletedAt] IS NULL))
    ) c
    JOIN [dbo].[SoccerTeamPosts] po WITH (NOLOCK)
        ON po.[TeamId] = c.[TeamId] AND po.[DeletedAt] IS NULL
    WHERE NOT EXISTS (
        SELECT 1 FROM [dbo].[SoccerTeamPostReads] r WITH (NOLOCK)
        WHERE r.[PostId] = po.[PostId] AND r.[UserId] = @UserId);
END
