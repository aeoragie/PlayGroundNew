-- 선수 포트폴리오 영상(외부 링크). 선수 대시보드 포트폴리오 섹션 + 공개 프로필 첫 화면(대표)에 노출.
CREATE TABLE [dbo].[SoccerPlayerPortfolioVideos]
(
    [VideoId]         UNIQUEIDENTIFIER NOT NULL DEFAULT NEWID(),
    [PlayerId]        UNIQUEIDENTIFIER NOT NULL,          -- SoccerPlayers.PlayerId (앱 계층 참조)
    [Title]           VARCHAR(300)     NOT NULL,          -- UTF-8 (한글 100자)
    [VideoUrl]        VARCHAR(2048)    NOT NULL,          -- 유튜브 등 외부 링크
    [ThumbnailUrl]    VARCHAR(2048)    NULL,
    [DurationSeconds] INT              NULL,              -- 영상 길이(초) — 표시는 m:ss
    [IsPrimary]       BIT              NOT NULL DEFAULT 0, -- 대표 영상 (선수당 1개 운영)
    [Tags]            VARCHAR(600)     NULL,              -- JSON 문자열 배열 예: ["#왼발 마무리","#침투"]
    [RecordedOn]      DATE             NULL,              -- 촬영/경기 일자 (목록 메타)

    [CreatedAt]       DATETIME2        NOT NULL DEFAULT GETUTCDATE(),
    [UpdatedAt]       DATETIME2        NOT NULL DEFAULT GETUTCDATE(),
    [DeletedAt]       DATETIME2        NULL,

    CONSTRAINT [PK_SoccerPlayerPortfolioVideos] PRIMARY KEY ([VideoId])
);
