-- 선수 커리어(소속 이력). 선수 대시보드 커리어 섹션 + 공개 프로필에 노출.
-- 과거 팀은 플랫폼 밖일 수 있어 TeamName 자유 입력, 플랫폼 팀이면 TeamId 연결(팀 확인 흐름의 기반).
CREATE TABLE [dbo].[SoccerPlayerCareers]
(
    [CareerId]      UNIQUEIDENTIFIER NOT NULL DEFAULT NEWID(),
    [PlayerId]      UNIQUEIDENTIFIER NOT NULL,          -- SoccerPlayers.PlayerId (앱 계층 참조)
    [TeamName]      VARCHAR(300)     NOT NULL,          -- UTF-8 (한글 100자)
    [TeamId]        UNIQUEIDENTIFIER NULL,              -- 플랫폼 팀이면 연결 (SoccerTeams.TeamId, 앱 계층 참조)
    [IsCurrent]     BIT              NOT NULL DEFAULT 0, -- 현재 소속 (타임라인 teal 도트)
    [BadgeLabel]    VARCHAR(150)     NULL,              -- UTF-8 (한글 50자) 특이 뱃지 ('서울 지역 대표 선발')
    [StartDate]     DATE             NOT NULL,          -- 기간 시작 (표시는 연.월)
    [EndDate]       DATE             NULL,              -- NULL = 현재
    [Role]          VARCHAR(150)     NULL,              -- UTF-8 (한글 50자) 'U15 · FW · 주전'
    [Note]          VARCHAR(600)     NULL,              -- UTF-8 (한글 200자)
    [IsVerified]    BIT              NOT NULL DEFAULT 0, -- 팀 관리자 확인됨 (확인 플로우는 후속)

    [CreatedAt]     DATETIME2        NOT NULL DEFAULT GETUTCDATE(),
    [UpdatedAt]     DATETIME2        NOT NULL DEFAULT GETUTCDATE(),
    [DeletedAt]     DATETIME2        NULL,

    CONSTRAINT [PK_SoccerPlayerCareers] PRIMARY KEY ([CareerId])
);
