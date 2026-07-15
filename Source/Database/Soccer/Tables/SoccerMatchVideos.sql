-- 경기 영상 (Records 미디어 탭 + 팀 대시보드 경기영상 공용). 선수 포트폴리오 영상과 별개 (소유·수명주기 다름).
-- 설계: Docs/Architecture/MatchSchemaDesign.md §3.6
CREATE TABLE [dbo].[SoccerMatchVideos]
(
    [VideoId]         UNIQUEIDENTIFIER NOT NULL DEFAULT NEWID(),
    [TournamentId]    UNIQUEIDENTIFIER NULL,              -- Records 미디어 탭 기준 (앱 계층 참조)
    [MatchId]         UNIQUEIDENTIFIER NULL,              -- 경기 연결 시 VS 배너(홈/원정 팀명) 구성
    [TeamId]          UNIQUEIDENTIFIER NULL,              -- 팀 대시보드 경기영상 기준 (대회 무관 팀 영상 허용)
    [Title]           VARCHAR(300)     NOT NULL,          -- UTF-8 (한글 100자)
    [VideoUrl]        VARCHAR(2048)    NOT NULL,
    [ThumbnailUrl]    VARCHAR(2048)    NULL,
    [VideoType]       VARCHAR(20)      NOT NULL DEFAULT 'Highlight', -- 'Highlight','FullMatch','Training'
    [DurationSeconds] INT              NULL,              -- 표시("1:42")는 클라이언트
    [RecordedOn]      DATE             NULL,

    [CreatedAt]       DATETIME2        NOT NULL DEFAULT GETUTCDATE(),
    [UpdatedAt]       DATETIME2        NOT NULL DEFAULT GETUTCDATE(),
    [DeletedAt]       DATETIME2        NULL,

    CONSTRAINT [PK_SoccerMatchVideos] PRIMARY KEY ([VideoId])
);
