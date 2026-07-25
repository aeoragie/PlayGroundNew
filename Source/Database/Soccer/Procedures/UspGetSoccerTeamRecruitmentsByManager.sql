-- 팀 대시보드 모집 섹션 — 관리자 소유 팀의 공고 목록 (정렬 규칙은 공개 조회와 동일).
-- RS1은 SoccerTeamRecruitmentsEntity로 매핑(AgeGroup·PositionsJson·Capacity 실컬럼 포함, 지원 통합 E5).
-- 수락 수(AcceptedCount)는 계산 컬럼이라 제너레이터가 매핑 못 한다 → RS2로 수락 지원의 공고 Id만 내리고
-- Persistence가 공고별로 COUNT한다("정원 N/M"·정원 충족 판정).
CREATE PROCEDURE [dbo].[UspGetSoccerTeamRecruitmentsByManager]
    @ManagerUserId UNIQUEIDENTIFIER
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        r.[RecruitmentId], r.[TeamId], r.[Title], r.[Description], r.[ConditionsJson],
        r.[DeadlineDate], r.[AgeGroup], r.[PositionsJson], r.[Capacity],
        r.[Status], r.[CreatedAt], r.[UpdatedAt], r.[DeletedAt]
    FROM [dbo].[SoccerTeamRecruitments] r WITH (NOLOCK)
    JOIN [dbo].[SoccerTeams] t WITH (NOLOCK)
        ON t.[TeamId] = r.[TeamId] AND t.[ManagerUserId] = @ManagerUserId AND t.[DeletedAt] IS NULL
    WHERE r.[DeletedAt] IS NULL
    ORDER BY
        CASE WHEN r.[Status] = 'Open'
              AND (r.[DeadlineDate] IS NULL OR r.[DeadlineDate] >= CAST(GETUTCDATE() AS DATE))
             THEN 0 ELSE 1 END,
        r.[CreatedAt] DESC;

    -- RS2: 수락된 지원의 공고 Id (공고별 COUNT = AcceptedCount)
    SELECT a.[RecruitmentId]
    FROM [dbo].[SoccerApplications] a WITH (NOLOCK)
    JOIN [dbo].[SoccerTeamRecruitments] r WITH (NOLOCK)
        ON r.[RecruitmentId] = a.[RecruitmentId] AND r.[DeletedAt] IS NULL
    JOIN [dbo].[SoccerTeams] t WITH (NOLOCK)
        ON t.[TeamId] = r.[TeamId] AND t.[ManagerUserId] = @ManagerUserId AND t.[DeletedAt] IS NULL
    WHERE a.[Status] = 'Accepted' AND a.[DeletedAt] IS NULL;
END
