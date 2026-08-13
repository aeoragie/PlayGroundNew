-- 온디맨드 검증 시드 — 공식 경기 상세(Design.Records) 확인용. 검증fc가 홈인 조별 경기 1건에
-- PK·전후반·주심·감독·경기순번 + 이벤트 10건(득점 4·경고 5·퇴장 1) + 홈/원정 라인업 전체.
-- 선행: 검증fc 로스터(Verification/VerificationRoster.Seed.sql). 재실행 안전. 로컬 개발 DB 전용.
SET NOCOUNT ON;

DECLARE @vf UNIQUEIDENTIFIER = (SELECT TOP 1 [TeamId] FROM [dbo].[SoccerTeams] WHERE [TeamName] IN ('검증fc', '플레이그라운드FC') AND [DeletedAt] IS NULL);
IF @vf IS NULL BEGIN RAISERROR('Team ''검증fc'' not found — run VerificationRoster first.', 16, 1); RETURN; END

DECLARE @t UNIQUEIDENTIFIER = 'C1000000-0000-0000-0000-000000000001';
DECLARE @m UNIQUEIDENTIFIER = 'C1000000-0000-0000-0000-0000000000A1';

DELETE FROM [dbo].[SoccerMatchEvents] WHERE [MatchId] = @m;
DELETE FROM [dbo].[SoccerMatchAppearances] WHERE [MatchId] = @m;
DELETE FROM [dbo].[SoccerMatches] WHERE [MatchId] = @m;
DELETE FROM [dbo].[SoccerTournamentStandings] WHERE [TournamentId] = @t;
DELETE FROM [dbo].[SoccerTournaments] WHERE [TournamentId] = @t;

INSERT INTO [dbo].[SoccerTournaments]
    ([TournamentId],[SeasonYear],[Name],[Format],[Scope],[AgeGroup],[RegionGroup],[Status],[StartDate],[EndDate],[TeamCount],[HostName],[MethodText],[MatchTimeText],[VenueText],[SourceName])
VALUES (@t, 2026, '2026 검증 유소년 챔피언십', 'Cup', 'Regional', 'U15', '서울', 'InProgress',
        '2026-03-14', '2026-03-29', 8, '검증축구협회', '조별 예선 + 토너먼트', '전·후반 각 25분', '검증체육공원', '대회 주최자 입력');

INSERT INTO [dbo].[SoccerTournamentStandings]
    ([TournamentId],[StageType],[GroupName],[TeamId],[TeamName],[TeamRank],[Played],[Won],[Drawn],[Lost],[Points],[GoalsFor],[GoalsAgainst],[IsQualified]) VALUES
(@t,'Group','1조',@vf,'검증fc',1,3,2,1,0,7,8,3,1),
(@t,'Group','1조',NULL,'서울 풍생중',2,3,2,0,1,6,7,4,1),
(@t,'Group','1조',NULL,'FC 여주',3,3,1,0,2,3,5,8,0),
(@t,'Group','1조',NULL,'강동 유나이티드',4,3,0,1,2,1,2,7,0);

-- 검증fc 2 : 2 서울 풍생중 (PK 4:2) · 전반 1:1 · 주심 이경준 · 1경기
INSERT INTO [dbo].[SoccerMatches]
    ([MatchId],[MatchType],[TournamentId],[StageType],[GroupName],[RoundName],[HomeTeamId],[HomeTeamName],[AwayTeamId],[AwayTeamName],
     [HomeScore],[AwayScore],[HomePkScore],[AwayPkScore],[FirstHalfHomeScore],[FirstHalfAwayScore],[Status],[MatchedAt],[VenueName],
     [RefereeName],[MatchSequence],[HomeCoachName],[AwayCoachName],[DataSource])
VALUES (@m,'Official',@t,'Group','1조','R1',@vf,'검증fc',NULL,'서울 풍생중',
        2,2,4,2,1,1,'Completed','2026-03-15T10:00:00','검증체육공원 A-1','이경준',1,'박정훈','오승현','Seed');

--.// 라인업 — 홈(검증fc, PlayerId 연결) 11명
DECLARE @home TABLE ([Name] VARCHAR(150), [Jersey] INT, [Pos] VARCHAR(10), [Starter] BIT, [Captain] BIT);
INSERT INTO @home VALUES
('김정현',1,'GK',1,1),('박도윤',4,'DF',1,0),('강지호',6,'MF',1,0),('이서준',8,'MF',1,0),
('김민준',9,'FW',1,0),('정하준',11,'FW',1,0),('신준우',7,'MF',1,0),('윤태양',10,'MF',1,0),
('임건우',5,'DF',0,0),('한이든',2,'FW',0,0),('서준우',3,'DF',0,0);

INSERT INTO [dbo].[SoccerMatchAppearances]
    ([MatchId],[TeamId],[TeamName],[PlayerId],[PlayerName],[JerseyNumber],[Position],[IsCaptain],[MinutesPlayed],[IsStarter])
SELECT @m, @vf, '검증fc',
    (SELECT TOP 1 p.[PlayerId] FROM [dbo].[SoccerPlayers] p
       JOIN [dbo].[SoccerTeamPlayers] tp ON tp.[PlayerId] = p.[PlayerId]
       WHERE tp.[TeamId] = @vf AND p.[Name] = h.[Name] AND p.[DeletedAt] IS NULL),
    h.[Name], h.[Jersey], h.[Pos], h.[Captain], CASE WHEN h.[Starter] = 1 THEN 50 ELSE 15 END, h.[Starter]
FROM @home h;

--.// 라인업 — 원정(서울 풍생중, 외부 선수 PlayerId NULL) 12명
DECLARE @away TABLE ([Name] VARCHAR(150), [Jersey] INT, [Pos] VARCHAR(10), [Starter] BIT, [Captain] BIT);
INSERT INTO @away VALUES
('기주하',31,'GK',1,1),('김시훈',4,'MF',1,0),('이강우',5,'DF',1,0),('황준우',21,'MF',1,0),
('민동률',22,'MF',1,0),('장민균',44,'MF',1,0),('손민',10,'FW',1,0),('김동후',88,'FW',1,0),
('채훈',35,'DF',0,0),('김이한',12,'MF',0,0),('박우찬',13,'MF',0,0),('손하준',15,'FW',0,0);

INSERT INTO [dbo].[SoccerMatchAppearances]
    ([MatchId],[TeamId],[TeamName],[PlayerId],[PlayerName],[JerseyNumber],[Position],[IsCaptain],[MinutesPlayed],[IsStarter])
SELECT @m, NULL, '서울 풍생중', NULL, [Name], [Jersey], [Pos], [Captain], CASE WHEN [Starter] = 1 THEN 50 ELSE 15 END, [Starter]
FROM @away;

--.// 이벤트 10건 (득점 4 · 경고 5 · 퇴장 1) — 분 오름차순, 홈/원정 혼합
DECLARE @evt TABLE ([Min] INT, [Side] CHAR(1), [Type] VARCHAR(20), [Player] VARCHAR(150), [Jersey] INT);
INSERT INTO @evt VALUES
(8,'H','Goal','김민준',9),(15,'A','YellowCard','기주하',31),(24,'A','Goal','손민',10),(30,'H','Goal','이서준',8),
(33,'H','YellowCard','박도윤',4),(41,'A','RedCard','이강우',5),(47,'A','Goal','김동후',88),(55,'H','YellowCard','강지호',6),
(60,'A','YellowCard','황준우',21),(66,'H','YellowCard','김정현',1);

INSERT INTO [dbo].[SoccerMatchEvents]
    ([MatchId],[TeamId],[TeamName],[EventType],[PlayerId],[PlayerName],[JerseyNumber],[MinuteOfPlay])
SELECT @m,
    CASE WHEN e.[Side] = 'H' THEN @vf ELSE NULL END,
    CASE WHEN e.[Side] = 'H' THEN '검증fc' ELSE '서울 풍생중' END,
    e.[Type],
    CASE WHEN e.[Side] = 'H' THEN
        (SELECT TOP 1 p.[PlayerId] FROM [dbo].[SoccerPlayers] p
           JOIN [dbo].[SoccerTeamPlayers] tp ON tp.[PlayerId] = p.[PlayerId]
           WHERE tp.[TeamId] = @vf AND p.[Name] = e.[Player] AND p.[DeletedAt] IS NULL)
    END,
    e.[Player], e.[Jersey], e.[Min]
FROM @evt e;

SELECT CONVERT(VARCHAR(36), @t) AS TournamentId, CONVERT(VARCHAR(36), @m) AS MatchId;
