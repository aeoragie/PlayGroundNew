-- 로컬 검증용 선수 계정 ↔ 리그 팀 선수 연결 (Account: VerificationPlayers.Seed.sql과 쌍).
-- 리그 시드(VerificationLeagueTeams.Seed.sql)가 Claimed 선수의 UserId를 NEWID() 더미로 넣으므로,
-- 두 명만 실제 검증 계정의 고정 GUID로 교체해 "본인 계정이 연결된 선수"를 만든다.
--   U12: 서울신답FCU12 등번호 1 신준우 ← verify-player-u12@test.local (…0D01)
--   U15: 광주광주FCU15 등번호 1 김정현 ← verify-player-u15@test.local (…0D11)
-- 리그 시드가 PlayerId를 재생성하므로 **리그 시드 재실행 후에는 이 스크립트도 재실행**한다.
-- 재실행 안전(UPDATE). 로컬 개발 DB 전용 — 운영 배포 금지.

UPDATE p
SET p.[UserId] = 'A0000000-0000-0000-0000-000000000D01'
FROM [dbo].[SoccerPlayers] p
JOIN [dbo].[SoccerTeamPlayers] tp ON tp.[PlayerId] = p.[PlayerId]
WHERE tp.[TeamId] = 'B0000000-0000-0000-0000-000000000001' -- 서울신답FCU12
  AND tp.[JerseyNumber] = '1';

IF @@ROWCOUNT <> 1
BEGIN
    RAISERROR('U12 target player not found. Run VerificationLeagueTeams.Seed.sql first.', 16, 1);
END

UPDATE p
SET p.[UserId] = 'A0000000-0000-0000-0000-000000000D11'
FROM [dbo].[SoccerPlayers] p
JOIN [dbo].[SoccerTeamPlayers] tp ON tp.[PlayerId] = p.[PlayerId]
WHERE tp.[TeamId] = 'B0000000-0000-0000-0000-000000000004' -- 광주광주FCU15
  AND tp.[JerseyNumber] = '1';

IF @@ROWCOUNT <> 1
BEGIN
    RAISERROR('U15 target player not found. Run VerificationLeagueTeams.Seed.sql first.', 16, 1);
END
