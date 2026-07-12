-- 선수 프로필. 온보딩에서 생성. 학부모 대리 관리(IsGuardianManaged) 지원.
CREATE TABLE [dbo].[SoccerPlayers]
(
    [PlayerId]           UNIQUEIDENTIFIER NOT NULL DEFAULT NEWID(),
    [UserId]             UNIQUEIDENTIFIER NULL,             -- 관리 주체 (Account.Users.UserId, 앱 계층 참조). 대리관리 시 보호자 UserId
    [Name]               NVARCHAR(50)     NOT NULL,
    [BirthDate]          DATE             NULL,
    [AgeGroup]           VARCHAR(20)      NULL,             -- 'U12','U15','U18'
    [Region]             NVARCHAR(100)    NULL,
    [TeamId]             UNIQUEIDENTIFIER NULL,             -- 소속팀 (SoccerTeams.TeamId, 앱 계층 참조)
    [IsGuardianManaged]  BIT              NOT NULL DEFAULT 0,-- 학부모 대리 관리 프로필

    -- KFA 데이터 적재 대비 (결정 #5)
    [DataSource]         VARCHAR(20)      NOT NULL DEFAULT 'User', -- 'User','KfaApi','Seed'
    [ExternalId]         VARCHAR(64)      NULL,             -- 외부 시스템 멱등키

    [CreatedAt]          DATETIME2        NOT NULL DEFAULT GETUTCDATE(),
    [UpdatedAt]          DATETIME2        NOT NULL DEFAULT GETUTCDATE(),
    [DeletedAt]          DATETIME2        NULL,             -- 소프트 삭제

    CONSTRAINT [PK_SoccerPlayers] PRIMARY KEY ([PlayerId])
);
