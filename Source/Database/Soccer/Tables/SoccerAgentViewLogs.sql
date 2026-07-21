-- 에이전트 열람 기록 (Design.AgentViewApproval — 신뢰의 핵심).
-- EventType: 'Approved'(승인 완료 — 심사 프로시저가 남김) / 'ProfileView'(디테일 권한 뷰 방문 —
--            공개 선수 프로필 조회 프로시저가 남김, 2026-07-21 권한 뷰 구현으로 적재 주체가 PlayGround로 확정) /
--            'RecordView'(에이전트 서비스 내 열람 — 그쪽이 적재). 표시 문구는 클라이언트가 유형으로 조립.
CREATE TABLE [dbo].[SoccerAgentViewLogs]
(
    [LogId]      UNIQUEIDENTIFIER NOT NULL DEFAULT NEWID(),
    [RequestId]  UNIQUEIDENTIFIER NOT NULL,               -- SoccerAgentViewRequests.RequestId (앱 계층 참조)
    [EventType]  VARCHAR(30)      NOT NULL,               -- 위 유형 3종
    [CreatedAt]  DATETIME2        NOT NULL DEFAULT GETUTCDATE(),

    CONSTRAINT [PK_SoccerAgentViewLogs] PRIMARY KEY ([LogId])
);
