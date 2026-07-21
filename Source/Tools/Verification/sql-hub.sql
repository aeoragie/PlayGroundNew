-- 허브 표시 조건 검증용 데이터 (Design.DashboardHub 3단계).
--
-- 핵심 시나리오는 **팀 관리자이면서 보호자인 계정**이다 — 역할 컬럼이 단일값이라
-- 이 계정의 역할은 TeamAdmin 하나뿐인데도 선수 대시보드로 갈 수 있어야 한다.
-- (예전 역할 가드였다면 허브 ↔ 선수 대시보드를 무한히 오간다.)
--
-- 되돌리기: 이 파일 맨 아래 ROLLBACK 절을 주석 해제해 다시 실행.
SET NOCOUNT ON;

-- verify-teamadmin-0713 — 이 계정의 UserId는 랜덤 GUID라 PC마다 다르다. 실행 전 반드시 확인:
--   sqlcmd -S .\SQLEXPRESS -d PlayGround_Account -E -Q "SELECT UserId FROM Users WHERE Email='verify-teamadmin-0713@test.local'"
-- (2026-07-21 스테일 GUID로 삽입해 아무 화면에도 안 뜨는 데이터를 만든 적 있음 — 현재 값으로 갱신해 둠)
DECLARE @TeamAdmin UNIQUEIDENTIFIER = '7D5432C8-9B94-4011-A028-D59090A6D251';
DECLARE @HubKid UNIQUEIDENTIFIER = 'E0000000-0000-0000-0000-00000000B001';

PRINT '--- 사전 상태 ---';
SELECT '팀 수' AS 항목, COUNT(*) AS 값 FROM SoccerTeams WHERE ManagerUserId = @TeamAdmin AND DeletedAt IS NULL
UNION ALL
SELECT '자녀 수', COUNT(*) FROM SoccerPlayers WHERE UserId = @TeamAdmin AND DeletedAt IS NULL;

--.// 재실행 안전
DELETE FROM SoccerTeamPlayers WHERE PlayerId = @HubKid;
DELETE FROM SoccerPlayers WHERE PlayerId = @HubKid;

--.// 팀 관리자 계정에 자녀 1명을 붙인다 → 관리 대상 2개(팀 1 + 자녀 1) → 허브 표시
INSERT INTO SoccerPlayers (PlayerId, UserId, Name, AgeGroup, IsGuardianManaged, BirthDate, HeightCm, PreferredFoot)
VALUES (@HubKid, @TeamAdmin, '박하준', 'U15', 1, '2012-08-09', 165, 'Right');

DECLARE @TeamId UNIQUEIDENTIFIER =
    (SELECT TOP 1 TeamId FROM SoccerTeams WHERE ManagerUserId = @TeamAdmin AND DeletedAt IS NULL);

INSERT INTO SoccerTeamPlayers (TeamId, PlayerId, JerseyNumber, Position, Grade, Status)
VALUES (@TeamId, @HubKid, '77', 'MF', '중2', 'Active');

PRINT '--- 이후 상태 (관리 대상 2개 기대) ---';
SELECT '팀 수' AS 항목, COUNT(*) AS 값 FROM SoccerTeams WHERE ManagerUserId = @TeamAdmin AND DeletedAt IS NULL
UNION ALL
SELECT '자녀 수', COUNT(*) FROM SoccerPlayers WHERE UserId = @TeamAdmin AND DeletedAt IS NULL;

/* ROLLBACK — 검증 후 시드 상태로 되돌린다
DELETE FROM SoccerTeamPlayers WHERE PlayerId = 'E0000000-0000-0000-0000-00000000B001';
DELETE FROM SoccerPlayers WHERE PlayerId = 'E0000000-0000-0000-0000-00000000B001';
*/
