-- 선수 프로필 초대코드. 팀이 등록한 Unclaimed 선수를 가족/선수가 Code로 연결(Claim)한다.
-- Claim 시 SoccerPlayers.UserId를 연결하고 Status를 'Claimed'로 바꾼다 (연결 로직은 후속).
CREATE TABLE [dbo].[SoccerPlayerInvites]
(
    [InviteId]         UNIQUEIDENTIFIER NOT NULL DEFAULT NEWID(),
    [Code]             VARCHAR(12)      NOT NULL,          -- 짧은 연결 코드
    [PlayerId]         UNIQUEIDENTIFIER NOT NULL,          -- SoccerPlayers.PlayerId
    [TeamId]           UNIQUEIDENTIFIER NOT NULL,          -- SoccerTeams.TeamId
    [Status]           VARCHAR(20)      NOT NULL DEFAULT 'Pending', -- 'Pending','Claimed','Expired','Revoked'
    [ClaimedByUserId]  UNIQUEIDENTIFIER NULL,              -- Account.Users.UserId (앱 계층 참조)
    [ExpiresAt]        DATETIME2        NULL,
    [ClaimedAt]        DATETIME2        NULL,
    [CreatedAt]        DATETIME2        NOT NULL DEFAULT GETUTCDATE(),

    CONSTRAINT [PK_SoccerPlayerInvites] PRIMARY KEY ([InviteId]),
    CONSTRAINT [UQ_SoccerPlayerInvites_Code] UNIQUE ([Code])
);
