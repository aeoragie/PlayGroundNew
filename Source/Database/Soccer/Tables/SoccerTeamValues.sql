-- 팀 핵심가치. 대시보드 팀 정보 + 공개 홈페이지 소개 탭에 노출.
CREATE TABLE [dbo].[SoccerTeamValues]
(
    [TeamValueId]   UNIQUEIDENTIFIER NOT NULL DEFAULT NEWID(),
    [TeamId]        UNIQUEIDENTIFIER NOT NULL,          -- SoccerTeams.TeamId (앱 계층 참조)
    [Title]         VARCHAR(150)     NOT NULL,          -- UTF-8 (한글 50자) 예: '성장 중심 지도'
    [Description]   VARCHAR(600)     NOT NULL,          -- UTF-8 (한글 200자) 구체적 약속 설명
    [DisplayOrder]  INT              NOT NULL DEFAULT 0,

    [CreatedAt]     DATETIME2        NOT NULL DEFAULT GETUTCDATE(),
    [UpdatedAt]     DATETIME2        NOT NULL DEFAULT GETUTCDATE(),
    [DeletedAt]     DATETIME2        NULL,

    CONSTRAINT [PK_SoccerTeamValues] PRIMARY KEY ([TeamValueId])
);
