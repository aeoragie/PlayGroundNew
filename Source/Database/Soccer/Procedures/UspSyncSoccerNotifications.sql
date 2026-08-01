-- 알림 조회 전처리 (Design.ClaimFlow + DECISION.NOTIFICATIONCENTER) — 목록 조회 프로시저들이 EXEC로 공유한다.
-- ① 지연 생성: 기록 수정 심사 결과 · 에이전트 열람 요청(둘 다 발송 훅이 없어 조회 시점에 멱등 INSERT).
-- ② 보관 90일 자동 정리: 90일 경과분 삭제 — 단 **미해소 액션형(연결 요청 Pending·선수단 초대 미확인)은 예외 유지**.
CREATE PROCEDURE [dbo].[UspSyncSoccerNotifications]
    @UserId UNIQUEIDENTIFIER
AS
BEGIN
    SET NOCOUNT ON;

    --.// ① 기록 수정 신청 심사 결과 지연 동기화 (MetaText=항목, SubText=심사 상태)
    INSERT INTO [dbo].[SoccerNotifications]
        ([RecipientUserId], [NotificationType], [RefId], [MetaText], [SubText], [TeamName], [CreatedAt])
    SELECT c.[RequestedByUserId], 'CorrectionReviewed', c.[CorrectionId],
           c.[FieldType], c.[Status], t.[TeamName], COALESCE(c.[ReviewedAt], GETUTCDATE())
    FROM [dbo].[SoccerRecordCorrections] c
    LEFT JOIN [dbo].[SoccerTeams] t ON t.[TeamId] = c.[TeamId] AND t.[DeletedAt] IS NULL
    WHERE c.[RequestedByUserId] = @UserId
      AND c.[Status] IN ('Accepted', 'Rejected') AND c.[DeletedAt] IS NULL
      AND NOT EXISTS (
          SELECT 1 FROM [dbo].[SoccerNotifications] n
          WHERE n.[RecipientUserId] = @UserId
            AND n.[NotificationType] = 'CorrectionReviewed' AND n.[RefId] = c.[CorrectionId]);

    --.// ① 에이전트 열람 요청 지연 동기화 (요청 생성은 에이전트 서비스라 발송 훅이 없다)
    INSERT INTO [dbo].[SoccerNotifications]
        ([RecipientUserId], [NotificationType], [RefId], [TargetPlayerId], [ActorName], [PlayerName], [CreatedAt])
    SELECT r.[GuardianUserId], 'ViewRequest', r.[RequestId], r.[PlayerId], a.[Name], p.[Name], r.[RequestedAt]
    FROM [dbo].[SoccerAgentViewRequests] r
    JOIN [dbo].[SoccerAgentProfiles] a ON a.[AgentId] = r.[AgentId] AND a.[DeletedAt] IS NULL
    JOIN [dbo].[SoccerPlayers] p ON p.[PlayerId] = r.[PlayerId]
    WHERE r.[GuardianUserId] = @UserId AND r.[Status] = 'Pending' AND r.[DeletedAt] IS NULL
      AND NOT EXISTS (
          SELECT 1 FROM [dbo].[SoccerNotifications] n
          WHERE n.[RecipientUserId] = @UserId
            AND n.[NotificationType] = 'ViewRequest' AND n.[RefId] = r.[RequestId]);

    --.// ① 에이전트 열람 승인 만료 임박 지연 동기화 (Design.AgentDashboard) — 승인 후 3일 전(만료 자동 판정 파생).
    -- 만료는 상태를 뒤집지 않고 ExpiresAt 파생으로 처리한다(기존 결정) — 여기서는 만료 3일 전 1회 알림만 만든다.
    -- 승인(Approved)이고 아직 미만료이며 ExpiresAt가 3일 이내인 건에 대해 멱등 INSERT(RefId=RequestId).
    INSERT INTO [dbo].[SoccerNotifications]
        ([RecipientUserId], [NotificationType], [RefId], [TargetPlayerId], [ActorName], [PlayerName], [CreatedAt])
    SELECT r.[GuardianUserId], 'AgentGrantExpiring', r.[RequestId], r.[PlayerId], a.[Name], p.[Name], GETUTCDATE()
    FROM [dbo].[SoccerAgentViewRequests] r
    JOIN [dbo].[SoccerAgentProfiles] a ON a.[AgentId] = r.[AgentId] AND a.[DeletedAt] IS NULL
    JOIN [dbo].[SoccerPlayers] p ON p.[PlayerId] = r.[PlayerId]
    WHERE r.[GuardianUserId] = @UserId AND r.[Status] = 'Approved' AND r.[DeletedAt] IS NULL
      AND r.[ExpiresAt] IS NOT NULL
      AND r.[ExpiresAt] > GETUTCDATE()
      AND r.[ExpiresAt] <= DATEADD(DAY, 3, GETUTCDATE())
      AND NOT EXISTS (
          SELECT 1 FROM [dbo].[SoccerNotifications] n
          WHERE n.[RecipientUserId] = @UserId
            AND n.[NotificationType] = 'AgentGrantExpiring' AND n.[RefId] = r.[RequestId]);

    --.// ② 보관 90일 자동 정리 — 미해소 액션형(연결 요청 Pending · 선수단 초대 미확인)은 나이와 무관하게 유지.
    DELETE n
    FROM [dbo].[SoccerNotifications] n
    LEFT JOIN [dbo].[SoccerPlayerClaimRequests] cr
        ON n.[NotificationType] = 'ClaimRequest' AND cr.[RequestId] = n.[RefId]
    LEFT JOIN [dbo].[SoccerApplications] app
        ON n.[NotificationType] = 'RosterInvite' AND app.[ApplicationId] = n.[RefId]
    WHERE n.[RecipientUserId] = @UserId
      AND n.[CreatedAt] < DATEADD(DAY, -90, GETUTCDATE())
      AND NOT (
            (n.[NotificationType] = 'ClaimRequest' AND cr.[Status] = 'Pending')
         OR (n.[NotificationType] = 'RosterInvite' AND app.[ApplicationId] IS NOT NULL AND app.[ConfirmedAt] IS NULL));
END
