-- 팀 로스터/소속. 선수의 팀 소속 속성(번호·포지션·학년)은 팀별이라 여기에 둔다.
CREATE TABLE [dbo].[SoccerTeamPlayers]
(
    [TeamPlayerId]  UNIQUEIDENTIFIER NOT NULL DEFAULT NEWID(),
    [TeamId]        UNIQUEIDENTIFIER NOT NULL,          -- SoccerTeams.TeamId (앱 계층 참조)
    [PlayerId]      UNIQUEIDENTIFIER NOT NULL,          -- SoccerPlayers.PlayerId (앱 계층 참조)
    [JerseyNumber]  VARCHAR(10)      NULL,              -- 등번호 (미입력 시 '-')
    [Position]      VARCHAR(60)      NULL,              -- 포지션 (FW/MF/DF/GK 등)
    [Grade]         VARCHAR(60)      NULL,              -- 학년 (대시보드에서 사용)
    [Status]        VARCHAR(20)      NOT NULL DEFAULT 'Active', -- 'Active','Inactive'

    [CreatedAt]     DATETIME2        NOT NULL DEFAULT GETUTCDATE(),
    [UpdatedAt]     DATETIME2        NOT NULL DEFAULT GETUTCDATE(),
    [DeletedAt]     DATETIME2        NULL,

    CONSTRAINT [PK_SoccerTeamPlayers] PRIMARY KEY ([TeamPlayerId])
);
