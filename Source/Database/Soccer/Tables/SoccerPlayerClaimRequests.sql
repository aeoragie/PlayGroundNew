-- 보호자 연결 요청 (Design.ClaimFlow) — Unclaimed → (코드 입력 + 연결 요청) Pending → (팀 관리자 승인) Claimed.
-- **기존 UspClaimSoccerPlayerInvite(코드 입력 = 즉시 연결)와 별개 경로다** — /claim 4스텝 플로우 전용.
-- 승인 시 선수 연결(UserId)·가족 연결(FamilyLink)·초대코드 소진이 한 트랜잭션으로 일어난다.
CREATE TABLE [dbo].[SoccerPlayerClaimRequests]
(
    [RequestId]         UNIQUEIDENTIFIER NOT NULL DEFAULT NEWID(),
    [InviteId]          UNIQUEIDENTIFIER NOT NULL,          -- SoccerPlayerInvites.InviteId (사용 코드)
    [PlayerId]          UNIQUEIDENTIFIER NOT NULL,          -- SoccerPlayers.PlayerId (앱 계층 참조)
    [TeamId]            UNIQUEIDENTIFIER NOT NULL,          -- SoccerTeams.TeamId (앱 계층 참조)
    [RequesterUserId]   UNIQUEIDENTIFIER NOT NULL,          -- Account.Users.UserId (앱 계층 참조)
    [RequesterName]     VARCHAR(300)     NOT NULL,          -- UTF-8 (한글 100자) 표시 스냅샷 (Account 조인 불가)
    [Relation]          VARCHAR(20)      NOT NULL,          -- 'Mother','Father','Guardian'
    [Status]            VARCHAR(20)      NOT NULL DEFAULT 'Pending', -- 'Pending','Approved','Rejected'
    [ReviewedAt]        DATETIME2        NULL,

    [CreatedAt]         DATETIME2        NOT NULL DEFAULT GETUTCDATE(),
    [UpdatedAt]         DATETIME2        NOT NULL DEFAULT GETUTCDATE(),
    [DeletedAt]         DATETIME2        NULL,

    CONSTRAINT [PK_SoccerPlayerClaimRequests] PRIMARY KEY ([RequestId])
);
