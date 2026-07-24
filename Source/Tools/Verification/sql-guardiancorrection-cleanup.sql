-- 보호자 기록 수정 신청 검증으로 만든 신청 물리 삭제 (스크립트는 소프트 삭제만 남긴다).
-- 검증 계정(김정현 보호자 D11)이 올린 Guardian 신청을 전부 지운다.
SET NOCOUNT ON;

DELETE FROM SoccerRecordCorrections
WHERE RequestedByUserId = 'A0000000-0000-0000-0000-000000000D11'
  AND RequestedByRole = 'Guardian';

SELECT '남은 보호자 검증 신청' AS 항목, COUNT(*) AS 값
FROM SoccerRecordCorrections
WHERE RequestedByUserId = 'A0000000-0000-0000-0000-000000000D11' AND RequestedByRole = 'Guardian';
