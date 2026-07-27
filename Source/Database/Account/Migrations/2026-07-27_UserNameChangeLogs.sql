-- 이름 변경 로그 (Design.SettingsFlows ①) — 신규 테이블. 멱등. **다른 PC 필수 실행.**
IF OBJECT_ID('dbo.UserNameChangeLogs', 'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[UserNameChangeLogs]
    (
        [NameChangeLogId] UNIQUEIDENTIFIER NOT NULL DEFAULT NEWID(),
        [UserId]          UNIQUEIDENTIFIER NOT NULL,
        [PreviousName]    VARCHAR(300)     NOT NULL,
        [NewName]         VARCHAR(300)     NOT NULL,
        [ChangedAt]       DATETIME2        NOT NULL DEFAULT GETUTCDATE(),
        CONSTRAINT [PK_UserNameChangeLogs] PRIMARY KEY ([NameChangeLogId])
    );
END
