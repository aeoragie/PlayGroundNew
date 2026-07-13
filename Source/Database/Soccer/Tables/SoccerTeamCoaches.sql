-- 팀 코칭스태프. 대시보드 팀 정보 + 공개 홈페이지 소개 탭에 노출.
CREATE TABLE [dbo].[SoccerTeamCoaches]
(
    [CoachId]       UNIQUEIDENTIFIER NOT NULL DEFAULT NEWID(),
    [TeamId]        UNIQUEIDENTIFIER NOT NULL,          -- SoccerTeams.TeamId (앱 계층 참조)
    [Name]          VARCHAR(150)     NOT NULL,          -- UTF-8 (한글 50자)
    [Role]          VARCHAR(60)      NOT NULL,          -- UTF-8 (한글 20자) 직책 ('감독','GK 코치')
    [Career]        VARCHAR(300)     NULL,              -- UTF-8 (한글 100자) 경력 한 줄
    [Certification] VARCHAR(100)     NULL,              -- 자격 ('KFA P2 인증')
    [Quote]         VARCHAR(600)     NULL,              -- UTF-8 (한글 200자) 지도 철학 인용문
    [Achievements]  VARCHAR(900)     NULL,              -- JSON 문자열 배열 (실적 칩) 예: ["프로 산하 이적 3명"]
    [InstagramUrl]  VARCHAR(2048)    NULL,
    [YoutubeUrl]    VARCHAR(2048)    NULL,
    [DisplayOrder]  INT              NOT NULL DEFAULT 0,

    [CreatedAt]     DATETIME2        NOT NULL DEFAULT GETUTCDATE(),
    [UpdatedAt]     DATETIME2        NOT NULL DEFAULT GETUTCDATE(),
    [DeletedAt]     DATETIME2        NULL,

    CONSTRAINT [PK_SoccerTeamCoaches] PRIMARY KEY ([CoachId])
);
