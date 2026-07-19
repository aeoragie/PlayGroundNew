-- @entity: SoccerCreatedMatchRecord
-- @source: join
-- @join: SoccerMatches AS m (MatchId)
-- 팀 관리자의 경기 결과 입력 (팀 대시보드 "＋ 결과 입력").
-- 경기 1행 + 우리 팀 득점 이벤트 N행을 한 트랜잭션으로 저장하고,
-- **대회 경기면 UspRecalculateSoccerTournamentStandings를 반드시 이어서 호출한다** (D5 확정안).
-- 수동 재계산에 의존하지 않는다 — 저장 경로가 곧 순위표 갱신 경로다.
--
-- 득점자는 우리 팀 것만 받는다(상대 팀 선수 명단을 우리가 알 수 없음). 상대 득점은 스코어로만 반영.
-- 결과셋 1개: 생성된 MatchId (팀이 없거나 권한 없으면 빈 결과셋).
-- 주의: 파라미터 줄에 꼬리 주석을 달면 제너레이터가 그 파라미터를 누락한다(기본값 없는 경우).
--       @TournamentId 빈 GUID = 친선 (제너레이터가 = NULL 파라미터를 non-nullable로 만들어 NULL을 못 보낸다)
--       @Scorers JSON: [{"PlayerId":"...","PlayerName":"...","AssistPlayerId":null,"MinuteOfPlay":23}]
CREATE PROCEDURE [dbo].[UspCreateSoccerTeamMatchResult]
    @ManagerUserId  UNIQUEIDENTIFIER,
    @TournamentId   UNIQUEIDENTIFIER = NULL,
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

    -- 빈 GUID·빈 문자열은 "값 없음"으로 정규화 (제너레이터 파라미터 타입 한계 보정)
    IF @TournamentId = '00000000-0000-0000-0000-000000000000'
    BEGIN
        SET @TournamentId = NULL;
    END

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

    -- 순위표 재계산은 스코프(대회+스테이지+조)가 특정될 때만 의미가 있다.
    -- League = 단일 스코프라 확정 가능 / Cup·Split = 조(GroupName)를 입력받기 전까지 특정 불가 →
    -- 없는 스코프를 만들어 엉뚱한 순위표를 찍느니 경기만 저장하고 재계산은 건너뛴다.
    DECLARE @StageType VARCHAR(20) = NULL;
    DECLARE @Format VARCHAR(20) = NULL;

    IF @TournamentId IS NOT NULL
    BEGIN
        SELECT @Format = [Format]
        FROM [dbo].[SoccerTournaments] WITH (NOLOCK)
        WHERE [TournamentId] = @TournamentId AND [DeletedAt] IS NULL;

        IF @Format IS NULL
        BEGIN
            RETURN;   -- 없는 대회 — 빈 결과셋
        END

        SET @StageType = CASE WHEN @Format = 'League' THEN 'League' ELSE 'Group' END;
    END

    DECLARE @MatchId UNIQUEIDENTIFIER = NEWID();

    BEGIN TRY
        BEGIN TRANSACTION;

        INSERT INTO [dbo].[SoccerMatches]
            ([MatchId], [TournamentId], [StageType],
             [HomeTeamId], [HomeTeamName], [AwayTeamId], [AwayTeamName],
             [HomeScore], [AwayScore], [Status], [MatchedAt], [VenueName], [DataSource])
        VALUES
            (@MatchId, @TournamentId, @StageType,
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

    -- 순위표 재계산 (D5 필수 경로). 트랜잭션 밖 — 재계산 실패가 경기 저장을 되돌리지 않는다.
    -- 수동 재계산에 의존하는 경로를 만들지 않는다: 리그 결과는 저장과 동시에 순위표가 최신이 된다.
    IF @TournamentId IS NOT NULL AND @StageType = 'League'
    BEGIN
        EXEC [dbo].[UspRecalculateSoccerTournamentStandings]
            @TournamentId = @TournamentId,
            @StageType = @StageType,
            @GroupName = NULL,
            @DataSource = 'User';
    END

    SELECT @MatchId AS [MatchId];
END
