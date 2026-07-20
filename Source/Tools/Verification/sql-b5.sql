-- B5 집계 경계 검증: 친선 경기를 리그 스코프에 억지로 넣어도 순위표가 흔들리지 않아야 한다.
SET NOCOUNT ON;

DECLARE @T UNIQUEIDENTIFIER = (SELECT TOP 1 TournamentId FROM SoccerTournaments WHERE Format = 'League' AND DeletedAt IS NULL);
DECLARE @Team VARCHAR(300) = (SELECT TOP 1 TeamName FROM SoccerTournamentStandings WHERE TournamentId = @T AND DeletedAt IS NULL ORDER BY TeamRank);

PRINT '--- 재계산 전 (기존 순위표) ---';
SELECT TeamRank, TeamName, Played, Won, Drawn, Lost, Points, GoalsFor, GoalsAgainst
FROM SoccerTournamentStandings WHERE TournamentId = @T AND DeletedAt IS NULL ORDER BY TeamRank;

-- 같은 리그 스코프에 '친선' 경기를 한 건 밀어 넣는다 (10:0 — 집계되면 티가 확 난다)
DECLARE @Fake UNIQUEIDENTIFIER = NEWID();
INSERT INTO SoccerMatches
    (MatchId, MatchType, TournamentId, StageType, HomeTeamName, AwayTeamName,
     HomeScore, AwayScore, Status, MatchedAt, DataSource)
VALUES
    (@Fake, 'Friendly', @T, 'League', @Team, 'B5 침입 테스트팀',
     10, 0, 'Completed', '2026-07-20 15:00', 'User');

EXEC UspRecalculateSoccerTournamentStandings @TournamentId = @T, @StageType = 'League', @GroupName = NULL, @DataSource = 'User';

PRINT '--- 친선 1건(10:0) 삽입 + 재계산 후 ---';
SELECT TeamRank, TeamName, Played, Won, Drawn, Lost, Points, GoalsFor, GoalsAgainst
FROM SoccerTournamentStandings WHERE TournamentId = @T AND DeletedAt IS NULL ORDER BY TeamRank;

PRINT '--- 침입 테스트팀이 순위표에 생겼는가 (0이어야 정상) ---';
SELECT COUNT(*) AS IntruderRows FROM SoccerTournamentStandings
WHERE TournamentId = @T AND TeamName = 'B5 침입 테스트팀' AND DeletedAt IS NULL;

-- 같은 경기를 공식으로 바꾸면 이번엔 반영돼야 한다 (필터가 실제로 동작하는지 반대 방향 확인)
UPDATE SoccerMatches SET MatchType = 'Official' WHERE MatchId = @Fake;
EXEC UspRecalculateSoccerTournamentStandings @TournamentId = @T, @StageType = 'League', @GroupName = NULL, @DataSource = 'User';

PRINT '--- 같은 경기를 Official로 바꾼 뒤 (이제는 반영되어야 함) ---';
SELECT COUNT(*) AS IntruderRows FROM SoccerTournamentStandings
WHERE TournamentId = @T AND TeamName = 'B5 침입 테스트팀' AND DeletedAt IS NULL;

-- 원상 복구
DELETE FROM SoccerMatches WHERE MatchId = @Fake;
DELETE FROM SoccerTournamentStandings WHERE TournamentId = @T AND TeamName = 'B5 침입 테스트팀';
EXEC UspRecalculateSoccerTournamentStandings @TournamentId = @T, @StageType = 'League', @GroupName = NULL, @DataSource = 'User';

PRINT '--- 복구 후 (맨 처음과 같아야 함) ---';
SELECT TeamRank, TeamName, Played, Won, Drawn, Lost, Points, GoalsFor, GoalsAgainst
FROM SoccerTournamentStandings WHERE TournamentId = @T AND DeletedAt IS NULL ORDER BY TeamRank;
