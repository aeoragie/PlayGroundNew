-- 보호자 뷰 — 팀 소식 (허브 자녀 카드 → 팀 소식). 읽기 전용, 안읽음 오렌지 점.
-- 열람 자격: @PlayerId가 내 자녀(SoccerPlayers.UserId 또는 FamilyLinks Guardian)이고 그 자녀가 팀에 Active 소속.
--   자격 없음·미존재는 @TeamId NULL → 전 결과셋 빈 결과(존재 여부 미노출).
-- **편입 이전 글도 열람 가능**(팀 히스토리) — 소속 시작 시점과 무관하게 팀 글 전부. 로스터 이탈 시 자동 차단(Active 조인).
-- RS1=팀명 스칼라 · RS2=글(공지·자료 전부) · RS3=첨부(보호자는 다운로드 가능 → FileUrl 포함) · RS4=내가 읽은 PostId.
CREATE PROCEDURE [dbo].[UspGetSoccerTeamPostsByGuardian]
    @UserId   UNIQUEIDENTIFIER,
    @PlayerId UNIQUEIDENTIFIER
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @TeamId UNIQUEIDENTIFIER = (
        SELECT TOP 1 tp.[TeamId]
        FROM [dbo].[SoccerTeamPlayers] tp WITH (NOLOCK)
        JOIN [dbo].[SoccerPlayers] p WITH (NOLOCK)
            ON p.[PlayerId] = tp.[PlayerId] AND p.[DeletedAt] IS NULL
        WHERE tp.[PlayerId] = @PlayerId AND tp.[Status] = 'Active' AND tp.[DeletedAt] IS NULL
          AND (
              p.[UserId] = @UserId
              OR EXISTS (
                  SELECT 1 FROM [dbo].[SoccerPlayerFamilyLinks] fl WITH (NOLOCK)
                  WHERE fl.[PlayerId] = @PlayerId AND fl.[UserId] = @UserId
                    AND fl.[Role] = 'Guardian' AND fl.[DeletedAt] IS NULL))
        ORDER BY tp.[CreatedAt] DESC);

    -- RS1: 팀명 (헤더·여러 자녀 팀명 접두)
    SELECT t.[TeamName]
    FROM [dbo].[SoccerTeams] t WITH (NOLOCK)
    WHERE t.[TeamId] = @TeamId AND t.[DeletedAt] IS NULL;

    -- RS2: 글 (고정 먼저 → 최신순)
    SELECT
        po.[PostId], po.[TeamId], po.[Type], po.[Title], po.[Body], po.[IsPinned], po.[IsPublic],
        po.[AuthorId], po.[AuthorName], po.[EditedAt], po.[CreatedAt], po.[UpdatedAt], po.[DeletedAt]
    FROM [dbo].[SoccerTeamPosts] po WITH (NOLOCK)
    WHERE po.[TeamId] = @TeamId AND po.[DeletedAt] IS NULL
    ORDER BY po.[IsPinned] DESC, po.[CreatedAt] DESC;

    -- RS3: 첨부 파일 (보호자는 다운로드 가능 → FileUrl 포함)
    SELECT f.[FileId], f.[PostId], f.[FileUrl], f.[FileName], f.[SizeBytes], f.[DisplayOrder], f.[CreatedAt]
    FROM [dbo].[SoccerTeamPostFiles] f WITH (NOLOCK)
    JOIN [dbo].[SoccerTeamPosts] po WITH (NOLOCK)
        ON po.[PostId] = f.[PostId] AND po.[TeamId] = @TeamId AND po.[DeletedAt] IS NULL
    ORDER BY f.[DisplayOrder], f.[CreatedAt];

    -- RS4: 내가 읽은 PostId (안읽음 점 판정)
    SELECT r.[PostId]
    FROM [dbo].[SoccerTeamPostReads] r WITH (NOLOCK)
    JOIN [dbo].[SoccerTeamPosts] po WITH (NOLOCK)
        ON po.[PostId] = r.[PostId] AND po.[TeamId] = @TeamId AND po.[DeletedAt] IS NULL
    WHERE r.[UserId] = @UserId;
END
