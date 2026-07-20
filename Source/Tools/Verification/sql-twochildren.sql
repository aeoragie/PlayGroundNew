-- 자녀 2명 검증 계정 만들기 — 김정현(기존) 옆에 둘째를 붙인다.
-- 선수 대시보드 자녀 전환·허브 표시 조건 검증용. 되돌리려면 아래 ROLLBACK 절 참고.
SET NOCOUNT ON;

DECLARE @Guardian UNIQUEIDENTIFIER = 'A0000000-0000-0000-0000-000000000D11'; -- verify-player-u15 (김정현 보호자)
DECLARE @TeamId UNIQUEIDENTIFIER = (SELECT TOP 1 TeamId FROM SoccerTeams WHERE ManagerUserId = 'A0000000-0000-0000-0000-000000000C11' AND DeletedAt IS NULL);
DECLARE @Second UNIQUEIDENTIFIER = 'E0000000-0000-0000-0000-00000000C11D';

--.// 재실행 안전 — 이전 검증분 제거
DELETE FROM SoccerPlayerCareers WHERE PlayerId = @Second;
DELETE FROM SoccerPlayerPortfolioVideos WHERE PlayerId = @Second;
DELETE FROM SoccerTeamPlayers WHERE PlayerId = @Second;
DELETE FROM SoccerPlayers WHERE PlayerId = @Second;

--.// 둘째 자녀 (같은 보호자 계정이 관리)
INSERT INTO SoccerPlayers (PlayerId, UserId, Name, AgeGroup, IsGuardianManaged, BirthDate, HeightCm, PreferredFoot)
VALUES (@Second, @Guardian, '김서연', 'U12', 1, '2015-05-20', 148, 'Left');

INSERT INTO SoccerTeamPlayers (TeamId, PlayerId, JerseyNumber, Position, Grade, Status)
VALUES (@TeamId, @Second, '11', 'FW', '초6', 'Active');

--.// 자녀별로 다른 커리어를 넣어 전환이 실제로 갈아끼우는지 눈으로 확인
DECLARE @First UNIQUEIDENTIFIER = (SELECT TOP 1 PlayerId FROM SoccerPlayers WHERE UserId = @Guardian AND PlayerId <> @Second AND DeletedAt IS NULL);

DELETE FROM SoccerPlayerCareers WHERE Note LIKE 'TWOCHILD%';
INSERT INTO SoccerPlayerCareers (PlayerId, TeamName, IsCurrent, StartDate, Role, Note)
VALUES
    (@First,  '첫째전용FC', 1, '2024-03-01', 'GK', 'TWOCHILD 첫째 것'),
    (@Second, '둘째전용FC', 1, '2025-03-01', 'FW', 'TWOCHILD 둘째 것');

SELECT '자녀 목록' AS Step;
EXEC UspGetSoccerPlayersByUser @UserId = @Guardian;

SELECT '첫째 커리어' AS Step;
EXEC UspGetSoccerPlayerCareersByUser @UserId = @Guardian, @TargetPlayerId = @First;

SELECT '둘째 커리어' AS Step;
EXEC UspGetSoccerPlayerCareersByUser @UserId = @Guardian, @TargetPlayerId = @Second;

SELECT '지정 없음(첫 자녀여야 함)' AS Step;
EXEC UspGetSoccerPlayerCareersByUser @UserId = @Guardian;

/* 원복:
DELETE FROM SoccerPlayerCareers WHERE Note LIKE 'TWOCHILD%';
DELETE FROM SoccerTeamPlayers WHERE PlayerId = 'E0000000-0000-0000-0000-00000000C11D';
DELETE FROM SoccerPlayers WHERE PlayerId = 'E0000000-0000-0000-0000-00000000C11D';
*/
