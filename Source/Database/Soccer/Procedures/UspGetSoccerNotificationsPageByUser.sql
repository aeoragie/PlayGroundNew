-- 알림 센터 페이지(/notifications) — 세그먼트 필터 + 페이지네이션 (DECISION.NOTIFICATIONCENTER).
-- @Filter: 'all' | 'action'(처리 필요 — 미해소 액션형) | 'unread'(읽지 않음). @Offset/@Limit로 "더 보기 20".
-- 결과셋 4개: ⓪전체 수 → ①처리 필요 수 → ②읽지 않음 수 → ③현재 필터의 페이지(최신순, SoccerNotificationRecord).
--   세그먼트 카운트를 모두 내려 클라가 라벨·hasMore(offset+len < 필터별 수)를 조립한다.
-- 지연 생성·90일 정리는 UspSyncSoccerNotifications가 담당(벨 패널과 공유).
-- IsActionRequired = 연결 요청(ClaimRequest) 라이브 Pending 또는 선수단 초대(RosterInvite) 미확인.
CREATE PROCEDURE [dbo].[UspGetSoccerNotificationsPageByUser]
    @UserId UNIQUEIDENTIFIER,
    @Filter VARCHAR(20),
    @Offset INT,
    @Limit  INT
AS
BEGIN
    SET NOCOUNT ON;

    EXEC [dbo].[UspSyncSoccerNotifications] @UserId;

    -- 라이브 상태·액션 필요 플래그를 한 번 계산해 임시 테이블에 담는다(카운트·페이지가 공유).
    SELECT
        n.[NotificationId], n.[NotificationType], n.[RefId], n.[TargetPlayerId],
        n.[ActorName], n.[PlayerName], n.[TeamName], n.[MetaText], n.[SubText], n.[Relation],
        n.[IsRead], n.[CreatedAt],
        COALESCE(
            r.[Status],
            CASE WHEN app.[ApplicationId] IS NOT NULL
                 THEN CASE WHEN app.[ConfirmedAt] IS NOT NULL THEN 'Confirmed' ELSE 'Pending' END
            END) AS [Status],
        CASE
            WHEN n.[NotificationType] = 'ClaimRequest' AND r.[Status] = 'Pending' THEN 1
            WHEN n.[NotificationType] = 'RosterInvite' AND app.[ApplicationId] IS NOT NULL AND app.[ConfirmedAt] IS NULL THEN 1
            ELSE 0 END AS [IsActionRequired]
    INTO #Notif
    FROM [dbo].[SoccerNotifications] n WITH (NOLOCK)
    LEFT JOIN [dbo].[SoccerPlayerClaimRequests] r WITH (NOLOCK)
        ON n.[NotificationType] = 'ClaimRequest' AND r.[RequestId] = n.[RefId]
    LEFT JOIN [dbo].[SoccerApplications] app WITH (NOLOCK)
        ON n.[NotificationType] = 'RosterInvite' AND app.[ApplicationId] = n.[RefId]
    WHERE n.[RecipientUserId] = @UserId;

    SELECT COUNT(*) AS [TotalCount] FROM #Notif;
    SELECT COUNT(*) AS [ActionRequiredCount] FROM #Notif WHERE [IsActionRequired] = 1;
    SELECT COUNT(*) AS [UnreadCount] FROM #Notif WHERE [IsRead] = 0;

    SELECT
        [NotificationId], [NotificationType], [RefId], [TargetPlayerId],
        [ActorName], [PlayerName], [TeamName], [MetaText], [SubText], [Relation],
        [IsRead], [CreatedAt], [Status]
    FROM #Notif
    WHERE (@Filter = 'all')
       OR (@Filter = 'action' AND [IsActionRequired] = 1)
       OR (@Filter = 'unread' AND [IsRead] = 0)
    ORDER BY [CreatedAt] DESC
    OFFSET @Offset ROWS FETCH NEXT @Limit ROWS ONLY;

    DROP TABLE #Notif;
END
