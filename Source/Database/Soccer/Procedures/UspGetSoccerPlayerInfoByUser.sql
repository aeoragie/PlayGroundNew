-- @entity: SoccerPlayerInfoRecord
-- @source: join
-- @join: SoccerPlayers AS p (PlayerId, Name, PhotoUrl, BirthDate, AgeGroup, HeightCm, WeightKg, PreferredFoot, SchoolName, GuardianPhone, IsGuardianManaged)
-- @join: SoccerTeamPlayers AS tp (JerseyNumber, Position, Grade)
-- @join: SoccerTeams AS t (TeamName)
-- 관리 주체(UserId) 기준 선수 프로필 묶음 조회 (선수 대시보드 프로필 섹션).
-- 결과셋 3개: 선수+소속(최신 1건) → 항목별 공개 설정 → 가족 계정.
-- 호출측은 ProcedureMultipleAsync + MultiQueryReader로 소비. 보호자 연락처 마스킹은 Persistence 매핑에서.
CREATE PROCEDURE [dbo].[UspGetSoccerPlayerInfoByUser]
    @UserId UNIQUEIDENTIFIER
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @PlayerId UNIQUEIDENTIFIER = (
        SELECT TOP 1 [PlayerId]
        FROM [dbo].[SoccerPlayers] WITH (NOLOCK)
        WHERE [UserId] = @UserId AND [DeletedAt] IS NULL
        ORDER BY [CreatedAt] DESC);

    SELECT
        p.[PlayerId], p.[Name], p.[PhotoUrl], p.[BirthDate], p.[AgeGroup],
        p.[HeightCm], p.[WeightKg], p.[PreferredFoot], p.[SchoolName], p.[GuardianPhone], p.[IsGuardianManaged],
        tp.[JerseyNumber], tp.[Position], tp.[Grade],
        t.[TeamName]
    FROM [dbo].[SoccerPlayers] p WITH (NOLOCK)
    LEFT JOIN [dbo].[SoccerTeamPlayers] tp WITH (NOLOCK)
        ON tp.[PlayerId] = p.[PlayerId] AND tp.[Status] = 'Active' AND tp.[DeletedAt] IS NULL
    LEFT JOIN [dbo].[SoccerTeams] t WITH (NOLOCK)
        ON t.[TeamId] = tp.[TeamId] AND t.[DeletedAt] IS NULL
    WHERE p.[PlayerId] = @PlayerId;

    SELECT
        v.[VisibilityId], v.[PlayerId], v.[FieldName], v.[IsPublic], v.[CreatedAt], v.[UpdatedAt]
    FROM [dbo].[SoccerPlayerFieldVisibilities] v WITH (NOLOCK)
    WHERE v.[PlayerId] = @PlayerId;

    SELECT
        f.[FamilyLinkId], f.[PlayerId], f.[UserId], f.[MemberName], f.[Role], f.[DisplayOrder],
        f.[CreatedAt], f.[UpdatedAt], f.[DeletedAt]
    FROM [dbo].[SoccerPlayerFamilyLinks] f WITH (NOLOCK)
    WHERE f.[PlayerId] = @PlayerId AND f.[DeletedAt] IS NULL
    ORDER BY f.[DisplayOrder];
END
