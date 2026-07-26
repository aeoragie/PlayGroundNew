-- 팀 게시판 (Design.TeamBoard, P1 4종 중 첫 번째). 관리자·코치가 공지·자료를 올리고
-- 로스터 보호자가 열람하며, 글 단위로 공개홈 노출을 선택한다. 단방향 공지(댓글 없음).
-- 삭제 = 소프트 삭제 30일 보관(운영 문의 대응) — 물리 삭제는 별도 정리 배치의 몫.
-- 작성 계정은 현재 팀 ManagerUserId 하나뿐이다(코치는 아직 계정 개념이 없다). AuthorId=관리자,
-- AuthorName은 발행 시점 표시명 스냅샷(JWT 이름 — 위조 방지). 코치 계정이 생기면 여기에 편입한다.
-- 조회수(ViewCount)는 컬럼으로 두지 않고 SoccerTeamPostReads COUNT에서 파생한다(단일 진실·증가 경합 없음).
CREATE TABLE [dbo].[SoccerTeamPosts]
(
    [PostId]     UNIQUEIDENTIFIER NOT NULL DEFAULT NEWID(),
    [TeamId]     UNIQUEIDENTIFIER NOT NULL,          -- SoccerTeams.TeamId (앱 계층 참조)
    [Type]       VARCHAR(20)      NOT NULL,          -- 'Notice','Material'
    [Title]      VARCHAR(300)     NOT NULL,          -- UTF-8 (한글 100자) 예: '8월 훈련 일정 변경 안내'
    [Body]       VARCHAR(6000)    NOT NULL,          -- UTF-8 (한글 2000자) 본문
    [IsPinned]   BIT              NOT NULL DEFAULT 0, -- 고정 — 팀당 최대 2개(SP가 강제), 목록 최상단
    [IsPublic]   BIT              NOT NULL DEFAULT 0, -- 공개홈 노출 — 기본 끔(실수 공개 방지)
    [AuthorId]   UNIQUEIDENTIFIER NOT NULL,          -- Account.Users.UserId (작성 계정 = 팀 관리자)
    [AuthorName] VARCHAR(300)     NULL,              -- UTF-8 작성자 표시명 스냅샷 (발행 시점 JWT 이름)
    [EditedAt]   DATETIME2        NULL,              -- 수정 시각 — "수정됨" 표기, 재알림 없음

    [CreatedAt]  DATETIME2        NOT NULL DEFAULT GETUTCDATE(),
    [UpdatedAt]  DATETIME2        NOT NULL DEFAULT GETUTCDATE(),
    [DeletedAt]  DATETIME2        NULL,              -- 소프트 삭제 30일 보관

    CONSTRAINT [PK_SoccerTeamPosts] PRIMARY KEY ([PostId])
);
