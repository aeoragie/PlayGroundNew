-- 공개 팀 홈페이지 조회 (Slug 기준, 비로그인 읽기전용). 비공개(IsPublicProfile=0)·미존재 팀은 빈 결과.
-- 결과셋 5개: 팀 → 핵심가치 → 코칭스태프 → 공식 채널 → 로스터.
-- 호출측은 ProcedureMultipleAsync + MultiQueryReader로 소비 (로스터는 SoccerTeamRosterRecord 매핑 재사용).
-- 공개/비공개 규칙(UserId 등 관리 정보 미노출)은 Persistence 매핑에서 적용한다.
CREATE PROCEDURE [dbo].[UspGetSoccerTeamHomeBySlug]
    @Slug VARCHAR(100)
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @TeamId UNIQUEIDENTIFIER = (
        SELECT TOP 1 [TeamId]
        FROM [dbo].[SoccerTeams] WITH (NOLOCK)
        WHERE [Slug] = @Slug AND [IsPublicProfile] = 1 AND [DeletedAt] IS NULL
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

    -- 프로필 공개(FieldName='Profile')를 끈 선수는 공개 로스터에서 제외 (행 없으면 기본 공개)
    SELECT
        tp.[TeamPlayerId], tp.[JerseyNumber], tp.[Position], tp.[Grade],
        p.[PlayerId], p.[Name], p.[Slug], p.[PhotoUrl], p.[AgeGroup], p.[UserId]
    FROM [dbo].[SoccerTeamPlayers] tp WITH (NOLOCK)
    JOIN [dbo].[SoccerPlayers] p WITH (NOLOCK) ON p.[PlayerId] = tp.[PlayerId]
    LEFT JOIN [dbo].[SoccerPlayerFieldVisibilities] fv WITH (NOLOCK)
        ON fv.[PlayerId] = p.[PlayerId] AND fv.[FieldName] = 'Profile'
    WHERE tp.[TeamId] = @TeamId
      AND tp.[Status] = 'Active' AND tp.[DeletedAt] IS NULL
      AND p.[DeletedAt] IS NULL
      AND COALESCE(fv.[IsPublic], 1) = 1
    ORDER BY
        CASE WHEN TRY_CAST(tp.[JerseyNumber] AS INT) IS NULL THEN 1 ELSE 0 END,
        TRY_CAST(tp.[JerseyNumber] AS INT),
        p.[Name];
END
