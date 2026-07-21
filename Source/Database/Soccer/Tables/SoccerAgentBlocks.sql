-- 에이전트 차단 ("이 에이전트의 요청 다시 받지 않기"). 요청 생성 거부는 에이전트 서비스가
-- 이 테이블을 조회해 강제한다 — PlayGround는 행 생성만.
CREATE TABLE [dbo].[SoccerAgentBlocks]
(
    [BlockId]         UNIQUEIDENTIFIER NOT NULL DEFAULT NEWID(),
    [GuardianUserId]  UNIQUEIDENTIFIER NOT NULL,          -- Account.Users.UserId (앱 계층 참조)
    [AgentId]         UNIQUEIDENTIFIER NOT NULL,          -- SoccerAgentProfiles.AgentId (앱 계층 참조)
    [CreatedAt]       DATETIME2        NOT NULL DEFAULT GETUTCDATE(),

    CONSTRAINT [PK_SoccerAgentBlocks] PRIMARY KEY ([BlockId]),
    CONSTRAINT [UQ_SoccerAgentBlocks_GuardianAgent] UNIQUE ([GuardianUserId], [AgentId])
);
