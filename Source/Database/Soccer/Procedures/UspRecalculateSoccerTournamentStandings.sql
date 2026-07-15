-- 순위표 자동 재계산 (D5 확정안 — MatchSchemaDesign.md §5). 결과셋 없음.
-- 스코프(대회+스테이지+조) 단위: Completed 경기에서 승점(승3·무1·패0)·득실을 집계해
-- 기존 행 갱신 + 신규 팀 삽입 후, 스코프 전체(0전 보존 행 포함) 순위를 재부여한다.
-- 정렬 = 승점 → 득실차 → 다득점 → 팀명. 승자승 등 특수 규칙·IsQualified는 수동 보정 영역 — 건드리지 않는다.
-- 호출: 경기 결과 저장 유즈케이스(자동) / 추후 Agent 대시보드 버튼(수동).
CREATE PROCEDURE [dbo].[UspRecalculateSoccerTournamentStandings]
    @TournamentId UNIQUEIDENTIFIER,
    @StageType VARCHAR(20),
    @GroupName VARCHAR(30) = NULL,
    @DataSource VARCHAR(20) = 'User'   -- 신규 삽입 행에만 적용 ('User','KfaApi','Seed')
AS
BEGIN
    SET NOCOUNT ON;

    -- 팀별 집계 (홈/원정 관점 UNION, 승부차기는 정규시간 스코어 기준 무승부)
    ;WITH MatchSides AS
    (
        SELECT
            m.[HomeTeamId] AS TeamId, m.[HomeTeamName] AS TeamName,
            m.[HomeScore] AS GoalsFor, m.[AwayScore] AS GoalsAgainst
        FROM [dbo].[SoccerMatches] m WITH (NOLOCK)
        WHERE m.[TournamentId] = @TournamentId AND m.[StageType] = @StageType
          AND ((@GroupName IS NULL AND m.[GroupName] IS NULL) OR m.[GroupName] = @GroupName)
          AND m.[Status] = 'Completed' AND m.[DeletedAt] IS NULL
          AND m.[HomeScore] IS NOT NULL AND m.[AwayScore] IS NOT NULL

        UNION ALL

        SELECT
            m.[AwayTeamId], m.[AwayTeamName],
            m.[AwayScore], m.[HomeScore]
        FROM [dbo].[SoccerMatches] m WITH (NOLOCK)
        WHERE m.[TournamentId] = @TournamentId AND m.[StageType] = @StageType
          AND ((@GroupName IS NULL AND m.[GroupName] IS NULL) OR m.[GroupName] = @GroupName)
          AND m.[Status] = 'Completed' AND m.[DeletedAt] IS NULL
          AND m.[HomeScore] IS NOT NULL AND m.[AwayScore] IS NOT NULL
    ),
    Totals AS
    (
        SELECT
            [TeamName],
            MAX([TeamId]) AS TeamId,   -- 같은 팀명은 같은 TeamId 가정 (대회 내 팀명 유일)
            COUNT(*) AS Played,
            SUM(CASE WHEN [GoalsFor] > [GoalsAgainst] THEN 1 ELSE 0 END) AS Won,
            SUM(CASE WHEN [GoalsFor] = [GoalsAgainst] THEN 1 ELSE 0 END) AS Drawn,
            SUM(CASE WHEN [GoalsFor] < [GoalsAgainst] THEN 1 ELSE 0 END) AS Lost,
            SUM([GoalsFor]) AS GoalsFor,
            SUM([GoalsAgainst]) AS GoalsAgainst
        FROM MatchSides
        GROUP BY [TeamName]
    )
    SELECT * INTO #Totals FROM Totals;

    --.// 기존 행 갱신 (TeamName 키)

    UPDATE s
    SET s.[TeamId] = COALESCE(s.[TeamId], t.[TeamId]),
        s.[Played] = t.[Played],
        s.[Won] = t.[Won],
        s.[Drawn] = t.[Drawn],
        s.[Lost] = t.[Lost],
        s.[Points] = t.[Won] * 3 + t.[Drawn],
        s.[GoalsFor] = t.[GoalsFor],
        s.[GoalsAgainst] = t.[GoalsAgainst],
        s.[UpdatedAt] = GETUTCDATE()
    FROM [dbo].[SoccerTournamentStandings] s
    JOIN #Totals t ON t.[TeamName] = s.[TeamName]
    WHERE s.[TournamentId] = @TournamentId AND s.[StageType] = @StageType
      AND ((@GroupName IS NULL AND s.[GroupName] IS NULL) OR s.[GroupName] = @GroupName)
      AND s.[DeletedAt] IS NULL;

    --.// 신규 팀 삽입

    INSERT INTO [dbo].[SoccerTournamentStandings]
        ([TournamentId], [StageType], [GroupName], [TeamId], [TeamName], [TeamRank],
         [Played], [Won], [Drawn], [Lost], [Points], [GoalsFor], [GoalsAgainst], [DataSource])
    SELECT
        @TournamentId, @StageType, @GroupName, t.[TeamId], t.[TeamName], 0,
        t.[Played], t.[Won], t.[Drawn], t.[Lost], t.[Won] * 3 + t.[Drawn], t.[GoalsFor], t.[GoalsAgainst], @DataSource
    FROM #Totals t
    WHERE NOT EXISTS (
        SELECT 1 FROM [dbo].[SoccerTournamentStandings] s
        WHERE s.[TournamentId] = @TournamentId AND s.[StageType] = @StageType
          AND ((@GroupName IS NULL AND s.[GroupName] IS NULL) OR s.[GroupName] = @GroupName)
          AND s.[TeamName] = t.[TeamName] AND s.[DeletedAt] IS NULL);

    --.// 스코프 전체 순위 재부여 (0전 보존 행 포함)

    ;WITH Ranked AS
    (
        SELECT
            s.[StandingId],
            ROW_NUMBER() OVER (ORDER BY s.[Points] DESC, s.[GoalsFor] - s.[GoalsAgainst] DESC, s.[GoalsFor] DESC, s.[TeamName]) AS NewRank
        FROM [dbo].[SoccerTournamentStandings] s
        WHERE s.[TournamentId] = @TournamentId AND s.[StageType] = @StageType
          AND ((@GroupName IS NULL AND s.[GroupName] IS NULL) OR s.[GroupName] = @GroupName)
          AND s.[DeletedAt] IS NULL
    )
    UPDATE s
    SET s.[TeamRank] = r.NewRank, s.[UpdatedAt] = GETUTCDATE()
    FROM [dbo].[SoccerTournamentStandings] s
    JOIN Ranked r ON r.[StandingId] = s.[StandingId]
    WHERE s.[TeamRank] <> r.NewRank;

    DROP TABLE #Totals;
END
