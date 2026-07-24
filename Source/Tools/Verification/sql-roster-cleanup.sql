-- 로스터 쓰기 검증으로 만든 선수 물리 삭제 (api-roster.js·shot-roster.js는 소프트 삭제만 남긴다).
-- 검증 계정(검증fc)에 붙은 '검증추가선수'·'UI검증선수'를 SoccerPlayers·TeamPlayers·Invites에서 지운다.
SET NOCOUNT ON;

DECLARE @Ids TABLE (PlayerId UNIQUEIDENTIFIER);
INSERT INTO @Ids
SELECT PlayerId FROM SoccerPlayers WHERE Name IN ('검증추가선수', 'UI검증선수', '숫자아님', '연령오류');

DELETE FROM SoccerPlayerInvites WHERE PlayerId IN (SELECT PlayerId FROM @Ids);
DELETE FROM SoccerTeamPlayers  WHERE PlayerId IN (SELECT PlayerId FROM @Ids);
DELETE FROM SoccerPlayers      WHERE PlayerId IN (SELECT PlayerId FROM @Ids);

SELECT '남은 검증 선수' AS 항목, COUNT(*) AS 값
FROM SoccerPlayers WHERE Name IN ('검증추가선수', 'UI검증선수', '숫자아님', '연령오류');
