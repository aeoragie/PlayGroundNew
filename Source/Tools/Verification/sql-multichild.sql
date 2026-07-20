-- 자녀 N명 검증 — 둘째를 연결해도 첫째가 살아 있어야 한다.
-- 예전 동작: UspClaimSoccerPlayerInvite가 기존 프로필을 무조건 소프트 삭제 → 첫째가 사라졌다.
-- 새 동작: 로스터에 없는 "온보딩 임시 프로필"만 병합·삭제하고, 이미 연결된 자녀는 건드리지 않는다.
SET NOCOUNT ON;

DECLARE @Guardian UNIQUEIDENTIFIER = 'A0000000-0000-0000-0000-00000000FF01';
DECLARE @TeamId UNIQUEIDENTIFIER = (SELECT TOP 1 TeamId FROM SoccerTeams WHERE ManagerUserId = 'A0000000-0000-0000-0000-000000000C11' AND DeletedAt IS NULL);

--.// 정리 (재실행 안전)
DELETE FROM SoccerPlayerInvites WHERE Code IN ('MCTEST1', 'MCTEST2');
DELETE FROM SoccerTeamPlayers WHERE PlayerId IN (SELECT PlayerId FROM SoccerPlayers WHERE Name LIKE 'MC-%');
DELETE FROM SoccerPlayers WHERE Name LIKE 'MC-%';

--.// 팀 로스터에 자녀 2명을 만든다 (팀이 만든 미연결 선수 = Unclaimed)
DECLARE @Child1 UNIQUEIDENTIFIER = NEWID(), @Child2 UNIQUEIDENTIFIER = NEWID();

INSERT INTO SoccerPlayers (PlayerId, UserId, Name, AgeGroup) VALUES
    (@Child1, NULL, 'MC-첫째', 'U12'),
    (@Child2, NULL, 'MC-둘째', 'U15');

INSERT INTO SoccerTeamPlayers (TeamId, PlayerId, JerseyNumber, Position, Status) VALUES
    (@TeamId, @Child1, '7', 'FW', 'Active'),
    (@TeamId, @Child2, '9', 'MF', 'Active');

INSERT INTO SoccerPlayerInvites (TeamId, PlayerId, Code, Status) VALUES
    (@TeamId, @Child1, 'MCTEST1', 'Pending'),
    (@TeamId, @Child2, 'MCTEST2', 'Pending');

--.// 첫째 연결
EXEC UspClaimSoccerPlayerInvite @UserId = @Guardian, @Code = 'MCTEST1';
PRINT '--- 첫째 연결 후 ---';
SELECT Name, CASE WHEN DeletedAt IS NULL THEN '살아있음' ELSE '삭제됨' END AS 상태
FROM SoccerPlayers WHERE Name LIKE 'MC-%' ORDER BY Name;

--.// 둘째 연결 — 여기서 첫째가 사라지면 실패다
EXEC UspClaimSoccerPlayerInvite @UserId = @Guardian, @Code = 'MCTEST2';
PRINT '--- 둘째 연결 후 (둘 다 살아있어야 정상) ---';
SELECT Name, CASE WHEN DeletedAt IS NULL THEN '살아있음' ELSE '삭제됨' END AS 상태
FROM SoccerPlayers WHERE Name LIKE 'MC-%' ORDER BY Name;

PRINT '--- 이 보호자가 관리하는 자녀 수 (2 기대) ---';
SELECT COUNT(*) AS 자녀수 FROM SoccerPlayers WHERE UserId = @Guardian AND DeletedAt IS NULL;

PRINT '--- 목록 조회 프로시저 ---';
EXEC UspGetSoccerPlayersByUser @UserId = @Guardian, @SeasonYear = 2026;

PRINT '--- PlayerId 지정 조회: 둘째만 나와야 한다 ---';
EXEC UspGetSoccerPlayerInfoByUser @UserId = @Guardian, @TargetPlayerId = @Child2;

PRINT '--- 남의 자녀 PlayerId 지정 (빈 결과 기대 — 소유 검사) ---';
DECLARE @Stranger UNIQUEIDENTIFIER = (SELECT TOP 1 PlayerId FROM SoccerPlayers WHERE UserId IS NOT NULL AND UserId <> @Guardian AND DeletedAt IS NULL);
EXEC UspGetSoccerPlayerInfoByUser @UserId = @Guardian, @TargetPlayerId = @Stranger;

--.// 정리
DELETE FROM SoccerPlayerInvites WHERE Code IN ('MCTEST1', 'MCTEST2');
DELETE FROM SoccerTeamPlayers WHERE PlayerId IN (@Child1, @Child2);
DELETE FROM SoccerPlayers WHERE PlayerId IN (@Child1, @Child2);
PRINT '--- 정리 완료 ---';
