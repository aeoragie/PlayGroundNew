-- 지원 상태 전환 (팀 관리자). 소유는 지원→공고→팀 ManagerUserId로 강제.
-- 허용 전환: Pending→Reviewing · Reviewing→Accepted · Reviewing→Rejected · Pending→Rejected.
-- Rejected는 RejectedAt를 찍고(30일 재지원 쿨다운 기준), 모든 전환이 ReviewedAt·UpdatedAt를 갱신한다.
-- **수락(Accepted)은 여기서 상태만 바꾼다 — 로스터 편입은 별도 단계(보호자 동의)로 넘긴다.**
-- 반환은 갱신된 행(ApplicationId, Status) — 소유 아님·잘못된 전환은 빈 결과(Command가 Forbidden).
-- @entity는 UspCreateSoccerApplication이 SoccerApplicationCreateRecord로 선언했으므로 여기서는 마커를 두지 않는다.
CREATE PROCEDURE [dbo].[UspUpdateSoccerApplicationStatus]
    @ManagerUserId   UNIQUEIDENTIFIER,
    @ApplicationId   UNIQUEIDENTIFIER,
    @NewStatus       VARCHAR(20)
AS
BEGIN
    SET NOCOUNT ON;

    -- 소유 확인 + 현재 상태 해석 (남의 팀이면 0행 → @Current NULL)
    DECLARE @Current VARCHAR(20) = (
        SELECT a.[Status]
        FROM [dbo].[SoccerApplications] a WITH (NOLOCK)
        JOIN [dbo].[SoccerTeamRecruitments] r WITH (NOLOCK)
            ON r.[RecruitmentId] = a.[RecruitmentId] AND r.[DeletedAt] IS NULL
        JOIN [dbo].[SoccerTeams] t WITH (NOLOCK)
            ON t.[TeamId] = r.[TeamId] AND t.[ManagerUserId] = @ManagerUserId AND t.[DeletedAt] IS NULL
        WHERE a.[ApplicationId] = @ApplicationId AND a.[DeletedAt] IS NULL);

    DECLARE @Valid BIT = CASE
        WHEN @Current = 'Pending'   AND @NewStatus = 'Reviewing' THEN 1
        WHEN @Current = 'Pending'   AND @NewStatus = 'Rejected'  THEN 1
        WHEN @Current = 'Reviewing' AND @NewStatus = 'Accepted'  THEN 1
        WHEN @Current = 'Reviewing' AND @NewStatus = 'Rejected'  THEN 1
        ELSE 0 END;

    IF @Valid = 1
    BEGIN
        UPDATE [dbo].[SoccerApplications]
        SET [Status] = @NewStatus,
            [ReviewedAt] = GETUTCDATE(),
            [RejectedAt] = CASE WHEN @NewStatus = 'Rejected' THEN GETUTCDATE() ELSE [RejectedAt] END,
            [UpdatedAt] = GETUTCDATE()
        WHERE [ApplicationId] = @ApplicationId AND [DeletedAt] IS NULL;

        SELECT a.[Status], a.[ApplicationId]
        FROM [dbo].[SoccerApplications] a WITH (NOLOCK)
        WHERE a.[ApplicationId] = @ApplicationId;
    END
    ELSE
    BEGIN
        SELECT a.[Status], a.[ApplicationId]
        FROM [dbo].[SoccerApplications] a WITH (NOLOCK)
        WHERE 1 = 0;
    END
END
