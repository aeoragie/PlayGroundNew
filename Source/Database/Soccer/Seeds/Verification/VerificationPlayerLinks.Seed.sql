-- 로컬 검증용 선수 계정 ↔ 검증fc 로스터 연결 (Account: Verification/VerificationPlayers.Seed.sql과 쌍).
-- 검증fc 로스터(VerificationRoster.Seed.sql)의 김정현(#1 GK U15)·신준우(#7 MF U12)를
-- 검증 계정의 고정 GUID로 연결해 "본인 계정이 연결된 선수"를 만든다.
--   U15: 검증fc 등번호 1 김정현 ← verify-player-u15@test.local (…0D11)
--   U12: 검증fc 등번호 7 신준우 ← verify-player-u12@test.local (…0D01)
-- 로스터가 PlayerId를 재생성(NEWID)하므로 **로스터 재실행 후에는 이 스크립트도 재실행**한다.
-- 재실행 안전(UPDATE). 로컬 개발 DB 전용 — 운영 배포 금지.

DECLARE @TeamId UNIQUEIDENTIFIER =
    (SELECT TOP 1 [TeamId] FROM [dbo].[SoccerTeams] WHERE [TeamName] = '검증fc' AND [DeletedAt] IS NULL);

IF @TeamId IS NULL
BEGIN
    RAISERROR('Team ''검증fc'' not found — run VerificationTeamInfo/Roster first.', 16, 1);
    RETURN;
END

UPDATE p
SET p.[UserId] = 'A0000000-0000-0000-0000-000000000D11'
FROM [dbo].[SoccerPlayers] p
JOIN [dbo].[SoccerTeamPlayers] tp ON tp.[PlayerId] = p.[PlayerId]
WHERE tp.[TeamId] = @TeamId AND tp.[JerseyNumber] = '1'; -- 김정현

IF @@ROWCOUNT <> 1
BEGIN
    RAISERROR('U15 target (검증fc #1) not found. Run VerificationRoster.Seed.sql first.', 16, 1);
END

UPDATE p
SET p.[UserId] = 'A0000000-0000-0000-0000-000000000D01'
FROM [dbo].[SoccerPlayers] p
JOIN [dbo].[SoccerTeamPlayers] tp ON tp.[PlayerId] = p.[PlayerId]
WHERE tp.[TeamId] = @TeamId AND tp.[JerseyNumber] = '7'; -- 신준우

IF @@ROWCOUNT <> 1
BEGIN
    RAISERROR('U12 target (검증fc #7) not found. Run VerificationRoster.Seed.sql first.', 16, 1);
END
