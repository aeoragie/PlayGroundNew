-- 팀. "팀 홈페이지 자동 생성"의 핵심 엔티티.
CREATE TABLE [dbo].[SoccerTeams]
(
    [TeamId]           UNIQUEIDENTIFIER NOT NULL DEFAULT NEWID(),
    [TeamName]         VARCHAR(300)     NOT NULL,         -- UTF-8 (한글 100자)
    [ShortName]        VARCHAR(60)      NULL,
    [TeamType]         VARCHAR(60)      NULL,             -- '클럽','학교','학원'
    [Region]           VARCHAR(300)     NULL,
    [AgeGroup]         VARCHAR(20)      NULL,             -- 'U12','U15','U18' 등
    [LogoUrl]          VARCHAR(2048)    NULL,
    [CoverImageUrl]    VARCHAR(2048)    NULL,             -- 공개 홈페이지 히어로 커버 사진
    [Description]      VARCHAR(3000)    NULL,
    [Slug]             VARCHAR(100)     NULL,             -- 공개 홈페이지 URL 슬러그
    [ManagerUserId]    UNIQUEIDENTIFIER NULL,             -- 팀 관리자 (Account.Users.UserId, 앱 계층 참조)
    [IsPublicProfile]  BIT              NOT NULL DEFAULT 1,

    -- 대시보드 팀 정보 (공개 홈페이지 소개 탭과 공유)
    [IsVerified]         BIT            NOT NULL DEFAULT 0, -- 인증팀 뱃지
    [IsRecruiting]       BIT            NOT NULL DEFAULT 0, -- **용도 종료** — 모집 공고(SoccerTeamRecruitments)에서 파생으로 대체됨. 읽는 코드 없음 (컬럼 제거는 마이그레이션 비용 대비 보류)
    [FoundedYear]        INT            NULL,              -- 창단연도
    [MonthlyFee]         INT            NULL,              -- 월 회비(원)
    [IsMonthlyFeePublic] BIT            NOT NULL DEFAULT 1, -- 회비 공개 여부
    [TrainingDays]       VARCHAR(60)    NULL,              -- UTF-8 (한글 20자) 훈련 요일 ('화목금토')

    -- KFA 데이터 적재 대비 (결정 #5)
    [DataSource]       VARCHAR(20)      NOT NULL DEFAULT 'User', -- 'User','KfaApi','Seed'
    [ExternalId]       VARCHAR(64)      NULL,             -- 외부 시스템 멱등키 (KFA TeamId 등)

    [CreatedAt]        DATETIME2        NOT NULL DEFAULT GETUTCDATE(),
    [UpdatedAt]        DATETIME2        NOT NULL DEFAULT GETUTCDATE(),
    [DeletedAt]        DATETIME2        NULL,             -- 소프트 삭제

    CONSTRAINT [PK_SoccerTeams] PRIMARY KEY ([TeamId])
);
