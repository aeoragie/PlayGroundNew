-- Claim 검증 데이터 원복 — 보호자 계정(verify-guardian-0721)이 만든 흔적 전부 + 검증용 경기·신청.
-- 시드 상태로 되돌린다: 선수 미연결, 코드 Pending, 요청·알림·가족 연결·친선경기·수정신청 삭제.
DECLARE @Guardian UNIQUEIDENTIFIER = (
    SELECT TOP 1 [ClaimedByUserId] FROM [dbo].[SoccerPlayerInvites] WHERE [ClaimedByUserId] IS NOT NULL AND [ClaimedAt] > '2026-07-21');

-- 보호자 UserId는 요청 테이블에서도 찾는다 (코드가 안 소진된 경우)
IF @Guardian IS NULL
    SET @Guardian = (SELECT TOP 1 [RequesterUserId] FROM [dbo].[SoccerPlayerClaimRequests] ORDER BY [CreatedAt] DESC);

-- 연결 해제 (검증fc 선수 중 이번에 연결된 것)
UPDATE p SET p.[UserId] = NULL, p.[IsGuardianManaged] = 0, p.[UpdatedAt] = GETUTCDATE()
FROM [dbo].[SoccerPlayers] p
WHERE p.[UserId] = @Guardian;

-- 가족 연결 삭제
DELETE FROM [dbo].[SoccerPlayerFamilyLinks] WHERE [UserId] = @Guardian;

-- 초대코드 원상 (Claimed → Pending)
UPDATE [dbo].[SoccerPlayerInvites]
SET [Status] = 'Pending', [ClaimedByUserId] = NULL, [ClaimedAt] = NULL
WHERE [ClaimedByUserId] = @Guardian;

-- 연결 요청·알림 전부 삭제 (검증에서 만든 것 — ClaimRequests가 이 검증 전엔 비어 있었다)
DELETE FROM [dbo].[SoccerPlayerClaimRequests];
DELETE FROM [dbo].[SoccerNotifications];

-- 검증용 친선경기 삭제
DELETE FROM [dbo].[SoccerMatches] WHERE [AwayTeamName] IN ('알림검증FC', '알림검증FC2') OR [HomeTeamName] IN ('알림검증FC', '알림검증FC2');

-- 검증용 기록 수정 신청 삭제
DELETE FROM [dbo].[SoccerRecordCorrections] WHERE [Description] = '검증용';

SELECT '원복 완료' AS [Result];
