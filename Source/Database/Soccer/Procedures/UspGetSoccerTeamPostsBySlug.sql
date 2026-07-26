-- 공개 팀 홈 ① 소개 탭 "팀 소식" 섹션 (Slug 기준, 비로그인 읽기전용).
-- 공개(IsPublic=1) 글만, 최신순. 유형 구분 없음(화면은 전부 "소식"). 비공개·미존재 팀은 빈 결과.
-- **첨부는 파일명만 내린다 — FileUrl을 SELECT하지 않는다**(공개홈 다운로드는 로그인 필요, 서버가 원천 차단).
-- 정렬은 최신순만(고정 개념 없음 — 방문자에겐 시간순 소식 흐름). 상한은 클라이언트(3건 + "지난 소식 보기").
CREATE PROCEDURE [dbo].[UspGetSoccerTeamPostsBySlug]
    @Slug VARCHAR(100)
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @TeamId UNIQUEIDENTIFIER = (
        SELECT TOP 1 [TeamId]
        FROM [dbo].[SoccerTeams] WITH (NOLOCK)
        WHERE [Slug] = @Slug AND [IsPublicProfile] = 1 AND [DeletedAt] IS NULL
        ORDER BY [CreatedAt] DESC);

    -- RS1: 공개 글 (관리 정보 미노출 — Type·IsPublic·AuthorId 등은 SELECT하지 않는다)
    SELECT po.[PostId], po.[Title], po.[Body], po.[EditedAt], po.[CreatedAt]
    FROM [dbo].[SoccerTeamPosts] po WITH (NOLOCK)
    WHERE po.[TeamId] = @TeamId AND po.[IsPublic] = 1 AND po.[DeletedAt] IS NULL
    ORDER BY po.[CreatedAt] DESC;

    -- RS2: 첨부 파일명만 (FileUrl 제외 — 게스트 다운로드 차단)
    SELECT f.[FileId], f.[PostId], f.[FileName], f.[SizeBytes], f.[DisplayOrder]
    FROM [dbo].[SoccerTeamPostFiles] f WITH (NOLOCK)
    JOIN [dbo].[SoccerTeamPosts] po WITH (NOLOCK)
        ON po.[PostId] = f.[PostId] AND po.[TeamId] = @TeamId AND po.[IsPublic] = 1 AND po.[DeletedAt] IS NULL
    ORDER BY f.[DisplayOrder], f.[CreatedAt];
END
