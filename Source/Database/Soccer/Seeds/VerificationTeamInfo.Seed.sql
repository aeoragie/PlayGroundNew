-- 로컬 검증용 팀 정보 시드 — '검증fc' 팀에 팀 정보(확장 컬럼·핵심가치·코칭스태프·공식 채널) 주입.
-- 선행 조건: 검증 계정·팀은 API로 먼저 생성한다 (이메일 가입 → 팀 온보딩).
--   1) verify-teamadmin-0713@test.local / password123!  → 팀 '검증fc' (로스터 포함) → 이 시드 실행
--   2) verify-empty-0714@test.local     / password123!  → 팀 'EmptyFC' (빈 상태 확인용 — 시드 없음)
-- 재실행 안전: 검증fc의 기존 팀 정보 행을 지우고 다시 삽입. 로컬 개발 DB 전용 — 운영 배포 금지.
DECLARE @TeamId UNIQUEIDENTIFIER =
    (SELECT TOP 1 [TeamId] FROM [dbo].[SoccerTeams] WHERE [TeamName] = '검증fc' AND [DeletedAt] IS NULL);

IF @TeamId IS NULL
BEGIN
    RAISERROR ('Team ''검증fc'' not found — create the verification account/team via onboarding first.', 16, 1);
    RETURN;
END

--.// SoccerTeams 확장 컬럼

UPDATE [dbo].[SoccerTeams]
SET [IsVerified] = 1,
    [TeamType] = '클럽',
    [Region] = '서울 강동구',
    [FoundedYear] = 2018,
    [MonthlyFee] = 250000,
    [IsMonthlyFeePublic] = 1,
    [TrainingDays] = '화목금토',
    [UpdatedAt] = GETUTCDATE()
WHERE [TeamId] = @TeamId;

--.// 핵심가치

DELETE FROM [dbo].[SoccerTeamValues] WHERE [TeamId] = @TeamId;

INSERT INTO [dbo].[SoccerTeamValues] ([TeamId], [Title], [Description], [DisplayOrder]) VALUES
(@TeamId, '성장 중심 지도', '승패보다 개인의 성장 곡선을 봅니다. 모든 선수가 분기마다 코치와 1:1 성장 리뷰를 갖습니다.', 1),
(@TeamId, '출전 기회 보장', '리그·컵 시즌 통산 전원 50% 이상 출전을 원칙으로 운영하고, 출전 기록을 투명하게 공개합니다.', 2),
(@TeamId, '학업 병행 존중', '시험 기간 훈련 조정, 야간 훈련 최소화. 축구와 학업이 함께 가는 일정을 설계합니다.', 3);

--.// 코칭스태프

DELETE FROM [dbo].[SoccerTeamCoaches] WHERE [TeamId] = @TeamId;

INSERT INTO [dbo].[SoccerTeamCoaches]
    ([TeamId], [Name], [Role], [Career], [Certification], [Quote], [Achievements], [InstagramUrl], [YoutubeUrl], [DisplayOrder]) VALUES
(@TeamId, '박정훈', '감독', '지도 12년차 · 前 K리그 유스 코치', 'KFA P2 인증',
 '"실수를 허용해야 도전하는 선수가 됩니다. 저는 결과보다 시도 횟수를 칭찬합니다."',
 '["프로 산하 이적 3명","축구부 진학 12명"]', 'https://instagram.com/verify-coach-park', 'https://youtube.com/@verify-coach-park', 1),
(@TeamId, '김수연', 'GK 코치', '지도 6년차 · 前 WK리그 골키퍼', 'KFA C 인증',
 '"골키퍼는 실점에서 배우는 포지션이에요. 실점 후의 태도를 가장 중요하게 지도합니다."',
 '["GK 전담 트레이닝","주 2회 영상 분석"]', 'https://instagram.com/verify-coach-kim', NULL, 2);

--.// 공식 채널

DELETE FROM [dbo].[SoccerTeamChannels] WHERE [TeamId] = @TeamId;

INSERT INTO [dbo].[SoccerTeamChannels] ([TeamId], [ChannelType], [Name], [Url], [Description], [DisplayOrder]) VALUES
(@TeamId, 'YouTube', '검증fc', 'https://youtube.com/@verify-fc', '경기 하이라이트', 1),
(@TeamId, 'Instagram', '@verify_fc', 'https://instagram.com/verify_fc', '훈련 일상 · 선수 소개 · 학부모 소통', 2);
