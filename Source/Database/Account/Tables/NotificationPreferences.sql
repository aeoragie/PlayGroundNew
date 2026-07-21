-- 알림 설정 (채널·항목 켬/끔). 행이 없는 항목은 기본값 적용 — Domain NotificationPreferenceItem.DefaultIsEnabled.
-- 승인형(연결 요청·열람 요청)은 여기 저장하지 않는다 — 항상 켜짐, 서버가 저장 자체를 거부 (미성년자 보호 관문).
CREATE TABLE [dbo].[NotificationPreferences]
(
    [PreferenceId]  UNIQUEIDENTIFIER NOT NULL DEFAULT NEWID(),
    [UserId]        UNIQUEIDENTIFIER NOT NULL,          -- Users.UserId
    [ItemName]      VARCHAR(30)      NOT NULL,          -- 'PushChannel','EmailChannel','MatchResult','Recruit','Review','VisitSummary'
    [IsEnabled]     BIT              NOT NULL,

    [CreatedAt]     DATETIME2        NOT NULL DEFAULT GETUTCDATE(),
    [UpdatedAt]     DATETIME2        NOT NULL DEFAULT GETUTCDATE(),

    CONSTRAINT [PK_NotificationPreferences] PRIMARY KEY ([PreferenceId]),
    CONSTRAINT [UQ_NotificationPreferences_UserItem] UNIQUE ([UserId], [ItemName])
);
