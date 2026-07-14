-- 로컬 검증용 리그 팀 시드 — U12 3팀(학년별 10명×3) / U15 3팀(학년별 14명×3), 총 216명.
-- 팀·선수 이름은 실데이터(크롤러 백업)에서 샘플링. 관리자 계정은 Account의
-- VerificationTeamAdmins.Seed.sql과 고정 GUID로 연결 (verify-u12-1@test.local 등).
-- Claimed 선수의 UserId는 NEWID() 표시용 더미. 재실행 안전(고정 TeamId 기준 삭제 후 삽입).
-- 로컬 개발 DB 전용 — 운영 배포 금지.
DECLARE @ids TABLE ([TeamId] UNIQUEIDENTIFIER);
INSERT INTO @ids VALUES ('B0000000-0000-0000-0000-000000000001'), ('B0000000-0000-0000-0000-000000000002'), ('B0000000-0000-0000-0000-000000000003'), ('B0000000-0000-0000-0000-000000000004'), ('B0000000-0000-0000-0000-000000000005'), ('B0000000-0000-0000-0000-000000000006');

DECLARE @pids TABLE ([PlayerId] UNIQUEIDENTIFIER);
INSERT INTO @pids SELECT [PlayerId] FROM [dbo].[SoccerTeamPlayers] WHERE [TeamId] IN (SELECT [TeamId] FROM @ids);

DELETE FROM [dbo].[SoccerPlayerInvites] WHERE [TeamId] IN (SELECT [TeamId] FROM @ids);
DELETE FROM [dbo].[SoccerTeamPlayers] WHERE [TeamId] IN (SELECT [TeamId] FROM @ids);
DELETE FROM [dbo].[SoccerPlayers] WHERE [PlayerId] IN (SELECT [PlayerId] FROM @pids);
DELETE FROM [dbo].[SoccerTeams] WHERE [TeamId] IN (SELECT [TeamId] FROM @ids);
GO
--.// 서울신답FCU12 (U12 · 관리자 A0000000-0000-0000-0000-000000000C01)

INSERT INTO [dbo].[SoccerTeams]
    ([TeamId], [TeamName], [TeamType], [Region], [AgeGroup], [LogoUrl], [Slug], [ManagerUserId],
     [IsVerified], [FoundedYear], [MonthlyFee], [IsMonthlyFeePublic], [TrainingDays])
VALUES
    ('B0000000-0000-0000-0000-000000000001', '서울신답FCU12', '클럽', '서울 성동구', 'U12',
     'https://api.dicebear.com/9.x/initials/svg?seed=sindap-fc-u12&backgroundColor=23408e&fontWeight=700',
     'sindap-fc-u12', 'A0000000-0000-0000-0000-000000000C01', 1, 2016, 180000, 1, '화목토');

DECLARE @roster TABLE (
    [PlayerId] UNIQUEIDENTIFIER DEFAULT NEWID(), [Name] VARCHAR(150), [Position] VARCHAR(60),
    [Grade] VARCHAR(60), [Number] VARCHAR(10), [IsClaimed] BIT, [PhotoUrl] VARCHAR(2048));

INSERT INTO @roster ([Name], [Position], [Grade], [Number], [IsClaimed], [PhotoUrl]) VALUES
('신준우', 'MF', '초6', '1', 1, 'https://images.pexels.com/photos/31855946/pexels-photo-31855946.jpeg?auto=compress&cs=tinysrgb&w=600'),
('하준영', 'MF', '초6', '2', 0, 'https://images.pexels.com/photos/37044687/pexels-photo-37044687.jpeg?auto=compress&cs=tinysrgb&w=600'),
('이하준', 'MF', '초6', '3', 0, 'https://images.pexels.com/photos/3886093/pexels-photo-3886093.jpeg?auto=compress&cs=tinysrgb&w=600'),
('손재윤', 'MF', '초6', '4', 1, NULL),
('정유찬', 'MF', '초6', '5', 0, NULL),
('고원준', 'MF', '초6', '6', 0, NULL),
('지민혁', 'FW', '초6', '7', 1, NULL),
('장민성', 'MF', '초6', '8', 0, NULL),
('백찬열', 'MF', '초6', '9', 0, NULL),
('정주형', 'MF', '초6', '10', 1, NULL),
('조예준', 'MF', '초5', '11', 0, 'https://images.pexels.com/photos/35481332/pexels-photo-35481332.jpeg?auto=compress&cs=tinysrgb&w=600'),
('이서원', 'MF', '초5', '12', 0, 'https://images.pexels.com/photos/31533672/pexels-photo-31533672.jpeg?auto=compress&cs=tinysrgb&w=600'),
('함재준', 'FW', '초5', '13', 1, 'https://images.pexels.com/photos/31855946/pexels-photo-31855946.jpeg?auto=compress&cs=tinysrgb&w=600'),
('김예성', 'DF', '초5', '14', 0, NULL),
('이민혁', 'MF', '초5', '15', 0, NULL),
('최시연', 'MF', '초5', '16', 1, NULL),
('황주호', 'MF', '초5', '17', 0, NULL),
('이안', 'MF', '초5', '18', 0, NULL),
('박시후', 'FW', '초5', '19', 1, NULL),
('박강우', 'FW', '초5', '20', 0, NULL),
('유시호', 'MF', '초4', '21', 0, 'https://images.pexels.com/photos/3886093/pexels-photo-3886093.jpeg?auto=compress&cs=tinysrgb&w=600'),
('송민승', 'MF', '초4', '22', 1, 'https://images.pexels.com/photos/33257251/pexels-photo-33257251.jpeg?auto=compress&cs=tinysrgb&w=600'),
('한승원', 'FW', '초4', '23', 0, 'https://images.pexels.com/photos/35481332/pexels-photo-35481332.jpeg?auto=compress&cs=tinysrgb&w=600'),
('이승윤', 'GK', '초4', '24', 0, NULL),
('이호겸', 'MF', '초4', '25', 1, NULL),
('유시하', 'FW', '초4', '26', 0, NULL),
('송지후', 'MF', '초4', '27', 0, NULL),
('최시하', 'MF', '초4', '28', 1, NULL),
('김우현', 'MF', '초4', '29', 0, NULL),
('정준후', 'FW', '초4', '30', 0, NULL);

INSERT INTO [dbo].[SoccerPlayers] ([PlayerId], [UserId], [Name], [PhotoUrl], [AgeGroup])
SELECT [PlayerId], CASE WHEN [IsClaimed] = 1 THEN NEWID() END, [Name], [PhotoUrl], 'U12'
FROM @roster;

INSERT INTO [dbo].[SoccerTeamPlayers] ([TeamId], [PlayerId], [JerseyNumber], [Position], [Grade])
SELECT 'B0000000-0000-0000-0000-000000000001', [PlayerId], [Number], [Position], [Grade]
FROM @roster;

INSERT INTO [dbo].[SoccerPlayerInvites] ([Code], [PlayerId], [TeamId])
SELECT UPPER(LEFT(REPLACE(CONVERT(VARCHAR(36), NEWID()), '-', ''), 8)), [PlayerId], 'B0000000-0000-0000-0000-000000000001'
FROM @roster
WHERE [IsClaimed] = 0;
GO
--.// 서울K리거강용FC (U12 · 관리자 A0000000-0000-0000-0000-000000000C02)

INSERT INTO [dbo].[SoccerTeams]
    ([TeamId], [TeamName], [TeamType], [Region], [AgeGroup], [LogoUrl], [Slug], [ManagerUserId],
     [IsVerified], [FoundedYear], [MonthlyFee], [IsMonthlyFeePublic], [TrainingDays])
VALUES
    ('B0000000-0000-0000-0000-000000000002', '서울K리거강용FC', '클럽', '서울 강서구', 'U12',
     'https://api.dicebear.com/9.x/initials/svg?seed=k-liger-fc-u12&backgroundColor=23408e&fontWeight=700',
     'k-liger-fc-u12', 'A0000000-0000-0000-0000-000000000C02', 0, 2019, NULL, 1, NULL);

DECLARE @roster TABLE (
    [PlayerId] UNIQUEIDENTIFIER DEFAULT NEWID(), [Name] VARCHAR(150), [Position] VARCHAR(60),
    [Grade] VARCHAR(60), [Number] VARCHAR(10), [IsClaimed] BIT, [PhotoUrl] VARCHAR(2048));

INSERT INTO @roster ([Name], [Position], [Grade], [Number], [IsClaimed], [PhotoUrl]) VALUES
('노유성', 'DF', '초6', '1', 1, 'https://images.pexels.com/photos/31855946/pexels-photo-31855946.jpeg?auto=compress&cs=tinysrgb&w=600'),
('홍성민', 'DF', '초6', '2', 0, 'https://images.pexels.com/photos/37044687/pexels-photo-37044687.jpeg?auto=compress&cs=tinysrgb&w=600'),
('강연우', 'MF', '초6', '3', 0, 'https://images.pexels.com/photos/3886093/pexels-photo-3886093.jpeg?auto=compress&cs=tinysrgb&w=600'),
('박수영', 'FW', '초6', '4', 1, NULL),
('이정현', 'MF', '초6', '5', 0, NULL),
('황지안', 'DF', '초6', '6', 0, NULL),
('신시우', 'MF', '초6', '7', 1, NULL),
('최시원', 'FW', '초6', '8', 0, NULL),
('백결', 'MF', '초6', '9', 0, NULL),
('최광훈', 'DF', '초6', '10', 1, NULL),
('고하빈', 'MF', '초5', '11', 0, 'https://images.pexels.com/photos/35481332/pexels-photo-35481332.jpeg?auto=compress&cs=tinysrgb&w=600'),
('최범찬', 'MF', '초5', '12', 0, 'https://images.pexels.com/photos/31533672/pexels-photo-31533672.jpeg?auto=compress&cs=tinysrgb&w=600'),
('최윤기', 'MF', '초5', '13', 1, 'https://images.pexels.com/photos/31855946/pexels-photo-31855946.jpeg?auto=compress&cs=tinysrgb&w=600'),
('김도현', 'GK', '초5', '14', 0, NULL),
('강해솔', 'FW', '초5', '15', 0, NULL),
('진태윤', 'DF', '초5', '16', 1, NULL),
('문지원', 'DF', '초5', '17', 0, NULL),
('허지우', 'DF', '초5', '18', 0, NULL),
('박서빈', 'DF', '초5', '19', 1, NULL),
('최영훈', 'FW', '초5', '20', 0, NULL),
('권우빈', 'MF', '초4', '21', 0, 'https://images.pexels.com/photos/3886093/pexels-photo-3886093.jpeg?auto=compress&cs=tinysrgb&w=600'),
('정라온', 'FW', '초4', '22', 1, 'https://images.pexels.com/photos/33257251/pexels-photo-33257251.jpeg?auto=compress&cs=tinysrgb&w=600'),
('채지용', 'FW', '초4', '23', 0, 'https://images.pexels.com/photos/35481332/pexels-photo-35481332.jpeg?auto=compress&cs=tinysrgb&w=600'),
('한시우', 'FW', '초4', '24', 0, NULL),
('정바다', 'MF', '초4', '25', 1, NULL),
('이하랑', 'MF', '초4', '26', 0, NULL),
('박현서', 'GK', '초4', '27', 0, NULL),
('박예담', 'MF', '초4', '28', 1, NULL),
('전우혁', 'DF', '초4', '29', 0, NULL),
('정은결', 'MF', '초4', '30', 0, NULL);

INSERT INTO [dbo].[SoccerPlayers] ([PlayerId], [UserId], [Name], [PhotoUrl], [AgeGroup])
SELECT [PlayerId], CASE WHEN [IsClaimed] = 1 THEN NEWID() END, [Name], [PhotoUrl], 'U12'
FROM @roster;

INSERT INTO [dbo].[SoccerTeamPlayers] ([TeamId], [PlayerId], [JerseyNumber], [Position], [Grade])
SELECT 'B0000000-0000-0000-0000-000000000002', [PlayerId], [Number], [Position], [Grade]
FROM @roster;

INSERT INTO [dbo].[SoccerPlayerInvites] ([Code], [PlayerId], [TeamId])
SELECT UPPER(LEFT(REPLACE(CONVERT(VARCHAR(36), NEWID()), '-', ''), 8)), [PlayerId], 'B0000000-0000-0000-0000-000000000002'
FROM @roster
WHERE [IsClaimed] = 0;
GO
--.// 전남순천중앙초 (U12 · 관리자 A0000000-0000-0000-0000-000000000C03)

INSERT INTO [dbo].[SoccerTeams]
    ([TeamId], [TeamName], [TeamType], [Region], [AgeGroup], [LogoUrl], [Slug], [ManagerUserId],
     [IsVerified], [FoundedYear], [MonthlyFee], [IsMonthlyFeePublic], [TrainingDays])
VALUES
    ('B0000000-0000-0000-0000-000000000003', '전남순천중앙초', '학교', '전남 순천시', 'U12',
     'https://api.dicebear.com/9.x/initials/svg?seed=suncheon-jungang&backgroundColor=23408e&fontWeight=700',
     'suncheon-jungang', 'A0000000-0000-0000-0000-000000000C03', 1, 2005, 150000, 0, '월수금');

DECLARE @roster TABLE (
    [PlayerId] UNIQUEIDENTIFIER DEFAULT NEWID(), [Name] VARCHAR(150), [Position] VARCHAR(60),
    [Grade] VARCHAR(60), [Number] VARCHAR(10), [IsClaimed] BIT, [PhotoUrl] VARCHAR(2048));

INSERT INTO @roster ([Name], [Position], [Grade], [Number], [IsClaimed], [PhotoUrl]) VALUES
('정시우', 'GK', '초6', '1', 1, 'https://images.pexels.com/photos/31855946/pexels-photo-31855946.jpeg?auto=compress&cs=tinysrgb&w=600'),
('최원', 'DF', '초6', '2', 0, 'https://images.pexels.com/photos/37044687/pexels-photo-37044687.jpeg?auto=compress&cs=tinysrgb&w=600'),
('이수헌', 'DF', '초6', '3', 0, 'https://images.pexels.com/photos/3886093/pexels-photo-3886093.jpeg?auto=compress&cs=tinysrgb&w=600'),
('이현민', 'DF', '초6', '4', 1, NULL),
('김민찬', 'DF', '초6', '5', 0, NULL),
('윤태경', 'DF', '초6', '6', 0, NULL),
('임유준', 'FW', '초6', '7', 1, NULL),
('장선호', 'MF', '초6', '8', 0, NULL),
('김하준', 'DF', '초6', '9', 0, NULL),
('이동준', 'FW', '초6', '10', 1, NULL),
('박정현', 'MF', '초5', '11', 0, 'https://images.pexels.com/photos/35481332/pexels-photo-35481332.jpeg?auto=compress&cs=tinysrgb&w=600'),
('손정인', 'MF', '초5', '12', 0, 'https://images.pexels.com/photos/31533672/pexels-photo-31533672.jpeg?auto=compress&cs=tinysrgb&w=600'),
('이연우', 'MF', '초5', '13', 1, 'https://images.pexels.com/photos/31855946/pexels-photo-31855946.jpeg?auto=compress&cs=tinysrgb&w=600'),
('이예승', 'FW', '초5', '14', 0, NULL),
('나경엽', 'MF', '초5', '15', 0, NULL),
('김지원', 'MF', '초5', '16', 1, NULL),
('차유민', 'MF', '초5', '17', 0, NULL),
('강지우', 'DF', '초5', '18', 0, NULL),
('정지호', 'GK', '초5', '19', 1, NULL),
('김지호', 'DF', '초5', '20', 0, NULL),
('김하진', 'FW', '초4', '21', 0, 'https://images.pexels.com/photos/3886093/pexels-photo-3886093.jpeg?auto=compress&cs=tinysrgb&w=600'),
('이태산', 'MF', '초4', '22', 1, 'https://images.pexels.com/photos/33257251/pexels-photo-33257251.jpeg?auto=compress&cs=tinysrgb&w=600'),
('이노아', 'MF', '초4', '23', 0, 'https://images.pexels.com/photos/35481332/pexels-photo-35481332.jpeg?auto=compress&cs=tinysrgb&w=600'),
('김주노', 'DF', '초4', '24', 0, NULL),
('위동현', 'DF', '초4', '25', 1, NULL),
('구하윤', 'DF', '초4', '26', 0, NULL),
('정선우', 'FW', '초4', '27', 0, NULL),
('김시혁', 'MF', '초4', '28', 1, NULL),
('강라온', 'MF', '초4', '29', 0, NULL),
('최민준', 'GK', '초4', '30', 0, NULL);

INSERT INTO [dbo].[SoccerPlayers] ([PlayerId], [UserId], [Name], [PhotoUrl], [AgeGroup])
SELECT [PlayerId], CASE WHEN [IsClaimed] = 1 THEN NEWID() END, [Name], [PhotoUrl], 'U12'
FROM @roster;

INSERT INTO [dbo].[SoccerTeamPlayers] ([TeamId], [PlayerId], [JerseyNumber], [Position], [Grade])
SELECT 'B0000000-0000-0000-0000-000000000003', [PlayerId], [Number], [Position], [Grade]
FROM @roster;

INSERT INTO [dbo].[SoccerPlayerInvites] ([Code], [PlayerId], [TeamId])
SELECT UPPER(LEFT(REPLACE(CONVERT(VARCHAR(36), NEWID()), '-', ''), 8)), [PlayerId], 'B0000000-0000-0000-0000-000000000003'
FROM @roster
WHERE [IsClaimed] = 0;
GO
--.// 광주광주FCU15 (U15 · 관리자 A0000000-0000-0000-0000-000000000C11)

INSERT INTO [dbo].[SoccerTeams]
    ([TeamId], [TeamName], [TeamType], [Region], [AgeGroup], [LogoUrl], [Slug], [ManagerUserId],
     [IsVerified], [FoundedYear], [MonthlyFee], [IsMonthlyFeePublic], [TrainingDays])
VALUES
    ('B0000000-0000-0000-0000-000000000004', '광주광주FCU15', '클럽', '광주 북구', 'U15',
     'https://api.dicebear.com/9.x/initials/svg?seed=gwangju-fc-u15&backgroundColor=23408e&fontWeight=700',
     'gwangju-fc-u15', 'A0000000-0000-0000-0000-000000000C11', 1, 2011, 250000, 1, '화목금토');

DECLARE @roster TABLE (
    [PlayerId] UNIQUEIDENTIFIER DEFAULT NEWID(), [Name] VARCHAR(150), [Position] VARCHAR(60),
    [Grade] VARCHAR(60), [Number] VARCHAR(10), [IsClaimed] BIT, [PhotoUrl] VARCHAR(2048));

INSERT INTO @roster ([Name], [Position], [Grade], [Number], [IsClaimed], [PhotoUrl]) VALUES
('김정현', 'GK', '중3', '1', 1, 'https://images.pexels.com/photos/31855946/pexels-photo-31855946.jpeg?auto=compress&cs=tinysrgb&w=600'),
('명성호', 'DF', '중3', '2', 0, 'https://images.pexels.com/photos/37044687/pexels-photo-37044687.jpeg?auto=compress&cs=tinysrgb&w=600'),
('최동빈', 'MF', '중3', '3', 0, 'https://images.pexels.com/photos/3886093/pexels-photo-3886093.jpeg?auto=compress&cs=tinysrgb&w=600'),
('정은찬', 'MF', '중3', '4', 1, NULL),
('김두현', 'DF', '중3', '5', 0, NULL),
('이지우', 'MF', '중3', '6', 0, NULL),
('강혁', 'MF', '중3', '7', 1, NULL),
('박현규', 'MF', '중3', '8', 0, NULL),
('김재승', 'DF', '중3', '9', 0, NULL),
('송지우', 'FW', '중3', '10', 1, NULL),
('유현서', 'FW', '중3', '11', 0, NULL),
('최은호', 'MF', '중3', '12', 0, NULL),
('최민성', 'MF', '중3', '13', 1, NULL),
('이남준', 'FW', '중3', '14', 0, NULL),
('김강빈', 'FW', '중2', '15', 0, 'https://images.pexels.com/photos/3886093/pexels-photo-3886093.jpeg?auto=compress&cs=tinysrgb&w=600'),
('양도훈', 'FW', '중2', '16', 1, 'https://images.pexels.com/photos/33257251/pexels-photo-33257251.jpeg?auto=compress&cs=tinysrgb&w=600'),
('박준영', 'DF', '중2', '17', 0, 'https://images.pexels.com/photos/35481332/pexels-photo-35481332.jpeg?auto=compress&cs=tinysrgb&w=600'),
('조선우', 'FW', '중2', '18', 0, NULL),
('이도윤', 'FW', '중2', '19', 1, NULL),
('곽주원', 'DF', '중2', '20', 0, NULL),
('이종혁', 'GK', '중2', '21', 0, NULL),
('정태준', 'FW', '중2', '22', 1, NULL),
('이호승', 'DF', '중2', '23', 0, NULL),
('하진서', 'FW', '중2', '24', 0, NULL),
('이성현', 'FW', '중2', '25', 1, NULL),
('최한준', 'MF', '중2', '26', 0, NULL),
('양도율', 'DF', '중2', '27', 0, NULL),
('이민찬', 'MF', '중2', '28', 1, NULL),
('박정민', 'DF', '중1', '29', 0, 'https://images.pexels.com/photos/35481332/pexels-photo-35481332.jpeg?auto=compress&cs=tinysrgb&w=600'),
('강민재', 'GK', '중1', '30', 0, 'https://images.pexels.com/photos/31533672/pexels-photo-31533672.jpeg?auto=compress&cs=tinysrgb&w=600'),
('오채민', 'FW', '중1', '31', 1, 'https://images.pexels.com/photos/31855946/pexels-photo-31855946.jpeg?auto=compress&cs=tinysrgb&w=600'),
('김도윤', 'MF', '중1', '32', 0, NULL),
('여창현', 'DF', '중1', '33', 0, NULL),
('윤시호', 'DF', '중1', '34', 1, NULL),
('오가람', 'FW', '중1', '35', 0, NULL),
('김남건', 'MF', '중1', '36', 0, NULL),
('박수훈', 'FW', '중1', '37', 1, NULL),
('최유찬', 'DF', '중1', '38', 0, NULL),
('이환희', 'FW', '중1', '39', 0, NULL),
('신재민', 'MF', '중1', '40', 1, NULL),
('김대한', 'FW', '중1', '41', 0, NULL),
('윤현도', 'MF', '중1', '42', 0, NULL);

INSERT INTO [dbo].[SoccerPlayers] ([PlayerId], [UserId], [Name], [PhotoUrl], [AgeGroup])
SELECT [PlayerId], CASE WHEN [IsClaimed] = 1 THEN NEWID() END, [Name], [PhotoUrl], 'U15'
FROM @roster;

INSERT INTO [dbo].[SoccerTeamPlayers] ([TeamId], [PlayerId], [JerseyNumber], [Position], [Grade])
SELECT 'B0000000-0000-0000-0000-000000000004', [PlayerId], [Number], [Position], [Grade]
FROM @roster;

INSERT INTO [dbo].[SoccerPlayerInvites] ([Code], [PlayerId], [TeamId])
SELECT UPPER(LEFT(REPLACE(CONVERT(VARCHAR(36), NEWID()), '-', ''), 8)), [PlayerId], 'B0000000-0000-0000-0000-000000000004'
FROM @roster
WHERE [IsClaimed] = 0;
GO
--.// 부산아이파크U15낙동중 (U15 · 관리자 A0000000-0000-0000-0000-000000000C12)

INSERT INTO [dbo].[SoccerTeams]
    ([TeamId], [TeamName], [TeamType], [Region], [AgeGroup], [LogoUrl], [Slug], [ManagerUserId],
     [IsVerified], [FoundedYear], [MonthlyFee], [IsMonthlyFeePublic], [TrainingDays])
VALUES
    ('B0000000-0000-0000-0000-000000000005', '부산아이파크U15낙동중', '학교', '부산 사상구', 'U15',
     'https://api.dicebear.com/9.x/initials/svg?seed=busan-ipark-u15&backgroundColor=23408e&fontWeight=700',
     'busan-ipark-u15', 'A0000000-0000-0000-0000-000000000C12', 0, 2008, NULL, 1, NULL);

DECLARE @roster TABLE (
    [PlayerId] UNIQUEIDENTIFIER DEFAULT NEWID(), [Name] VARCHAR(150), [Position] VARCHAR(60),
    [Grade] VARCHAR(60), [Number] VARCHAR(10), [IsClaimed] BIT, [PhotoUrl] VARCHAR(2048));

INSERT INTO @roster ([Name], [Position], [Grade], [Number], [IsClaimed], [PhotoUrl]) VALUES
('윤진호', 'GK', '중3', '1', 1, 'https://images.pexels.com/photos/31855946/pexels-photo-31855946.jpeg?auto=compress&cs=tinysrgb&w=600'),
('지윤호', 'MF', '중3', '2', 0, 'https://images.pexels.com/photos/37044687/pexels-photo-37044687.jpeg?auto=compress&cs=tinysrgb&w=600'),
('봉재준', 'FW', '중3', '3', 0, 'https://images.pexels.com/photos/3886093/pexels-photo-3886093.jpeg?auto=compress&cs=tinysrgb&w=600'),
('문태웅', 'MF', '중3', '4', 1, NULL),
('박지완', 'DF', '중3', '5', 0, NULL),
('김은환', 'MF', '중3', '6', 0, NULL),
('유동현', 'FW', '중3', '7', 1, NULL),
('천예성', 'MF', '중3', '8', 0, NULL),
('장재영', 'FW', '중3', '9', 0, NULL),
('유동엽', 'MF', '중3', '10', 1, NULL),
('제보검', 'FW', '중3', '11', 0, NULL),
('김강현', 'FW', '중3', '12', 0, NULL),
('박윤준', 'MF', '중3', '13', 1, NULL),
('백희제', 'MF', '중3', '14', 0, NULL),
('정재용', 'MF', '중2', '15', 0, 'https://images.pexels.com/photos/3886093/pexels-photo-3886093.jpeg?auto=compress&cs=tinysrgb&w=600'),
('이현율', 'MF', '중2', '16', 1, 'https://images.pexels.com/photos/33257251/pexels-photo-33257251.jpeg?auto=compress&cs=tinysrgb&w=600'),
('차예성', 'MF', '중2', '17', 0, 'https://images.pexels.com/photos/35481332/pexels-photo-35481332.jpeg?auto=compress&cs=tinysrgb&w=600'),
('어태환', 'FW', '중2', '18', 0, NULL),
('고민결', 'MF', '중2', '19', 1, NULL),
('홍하민', 'DF', '중2', '20', 0, NULL),
('서유찬', 'GK', '중2', '21', 0, NULL),
('박성준', 'DF', '중2', '22', 1, NULL),
('이진석', 'DF', '중2', '23', 0, NULL),
('정시현', 'DF', '중2', '24', 0, NULL),
('손지오', 'MF', '중2', '25', 1, NULL),
('김무찬', 'MF', '중2', '26', 0, NULL),
('강지율', 'FW', '중2', '27', 0, NULL),
('김준수', 'FW', '중2', '28', 1, NULL),
('오선우', 'GK', '중1', '29', 0, 'https://images.pexels.com/photos/35481332/pexels-photo-35481332.jpeg?auto=compress&cs=tinysrgb&w=600'),
('이시우', 'MF', '중1', '30', 0, 'https://images.pexels.com/photos/31533672/pexels-photo-31533672.jpeg?auto=compress&cs=tinysrgb&w=600'),
('정현진', 'MF', '중1', '31', 1, 'https://images.pexels.com/photos/31855946/pexels-photo-31855946.jpeg?auto=compress&cs=tinysrgb&w=600'),
('정민기', 'MF', '중1', '32', 0, NULL),
('정준우', 'MF', '중1', '33', 0, NULL),
('김서준', 'MF', '중1', '34', 1, NULL),
('하지후', 'FW', '중1', '35', 0, NULL),
('한지호', 'FW', '중1', '36', 0, NULL),
('홍민재', 'MF', '중1', '37', 1, NULL),
('김선준', 'DF', '중1', '38', 0, NULL),
('이성혁', 'FW', '중1', '39', 0, NULL),
('방진영', 'GK', '중1', '40', 1, NULL),
('이가람', 'DF', '중1', '41', 0, NULL),
('송윤빈', 'DF', '중1', '42', 0, NULL);

INSERT INTO [dbo].[SoccerPlayers] ([PlayerId], [UserId], [Name], [PhotoUrl], [AgeGroup])
SELECT [PlayerId], CASE WHEN [IsClaimed] = 1 THEN NEWID() END, [Name], [PhotoUrl], 'U15'
FROM @roster;

INSERT INTO [dbo].[SoccerTeamPlayers] ([TeamId], [PlayerId], [JerseyNumber], [Position], [Grade])
SELECT 'B0000000-0000-0000-0000-000000000005', [PlayerId], [Number], [Position], [Grade]
FROM @roster;

INSERT INTO [dbo].[SoccerPlayerInvites] ([Code], [PlayerId], [TeamId])
SELECT UPPER(LEFT(REPLACE(CONVERT(VARCHAR(36), NEWID()), '-', ''), 8)), [PlayerId], 'B0000000-0000-0000-0000-000000000005'
FROM @roster
WHERE [IsClaimed] = 0;
GO
--.// 전북U15군산시민축구단 (U15 · 관리자 A0000000-0000-0000-0000-000000000C13)

INSERT INTO [dbo].[SoccerTeams]
    ([TeamId], [TeamName], [TeamType], [Region], [AgeGroup], [LogoUrl], [Slug], [ManagerUserId],
     [IsVerified], [FoundedYear], [MonthlyFee], [IsMonthlyFeePublic], [TrainingDays])
VALUES
    ('B0000000-0000-0000-0000-000000000006', '전북U15군산시민축구단', '클럽', '전북 군산시', 'U15',
     'https://api.dicebear.com/9.x/initials/svg?seed=gunsan-citizen-u15&backgroundColor=23408e&fontWeight=700',
     'gunsan-citizen-u15', 'A0000000-0000-0000-0000-000000000C13', 1, 2014, 220000, 1, '월수금토');

DECLARE @roster TABLE (
    [PlayerId] UNIQUEIDENTIFIER DEFAULT NEWID(), [Name] VARCHAR(150), [Position] VARCHAR(60),
    [Grade] VARCHAR(60), [Number] VARCHAR(10), [IsClaimed] BIT, [PhotoUrl] VARCHAR(2048));

INSERT INTO @roster ([Name], [Position], [Grade], [Number], [IsClaimed], [PhotoUrl]) VALUES
('장한이', 'GK', '중3', '1', 1, 'https://images.pexels.com/photos/31855946/pexels-photo-31855946.jpeg?auto=compress&cs=tinysrgb&w=600'),
('박효재', 'DF', '중3', '2', 0, 'https://images.pexels.com/photos/37044687/pexels-photo-37044687.jpeg?auto=compress&cs=tinysrgb&w=600'),
('장지웅', 'DF', '중3', '3', 0, 'https://images.pexels.com/photos/3886093/pexels-photo-3886093.jpeg?auto=compress&cs=tinysrgb&w=600'),
('이하람', 'DF', '중3', '4', 1, NULL),
('장승한', 'MF', '중3', '5', 0, NULL),
('지준서', 'FW', '중3', '6', 0, NULL),
('김하원', 'DF', '중3', '7', 1, NULL),
('김동윤', 'FW', '중3', '8', 0, NULL),
('김형준', 'MF', '중3', '9', 0, NULL),
('문하임', 'MF', '중3', '10', 1, NULL),
('정유민', 'MF', '중3', '11', 0, NULL),
('정승운', 'FW', '중3', '12', 0, NULL),
('박지섭', 'MF', '중3', '13', 1, NULL),
('김도연', 'MF', '중3', '14', 0, NULL),
('두준서', 'MF', '중2', '15', 0, 'https://images.pexels.com/photos/3886093/pexels-photo-3886093.jpeg?auto=compress&cs=tinysrgb&w=600'),
('김서윤', 'GK', '중2', '16', 1, 'https://images.pexels.com/photos/33257251/pexels-photo-33257251.jpeg?auto=compress&cs=tinysrgb&w=600'),
('김은찬', 'MF', '중2', '17', 0, 'https://images.pexels.com/photos/35481332/pexels-photo-35481332.jpeg?auto=compress&cs=tinysrgb&w=600'),
('곽규민', 'FW', '중2', '18', 0, NULL),
('전채완', 'DF', '중2', '19', 1, NULL),
('김인서', 'DF', '중2', '20', 0, NULL),
('장민우', 'FW', '중2', '21', 0, NULL),
('강건욱', 'MF', '중2', '22', 1, NULL),
('최도진', 'DF', '중2', '23', 0, NULL),
('김태훈', 'FW', '중2', '24', 0, NULL),
('이민규', 'DF', '중2', '25', 1, NULL),
('박서준', 'MF', '중2', '26', 0, NULL),
('김현우', 'FW', '중2', '27', 0, NULL),
('박지환', 'DF', '중2', '28', 1, NULL),
('박예준', 'FW', '중1', '29', 0, 'https://images.pexels.com/photos/35481332/pexels-photo-35481332.jpeg?auto=compress&cs=tinysrgb&w=600'),
('오태윤', 'DF', '중1', '30', 0, 'https://images.pexels.com/photos/31533672/pexels-photo-31533672.jpeg?auto=compress&cs=tinysrgb&w=600'),
('박성원', 'MF', '중1', '31', 1, 'https://images.pexels.com/photos/31855946/pexels-photo-31855946.jpeg?auto=compress&cs=tinysrgb&w=600'),
('최윤서1', 'DF', '중1', '32', 0, NULL),
('권도영', 'MF', '중1', '33', 0, NULL),
('최태성', 'DF', '중1', '34', 1, NULL),
('이연준', 'MF', '중1', '35', 0, NULL),
('이광희', 'GK', '중1', '36', 0, NULL),
('박연우', 'FW', '중1', '37', 1, NULL),
('이채윤', 'FW', '중1', '38', 0, NULL),
('손서윤', 'DF', '중1', '39', 0, NULL),
('박유건', 'MF', '중1', '40', 1, NULL),
('주현규', 'FW', '중1', '41', 0, NULL),
('정현구', 'GK', '중1', '42', 0, NULL);

INSERT INTO [dbo].[SoccerPlayers] ([PlayerId], [UserId], [Name], [PhotoUrl], [AgeGroup])
SELECT [PlayerId], CASE WHEN [IsClaimed] = 1 THEN NEWID() END, [Name], [PhotoUrl], 'U15'
FROM @roster;

INSERT INTO [dbo].[SoccerTeamPlayers] ([TeamId], [PlayerId], [JerseyNumber], [Position], [Grade])
SELECT 'B0000000-0000-0000-0000-000000000006', [PlayerId], [Number], [Position], [Grade]
FROM @roster;

INSERT INTO [dbo].[SoccerPlayerInvites] ([Code], [PlayerId], [TeamId])
SELECT UPPER(LEFT(REPLACE(CONVERT(VARCHAR(36), NEWID()), '-', ''), 8)), [PlayerId], 'B0000000-0000-0000-0000-000000000006'
FROM @roster
WHERE [IsClaimed] = 0;
GO
