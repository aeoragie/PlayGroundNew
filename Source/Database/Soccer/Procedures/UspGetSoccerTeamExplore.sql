-- @entity: SoccerTeamExploreRecord
-- @source: join
-- @join: SoccerTeams AS t (TeamId, TeamName, TeamType, Region, AgeGroup, LogoUrl, CoverImageUrl, Slug, IsVerified, IsRecruiting)
-- 팀 탐색 공개 목록 (비로그인). 결과셋 4개:
--   ① 공개 팀 목록 → ② 핵심가치(제목·순서 — 카드 칩 최대 2는 매핑에서)
--   → ③ 로스터 소속(TeamId만 — 선수단 수 집계는 C#) → ④ 올해 종료·공식 경기(전적 집계는 C#)
-- 필터·정렬·페이징은 클라이언트 담당 (URL 동기화·즉시 적용 — Design.SearchFilter).
CREATE PROCEDURE [dbo].[UspGetSoccerTeamExplore]
AS
BEGIN
    SET NOCOUNT ON;

    -- IsRecruiting은 컬럼이 아니라 **모집 공고에서 파생**한다 (모집중 = Open + 마감일 미경과 공고 보유).
    -- SoccerTeams.IsRecruiting 컬럼은 공고 스키마 도입으로 용도 종료 — 읽지 않는다.
    SELECT
        t.[TeamId], t.[TeamName], t.[TeamType], t.[Region], t.[AgeGroup],
        t.[LogoUrl], t.[CoverImageUrl], t.[Slug], t.[IsVerified],
        CAST(CASE WHEN EXISTS (
            SELECT 1 FROM [dbo].[SoccerTeamRecruitments] r WITH (NOLOCK)
            WHERE r.[TeamId] = t.[TeamId] AND r.[Status] = 'Open' AND r.[DeletedAt] IS NULL
              AND (r.[DeadlineDate] IS NULL OR r.[DeadlineDate] >= CAST(GETUTCDATE() AS DATE)))
        THEN 1 ELSE 0 END AS BIT) AS [IsRecruiting]
    FROM [dbo].[SoccerTeams] t WITH (NOLOCK)
    WHERE t.[IsPublicProfile] = 1 AND t.[DeletedAt] IS NULL AND t.[Slug] IS NOT NULL;

    SELECT
        v.[TeamValueId], v.[TeamId], v.[Title], v.[Description], v.[DisplayOrder],
        v.[CreatedAt], v.[UpdatedAt], v.[DeletedAt]
    FROM [dbo].[SoccerTeamValues] v WITH (NOLOCK)
    JOIN [dbo].[SoccerTeams] t WITH (NOLOCK)
        ON t.[TeamId] = v.[TeamId] AND t.[IsPublicProfile] = 1 AND t.[DeletedAt] IS NULL
    WHERE v.[DeletedAt] IS NULL
    ORDER BY v.[TeamId], v.[DisplayOrder];

    -- 선수단 수 집계용 — TeamId만 필요 (SoccerTeamPlayersEntity 부분 매핑)
    SELECT tp.[TeamId]
    FROM [dbo].[SoccerTeamPlayers] tp WITH (NOLOCK)
    JOIN [dbo].[SoccerPlayers] p WITH (NOLOCK) ON p.[PlayerId] = tp.[PlayerId] AND p.[DeletedAt] IS NULL
    WHERE tp.[Status] = 'Active' AND tp.[DeletedAt] IS NULL;

    -- 올해 전적 집계용 — 종료된 공식 경기만 (SoccerMatchesEntity 부분 매핑, 승/무/패 파생은 C#)
    SELECT m.[HomeTeamId], m.[AwayTeamId], m.[HomeScore], m.[AwayScore]
    FROM [dbo].[SoccerMatches] m WITH (NOLOCK)
    WHERE m.[Status] = 'Completed' AND m.[MatchType] = 'Official' AND m.[DeletedAt] IS NULL
      AND m.[MatchedAt] IS NOT NULL AND YEAR(m.[MatchedAt]) = YEAR(GETUTCDATE())
      AND (m.[HomeTeamId] IS NOT NULL OR m.[AwayTeamId] IS NOT NULL);
END
