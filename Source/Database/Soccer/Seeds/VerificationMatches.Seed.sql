-- 로컬 검증용 경기 도메인 시드 — 3형식 대회(Cup/League/Split) + 친선 + 이벤트·출전·미디어·수상.
-- 순위표는 직접 넣지 않고 UspRecalculateSoccerTournamentStandings 호출로 생성 (D5 자동 재계산 경로 검증).
-- 선행: VerificationLeagueTeams / VerificationPlayerLinks (리그 팀·선수 계정 연결).
-- 재실행 안전: DataSource='Seed' 행과 종속 행을 지우고 다시 삽입. 로컬 개발 DB 전용 — 운영 배포 금지.

--.// 선행 데이터 해석

DECLARE @U15Player UNIQUEIDENTIFIER =   -- 김정현 (verify-player-u15)
    (SELECT TOP 1 [PlayerId] FROM [dbo].[SoccerPlayers]
     WHERE [UserId] = 'A0000000-0000-0000-0000-000000000D11' AND [DeletedAt] IS NULL);
DECLARE @U12Player UNIQUEIDENTIFIER =   -- 신준우 (verify-player-u12)
    (SELECT TOP 1 [PlayerId] FROM [dbo].[SoccerPlayers]
     WHERE [UserId] = 'A0000000-0000-0000-0000-000000000D01' AND [DeletedAt] IS NULL);

DECLARE @GwangjuTeam UNIQUEIDENTIFIER =
    (SELECT TOP 1 [TeamId] FROM [dbo].[SoccerTeams] WHERE [TeamName] = '광주광주FCU15' AND [DeletedAt] IS NULL);
DECLARE @BusanTeam UNIQUEIDENTIFIER =
    (SELECT TOP 1 [TeamId] FROM [dbo].[SoccerTeams] WHERE [TeamName] = '부산아이파크U15낙동중' AND [DeletedAt] IS NULL);
DECLARE @JeonbukTeam UNIQUEIDENTIFIER =
    (SELECT TOP 1 [TeamId] FROM [dbo].[SoccerTeams] WHERE [TeamName] = '전북U15군산시민축구단' AND [DeletedAt] IS NULL);
DECLARE @SindapTeam UNIQUEIDENTIFIER =
    (SELECT TOP 1 [TeamId] FROM [dbo].[SoccerTeams] WHERE [TeamName] = '서울신답FCU12' AND [DeletedAt] IS NULL);
DECLARE @VerifyFcTeam UNIQUEIDENTIFIER =
    (SELECT TOP 1 [TeamId] FROM [dbo].[SoccerTeams] WHERE [Slug] = '검증fc' AND [DeletedAt] IS NULL);

IF @U15Player IS NULL OR @GwangjuTeam IS NULL OR @SindapTeam IS NULL OR @VerifyFcTeam IS NULL
BEGIN
    RAISERROR('Verification teams/players not found. Run league/player seeds first.', 16, 1);
    RETURN;
END

--.// 기존 시드 데이터 정리 (종속 → 상위 순)

DELETE e FROM [dbo].[SoccerMatchEvents] e
    JOIN [dbo].[SoccerMatches] m ON m.[MatchId] = e.[MatchId] WHERE m.[DataSource] = 'Seed';
DELETE a FROM [dbo].[SoccerMatchAppearances] a
    JOIN [dbo].[SoccerMatches] m ON m.[MatchId] = a.[MatchId] WHERE m.[DataSource] = 'Seed';
DELETE v FROM [dbo].[SoccerMatchVideos] v
    JOIN [dbo].[SoccerMatches] m ON m.[MatchId] = v.[MatchId] WHERE m.[DataSource] = 'Seed';
DELETE v FROM [dbo].[SoccerMatchVideos] v
    JOIN [dbo].[SoccerTournaments] t ON t.[TournamentId] = v.[TournamentId] WHERE t.[DataSource] = 'Seed';
DELETE n FROM [dbo].[SoccerTournamentNews] n
    JOIN [dbo].[SoccerTournaments] t ON t.[TournamentId] = n.[TournamentId] WHERE t.[DataSource] = 'Seed';
DELETE w FROM [dbo].[SoccerTournamentAwards] w
    JOIN [dbo].[SoccerTournaments] t ON t.[TournamentId] = w.[TournamentId] WHERE t.[DataSource] = 'Seed';
DELETE FROM [dbo].[SoccerTournamentStandings] WHERE [DataSource] IN ('Seed', 'User');  -- 재계산 생성분(User) 포함
DELETE FROM [dbo].[SoccerMatches] WHERE [DataSource] = 'Seed';
DELETE FROM [dbo].[SoccerTournaments] WHERE [DataSource] = 'Seed';

--.// 대회 4개 (Cup 2026/2025 같은 시리즈 + League + Split)

DECLARE @Cup26 UNIQUEIDENTIFIER = 'D0000000-0000-0000-0000-0000000000C1';
DECLARE @Cup25 UNIQUEIDENTIFIER = 'D0000000-0000-0000-0000-0000000000C2';
DECLARE @League26 UNIQUEIDENTIFIER = 'D0000000-0000-0000-0000-0000000000A1';
DECLARE @Split26 UNIQUEIDENTIFIER = 'D0000000-0000-0000-0000-0000000000E1';

INSERT INTO [dbo].[SoccerTournaments]
    ([TournamentId], [SeasonYear], [Name], [SeriesSlug], [Format], [Scope], [AgeGroup], [RegionGroup], [Status],
     [StartDate], [EndDate], [TeamCount], [HostName], [MethodText], [MatchTimeText], [TiebreakText],
     [SourceName], [DataSource]) VALUES
(@Cup26, 2026, '2026 PlayGround 전국 U15 챔피언십', 'playground-u15-championship', 'Cup', 'National', 'U15', NULL, 'InProgress',
 '2026-06-01', '2026-08-15', 8, 'PlayGround', '조별 예선(2개조 풀리그) 후 상위 2팀 토너먼트', '전·후반 각 35분', '승점 → 득실차 → 다득점',
 'PlayGround 운영팀', 'Seed'),
(@Cup25, 2025, '2025 PlayGround 전국 U15 챔피언십', 'playground-u15-championship', 'Cup', 'National', 'U15', NULL, 'Completed',
 '2025-06-01', '2025-08-10', 8, 'PlayGround', '조별 예선(2개조 풀리그) 후 상위 2팀 토너먼트', '전·후반 각 35분', '승점 → 득실차 → 다득점',
 'PlayGround 운영팀', 'Seed'),
(@League26, 2026, '2026 서울 U12 주말리그', 'seoul-u12-weekend-league', 'League', 'Regional', 'U12', '서울', 'InProgress',
 '2026-04-01', '2026-11-30', 4, '서울시축구협회', '단일 리그 홈앤드어웨이', '전·후반 각 25분', '승점 → 득실차 → 다득점',
 '서울시축구협회', 'Seed'),
(@Split26, 2026, '2026 U15 왕중왕전 스플릿리그', 'u15-king-split', 'Split', 'National', 'U15', NULL, 'Scheduled',
 '2026-09-01', '2026-11-20', 12, '대한축구협회', '1차 풀리그 후 조 1~4위 스플릿 재배치', '전·후반 각 35분', '승점 → 득실차 → 다득점',
 'KFA', 'Seed');

--.// Cup 2026 — 1조 (광주·부산 + 외부 2팀, R1~R3 중 4경기 종료)

INSERT INTO [dbo].[SoccerMatches]
    ([MatchId], [TournamentId], [StageType], [GroupName], [RoundName],
     [HomeTeamId], [HomeTeamName], [AwayTeamId], [AwayTeamName],
     [HomeScore], [AwayScore], [Status], [MatchedAt], [VenueName], [DataSource]) VALUES
('E0000000-0000-0000-0000-000000000A01', @Cup26, 'Group', '1조', 'R1',
 @GwangjuTeam, '광주광주FCU15', NULL, '무등FC', 3, 1, 'Completed', '2026-06-07 10:00', '광주월드컵보조구장', 'Seed'),
('E0000000-0000-0000-0000-000000000A02', @Cup26, 'Group', '1조', 'R1',
 @BusanTeam, '부산아이파크U15낙동중', NULL, '순천매산중', 2, 0, 'Completed', '2026-06-07 12:00', '광주월드컵보조구장', 'Seed'),
('E0000000-0000-0000-0000-000000000A03', @Cup26, 'Group', '1조', 'R2',
 @GwangjuTeam, '광주광주FCU15', @BusanTeam, '부산아이파크U15낙동중', 1, 1, 'Completed', '2026-06-14 10:00', '광주월드컵보조구장', 'Seed'),
('E0000000-0000-0000-0000-000000000A04', @Cup26, 'Group', '1조', 'R2',
 NULL, '무등FC', NULL, '순천매산중', 0, 2, 'Completed', '2026-06-14 12:00', '광주월드컵보조구장', 'Seed'),
('E0000000-0000-0000-0000-000000000A05', @Cup26, 'Group', '1조', 'R3',
 NULL, '순천매산중', @GwangjuTeam, '광주광주FCU15', 0, 2, 'Completed', '2026-06-21 10:00', '광주월드컵보조구장', 'Seed'),
('E0000000-0000-0000-0000-000000000A06', @Cup26, 'Group', '1조', 'R3',
 @BusanTeam, '부산아이파크U15낙동중', NULL, '무등FC', 1, 0, 'Completed', '2026-06-21 12:00', '광주월드컵보조구장', 'Seed');

--.// Cup 2026 — 2조 (외부 4팀, 2경기만 종료)

INSERT INTO [dbo].[SoccerMatches]
    ([MatchId], [TournamentId], [StageType], [GroupName], [RoundName],
     [HomeTeamId], [HomeTeamName], [AwayTeamId], [AwayTeamName],
     [HomeScore], [AwayScore], [Status], [MatchedAt], [VenueName], [DataSource]) VALUES
('E0000000-0000-0000-0000-000000000B01', @Cup26, 'Group', '2조', 'R1',
 @JeonbukTeam, '전북U15군산시민축구단', NULL, '한강중', 2, 1, 'Completed', '2026-06-07 14:00', '군산공설운동장', 'Seed'),
('E0000000-0000-0000-0000-000000000B02', @Cup26, 'Group', '2조', 'R1',
 NULL, '대전유성FC', NULL, '제주서귀포FC', 1, 1, 'Completed', '2026-06-07 16:00', '군산공설운동장', 'Seed');

--.// Cup 2026 — 토너먼트 (4강 PK 사례 + 결승 예정)

INSERT INTO [dbo].[SoccerMatches]
    ([MatchId], [TournamentId], [StageType], [GroupName], [RoundName],
     [HomeTeamId], [HomeTeamName], [AwayTeamId], [AwayTeamName],
     [HomeScore], [AwayScore], [HomePkScore], [AwayPkScore], [Status], [MatchedAt], [VenueName], [DataSource]) VALUES
('E0000000-0000-0000-0000-000000000C01', @Cup26, 'Knockout', NULL, 'SF',
 @GwangjuTeam, '광주광주FCU15', @JeonbukTeam, '전북U15군산시민축구단', 2, 2, 4, 3, 'Completed', '2026-07-05 10:00', '목동운동장', 'Seed'),
('E0000000-0000-0000-0000-000000000C02', @Cup26, 'Knockout', NULL, 'F',
 @GwangjuTeam, '광주광주FCU15', @BusanTeam, '부산아이파크U15낙동중', NULL, NULL, NULL, NULL, 'Scheduled', '2026-08-15 14:00', '목동운동장', 'Seed');

--.// League 2026 — 서울 U12 (2경기 종료 + 2경기 예정, 월별 분산)

INSERT INTO [dbo].[SoccerMatches]
    ([MatchId], [TournamentId], [StageType], [GroupName], [RoundName],
     [HomeTeamId], [HomeTeamName], [AwayTeamId], [AwayTeamName],
     [HomeScore], [AwayScore], [Status], [MatchedAt], [VenueName], [DataSource]) VALUES
('E0000000-0000-0000-0000-000000000D01', @League26, 'League', NULL, NULL,
 @SindapTeam, '서울신답FCU12', NULL, '강동리틀FC', 2, 0, 'Completed', '2026-04-12 10:00', '신답체육공원', 'Seed'),
('E0000000-0000-0000-0000-000000000D02', @League26, 'League', NULL, NULL,
 NULL, '마포유나이티드U12', @SindapTeam, '서울신답FCU12', 1, 1, 'Completed', '2026-05-10 10:00', '상암보조구장', 'Seed'),
('E0000000-0000-0000-0000-000000000D03', @League26, 'League', NULL, NULL,
 @SindapTeam, '서울신답FCU12', NULL, '송파FCU12', NULL, NULL, 'Scheduled', '2026-08-09 10:00', '신답체육공원', 'Seed'),
('E0000000-0000-0000-0000-000000000D04', @League26, 'League', NULL, NULL,
 NULL, '강동리틀FC', NULL, '마포유나이티드U12', NULL, NULL, 'Scheduled', '2026-08-16 10:00', '강동구민운동장', 'Seed');

--.// 친선 — 검증fc (대회 무관, 팀 대시보드 경기 결과용)

-- MatchType은 명시한다 — 컬럼 기본값이 'Official'이라 빠뜨리면 친선이 순위표·시즌 스탯에 섞인다
INSERT INTO [dbo].[SoccerMatches]
    ([MatchId], [MatchType], [TournamentId], [HomeTeamId], [HomeTeamName], [AwayTeamId], [AwayTeamName],
     [HomeScore], [AwayScore], [Status], [MatchedAt], [VenueName], [DataSource]) VALUES
('E0000000-0000-0000-0000-000000000F01', 'Friendly', NULL, @VerifyFcTeam, '검증FC', NULL, '강동 SC', 3, 1, 'Completed', '2026-07-05 16:00', '강동구민운동장', 'Seed'),
('E0000000-0000-0000-0000-000000000F02', 'Friendly', NULL, @VerifyFcTeam, '검증FC', NULL, '마포 유나이티드', 1, 1, 'Completed', '2026-06-28 16:00', '상암보조구장', 'Seed');

--.// 득점 이벤트 (김정현 = 광주 GK — 검증용 득점 2·도움 1, 신준우 1골)

INSERT INTO [dbo].[SoccerMatchEvents]
    ([MatchId], [TeamId], [TeamName], [EventType], [PlayerId], [PlayerName], [AssistPlayerId], [AssistPlayerName], [MinuteOfPlay]) VALUES
-- Cup R1: 광주 3득점 (김정현 1골 1도움), 무등FC 1득점 (외부 선수)
('E0000000-0000-0000-0000-000000000A01', @GwangjuTeam, '광주광주FCU15', 'Goal', @U15Player, '김정현', NULL, NULL, 12),
('E0000000-0000-0000-0000-000000000A01', @GwangjuTeam, '광주광주FCU15', 'Goal', NULL, '박강토', @U15Player, '김정현', 34),
('E0000000-0000-0000-0000-000000000A01', @GwangjuTeam, '광주광주FCU15', 'Goal', NULL, '이도현', NULL, NULL, 58),
('E0000000-0000-0000-0000-000000000A01', NULL, '무등FC', 'Goal', NULL, '정우성', NULL, NULL, 66),
-- Cup R2: 광주 1득점 (김정현 PK골)
('E0000000-0000-0000-0000-000000000A03', @GwangjuTeam, '광주광주FCU15', 'PenaltyGoal', @U15Player, '김정현', NULL, NULL, 71),
('E0000000-0000-0000-0000-000000000A03', @BusanTeam, '부산아이파크U15낙동중', 'Goal', NULL, '최지훈', NULL, NULL, 44),
-- League: 신답 2득점 (신준우 1골)
('E0000000-0000-0000-0000-000000000D01', @SindapTeam, '서울신답FCU12', 'Goal', @U12Player, '신준우', NULL, NULL, 18),
('E0000000-0000-0000-0000-000000000D01', @SindapTeam, '서울신답FCU12', 'Goal', NULL, '한이든', @U12Player, '신준우', 40),
-- 친선 검증FC: F01 3득점 (김민준 ×2 + 정하준, 도움 이서준), F02 1득점 (강지호, 도움 김민준)
('E0000000-0000-0000-0000-000000000F01', @VerifyFcTeam, '검증FC', 'Goal', NULL, '김민준', NULL, '이서준', 15),
('E0000000-0000-0000-0000-000000000F01', @VerifyFcTeam, '검증FC', 'Goal', NULL, '김민준', NULL, '이서준', 52),
('E0000000-0000-0000-0000-000000000F01', @VerifyFcTeam, '검증FC', 'Goal', NULL, '정하준', NULL, NULL, 70),
('E0000000-0000-0000-0000-000000000F02', @VerifyFcTeam, '검증FC', 'Goal', NULL, '강지호', NULL, '김민준', 33);

--.// 출전 기록 (시즌 통계 원천)

INSERT INTO [dbo].[SoccerMatchAppearances] ([MatchId], [TeamId], [PlayerId], [MinutesPlayed], [IsStarter]) VALUES
('E0000000-0000-0000-0000-000000000A01', @GwangjuTeam, @U15Player, 70, 1),
('E0000000-0000-0000-0000-000000000A03', @GwangjuTeam, @U15Player, 70, 1),
('E0000000-0000-0000-0000-000000000A05', @GwangjuTeam, @U15Player, 55, 1),
('E0000000-0000-0000-0000-000000000C01', @GwangjuTeam, @U15Player, 70, 1),
('E0000000-0000-0000-0000-000000000D01', @SindapTeam, @U12Player, 50, 1),
('E0000000-0000-0000-0000-000000000D02', @SindapTeam, @U12Player, 42, 0);

--.// 순위표 — 자동 재계산 경로 검증 (D5)

EXEC [dbo].[UspRecalculateSoccerTournamentStandings] @TournamentId = @Cup26, @StageType = 'Group', @GroupName = '1조', @DataSource = 'Seed';
EXEC [dbo].[UspRecalculateSoccerTournamentStandings] @TournamentId = @Cup26, @StageType = 'Group', @GroupName = '2조', @DataSource = 'Seed';
EXEC [dbo].[UspRecalculateSoccerTournamentStandings] @TournamentId = @League26, @StageType = 'League', @GroupName = NULL, @DataSource = 'Seed';

-- 진출권(상위 2팀) 표시 — 수동 보정 영역이므로 시드에서 직접 설정
UPDATE [dbo].[SoccerTournamentStandings]
SET [IsQualified] = 1, [UpdatedAt] = GETUTCDATE()
WHERE [TournamentId] = @Cup26 AND [StageType] = 'Group' AND [TeamRank] <= 2 AND [DeletedAt] IS NULL;

--.// 미디어 (영상·뉴스) + 수상 (2025 종료 대회)

INSERT INTO [dbo].[SoccerMatchVideos]
    ([TournamentId], [MatchId], [TeamId], [Title], [VideoUrl], [VideoType], [DurationSeconds], [RecordedOn]) VALUES
(@Cup26, 'E0000000-0000-0000-0000-000000000C01', NULL, '4강 광주광주FCU15 vs 전북U15군산시민축구단 — 승부차기 하이라이트',
 'https://youtube.com/watch?v=verify-match-1', 'Highlight', 245, '2026-07-05'),
(@Cup26, NULL, NULL, '조별 예선 1조 종합 하이라이트', 'https://youtube.com/watch?v=verify-match-2', 'Highlight', 480, '2026-06-21'),
(NULL, 'E0000000-0000-0000-0000-000000000F01', @VerifyFcTeam, '친선 검증FC vs 강동 SC 풀경기',
 'https://youtube.com/watch?v=verify-match-3', 'FullMatch', 4200, '2026-07-05');

INSERT INTO [dbo].[SoccerTournamentNews] ([TournamentId], [Title], [Url], [PublisherName], [PublishedOn]) VALUES
(@Cup26, 'U15 챔피언십 4강, 승부차기 끝에 광주가 결승행', 'https://news.example.com/u15-sf', '유소년축구뉴스', '2026-07-06');

INSERT INTO [dbo].[SoccerTournamentAwards] ([TournamentId], [AwardType], [TeamId], [TeamName], [DisplayOrder]) VALUES
(@Cup25, 'Champion', @JeonbukTeam, '전북U15군산시민축구단', 1),
(@Cup25, 'RunnerUp', @GwangjuTeam, '광주광주FCU15', 2),
(@Cup25, 'FairPlay', @BusanTeam, '부산아이파크U15낙동중', 3);
