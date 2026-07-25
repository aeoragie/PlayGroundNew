-- 선수 모집 지원 (Design.Application, E5). 공고(SoccerTeamRecruitments)에 보호자가 자녀로 지원한다.
-- 상태 머신: Pending(대기) → Reviewing(검토중) → Accepted(수락) | Rejected(종료).
-- 수락 이후 로스터 편입은 별도 트랜잭션(보호자 동의) — 지원 상태는 Accepted에서 고정.
-- 취소 = 소프트 삭제(대기 상태에서만). 보류(Rejected) 후 30일 지나면 같은 선수·공고 재지원 허용.
CREATE TABLE [dbo].[SoccerApplications]
(
    [ApplicationId]  UNIQUEIDENTIFIER NOT NULL DEFAULT NEWID(),
    [RecruitmentId]  UNIQUEIDENTIFIER NOT NULL,          -- SoccerTeamRecruitments.RecruitmentId (앱 계층 참조)
    [PlayerId]       UNIQUEIDENTIFIER NOT NULL,          -- 지원 선수(자녀) SoccerPlayers.PlayerId
    [GuardianUserId] UNIQUEIDENTIFIER NOT NULL,          -- 신청 보호자 Account.Users.UserId (앱 계층 참조)
    [DesiredPosition] VARCHAR(20)     NULL,              -- 희망 포지션 (공고 PositionsJson 중 하나)
    [Introduction]   VARCHAR(1500)    NULL,              -- UTF-8 (한글 500자) 자기소개 (선택)
    [Status]         VARCHAR(20)      NOT NULL DEFAULT 'Pending', -- 'Pending','Reviewing','Accepted','Rejected'

    [Route]          VARCHAR(20)      NOT NULL DEFAULT 'Direct', -- 'Direct'(직접) | 'AgentRef'(에이전트 추천)
    [RefAgentId]     UNIQUEIDENTIFIER NULL,              -- AgentRef일 때 추천 에이전트 (SoccerAgentProfiles)

    [ReviewedAt]     DATETIME2        NULL,              -- 검토중/수락/종료 전환 시각
    [RejectedAt]     DATETIME2        NULL,              -- 보류(Rejected) 시각 — 30일 재지원 쿨다운 기준

    [CreatedAt]      DATETIME2        NOT NULL DEFAULT GETUTCDATE(),
    [UpdatedAt]      DATETIME2        NOT NULL DEFAULT GETUTCDATE(),
    [DeletedAt]      DATETIME2        NULL,              -- 취소 = 소프트 삭제 (대기 상태에서만)

    CONSTRAINT [PK_SoccerApplications] PRIMARY KEY ([ApplicationId])
);
