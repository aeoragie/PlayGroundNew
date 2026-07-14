-- 로컬 검증용 선수 커리어·포트폴리오 시드 — 검증 선수 계정(D01·D11)이 연결된 선수에 주입.
-- 선행: VerificationPlayerLinks.Seed.sql (계정↔선수 UserId 연결). 고정 UserId로 선수를 해석하므로
-- 리그 시드 재실행 후에도 이 스크립트만 다시 돌리면 된다.
-- 김정현(U15) = 커리어 2건(현재 팀 확인됨 + 과거 본인 입력) + 영상 3건(대표 1).
-- 신준우(U12) = 커리어 1건(본인 입력) + 영상 0건 — 빈 포트폴리오 상태 검증.
-- 재실행 안전(삭제 후 삽입). 로컬 개발 DB 전용 — 운영 배포 금지.

DECLARE @U12PlayerId UNIQUEIDENTIFIER =
    (SELECT TOP 1 [PlayerId] FROM [dbo].[SoccerPlayers]
     WHERE [UserId] = 'A0000000-0000-0000-0000-000000000D01' AND [DeletedAt] IS NULL);
DECLARE @U15PlayerId UNIQUEIDENTIFIER =
    (SELECT TOP 1 [PlayerId] FROM [dbo].[SoccerPlayers]
     WHERE [UserId] = 'A0000000-0000-0000-0000-000000000D11' AND [DeletedAt] IS NULL);

IF @U12PlayerId IS NULL OR @U15PlayerId IS NULL
BEGIN
    RAISERROR('Verification players not found. Run VerificationPlayerLinks.Seed.sql first.', 16, 1);
    RETURN;
END

DECLARE @U15TeamId UNIQUEIDENTIFIER =
    (SELECT TOP 1 tp.[TeamId] FROM [dbo].[SoccerTeamPlayers] tp
     WHERE tp.[PlayerId] = @U15PlayerId AND tp.[Status] = 'Active' AND tp.[DeletedAt] IS NULL);
DECLARE @U12TeamId UNIQUEIDENTIFIER =
    (SELECT TOP 1 tp.[TeamId] FROM [dbo].[SoccerTeamPlayers] tp
     WHERE tp.[PlayerId] = @U12PlayerId AND tp.[Status] = 'Active' AND tp.[DeletedAt] IS NULL);

--.// 커리어

DELETE FROM [dbo].[SoccerPlayerCareers] WHERE [PlayerId] IN (@U12PlayerId, @U15PlayerId);

INSERT INTO [dbo].[SoccerPlayerCareers]
    ([PlayerId], [TeamName], [TeamId], [IsCurrent], [BadgeLabel], [StartDate], [EndDate], [Role], [Note], [IsVerified]) VALUES
(@U15PlayerId, '광주광주FCU15', @U15TeamId, 1, '광주 지역 대표 선발', '2024-03-01', NULL,
 'U15 · GK · 주전', '2026 시즌 리그 무실점 경기 6회. 팀 주장.', 1),
(@U15PlayerId, '광주서구유소년FC', NULL, 0, NULL, '2021-05-01', '2024-02-01',
 'U12 · GK', '유소년 첫 소속팀. 지역 대회 4회 출전.', 0),
(@U12PlayerId, '서울신답FCU12', @U12TeamId, 1, NULL, '2025-03-01', NULL,
 'U12 · MF', '드리블 돌파가 강점. 주중 훈련 개근.', 0);

--.// 포트폴리오 영상 (김정현만 — 신준우는 빈 상태 검증)

DELETE FROM [dbo].[SoccerPlayerPortfolioVideos] WHERE [PlayerId] IN (@U12PlayerId, @U15PlayerId);

INSERT INTO [dbo].[SoccerPlayerPortfolioVideos]
    ([PlayerId], [Title], [VideoUrl], [ThumbnailUrl], [DurationSeconds], [IsPrimary], [Tags], [RecordedOn]) VALUES
(@U15PlayerId, '2026 상반기 하이라이트 — 선방 모음', 'https://youtube.com/watch?v=verify-u15-1',
 'https://images.pexels.com/photos/33257251/pexels-photo-33257251.jpeg?auto=compress&cs=tinysrgb&w=1200',
 154, 1, '["#선방","#빌드업","#리더십"]', '2026-07-01'),
(@U15PlayerId, '리그 12R vs 무등FC — 무실점 경기', 'https://youtube.com/watch?v=verify-u15-2',
 'https://images.pexels.com/photos/35481332/pexels-photo-35481332.jpeg?auto=compress&cs=tinysrgb&w=600',
 102, 0, '["#선방"]', '2026-07-05'),
(@U15PlayerId, '개인 훈련 — 캐칭 루틴', 'https://youtube.com/watch?v=verify-u15-3',
 NULL, 130, 0, NULL, '2026-06-15');
