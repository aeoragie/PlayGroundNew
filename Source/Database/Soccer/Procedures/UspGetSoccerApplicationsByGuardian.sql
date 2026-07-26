-- @entity: SoccerApplicationGuardianRecord
-- @source: join
-- @join: SoccerApplications AS a (ApplicationId, RecruitmentId, PlayerId, DesiredPosition, Status, CreatedAt, ConfirmedAt)
-- @join: SoccerPlayers AS p (Name)
-- @join: SoccerTeamRecruitments AS r (Title)
-- @join: SoccerTeams AS t (TeamName, Slug)
-- 보호자 지원 현황 — 내가(GuardianUserId) 올린 지원 전부. 최신순.
-- Name은 지원 자녀(SoccerPlayers), Title은 공고, TeamName·Slug는 그 공고의 팀.
-- TeamSlug는 공개 여부와 무관하게 내 지원 맥락(팀 홈 링크)이므로 그대로 내려준다.
CREATE PROCEDURE [dbo].[UspGetSoccerApplicationsByGuardian]
    @GuardianUserId UNIQUEIDENTIFIER
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        a.[ApplicationId], a.[RecruitmentId], a.[PlayerId], a.[DesiredPosition], a.[Status], a.[CreatedAt], a.[ConfirmedAt],
        p.[Name],
        r.[Title],
        t.[TeamName], t.[Slug]
    FROM [dbo].[SoccerApplications] a WITH (NOLOCK)
    JOIN [dbo].[SoccerPlayers] p WITH (NOLOCK)
        ON p.[PlayerId] = a.[PlayerId] AND p.[DeletedAt] IS NULL
    JOIN [dbo].[SoccerTeamRecruitments] r WITH (NOLOCK)
        ON r.[RecruitmentId] = a.[RecruitmentId] AND r.[DeletedAt] IS NULL
    JOIN [dbo].[SoccerTeams] t WITH (NOLOCK)
        ON t.[TeamId] = r.[TeamId] AND t.[DeletedAt] IS NULL
    WHERE a.[GuardianUserId] = @GuardianUserId AND a.[DeletedAt] IS NULL
    ORDER BY a.[CreatedAt] DESC;
END
