-- 기능 테스트 보정 — 김보호(ft-parent)를 자녀 2명 보호자로 맞춘다.
--   ① 박서준(자녀1, 소속팀 없음) 대리관리 프로필 생성 + 가족연결
--   ② 강민(자녀2)을 본인연결 → 보호자 대리관리로 전환(IsGuardianManaged=1 + 가족연결)
-- 재실행 안전(박서준 중복 방지). 되돌리기는 ft-cleanup.sql(ft 계정 전체 삭제).
SET NOCOUNT ON;

DECLARE @Parent UNIQUEIDENTIFIER = (SELECT UserId FROM PlayGround_Account.dbo.Users WHERE Email = 'ft-parent@test.local');
DECLARE @UserA UNIQUEIDENTIFIER = (SELECT UserId FROM PlayGround_Account.dbo.Users WHERE Email = 'ft-team-a@test.local');
DECLARE @TeamA UNIQUEIDENTIFIER = (SELECT TOP 1 TeamId FROM SoccerTeams WHERE ManagerUserId = @UserA AND DeletedAt IS NULL ORDER BY CreatedAt);
DECLARE @Gangmin UNIQUEIDENTIFIER = (
    SELECT TOP 1 p.PlayerId FROM SoccerPlayers p
    JOIN SoccerTeamPlayers tp ON tp.PlayerId = p.PlayerId AND tp.TeamId = @TeamA AND tp.DeletedAt IS NULL
    WHERE p.Name = N'강민');

--.// ① 박서준 — 없으면 생성
DECLARE @Seojun UNIQUEIDENTIFIER = (SELECT TOP 1 PlayerId FROM SoccerPlayers WHERE UserId = @Parent AND Name = N'박서준' AND DeletedAt IS NULL);
IF @Seojun IS NULL
BEGIN
    SET @Seojun = NEWID();
    INSERT INTO SoccerPlayers (PlayerId, UserId, Name, Slug, AgeGroup, IsGuardianManaged, BirthDate)
    VALUES (@Seojun, @Parent, N'박서준', 'park-seojun-ft', 'U15', 1, '2012-03-15');
END

IF NOT EXISTS (SELECT 1 FROM SoccerPlayerFamilyLinks WHERE PlayerId = @Seojun AND UserId = @Parent AND DeletedAt IS NULL)
    INSERT INTO SoccerPlayerFamilyLinks (FamilyLinkId, PlayerId, UserId, MemberName, Role, DisplayOrder, Relation)
    VALUES (NEWID(), @Seojun, @Parent, N'김보호', 'Guardian', 0, 'Mother');

--.// ② 강민 — 본인연결을 보호자 대리관리로 전환
UPDATE SoccerPlayers SET IsGuardianManaged = 1, UpdatedAt = GETUTCDATE() WHERE PlayerId = @Gangmin;

IF NOT EXISTS (SELECT 1 FROM SoccerPlayerFamilyLinks WHERE PlayerId = @Gangmin AND UserId = @Parent AND DeletedAt IS NULL)
    INSERT INTO SoccerPlayerFamilyLinks (FamilyLinkId, PlayerId, UserId, MemberName, Role, DisplayOrder, Relation)
    VALUES (NEWID(), @Gangmin, @Parent, N'김보호', 'Guardian', 0, 'Mother');

PRINT '--- 보정 후 김보호 자녀 ---';
SELECT p.Name, p.IsGuardianManaged AS 대리, CASE WHEN p.TeamId IS NOT NULL OR EXISTS(SELECT 1 FROM SoccerTeamPlayers tp WHERE tp.PlayerId=p.PlayerId AND tp.DeletedAt IS NULL) THEN '소속있음' ELSE '무소속' END AS 소속,
       (SELECT COUNT(*) FROM SoccerPlayerFamilyLinks f WHERE f.PlayerId=p.PlayerId AND f.UserId=@Parent AND f.DeletedAt IS NULL) AS 가족연결
FROM SoccerPlayers p WHERE p.UserId = @Parent AND p.DeletedAt IS NULL ORDER BY p.CreatedAt;
