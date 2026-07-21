-- 에이전트 프로필 (설계 결정 4 선반영 — 에이전트 서비스가 쓰기 주체, PlayGround는 읽기만).
-- 열람 요청 심사 화면의 신원 카드 데이터. 인증(IsVerified=1) 에이전트만 요청을 만들 수 있다(에이전트 서비스 규칙).
CREATE TABLE [dbo].[SoccerAgentProfiles]
(
    [AgentId]         UNIQUEIDENTIFIER NOT NULL DEFAULT NEWID(),
    [UserId]          UNIQUEIDENTIFIER NULL,              -- Account.Users.UserId (앱 계층 참조)
    [Name]            VARCHAR(300)     NOT NULL,          -- UTF-8 (한글 100자) 표시 이름
    [AgencyName]      VARCHAR(300)     NULL,              -- UTF-8 소속 에이전시
    [RegisteredYear]  INT              NULL,              -- 등록 연도
    [IsVerified]      BIT              NOT NULL DEFAULT 0,
    [BrokerageCount]  INT              NOT NULL DEFAULT 0, -- 중개 이력 (에이전트 서비스 집계)
    [Rating]          DECIMAL(2,1)     NULL,              -- 팀·학부모 평가 (예: 4.7)
    [ActiveRegions]   VARCHAR(300)     NULL,              -- UTF-8 활동 지역 (예: '서울·경기')

    [CreatedAt]       DATETIME2        NOT NULL DEFAULT GETUTCDATE(),
    [UpdatedAt]       DATETIME2        NOT NULL DEFAULT GETUTCDATE(),
    [DeletedAt]       DATETIME2        NULL,

    CONSTRAINT [PK_SoccerAgentProfiles] PRIMARY KEY ([AgentId])
);
