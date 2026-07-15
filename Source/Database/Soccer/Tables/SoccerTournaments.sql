-- 대회/리그 (통합 — Format으로 구분). 매년 새 행(SeasonYear별), SeriesSlug로 연도별 행을 묶는다.
-- 설계: Docs/Architecture/MatchSchemaDesign.md §3.1
CREATE TABLE [dbo].[SoccerTournaments]
(
    [TournamentId]     UNIQUEIDENTIFIER NOT NULL DEFAULT NEWID(),
    [SeasonYear]       INT              NOT NULL,          -- 시즌 = 연도 (아카이브 연도 칩 = DISTINCT)
    [Name]             VARCHAR(300)     NOT NULL,          -- UTF-8 (한글 100자)
    [SeriesSlug]       VARCHAR(100)     NULL,              -- 같은 대회의 연도별 행을 묶는 키 (역대 우승팀 조회)
    [Format]           VARCHAR(20)      NOT NULL,          -- 'Cup'(조별+토너먼트),'Split'(풀리그+스플릿),'League' — 상세 탭 구성 결정
    [Scope]            VARCHAR(20)      NOT NULL,          -- 'National'(전국),'Regional'(지방)
    [AgeGroup]         VARCHAR(20)      NOT NULL,          -- 'U12','U15','U18'
    [RegionGroup]      VARCHAR(60)      NULL,              -- UTF-8 (한글 20자) 리그 지역 그룹 ('서울','인천') — League만
    [Status]           VARCHAR(20)      NOT NULL DEFAULT 'Scheduled', -- 'Scheduled'(예정),'InProgress'(진행중),'Completed'(종료)
    [StartDate]        DATE             NULL,
    [EndDate]          DATE             NULL,
    [TeamCount]        INT              NULL,              -- 참가팀 수 (외부 적재 시 참가팀 전체가 없을 수 있어 저장)
    [HostName]         VARCHAR(300)     NULL,              -- UTF-8 (한글 100자) 주최
    [MethodText]       VARCHAR(600)     NULL,              -- UTF-8 (한글 200자) 개요 '방식'
    [MatchTimeText]    VARCHAR(600)     NULL,              -- UTF-8 (한글 200자) 개요 '경기시간'
    [VenueText]        VARCHAR(600)     NULL,              -- UTF-8 (한글 200자) 개요 '구장'
    [TiebreakText]     VARCHAR(600)     NULL,              -- UTF-8 (한글 200자) 개요 '순위 결정'
    [RegulationPdfUrl] VARCHAR(2048)    NULL,              -- 규정 PDF
    [SourceName]       VARCHAR(300)     NULL,              -- UTF-8 (한글 100자) 출처 표기
    [SourceUrl]        VARCHAR(2048)    NULL,

    -- 에이전트 축 선반영 (결정 #4) — API 가드·UI 숨김
    [OrganizerUserId]  UNIQUEIDENTIFIER NULL,              -- 개최자 (Account.Users.UserId, 앱 계층 참조)
    [OrganizerType]    VARCHAR(20)      NULL,              -- 'Platform','Agent','External'

    -- KFA 데이터 적재 대비 (결정 #5)
    [DataSource]       VARCHAR(20)      NOT NULL DEFAULT 'User', -- 'User','KfaApi','Seed'
    [ExternalId]       VARCHAR(64)      NULL,              -- 외부 시스템 멱등키
    [SyncStatus]       VARCHAR(20)      NULL,              -- 'Synced','Pending','Failed' (외부 소스만)

    [CreatedAt]        DATETIME2        NOT NULL DEFAULT GETUTCDATE(),
    [UpdatedAt]        DATETIME2        NOT NULL DEFAULT GETUTCDATE(),
    [DeletedAt]        DATETIME2        NULL,

    CONSTRAINT [PK_SoccerTournaments] PRIMARY KEY ([TournamentId])
);
