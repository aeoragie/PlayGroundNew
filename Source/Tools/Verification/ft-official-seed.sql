-- 기능 테스트 P2-8 — 공식 경기(주최측 흉내) 시드. **팀A·팀B·강민이 UI로 만들어진 뒤** 실행한다.
-- 테스트리그 2026(League) + 팀A vs 팀B 공식 경기(3:1) + 강민 출전·2골 + 순위표 재계산.
-- 팀은 ft-team-a/ft-team-b 관리자로 창단된 것을 이메일→UserId(Account)로 역추적해 찾는다.
SET NOCOUNT ON;

DECLARE @UserA UNIQUEIDENTIFIER = (SELECT UserId FROM PlayGround_Account.dbo.Users WHERE Email = 'ft-team-a@test.local');
DECLARE @UserB UNIQUEIDENTIFIER = (SELECT UserId FROM PlayGround_Account.dbo.Users WHERE Email = 'ft-team-b@test.local');
DECLARE @TeamA UNIQUEIDENTIFIER = (SELECT TOP 1 TeamId FROM SoccerTeams WHERE ManagerUserId = @UserA AND DeletedAt IS NULL ORDER BY CreatedAt);
DECLARE @TeamB UNIQUEIDENTIFIER = (SELECT TOP 1 TeamId FROM SoccerTeams WHERE ManagerUserId = @UserB AND DeletedAt IS NULL ORDER BY CreatedAt);
DECLARE @TeamAName VARCHAR(300) = (SELECT TeamName FROM SoccerTeams WHERE TeamId = @TeamA);
DECLARE @TeamBName VARCHAR(300) = (SELECT TeamName FROM SoccerTeams WHERE TeamId = @TeamB);
-- 강민 = 팀A 로스터의 FW (없으면 팀A 아무 선수)
DECLARE @Gangmin UNIQUEIDENTIFIER = (
    SELECT TOP 1 p.PlayerId FROM SoccerPlayers p
    JOIN SoccerTeamPlayers tp ON tp.PlayerId = p.PlayerId AND tp.TeamId = @TeamA AND tp.Status='Active' AND tp.DeletedAt IS NULL
    WHERE p.DeletedAt IS NULL ORDER BY CASE WHEN p.Name = N'강민' THEN 0 ELSE 1 END, tp.JerseyNumber);
DECLARE @GangminName VARCHAR(150) = (SELECT Name FROM SoccerPlayers WHERE PlayerId = @Gangmin);

IF @TeamA IS NULL OR @TeamB IS NULL OR @Gangmin IS NULL
BEGIN
    PRINT '중단: 팀A/팀B/강민을 찾지 못했다. 먼저 UI로 두 팀 창단 + 팀A 로스터를 만들어라.';
    PRINT CONCAT('  TeamA=', ISNULL(CONVERT(varchar(50),@TeamA),'NULL'), ' TeamB=', ISNULL(CONVERT(varchar(50),@TeamB),'NULL'), ' Gangmin=', ISNULL(CONVERT(varchar(50),@Gangmin),'NULL'));
    RETURN;
END

-- 재실행 안전 — 이전 시드 제거
DECLARE @OldT UNIQUEIDENTIFIER = (SELECT TOP 1 TournamentId FROM SoccerTournaments WHERE Name = N'테스트리그 2026');
IF @OldT IS NOT NULL
BEGIN
    DELETE FROM SoccerMatchEvents WHERE MatchId IN (SELECT MatchId FROM SoccerMatches WHERE TournamentId = @OldT);
    DELETE FROM SoccerMatchAppearances WHERE MatchId IN (SELECT MatchId FROM SoccerMatches WHERE TournamentId = @OldT);
    DELETE FROM SoccerTournamentStandings WHERE TournamentId = @OldT;
    DELETE FROM SoccerMatches WHERE TournamentId = @OldT;
    DELETE FROM SoccerTournaments WHERE TournamentId = @OldT;
END

DECLARE @T UNIQUEIDENTIFIER = NEWID();
INSERT INTO SoccerTournaments (TournamentId, SeasonYear, Name, SeriesSlug, Format, Scope, AgeGroup, RegionGroup, Status, StartDate, EndDate, HostName, DataSource)
VALUES (@T, 2026, N'테스트리그 2026', 'test-league', 'League', 'Regional', 'U15', N'서울', 'InProgress', '2026-03-01', '2026-11-30', N'테스트협회', 'Seed');

DECLARE @M UNIQUEIDENTIFIER = NEWID();
INSERT INTO SoccerMatches (MatchId, MatchType, TournamentId, StageType, RoundName, HomeTeamId, HomeTeamName, AwayTeamId, AwayTeamName, HomeScore, AwayScore, Status, MatchedAt, VenueName, DataSource)
VALUES (@M, 'Official', @T, 'League', 'R1', @TeamA, @TeamAName, @TeamB, @TeamBName, 3, 1, 'Completed', '2026-05-10 15:00', N'테스트구장', 'Seed');

-- 강민 출전 + 2골 (자녀2 공식 시즌통계·득점 확인용)
INSERT INTO SoccerMatchAppearances (MatchId, TeamId, PlayerId, MinutesPlayed, IsStarter)
VALUES (@M, @TeamA, @Gangmin, 90, 1);

INSERT INTO SoccerMatchEvents (MatchId, TeamId, TeamName, EventType, PlayerId, PlayerName, MinuteOfPlay)
VALUES (@M, @TeamA, @TeamAName, 'Goal', @Gangmin, @GangminName, 23),
       (@M, @TeamA, @TeamAName, 'Goal', @Gangmin, @GangminName, 67);

-- 순위표 재계산 (League 스코프)
EXEC UspRecalculateSoccerTournamentStandings @TournamentId = @T, @StageType = 'League', @GroupName = NULL, @DataSource = 'Seed';

PRINT '--- 시드 완료 ---';
SELECT s.Rank, s.TeamName, s.Played, s.Won, s.Drawn, s.Lost, s.Points
FROM SoccerTournamentStandings s WHERE s.TournamentId = @T ORDER BY s.Rank;
PRINT CONCAT('강민(', @GangminName, ') 공식 2골 · 출전 1경기 반영됨');
