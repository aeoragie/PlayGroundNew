-- 득점 이벤트 (선수 득점·도움 집계 원천). 한 골 = 한 행, 도움은 같은 행의 Assist 컬럼.
-- 자책골(OwnGoal)은 상대 팀 득점 처리·개인 득점 미집계. 설계: Docs/Architecture/MatchSchemaDesign.md §3.3
CREATE TABLE [dbo].[SoccerMatchEvents]
(
    [EventId]           UNIQUEIDENTIFIER NOT NULL DEFAULT NEWID(),
    [MatchId]           UNIQUEIDENTIFIER NOT NULL,          -- SoccerMatches.MatchId (앱 계층 참조)
    [TeamId]            UNIQUEIDENTIFIER NULL,              -- 득점 팀 (SoccerTeams.TeamId, 외부 팀은 NULL)
    [TeamName]          VARCHAR(300)     NOT NULL,          -- UTF-8 (한글 100자)
    [EventType]         VARCHAR(20)      NOT NULL DEFAULT 'Goal', -- 'Goal','OwnGoal','PenaltyGoal'
    [PlayerId]          UNIQUEIDENTIFIER NULL,              -- 득점자 (SoccerPlayers.PlayerId, 외부 선수는 NULL)
    [PlayerName]        VARCHAR(150)     NULL,              -- UTF-8 (한글 50자) 미상은 NULL
    [AssistPlayerId]    UNIQUEIDENTIFIER NULL,              -- 도움 (없으면 NULL)
    [AssistPlayerName]  VARCHAR(150)     NULL,
    [MinuteOfPlay]      INT              NULL,              -- 표시용 ('23′)

    [CreatedAt]         DATETIME2        NOT NULL DEFAULT GETUTCDATE(),
    [UpdatedAt]         DATETIME2        NOT NULL DEFAULT GETUTCDATE(),
    [DeletedAt]         DATETIME2        NULL,

    CONSTRAINT [PK_SoccerMatchEvents] PRIMARY KEY ([EventId])
);
