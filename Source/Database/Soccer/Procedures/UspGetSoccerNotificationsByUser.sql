-- @entity: SoccerNotificationRecord
-- @source: join
-- @join: SoccerNotifications AS n (NotificationId, NotificationType, RefId, TargetPlayerId, ActorName, PlayerName, TeamName, MetaText, SubText, Relation, IsRead, CreatedAt)
-- @join: SoccerPlayerClaimRequests AS r (Status)
-- 알림 목록 — 결과셋 2개: ⓪미읽음 카운트(벨) → ①최근 50건.
-- 액션형(ClaimRequest)의 처리 여부는 스냅샷이 아니라 요청 상태를 라이브로 조인한다(Status —
-- 처리 후 재조회 시 버튼 대신 완료 박스를 그리기 위해).
--
-- 선행: 기록 수정 신청 심사 결과의 **지연 생성**. 심사(Accepted/Rejected)는 주최측 대회 운영
-- 서비스가 DB를 직접 바꾸므로(설계 결정 6·7) 우리 코드에 발송 훅이 없다 — 조회 시점에
-- "심사가 끝났는데 알림이 없는 신청"을 찾아 알림 행을 만든다. NOT EXISTS로 멱등.
CREATE PROCEDURE [dbo].[UspGetSoccerNotificationsByUser]
    @UserId UNIQUEIDENTIFIER
AS
BEGIN
    SET NOCOUNT ON;

    --.// 기록 수정 신청 심사 결과 지연 동기화 (MetaText=항목, SubText=심사 상태)

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

    --.// ⓪ 미읽음 카운트 (벨 뱃지 — 목록 50건 컷과 무관한 전체 수)

    SELECT COUNT(*) AS [UnreadCount]
    FROM [dbo].[SoccerNotifications] WITH (NOLOCK)
    WHERE [RecipientUserId] = @UserId AND [IsRead] = 0;

    --.// ① 목록 (최근 50건)

    SELECT TOP 50
        n.[NotificationId], n.[NotificationType], n.[RefId], n.[TargetPlayerId],
        n.[ActorName], n.[PlayerName], n.[TeamName], n.[MetaText], n.[SubText], n.[Relation],
        n.[IsRead], n.[CreatedAt],
        r.[Status]
    FROM [dbo].[SoccerNotifications] n WITH (NOLOCK)
    LEFT JOIN [dbo].[SoccerPlayerClaimRequests] r WITH (NOLOCK)
        ON n.[NotificationType] = 'ClaimRequest' AND r.[RequestId] = n.[RefId]
    WHERE n.[RecipientUserId] = @UserId
    ORDER BY n.[CreatedAt] DESC;
END
