-- @entity: SoccerAgentViewRequestRecord
-- @source: join
-- @join: SoccerAgentViewRequests AS r (RequestId, AgentId, Message, Status, RequestedAt, ExpiresAt)
-- @join: SoccerPlayers AS p (PlayerId, Name, AgeGroup)
-- @join: SoccerTeamPlayers AS tp (Position)
-- 열람 요청 심사 화면 조회 — 심사 주체(GuardianUserId) 본인 것만. 결과셋 3개:
-- ① 요청+선수(이름·메타) → ② 에이전트 프로필(테이블 엔티티 — 이름 충돌 회피로 결과셋 분리) → ③ 열람 기록(최신순).
CREATE PROCEDURE [dbo].[UspGetSoccerAgentViewRequest]
    @GuardianUserId UNIQUEIDENTIFIER,
    @RequestId UNIQUEIDENTIFIER
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        r.[RequestId], r.[AgentId], r.[Message], r.[Status], r.[RequestedAt], r.[ExpiresAt],
        p.[PlayerId], p.[Name], p.[AgeGroup], tp.[Position]
    FROM [dbo].[SoccerAgentViewRequests] r WITH (NOLOCK)
    JOIN [dbo].[SoccerPlayers] p WITH (NOLOCK) ON p.[PlayerId] = r.[PlayerId] AND p.[DeletedAt] IS NULL
    LEFT JOIN [dbo].[SoccerTeamPlayers] tp WITH (NOLOCK)
        ON tp.[TeamId] = p.[TeamId] AND tp.[PlayerId] = p.[PlayerId]
       AND tp.[Status] = 'Active' AND tp.[DeletedAt] IS NULL
    WHERE r.[RequestId] = @RequestId AND r.[GuardianUserId] = @GuardianUserId AND r.[DeletedAt] IS NULL;

    SELECT
        a.[AgentId], a.[UserId], a.[Name], a.[AgencyName], a.[RegisteredYear], a.[IsVerified],
        a.[BrokerageCount], a.[Rating], a.[ActiveRegions], a.[CreatedAt], a.[UpdatedAt], a.[DeletedAt]
    FROM [dbo].[SoccerAgentProfiles] a WITH (NOLOCK)
    JOIN [dbo].[SoccerAgentViewRequests] r WITH (NOLOCK)
        ON r.[AgentId] = a.[AgentId] AND r.[RequestId] = @RequestId AND r.[GuardianUserId] = @GuardianUserId
    WHERE a.[DeletedAt] IS NULL;

    SELECT
        l.[LogId], l.[RequestId], l.[EventType], l.[CreatedAt]
    FROM [dbo].[SoccerAgentViewLogs] l WITH (NOLOCK)
    JOIN [dbo].[SoccerAgentViewRequests] r WITH (NOLOCK)
        ON r.[RequestId] = l.[RequestId] AND r.[GuardianUserId] = @GuardianUserId
    WHERE l.[RequestId] = @RequestId
    ORDER BY l.[CreatedAt] DESC;
END
