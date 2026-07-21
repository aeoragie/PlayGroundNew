-- 에이전트 상세 정보 열람 요청 (Design.AgentViewApproval) — 미성년자 보호 관문.
-- **요청 생성은 에이전트 서비스의 몫이다** — PlayGround(보호자)는 심사(승인/거절/철회)만 한다.
-- 상태: Pending → Approved(ExpiresAt = 승인+30일) / Denied. Approved → Revoked(철회 — 화면은 거절과 동급).
-- 열람 범위는 고정 세트(사용자 선택 없음) — 컬럼으로 두지 않는다. 연락처는 항상 제외(플랫폼 규칙).
-- 거절 후 같은 에이전트 30일 재요청 쿨다운은 요청 생성 측(에이전트 서비스)이 강제한다.
CREATE TABLE [dbo].[SoccerAgentViewRequests]
(
    [RequestId]       UNIQUEIDENTIFIER NOT NULL DEFAULT NEWID(),
    [AgentId]         UNIQUEIDENTIFIER NOT NULL,          -- SoccerAgentProfiles.AgentId (앱 계층 참조)
    [PlayerId]        UNIQUEIDENTIFIER NOT NULL,          -- SoccerPlayers.PlayerId (앱 계층 참조)
    [GuardianUserId]  UNIQUEIDENTIFIER NOT NULL,          -- 심사 주체 (Account.Users, 앱 계층 참조)
    [Message]         VARCHAR(1500)    NOT NULL,          -- UTF-8 (한글 500자) 에이전트가 쓴 요청 메시지
    [Status]          VARCHAR(20)      NOT NULL DEFAULT 'Pending', -- 'Pending','Approved','Denied','Revoked'
    [RequestedAt]     DATETIME2        NOT NULL DEFAULT GETUTCDATE(),
    [ReviewedAt]      DATETIME2        NULL,
    [ExpiresAt]       DATETIME2        NULL,              -- 승인 시각 + 30일 (만료 판정은 조회 파생)

    [CreatedAt]       DATETIME2        NOT NULL DEFAULT GETUTCDATE(),
    [UpdatedAt]       DATETIME2        NOT NULL DEFAULT GETUTCDATE(),
    [DeletedAt]       DATETIME2        NULL,

    CONSTRAINT [PK_SoccerAgentViewRequests] PRIMARY KEY ([RequestId])
);
