-- 팀 게시판 글 첨부 (Design.TeamBoard). pdf·이미지·hwp, 글당 최대 3개(SP·Command가 강제).
-- FileUrl은 로그인 열람자(보호자·스태프)에게만 내려간다 — 공개홈 게스트에게는 FileName만 노출하고
-- 다운로드는 로그인 필요(공개 조회 SP가 URL을 애초에 SELECT하지 않는다).
-- 글 저장은 통째 교체(기존 파일 행 삭제 후 재삽입) — 순서 변경·삭제를 한 번에 반영한다.
CREATE TABLE [dbo].[SoccerTeamPostFiles]
(
    [FileId]       UNIQUEIDENTIFIER NOT NULL DEFAULT NEWID(),
    [PostId]       UNIQUEIDENTIFIER NOT NULL,        -- SoccerTeamPosts.PostId (앱 계층 참조)
    [FileUrl]      VARCHAR(400)     NOT NULL,        -- /uploads/team-board/... (로그인 열람자에게만 노출)
    [FileName]     VARCHAR(300)     NOT NULL,        -- UTF-8 원본 파일명 (한글 100자)
    [SizeBytes]    BIGINT           NOT NULL DEFAULT 0,
    [DisplayOrder] INT              NOT NULL DEFAULT 0,
    [CreatedAt]    DATETIME2        NOT NULL DEFAULT GETUTCDATE(),

    CONSTRAINT [PK_SoccerTeamPostFiles] PRIMARY KEY ([FileId])
);
