-- 기능 테스트 종료 후 ft-* 데이터 전량 삭제 (물리). Soccer 도메인 → Account 순.
-- ft-team-a/b·ft-parent·ft-player·ft-general·ft-agent와 그들이 만든 팀·선수·경기·요청·알림 제거.
SET NOCOUNT ON;

DECLARE @Users TABLE (UserId UNIQUEIDENTIFIER);
INSERT INTO @Users SELECT UserId FROM PlayGround_Account.dbo.Users WHERE Email LIKE 'ft-%@test.local';

-- 팀 관련
DECLARE @Teams TABLE (TeamId UNIQUEIDENTIFIER);
INSERT INTO @Teams SELECT TeamId FROM SoccerTeams WHERE ManagerUserId IN (SELECT UserId FROM @Users);

-- 공식 시드 대회
DECLARE @Tourn TABLE (TournamentId UNIQUEIDENTIFIER);
INSERT INTO @Tourn SELECT TournamentId FROM SoccerTournaments WHERE Name = N'테스트리그 2026';

-- 경기/이벤트/출전/순위표 (팀 또는 시드 대회 관련)
DECLARE @Matches TABLE (MatchId UNIQUEIDENTIFIER);
INSERT INTO @Matches
    SELECT MatchId FROM SoccerMatches
    WHERE TournamentId IN (SELECT TournamentId FROM @Tourn)
       OR HomeTeamId IN (SELECT TeamId FROM @Teams) OR AwayTeamId IN (SELECT TeamId FROM @Teams);

DELETE FROM SoccerMatchEvents WHERE MatchId IN (SELECT MatchId FROM @Matches);
DELETE FROM SoccerMatchAppearances WHERE MatchId IN (SELECT MatchId FROM @Matches);
DELETE FROM SoccerMatchVideos WHERE MatchId IN (SELECT MatchId FROM @Matches);
DELETE FROM SoccerMatches WHERE MatchId IN (SELECT MatchId FROM @Matches);
DELETE FROM SoccerTournamentStandings WHERE TournamentId IN (SELECT TournamentId FROM @Tourn);
DELETE FROM SoccerTournaments WHERE TournamentId IN (SELECT TournamentId FROM @Tourn);

-- 선수(팀 소속 + 계정 소유) 및 부속
DECLARE @Players TABLE (PlayerId UNIQUEIDENTIFIER);
INSERT INTO @Players
    SELECT PlayerId FROM SoccerPlayers WHERE UserId IN (SELECT UserId FROM @Users)
    UNION SELECT tp.PlayerId FROM SoccerTeamPlayers tp WHERE tp.TeamId IN (SELECT TeamId FROM @Teams);

DELETE FROM SoccerRecordCorrections WHERE RequestedByUserId IN (SELECT UserId FROM @Users);
DELETE FROM SoccerPlayerClaimRequests WHERE RequesterUserId IN (SELECT UserId FROM @Users);
DELETE FROM SoccerNotifications WHERE RecipientUserId IN (SELECT UserId FROM @Users);
DELETE FROM SoccerPlayerInvites WHERE PlayerId IN (SELECT PlayerId FROM @Players);
DELETE FROM SoccerPlayerFamilyLinks WHERE PlayerId IN (SELECT PlayerId FROM @Players) OR UserId IN (SELECT UserId FROM @Users);
DELETE FROM SoccerPlayerCareers WHERE PlayerId IN (SELECT PlayerId FROM @Players);
DELETE FROM SoccerPlayerPortfolioVideos WHERE PlayerId IN (SELECT PlayerId FROM @Players);
DELETE FROM SoccerPlayerFieldVisibilities WHERE PlayerId IN (SELECT PlayerId FROM @Players);
DELETE FROM SoccerTeamPlayers WHERE TeamId IN (SELECT TeamId FROM @Teams) OR PlayerId IN (SELECT PlayerId FROM @Players);
DELETE FROM SoccerPlayers WHERE PlayerId IN (SELECT PlayerId FROM @Players);

-- 팀 부속 + 팀
DELETE FROM SoccerTeamValues WHERE TeamId IN (SELECT TeamId FROM @Teams);
DELETE FROM SoccerTeamCoaches WHERE TeamId IN (SELECT TeamId FROM @Teams);
DELETE FROM SoccerTeamChannels WHERE TeamId IN (SELECT TeamId FROM @Teams);
DELETE FROM SoccerTeamRecruitments WHERE TeamId IN (SELECT TeamId FROM @Teams);
DELETE FROM SoccerTeamCareerOutcomes WHERE TeamId IN (SELECT TeamId FROM @Teams);
DELETE FROM SoccerTeamReviews WHERE TeamId IN (SELECT TeamId FROM @Teams);
DELETE FROM SoccerTeams WHERE TeamId IN (SELECT TeamId FROM @Teams);

-- 계정 (별도 DB)
DELETE FROM PlayGround_Account.dbo.SocialAccounts WHERE UserId IN (SELECT UserId FROM @Users);
DELETE FROM PlayGround_Account.dbo.NotificationPreferences WHERE UserId IN (SELECT UserId FROM @Users);
DELETE FROM PlayGround_Account.dbo.Users WHERE UserId IN (SELECT UserId FROM @Users);

SELECT '남은 ft 계정' AS 항목, COUNT(*) AS 값 FROM PlayGround_Account.dbo.Users WHERE Email LIKE 'ft-%@test.local';
