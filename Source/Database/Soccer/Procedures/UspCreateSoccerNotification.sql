-- @entity: SoccerNotificationCreatedRecord
-- @source: join
-- @join: SoccerNotifications AS n (NotificationId)
-- 범용 알림 단건 발송 — Application 주도 발송(친선경기 결과 등 수신 설정 필터가 필요한 유형)용.
-- 같은 (수신자, 유형, RefId, TargetPlayerId) 알림이 이미 있으면 만들지 않는다 (재시도 멱등).
-- Claim 계열 알림은 여기가 아니라 요청·승인 프로시저 트랜잭션 안에서 만든다.
CREATE PROCEDURE [dbo].[UspCreateSoccerNotification]
    @RecipientUserId UNIQUEIDENTIFIER,
    @NotificationType VARCHAR(30),
    @RefId UNIQUEIDENTIFIER,
    @TargetPlayerId UNIQUEIDENTIFIER = NULL,
    @ActorName VARCHAR(300) = NULL,
    @PlayerName VARCHAR(300) = NULL,
    @TeamName VARCHAR(300) = NULL,
    @MetaText VARCHAR(300) = NULL,
    @SubText VARCHAR(300) = NULL
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @NotificationId UNIQUEIDENTIFIER;

    SELECT @NotificationId = [NotificationId]
    FROM [dbo].[SoccerNotifications]
    WHERE [RecipientUserId] = @RecipientUserId AND [NotificationType] = @NotificationType
      AND [RefId] = @RefId
      AND ([TargetPlayerId] = @TargetPlayerId OR ([TargetPlayerId] IS NULL AND @TargetPlayerId IS NULL));

    IF @NotificationId IS NULL
    BEGIN
        SET @NotificationId = NEWID();

        INSERT INTO [dbo].[SoccerNotifications]
            ([NotificationId], [RecipientUserId], [NotificationType], [RefId], [TargetPlayerId],
             [ActorName], [PlayerName], [TeamName], [MetaText], [SubText])
        VALUES (@NotificationId, @RecipientUserId, @NotificationType, @RefId, @TargetPlayerId,
                @ActorName, @PlayerName, @TeamName, @MetaText, @SubText);
    END

    SELECT n.[NotificationId]
    FROM [dbo].[SoccerNotifications] n WITH (NOLOCK)
    WHERE n.[NotificationId] = @NotificationId;
END
