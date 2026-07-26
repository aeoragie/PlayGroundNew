-- 선수단 초대 확인 → 로스터 편입 (보호자). Design.Application §4 — 수락은 팀이, 편입은 보호자 동의로.
-- 소유·상태 검증: 내(GuardianUserId) 지원이고 Status='Accepted'일 때만. 아니면 빈 결과(존재 여부 미노출 → Command가 Forbidden).
-- 편입 = SoccerTeamPlayers 행 삽입뿐이다 — 선수(SoccerPlayers)는 이미 있는 내 자녀라 새로 만들지 않고 초대코드도 없다.
--   같은 팀에 Active 소속이 이미 있으면 건너뛴다(멱등). ConfirmedAt를 편입 완료 마커로 찍는다.
--   RosterInvite 알림은 읽음 처리(액션형 — 처리 후 완료 박스로 그려진다).
-- 반환은 UspCreateSoccerApplication과 같은 모양(SoccerApplicationCreateRecord: Status, ApplicationId) — 마커를 다시 두지 않는다.
CREATE PROCEDURE [dbo].[UspConfirmSoccerApplicationInvite]
    @GuardianUserId UNIQUEIDENTIFIER,
    @ApplicationId  UNIQUEIDENTIFIER
AS
BEGIN
    SET NOCOUNT ON;

    -- 소유 + 수락 상태 확인 → 편입 대상 팀·선수 파생 (아니면 @TeamId NULL)
    DECLARE @TeamId UNIQUEIDENTIFIER, @PlayerId UNIQUEIDENTIFIER;

    SELECT @TeamId = r.[TeamId], @PlayerId = a.[PlayerId]
    FROM [dbo].[SoccerApplications] a WITH (NOLOCK)
    JOIN [dbo].[SoccerTeamRecruitments] r WITH (NOLOCK)
        ON r.[RecruitmentId] = a.[RecruitmentId] AND r.[DeletedAt] IS NULL
    WHERE a.[ApplicationId] = @ApplicationId
      AND a.[GuardianUserId] = @GuardianUserId
      AND a.[Status] = 'Accepted'
      AND a.[DeletedAt] IS NULL;

    IF @TeamId IS NOT NULL
    BEGIN
        BEGIN TRY
            BEGIN TRANSACTION;

            -- 로스터 편입 — 같은 팀에 Active 소속이 없을 때만(멱등). 소프트 삭제된 과거 소속은 무시하고 새로 만든다.
            IF NOT EXISTS (
                SELECT 1 FROM [dbo].[SoccerTeamPlayers]
                WHERE [TeamId] = @TeamId AND [PlayerId] = @PlayerId
                  AND [Status] = 'Active' AND [DeletedAt] IS NULL)
            BEGIN
                INSERT INTO [dbo].[SoccerTeamPlayers] ([TeamId], [PlayerId], [Status])
                VALUES (@TeamId, @PlayerId, 'Active');
            END

            -- 편입 완료 마커
            UPDATE [dbo].[SoccerApplications]
            SET [ConfirmedAt] = GETUTCDATE(), [UpdatedAt] = GETUTCDATE()
            WHERE [ApplicationId] = @ApplicationId AND [ConfirmedAt] IS NULL;

            -- 선수단 초대 알림 읽음 처리 (처리 = 읽음)
            UPDATE [dbo].[SoccerNotifications]
            SET [IsRead] = 1, [ReadAt] = GETUTCDATE()
            WHERE [RecipientUserId] = @GuardianUserId AND [NotificationType] = 'RosterInvite'
              AND [RefId] = @ApplicationId AND [IsRead] = 0;

            COMMIT TRANSACTION;
        END TRY
        BEGIN CATCH
            IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
            THROW;
        END CATCH
    END

    -- 성공(@TeamId 있음)이면 1행, 거부면 0행 — Command가 Forbidden으로 변환한다.
    SELECT a.[Status], a.[ApplicationId]
    FROM [dbo].[SoccerApplications] a WITH (NOLOCK)
    WHERE a.[ApplicationId] = @ApplicationId AND @TeamId IS NOT NULL;
END
