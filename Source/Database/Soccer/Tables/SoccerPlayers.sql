-- 선수 프로필. 온보딩에서 생성. 학부모 대리 관리(IsGuardianManaged) 지원.
CREATE TABLE [dbo].[SoccerPlayers]
(
    [PlayerId]           UNIQUEIDENTIFIER NOT NULL DEFAULT NEWID(),
    [UserId]             UNIQUEIDENTIFIER NULL,             -- 관리 주체 (Account.Users.UserId, 앱 계층 참조). 대리관리 시 보호자 UserId
    [Name]               VARCHAR(150)     NOT NULL,         -- UTF-8 (한글 50자)
    [Slug]               VARCHAR(150)     NOT NULL,         -- 공개 프로필 URL (/player/{slug}) — 이름 기반, 중복 시 -N (UTF-8)
    [PhotoUrl]           VARCHAR(2048)    NULL,             -- 선수 사진 (카드 뷰·프로필)
    [BirthDate]          DATE             NULL,
    [AgeGroup]           VARCHAR(20)      NULL,             -- 'U12','U15','U18'
    [Region]             VARCHAR(300)     NULL,

    -- 선수 대시보드 프로필 (공개 여부는 SoccerPlayerFieldVisibilities)
    [HeightCm]           INT              NULL,             -- 키(cm)
    [WeightKg]           INT              NULL,             -- 몸무게(kg)
    [PreferredFoot]      VARCHAR(20)      NULL,             -- 주발 'Left','Right','Both'
    [SchoolName]         VARCHAR(300)     NULL,             -- UTF-8 (한글 100자)
    [GuardianPhone]      VARCHAR(30)      NULL,             -- 보호자 연락처 (노출 시 서버에서 마스킹)
    [TeamId]             UNIQUEIDENTIFIER NULL,             -- 소속팀 (SoccerTeams.TeamId, 앱 계층 참조)
    [IsGuardianManaged]  BIT              NOT NULL DEFAULT 0,-- 학부모 대리 관리 프로필

    -- KFA 데이터 적재 대비 (결정 #5)
    [DataSource]         VARCHAR(20)      NOT NULL DEFAULT 'User', -- 'User','KfaApi','Seed'
    [ExternalId]         VARCHAR(64)      NULL,             -- 외부 시스템 멱등키

    [CreatedAt]          DATETIME2        NOT NULL DEFAULT GETUTCDATE(),
    [UpdatedAt]          DATETIME2        NOT NULL DEFAULT GETUTCDATE(),
    [DeletedAt]          DATETIME2        NULL,             -- 소프트 삭제

    CONSTRAINT [PK_SoccerPlayers] PRIMARY KEY ([PlayerId]),
    CONSTRAINT [UQ_SoccerPlayers_Slug] UNIQUE ([Slug])
);
