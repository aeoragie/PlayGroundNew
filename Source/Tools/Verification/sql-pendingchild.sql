-- 허브 "승인 대기 자녀" 검증 시드 — 김정현 보호자(D11)가 김이든(검증fc) 연결을 신청한 상태를 만든다.
-- 김정현은 이미 연결된 자녀(Claimed) → 허브에 Claimed 1 + Pending 1 = 관리 대상 2 → 허브 표시.
-- 되돌리기: 맨 아래 ROLLBACK 절 참고.
SET NOCOUNT ON;

DECLARE @Guardian UNIQUEIDENTIFIER = 'A0000000-0000-0000-0000-000000000D11'; -- verify-player-u15 (김정현 보호자)
DECLARE @Player   UNIQUEIDENTIFIER = '684E4DF5-0058-4FC8-9CD5-7F0858F583EC'; -- 김이든
DECLARE @Invite   UNIQUEIDENTIFIER = 'D736BD1C-1F00-4889-AB4B-00ED8D26645E';
DECLARE @Team     UNIQUEIDENTIFIER = '5883886C-4CBC-4C95-9AFD-4FFD88680E72'; -- 검증fc

-- 재실행 안전 — 이전 검증 요청 제거
DELETE FROM SoccerPlayerClaimRequests WHERE RequesterUserId = @Guardian AND PlayerId = @Player;

INSERT INTO SoccerPlayerClaimRequests
    (InviteId, PlayerId, TeamId, RequesterUserId, RequesterName, Relation, Status, CreatedAt)
VALUES
    (@Invite, @Player, @Team, @Guardian, '김정현엄마', 'Mother', 'Pending', DATEADD(DAY, -1, GETUTCDATE()));

PRINT '--- 심은 Pending 요청 ---';
SELECT r.Status, p.Name AS Player, t.TeamName, r.CreatedAt
FROM SoccerPlayerClaimRequests r
JOIN SoccerPlayers p ON p.PlayerId = r.PlayerId
JOIN SoccerTeams t ON t.TeamId = r.TeamId
WHERE r.RequesterUserId = @Guardian AND r.PlayerId = @Player;

/* ROLLBACK — 검증 후 원복
DELETE FROM SoccerPlayerClaimRequests
WHERE RequesterUserId = 'A0000000-0000-0000-0000-000000000D11'
  AND PlayerId = '684E4DF5-0058-4FC8-9CD5-7F0858F583EC';
*/
