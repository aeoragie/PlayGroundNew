-- 로컬 검증용 선수단 시드 — '검증fc' 로스터를 실감 데이터(사진·연령 그룹·Claim 상태)로 재구성.
-- 선행 조건: 검증 계정·팀 생성 (VerificationTeamInfo.Seed.sql 헤더 참조).
-- 재실행 안전: 검증fc의 기존 로스터(선수 본체 포함)를 지우고 다시 삽입 — 이 시드가 로스터의 단일 소스.
-- Claimed 선수의 UserId는 NEWID() 표시용 더미 (Account에 실제 사용자 없음 — 뱃지 확인 목적).
-- 로컬 개발 DB 전용 — 운영 배포 금지.
DECLARE @TeamId UNIQUEIDENTIFIER =
    (SELECT TOP 1 [TeamId] FROM [dbo].[SoccerTeams] WHERE [TeamName] IN ('검증fc', '플레이그라운드FC') AND [DeletedAt] IS NULL);

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
-- #1 김정현(U15 GK)·#7 신준우(U12 MF)는 검증 선수 계정과 연결되는 자리
-- (VerificationPlayerLinks.Seed.sql이 등번호로 UserId 주입). 나머지 Claimed는 표시용 더미.

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
('김민준', 'U15', 'https://images.pexels.com/photos/31855946/pexels-photo-31855946.jpeg?auto=compress&cs=tinysrgb&w=600', 'FW', 'U14', '9', 1),
('이서준', 'U15', 'https://images.pexels.com/photos/37044687/pexels-photo-37044687.jpeg?auto=compress&cs=tinysrgb&w=600', 'MF', 'U15', '8', 1),
('박도윤', 'U15', 'https://images.pexels.com/photos/3886093/pexels-photo-3886093.jpeg?auto=compress&cs=tinysrgb&w=600', 'DF', 'U14', '4', 0),
('김정현', 'U15', 'https://images.pexels.com/photos/33257251/pexels-photo-33257251.jpeg?auto=compress&cs=tinysrgb&w=600', 'GK', 'U15', '1', 1),
('정하준', 'U15', NULL, 'FW', 'U14', '11', 0),
('강지호', 'U15', NULL, 'MF', 'U15', '6', 1),
('신준우', 'U12', 'https://images.pexels.com/photos/35481332/pexels-photo-35481332.jpeg?auto=compress&cs=tinysrgb&w=600', 'MF', 'U11', '7', 1),
('한이든', 'U12', NULL, 'FW', 'U12', '10', 0),
('서준우', 'U12', NULL, 'DF', 'U11', '3', 0),
('윤태양', 'U18', 'https://images.pexels.com/photos/31855946/pexels-photo-31855946.jpeg?auto=compress&cs=tinysrgb&w=600', 'MF', 'U17', '10', 1),
('임건우', 'U18', NULL, 'DF', 'U16', '5', 1);

--.// 3개 테이블 삽입

-- Slug(NOT NULL·UNIQUE)은 로마자화 + PlayerId 파생 유니크 접미 (프로시저 UspAddSoccerTeamPlayer 패턴).
INSERT INTO [dbo].[SoccerPlayers] ([PlayerId], [UserId], [Name], [Slug], [PhotoUrl], [AgeGroup])
SELECT [PlayerId], CASE WHEN [IsClaimed] = 1 THEN NEWID() END, [Name],
       dbo.UfnRomanizeKoreanSlug([Name]) + '-vf' + LEFT(REPLACE(CONVERT(VARCHAR(36), [PlayerId]), '-', ''), 6),
       [PhotoUrl], [AgeGroup]
FROM @roster;

INSERT INTO [dbo].[SoccerTeamPlayers] ([TeamId], [PlayerId], [JerseyNumber], [Position], [Grade])
SELECT @TeamId, [PlayerId], [Number], [Position], [Grade]
FROM @roster;

-- 초대코드는 Unclaimed 선수에게만 발급 (Pending 상태)
INSERT INTO [dbo].[SoccerPlayerInvites] ([Code], [PlayerId], [TeamId])
SELECT UPPER(LEFT(REPLACE(CONVERT(VARCHAR(36), NEWID()), '-', ''), 6)), [PlayerId], @TeamId
FROM @roster
WHERE [IsClaimed] = 0;
