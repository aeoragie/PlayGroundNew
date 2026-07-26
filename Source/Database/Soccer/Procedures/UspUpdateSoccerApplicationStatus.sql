-- 지원 상태 전환 (팀 관리자). 소유는 지원→공고→팀 ManagerUserId로 강제.
-- 허용 전환: Pending→Reviewing · Reviewing→Accepted · Reviewing→Rejected · Pending→Rejected.
-- Rejected는 RejectedAt를 찍고(30일 재지원 쿨다운 기준), 모든 전환이 ReviewedAt·UpdatedAt를 갱신한다.
-- **수락(Accepted)은 여기서 상태만 바꾸고 로스터 편입은 별도 단계(보호자 동의)로 넘긴다.**
-- 수락 시 보호자에게 **선수단 초대(RosterInvite) 알림**을 같은 트랜잭션으로 발송한다(Claim 승인 알림과 같은 방식).
--   알림은 액션형 — 보호자가 [초대 확인]을 누르면 UspConfirmSoccerApplicationInvite가 로스터에 편입한다.
--   처리 여부(RosterInvite 알림의 상태)는 스냅샷이 아니라 SoccerApplications.ConfirmedAt를 라이브로 조인해 파생한다.
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
        BEGIN TRY
            BEGIN TRANSACTION;

            UPDATE [dbo].[SoccerApplications]
            SET [Status] = @NewStatus,
                [ReviewedAt] = GETUTCDATE(),
                [RejectedAt] = CASE WHEN @NewStatus = 'Rejected' THEN GETUTCDATE() ELSE [RejectedAt] END,
                [UpdatedAt] = GETUTCDATE()
            WHERE [ApplicationId] = @ApplicationId AND [DeletedAt] IS NULL;

            -- 수락 → 보호자에게 선수단 초대 알림 (스냅샷: 팀명 + 선수명, RefId = ApplicationId).
            -- 같은 지원의 RosterInvite가 이미 있으면 만들지 않는다(재실행 멱등 — 수락은 단방향이지만 방어).
            IF @NewStatus = 'Accepted'
            BEGIN
                INSERT INTO [dbo].[SoccerNotifications]
                    ([RecipientUserId], [NotificationType], [RefId], [TargetPlayerId], [PlayerName], [TeamName])
                SELECT a.[GuardianUserId], 'RosterInvite', a.[ApplicationId], a.[PlayerId], p.[Name], t.[TeamName]
                FROM [dbo].[SoccerApplications] a
                JOIN [dbo].[SoccerPlayers] p ON p.[PlayerId] = a.[PlayerId]
                JOIN [dbo].[SoccerTeamRecruitments] r ON r.[RecruitmentId] = a.[RecruitmentId]
                JOIN [dbo].[SoccerTeams] t ON t.[TeamId] = r.[TeamId]
                WHERE a.[ApplicationId] = @ApplicationId
                  AND NOT EXISTS (
                      SELECT 1 FROM [dbo].[SoccerNotifications] n
                      WHERE n.[NotificationType] = 'RosterInvite' AND n.[RefId] = a.[ApplicationId]);
            END

            COMMIT TRANSACTION;
        END TRY
        BEGIN CATCH
            IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
            THROW;
        END CATCH

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
