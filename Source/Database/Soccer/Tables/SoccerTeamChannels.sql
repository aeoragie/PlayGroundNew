-- 팀 공식 채널 (유튜브·인스타그램 등). 대시보드 팀 정보 + 공개 홈페이지에 노출.
CREATE TABLE [dbo].[SoccerTeamChannels]
(
    [ChannelId]     UNIQUEIDENTIFIER NOT NULL DEFAULT NEWID(),
    [TeamId]        UNIQUEIDENTIFIER NOT NULL,          -- SoccerTeams.TeamId (앱 계층 참조)
    [ChannelType]   VARCHAR(20)      NOT NULL,          -- 'YouTube','Instagram' (enum 멤버 이름 그대로)
    [Name]          VARCHAR(300)     NOT NULL,          -- UTF-8 (한글 100자) 채널명 또는 @핸들
    [Url]           VARCHAR(2048)    NOT NULL,
    [Description]   VARCHAR(300)     NULL,              -- UTF-8 (한글 100자) 예: '경기 하이라이트'
    [DisplayOrder]  INT              NOT NULL DEFAULT 0,

    [CreatedAt]     DATETIME2        NOT NULL DEFAULT GETUTCDATE(),
    [UpdatedAt]     DATETIME2        NOT NULL DEFAULT GETUTCDATE(),
    [DeletedAt]     DATETIME2        NULL,

    CONSTRAINT [PK_SoccerTeamChannels] PRIMARY KEY ([ChannelId])
);
