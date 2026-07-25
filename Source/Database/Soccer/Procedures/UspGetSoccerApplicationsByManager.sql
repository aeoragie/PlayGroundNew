-- @entity: SoccerApplicationManagerRecord
-- @source: join
-- @join: SoccerApplications AS a (ApplicationId, RecruitmentId, PlayerId, DesiredPosition, Introduction, Status, Route, RefAgentId, CreatedAt)
-- @join: SoccerPlayers AS p (Name, AgeGroup, PhotoUrl)
-- @join: SoccerTeamRecruitments AS r (Title)
-- @join: SoccerTeamPlayers AS tp (Position)
-- 팀 대시보드 지원자 목록 — 관리자 소유 팀의 공고에 접수된 지원 전부. 최신순.
-- 소유는 공고→팀 ManagerUserId 조인으로 강제(남의 팀 지원은 애초에 나오지 않는다).
-- Name·AgeGroup·PhotoUrl은 지원 선수(SoccerPlayers), Title은 공고, Position은 선수의 소속 로스터(있으면).
-- 지원자는 이 팀 로스터가 아닐 수 있어 Position은 OUTER APPLY(없으면 NULL) — 지원의 DesiredPosition과 별개.
-- RS2: 이 목록이 참조하는 에이전트 프로필(AgentRef 경로) — 이름은 Persistence가 RefAgentId로 매핑.
--      에이전트 추천 지원의 생산자는 에이전트 서비스다(결정 4·7) — 현재 경로는 전부 Direct라 보통 빈 결과.
CREATE PROCEDURE [dbo].[UspGetSoccerApplicationsByManager]
    @ManagerUserId UNIQUEIDENTIFIER
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        a.[ApplicationId], a.[RecruitmentId], a.[PlayerId], a.[DesiredPosition], a.[Introduction],
        a.[Status], a.[Route], a.[RefAgentId], a.[CreatedAt],
        p.[Name], p.[AgeGroup], p.[PhotoUrl],
        r.[Title],
        tp.[Position]
    FROM [dbo].[SoccerApplications] a WITH (NOLOCK)
    JOIN [dbo].[SoccerTeamRecruitments] r WITH (NOLOCK)
        ON r.[RecruitmentId] = a.[RecruitmentId] AND r.[DeletedAt] IS NULL
    JOIN [dbo].[SoccerTeams] t WITH (NOLOCK)
        ON t.[TeamId] = r.[TeamId] AND t.[ManagerUserId] = @ManagerUserId AND t.[DeletedAt] IS NULL
    JOIN [dbo].[SoccerPlayers] p WITH (NOLOCK)
        ON p.[PlayerId] = a.[PlayerId] AND p.[DeletedAt] IS NULL
    OUTER APPLY (
        SELECT TOP 1 x.[Position]
        FROM [dbo].[SoccerTeamPlayers] x WITH (NOLOCK)
        WHERE x.[PlayerId] = a.[PlayerId] AND x.[Status] = 'Active' AND x.[DeletedAt] IS NULL
        ORDER BY x.[CreatedAt] DESC) tp
    WHERE a.[DeletedAt] IS NULL
    ORDER BY a.[CreatedAt] DESC;

    -- RS2: 참조 에이전트 프로필 (AgentId, Name) — SoccerAgentProfilesEntity로 읽어 이름 사전 구성
    SELECT ag.[AgentId], ag.[Name]
    FROM [dbo].[SoccerAgentProfiles] ag WITH (NOLOCK)
    WHERE ag.[DeletedAt] IS NULL
      AND ag.[AgentId] IN (
        SELECT a.[RefAgentId]
        FROM [dbo].[SoccerApplications] a WITH (NOLOCK)
        JOIN [dbo].[SoccerTeamRecruitments] r WITH (NOLOCK)
            ON r.[RecruitmentId] = a.[RecruitmentId] AND r.[DeletedAt] IS NULL
        JOIN [dbo].[SoccerTeams] t WITH (NOLOCK)
            ON t.[TeamId] = r.[TeamId] AND t.[ManagerUserId] = @ManagerUserId AND t.[DeletedAt] IS NULL
        WHERE a.[DeletedAt] IS NULL AND a.[RefAgentId] IS NOT NULL);
END
