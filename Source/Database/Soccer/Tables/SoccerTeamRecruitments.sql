-- 선수 모집 공고 (Design.TeamPublicHome ④ 모집). 팀 대시보드에서 작성·마감·삭제하고 공개 홈이 열람한다.
-- 지원(Application) 스키마는 별도 — 공고만 다룬다. "모집중" 판정 = Status='Open' AND 마감일 미경과 (파생).
-- 마감(Closed)은 재오픈 없음 — 새 모집은 새 공고로 올린다 (마감 카드가 이력으로 남는 구조).
CREATE TABLE [dbo].[SoccerTeamRecruitments]
(
    [RecruitmentId]  UNIQUEIDENTIFIER NOT NULL DEFAULT NEWID(),
    [TeamId]         UNIQUEIDENTIFIER NOT NULL,          -- SoccerTeams.TeamId (앱 계층 참조)
    [Title]          VARCHAR(300)     NOT NULL,          -- UTF-8 (한글 100자) 예: 'U15 공격수 모집'
    [Description]    VARCHAR(1500)    NOT NULL,          -- UTF-8 (한글 500자) 공고 본문
    [ConditionsJson] VARCHAR(600)     NULL,              -- 조건 칩 목록 JSON 배열 (예: ["테스트 1회 · 주말"])
    [DeadlineDate]   DATE             NULL,              -- 마감일 — 칩 "마감 M/d", 경과 시 모집중에서 제외
    [Status]         VARCHAR(20)      NOT NULL DEFAULT 'Open', -- 'Open','Closed'

    [CreatedAt]      DATETIME2        NOT NULL DEFAULT GETUTCDATE(),
    [UpdatedAt]      DATETIME2        NOT NULL DEFAULT GETUTCDATE(),
    [DeletedAt]      DATETIME2        NULL,              -- 삭제 = 소프트 (실행취소 지원)

    CONSTRAINT [PK_SoccerTeamRecruitments] PRIMARY KEY ([RecruitmentId])
);
