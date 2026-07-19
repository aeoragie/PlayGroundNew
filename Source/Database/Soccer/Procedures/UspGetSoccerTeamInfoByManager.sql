-- 팀 관리자 기준 팀 정보 묶음 조회 (대시보드 팀 정보 섹션).
-- 결과셋 4개: 팀(최신 1건) → 핵심가치 → 코칭스태프 → 공식 채널.
-- 호출측은 ProcedureMultipleAsync + MultiQueryReader로 소비 (테이블 엔티티 매핑).
CREATE PROCEDURE [dbo].[UspGetSoccerTeamInfoByManager]
    @ManagerUserId UNIQUEIDENTIFIER
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @TeamId UNIQUEIDENTIFIER = (
        SELECT TOP 1 [TeamId]
        FROM [dbo].[SoccerTeams] WITH (NOLOCK)
        WHERE [ManagerUserId] = @ManagerUserId AND [DeletedAt] IS NULL
        ORDER BY [CreatedAt] DESC);

    SELECT
        t.[TeamId], t.[TeamName], t.[ShortName], t.[TeamType], t.[Region], t.[AgeGroup],
        t.[LogoUrl], t.[CoverImageUrl], t.[Description], t.[Slug], t.[ManagerUserId], t.[IsPublicProfile],
        t.[IsVerified], t.[FoundedYear], t.[MonthlyFee], t.[IsMonthlyFeePublic], t.[TrainingDays],
        t.[DataSource], t.[ExternalId], t.[CreatedAt], t.[UpdatedAt], t.[DeletedAt]
    FROM [dbo].[SoccerTeams] t WITH (NOLOCK)
    WHERE t.[TeamId] = @TeamId;

    SELECT
        v.[TeamValueId], v.[TeamId], v.[Title], v.[Description], v.[DisplayOrder],
        v.[CreatedAt], v.[UpdatedAt], v.[DeletedAt]
    FROM [dbo].[SoccerTeamValues] v WITH (NOLOCK)
    WHERE v.[TeamId] = @TeamId AND v.[DeletedAt] IS NULL
    ORDER BY v.[DisplayOrder];

    SELECT
        c.[CoachId], c.[TeamId], c.[Name], c.[Role], c.[Career], c.[Certification],
        c.[Quote], c.[Achievements], c.[InstagramUrl], c.[YoutubeUrl], c.[DisplayOrder],
        c.[CreatedAt], c.[UpdatedAt], c.[DeletedAt]
    FROM [dbo].[SoccerTeamCoaches] c WITH (NOLOCK)
    WHERE c.[TeamId] = @TeamId AND c.[DeletedAt] IS NULL
    ORDER BY c.[DisplayOrder];

    SELECT
        ch.[ChannelId], ch.[TeamId], ch.[ChannelType], ch.[Name], ch.[Url], ch.[Description],
        ch.[DisplayOrder], ch.[CreatedAt], ch.[UpdatedAt], ch.[DeletedAt]
    FROM [dbo].[SoccerTeamChannels] ch WITH (NOLOCK)
    WHERE ch.[TeamId] = @TeamId AND ch.[DeletedAt] IS NULL
    ORDER BY ch.[DisplayOrder];
END
