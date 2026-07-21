-- 진학·진로 사례 (Design.TeamPublicHome ⑤ 진학·진로). 팀 대시보드(팀 정보 섹션)에서 관리하고 공개 홈이 열람한다.
-- 요약 3카드(프로 산하 이적/축구부 진학/상급팀 승격 N명)는 사례 PlayerCount 합산으로 파생 — 별도 저장 없음(수치 어긋남 방지).
-- "선수 개인이 공개에 동의한 사례만 표시됩니다" — 동의 플로우는 없고 관리자 입력 안내 카피로 강제(입력 폼에 명시).
CREATE TABLE [dbo].[SoccerTeamCareerOutcomes]
(
    [OutcomeId]    UNIQUEIDENTIFIER NOT NULL DEFAULT NEWID(),
    [TeamId]       UNIQUEIDENTIFIER NOT NULL,          -- SoccerTeams.TeamId (앱 계층 참조)
    [OutcomeYear]  INT              NOT NULL,          -- 타임라인 연도 (예: 2026)
    [OutcomeType]  VARCHAR(20)      NOT NULL,          -- 'ProTransfer'(프로 산하),'SchoolTeam'(축구부),'Promotion'(승격)
    [Title]        VARCHAR(300)     NOT NULL,          -- UTF-8 (한글 100자) 예: 'K리그 산하 U18 이적 1명'
    [Detail]       VARCHAR(600)     NULL,              -- UTF-8 (한글 200자) 예: 'MF · U15 출신 · 3년 재적'
    [PlayerCount]  INT              NOT NULL DEFAULT 1,-- 사례 인원 — 요약 카드 합산용

    [CreatedAt]    DATETIME2        NOT NULL DEFAULT GETUTCDATE(),
    [UpdatedAt]    DATETIME2        NOT NULL DEFAULT GETUTCDATE(),
    [DeletedAt]    DATETIME2        NULL,              -- 삭제 = 소프트 (실행취소 지원)

    CONSTRAINT [PK_SoccerTeamCareerOutcomes] PRIMARY KEY ([OutcomeId])
);
