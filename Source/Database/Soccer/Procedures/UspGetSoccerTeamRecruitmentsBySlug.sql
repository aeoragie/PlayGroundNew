-- 공개 팀 홈 모집 탭 (Slug 기준, 비로그인 읽기전용). 비공개·미존재 팀은 빈 결과.
-- 정렬: 모집중(Open·마감일 미경과) 먼저 → 최신순. "모집중" 최종 판정은 Persistence(단일 기준 공유).
-- RS1은 SoccerTeamRecruitmentsEntity로 매핑(AgeGroup·PositionsJson·Capacity 실컬럼 포함, 지원 통합 E5).
-- 수락 수(AcceptedCount)는 계산 컬럼이라 제너레이터가 매핑 못 한다 → RS2로 수락 지원의 공고 Id만 내리고
-- Persistence가 공고별로 COUNT한다("정원 N/M"·정원 충족 판정).
CREATE PROCEDURE [dbo].[UspGetSoccerTeamRecruitmentsBySlug]
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
        r.[RecruitmentId], r.[TeamId], r.[Title], r.[Description], r.[ConditionsJson],
        r.[DeadlineAt], r.[AgeGroup], r.[PositionsJson], r.[Capacity],
        r.[Status], r.[CreatedAt], r.[UpdatedAt], r.[DeletedAt]
    FROM [dbo].[SoccerTeamRecruitments] r WITH (NOLOCK)
    WHERE r.[TeamId] = @TeamId AND r.[DeletedAt] IS NULL
    ORDER BY
        CASE WHEN r.[Status] = 'Open'
              AND (r.[DeadlineAt] IS NULL OR r.[DeadlineAt] > GETUTCDATE())
             THEN 0 ELSE 1 END,
        r.[CreatedAt] DESC;

    -- RS2: 수락된 지원의 공고 Id (공고별 COUNT = AcceptedCount)
    SELECT a.[RecruitmentId]
    FROM [dbo].[SoccerApplications] a WITH (NOLOCK)
    JOIN [dbo].[SoccerTeamRecruitments] r WITH (NOLOCK)
        ON r.[RecruitmentId] = a.[RecruitmentId] AND r.[TeamId] = @TeamId AND r.[DeletedAt] IS NULL
    WHERE a.[Status] = 'Accepted' AND a.[DeletedAt] IS NULL;
END
