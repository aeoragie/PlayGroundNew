-- 팀. "팀 홈페이지 자동 생성"의 핵심 엔티티.
CREATE TABLE [dbo].[SoccerTeams]
(
    [TeamId]           UNIQUEIDENTIFIER NOT NULL DEFAULT NEWID(),
    [TeamName]         NVARCHAR(100)    NOT NULL,
    [ShortName]        NVARCHAR(20)     NULL,
    [TeamType]         NVARCHAR(20)     NULL,             -- '클럽','학교','학원'
    [Region]           NVARCHAR(100)    NULL,
    [AgeGroup]         VARCHAR(20)      NULL,             -- 'U12','U15','U18' 등
    [LogoUrl]          VARCHAR(2048)    NULL,
    [Description]      NVARCHAR(1000)   NULL,
    [Slug]             VARCHAR(100)     NULL,             -- 공개 홈페이지 URL 슬러그
    [ManagerUserId]    UNIQUEIDENTIFIER NULL,             -- 팀 관리자 (Account.Users.UserId, 앱 계층 참조)
    [IsPublicProfile]  BIT              NOT NULL DEFAULT 1,

    -- KFA 데이터 적재 대비 (결정 #5)
    [DataSource]       VARCHAR(20)      NOT NULL DEFAULT 'User', -- 'User','KfaApi','Seed'
    [ExternalId]       VARCHAR(64)      NULL,             -- 외부 시스템 멱등키 (KFA TeamId 등)

    [CreatedAt]        DATETIME2        NOT NULL DEFAULT GETUTCDATE(),
    [UpdatedAt]        DATETIME2        NOT NULL DEFAULT GETUTCDATE(),
    [DeletedAt]        DATETIME2        NULL,             -- 소프트 삭제

    CONSTRAINT [PK_SoccerTeams] PRIMARY KEY ([TeamId])
);
