-- 팀 게시판 글 읽음 기록 (Design.TeamBoard). 보호자가 글을 열면 한 행이 생긴다.
-- 두 가지를 산출한다: ① 보호자 뷰 안읽음 오렌지 점(내 UserId 행이 없으면 안읽음)
--                    ② 관리자 목록 조회수(글별 COUNT — 스태프에게만 표시).
-- 관리자가 자기 대시보드에서 글을 봐도 행을 만들지 않는다(조회수 부풀림 방지) — 보호자 열람만 적재.
-- 복합 PK로 한 사용자·한 글은 한 번만 읽음 처리된다(재열람은 무시, ReadAt 유지).
CREATE TABLE [dbo].[SoccerTeamPostReads]
(
    [PostId]  UNIQUEIDENTIFIER NOT NULL,            -- SoccerTeamPosts.PostId
    [UserId]  UNIQUEIDENTIFIER NOT NULL,            -- Account.Users.UserId (열람한 보호자)
    [ReadAt]  DATETIME2        NOT NULL DEFAULT GETUTCDATE(),

    CONSTRAINT [PK_SoccerTeamPostReads] PRIMARY KEY ([PostId], [UserId])
);
