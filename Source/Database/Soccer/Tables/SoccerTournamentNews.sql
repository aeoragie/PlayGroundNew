-- 대회 뉴스 링크 (Records 미디어 탭 '인터넷 뉴스' 서브탭). 설계: Docs/Architecture/MatchSchemaDesign.md §3.7
CREATE TABLE [dbo].[SoccerTournamentNews]
(
    [NewsId]        UNIQUEIDENTIFIER NOT NULL DEFAULT NEWID(),
    [TournamentId]  UNIQUEIDENTIFIER NOT NULL,          -- SoccerTournaments.TournamentId (앱 계층 참조)
    [Title]         VARCHAR(300)     NOT NULL,          -- UTF-8 (한글 100자)
    [Url]           VARCHAR(2048)    NOT NULL,
    [PublisherName] VARCHAR(150)     NULL,              -- UTF-8 (한글 50자) 매체명
    [PublishedOn]   DATE             NULL,

    [CreatedAt]     DATETIME2        NOT NULL DEFAULT GETUTCDATE(),
    [UpdatedAt]     DATETIME2        NOT NULL DEFAULT GETUTCDATE(),
    [DeletedAt]     DATETIME2        NULL,

    CONSTRAINT [PK_SoccerTournamentNews] PRIMARY KEY ([NewsId])
);
