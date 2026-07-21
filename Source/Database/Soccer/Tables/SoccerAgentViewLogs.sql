-- 에이전트 열람 기록 (Design.AgentViewApproval — 신뢰의 핵심). 적재는 에이전트 서비스,
-- PlayGround(보호자 심사 화면)는 읽기 + 승인 이벤트 1건만 직접 남긴다.
-- EventType: 'Approved'(승인 완료 — 심사 프로시저가 남김) / 'ProfileView'(디테일 권한 뷰 방문) /
--            'RecordView'(경기별 상세 기록 열람) — 표시 문구는 클라이언트가 유형으로 조립.
CREATE TABLE [dbo].[SoccerAgentViewLogs]
(
    [LogId]      UNIQUEIDENTIFIER NOT NULL DEFAULT NEWID(),
    [RequestId]  UNIQUEIDENTIFIER NOT NULL,               -- SoccerAgentViewRequests.RequestId (앱 계층 참조)
    [EventType]  VARCHAR(30)      NOT NULL,               -- 위 유형 3종
    [CreatedAt]  DATETIME2        NOT NULL DEFAULT GETUTCDATE(),

    CONSTRAINT [PK_SoccerAgentViewLogs] PRIMARY KEY ([LogId])
);
