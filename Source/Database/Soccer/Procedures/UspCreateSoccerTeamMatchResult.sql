-- @entity: SoccerCreatedMatchRecord
-- @source: join
-- @join: SoccerMatches AS m (MatchId)
-- 팀 관리자의 친선경기 결과 입력 (팀 대시보드 "＋ 결과 입력").
-- **이 경로는 항상 친선경기만 저장한다** (설계 결정 7 / Design.FriendlyMatch) —
-- 대회·리그 공식 기록의 주체는 주최측이고, 팀에게는 읽기 전용이다.
-- 따라서 대회를 받지 않고(TournamentId 없음), 순위표 재계산도 하지 않는다.
-- 순위표는 MatchType='Official'만 집계하므로 여기서 저장한 경기는 순위에 영향을 주지 않는다.
--
-- 경기 1행 + 우리 팀 득점 이벤트 N행을 한 트랜잭션으로 저장한다.
-- 득점자는 우리 팀 것만 받는다(상대 팀 선수 명단을 우리가 알 수 없음). 상대 득점은 스코어로만 반영.
-- 결과셋 1개: 생성된 MatchId (팀이 없거나 필수값이 없으면 빈 결과셋).
-- 주의: 파라미터 줄에 꼬리 주석을 달면 제너레이터가 그 파라미터를 누락한다(기본값 없는 경우).
--       @Scorers JSON: [{"PlayerId":"...","PlayerName":"...","AssistPlayerId":null,"MinuteOfPlay":23}]
CREATE PROCEDURE [dbo].[UspCreateSoccerTeamMatchResult]
    @ManagerUserId  UNIQUEIDENTIFIER,
    @OpponentName   VARCHAR(300) = NULL,
    @IsHome         BIT = 1,
    @OurScore       INT = 0,
    @OpponentScore  INT = 0,
    @MatchedAt      DATETIME2 = NULL,
    @VenueName      VARCHAR(300) = NULL,
    @Scorers        VARCHAR(MAX) = NULL
AS
BEGIN
    SET NOCOUNT ON;

    IF @VenueName = ''
    BEGIN
        SET @VenueName = NULL;
    END

    IF @OpponentName IS NULL OR LEN(LTRIM(RTRIM(@OpponentName))) = 0 OR @MatchedAt IS NULL
    BEGIN
        RETURN;   -- 필수값 누락 — 빈 결과셋 (형식 검증은 Application에서 이미 끝났다)
    END

    DECLARE @TeamId UNIQUEIDENTIFIER = (
        SELECT TOP 1 [TeamId]
        FROM [dbo].[SoccerTeams] WITH (NOLOCK)
        WHERE [ManagerUserId] = @ManagerUserId AND [DeletedAt] IS NULL
        ORDER BY [CreatedAt] DESC);

    IF @TeamId IS NULL
    BEGIN
        RETURN;   -- 관리하는 팀이 없다 — 빈 결과셋
    END

    DECLARE @TeamName VARCHAR(300) = (
        SELECT [TeamName] FROM [dbo].[SoccerTeams] WITH (NOLOCK) WHERE [TeamId] = @TeamId);

    DECLARE @MatchId UNIQUEIDENTIFIER = NEWID();

    BEGIN TRY
        BEGIN TRANSACTION;

        -- TournamentId·StageType은 NULL (친선은 대회에 속하지 않는다)
        INSERT INTO [dbo].[SoccerMatches]
            ([MatchId], [MatchType],
             [HomeTeamId], [HomeTeamName], [AwayTeamId], [AwayTeamName],
             [HomeScore], [AwayScore], [Status], [MatchedAt], [VenueName], [DataSource])
        VALUES
            (@MatchId, 'Friendly',
             CASE WHEN @IsHome = 1 THEN @TeamId END,
             CASE WHEN @IsHome = 1 THEN @TeamName ELSE @OpponentName END,
             CASE WHEN @IsHome = 0 THEN @TeamId END,
             CASE WHEN @IsHome = 0 THEN @TeamName ELSE @OpponentName END,
             CASE WHEN @IsHome = 1 THEN @OurScore ELSE @OpponentScore END,
             CASE WHEN @IsHome = 1 THEN @OpponentScore ELSE @OurScore END,
             'Completed', @MatchedAt, @VenueName, 'User');

        -- 득점자(선택) — 우리 팀 이벤트만. 스코어와 개수가 달라도 저장은 허용한다(미상 득점 존재).
        IF @Scorers IS NOT NULL AND LEN(@Scorers) > 2
        BEGIN
            INSERT INTO [dbo].[SoccerMatchEvents]
                ([MatchId], [TeamId], [TeamName], [EventType], [PlayerId], [PlayerName], [AssistPlayerId], [AssistPlayerName], [MinuteOfPlay])
            SELECT
                @MatchId, @TeamId, @TeamName, 'Goal',
                s.[PlayerId], s.[PlayerName], s.[AssistPlayerId], s.[AssistPlayerName], s.[MinuteOfPlay]
            FROM OPENJSON(@Scorers)
            WITH (
                [PlayerId]          UNIQUEIDENTIFIER '$.PlayerId',
                [PlayerName]        VARCHAR(150)     '$.PlayerName',
                [AssistPlayerId]    UNIQUEIDENTIFIER '$.AssistPlayerId',
                [AssistPlayerName]  VARCHAR(150)     '$.AssistPlayerName',
                [MinuteOfPlay]      INT              '$.MinuteOfPlay'
            ) s;
        END

        COMMIT TRANSACTION;
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0
        BEGIN
            ROLLBACK TRANSACTION;
        END;

        THROW;   -- THROW 앞 문장은 반드시 세미콜론으로 끝나야 한다
    END CATCH

    SELECT @MatchId AS [MatchId];
END
