-- 에이전트 열람 요청 검증 시드 — **에이전트 서비스(요청 생성·프로필)를 흉내 낸다** (sql-b6 성격).
-- 대상 선수: 김정현(U15) — 보호자 = verify-player-u15 계정(김정현 관리 계정).
DECLARE @Guardian UNIQUEIDENTIFIER = (
    SELECT [UserId] FROM [PlayGround_Account].dbo.[Users] WHERE [Email] = 'verify-player-u15@test.local');
DECLARE @Player UNIQUEIDENTIFIER = (
    SELECT TOP 1 [PlayerId] FROM [dbo].[SoccerPlayers] WHERE [UserId] = @Guardian AND [DeletedAt] IS NULL);

IF @Guardian IS NULL OR @Player IS NULL
BEGIN
    RAISERROR('guardian or player not found', 16, 1);
    RETURN;
END

DECLARE @AgentId UNIQUEIDENTIFIER = 'A9000000-0000-0000-0000-000000000001';

IF NOT EXISTS (SELECT 1 FROM [dbo].[SoccerAgentProfiles] WHERE [AgentId] = @AgentId)
BEGIN
    INSERT INTO [dbo].[SoccerAgentProfiles]
        ([AgentId], [Name], [AgencyName], [RegisteredYear], [IsVerified], [BrokerageCount], [Rating], [ActiveRegions])
    VALUES (@AgentId, '박OO', '드림 스포츠 에이전시', 2024, 1, 14, 4.7, '서울·경기');
END

DECLARE @RequestId UNIQUEIDENTIFIER = NEWID();
INSERT INTO [dbo].[SoccerAgentViewRequests] ([RequestId], [AgentId], [PlayerId], [GuardianUserId], [Message])
VALUES (@RequestId, @AgentId, @Player, @Guardian,
        N'선수의 상세 정보 열람을 요청합니다. U15 리그 기록을 보고 연락드려요. 서울 지역 고등 팀 두 곳에서 공격수를 찾고 있습니다.');

SELECT CONVERT(VARCHAR(36), @RequestId) AS [RequestId];
