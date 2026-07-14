-- 로컬 검증용 선수단 시드 — '검증fc' 로스터를 실감 데이터(사진·연령 그룹·Claim 상태)로 재구성.
-- 선행 조건: 검증 계정·팀 생성 (VerificationTeamInfo.Seed.sql 헤더 참조).
-- 재실행 안전: 검증fc의 기존 로스터(선수 본체 포함)를 지우고 다시 삽입 — 이 시드가 로스터의 단일 소스.
-- Claimed 선수의 UserId는 NEWID() 표시용 더미 (Account에 실제 사용자 없음 — 뱃지 확인 목적).
-- 로컬 개발 DB 전용 — 운영 배포 금지.
DECLARE @TeamId UNIQUEIDENTIFIER =
    (SELECT TOP 1 [TeamId] FROM [dbo].[SoccerTeams] WHERE [TeamName] = '검증fc' AND [DeletedAt] IS NULL);

IF @TeamId IS NULL
BEGIN
    RAISERROR ('Team ''검증fc'' not found — create the verification account/team via onboarding first.', 16, 1);
    RETURN;
END

--.// 기존 로스터 제거 (invites → 소속 → 선수 본체 순)

DECLARE @old TABLE ([PlayerId] UNIQUEIDENTIFIER);
INSERT INTO @old SELECT [PlayerId] FROM [dbo].[SoccerTeamPlayers] WHERE [TeamId] = @TeamId;

DELETE FROM [dbo].[SoccerPlayerInvites] WHERE [TeamId] = @TeamId;
DELETE FROM [dbo].[SoccerTeamPlayers] WHERE [TeamId] = @TeamId;
DELETE FROM [dbo].[SoccerPlayers] WHERE [PlayerId] IN (SELECT [PlayerId] FROM @old);

--.// 로스터 정의 (U15 6명 · U12 3명 · U18 2명 — 팀 대시보드 레퍼런스 구성)

DECLARE @roster TABLE (
    [PlayerId]  UNIQUEIDENTIFIER DEFAULT NEWID(),
    [Name]      VARCHAR(150),
    [AgeGroup]  VARCHAR(20),
    [PhotoUrl]  VARCHAR(2048),
    [Position]  VARCHAR(60),
    [Grade]     VARCHAR(60),
    [Number]    VARCHAR(10),
    [IsClaimed] BIT);

INSERT INTO @roster ([Name], [AgeGroup], [PhotoUrl], [Position], [Grade], [Number], [IsClaimed]) VALUES
('김민준', 'U15', 'https://images.pexels.com/photos/31855946/pexels-photo-31855946.jpeg?auto=compress&cs=tinysrgb&w=600', 'FW', '중2', '9', 1),
('이서준', 'U15', 'https://images.pexels.com/photos/37044687/pexels-photo-37044687.jpeg?auto=compress&cs=tinysrgb&w=600', 'MF', '중3', '8', 1),
('박도윤', 'U15', 'https://images.pexels.com/photos/3886093/pexels-photo-3886093.jpeg?auto=compress&cs=tinysrgb&w=600', 'DF', '중2', '4', 0),
('최시우', 'U15', 'https://images.pexels.com/photos/33257251/pexels-photo-33257251.jpeg?auto=compress&cs=tinysrgb&w=600', 'GK', '중1', '1', 0),
('정하준', 'U15', NULL, 'FW', '중2', '11', 0),
('강지호', 'U15', NULL, 'MF', '중3', '6', 1),
('오지안', 'U12', 'https://images.pexels.com/photos/35481332/pexels-photo-35481332.jpeg?auto=compress&cs=tinysrgb&w=600', 'MF', '초5', '7', 1),
('한이든', 'U12', NULL, 'FW', '초6', '10', 0),
('서준우', 'U12', NULL, 'DF', '초5', '3', 0),
('윤태양', 'U18', 'https://images.pexels.com/photos/31855946/pexels-photo-31855946.jpeg?auto=compress&cs=tinysrgb&w=600', 'MF', '고2', '10', 1),
('임건우', 'U18', NULL, 'DF', '고1', '5', 1);

--.// 3개 테이블 삽입

INSERT INTO [dbo].[SoccerPlayers] ([PlayerId], [UserId], [Name], [PhotoUrl], [AgeGroup])
SELECT [PlayerId], CASE WHEN [IsClaimed] = 1 THEN NEWID() END, [Name], [PhotoUrl], [AgeGroup]
FROM @roster;

INSERT INTO [dbo].[SoccerTeamPlayers] ([TeamId], [PlayerId], [JerseyNumber], [Position], [Grade])
SELECT @TeamId, [PlayerId], [Number], [Position], [Grade]
FROM @roster;

-- 초대코드는 Unclaimed 선수에게만 발급 (Pending 상태)
INSERT INTO [dbo].[SoccerPlayerInvites] ([Code], [PlayerId], [TeamId])
SELECT UPPER(LEFT(REPLACE(CONVERT(VARCHAR(36), NEWID()), '-', ''), 8)), [PlayerId], @TeamId
FROM @roster
WHERE [IsClaimed] = 0;
