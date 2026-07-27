-- 데이터 내려받기 요청 (Design.SettingsFlows ③) — 신규 테이블. 멱등. **다른 PC 필수 실행.**
IF OBJECT_ID('dbo.SoccerDataExportRequests', 'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[SoccerDataExportRequests]
    (
        [RequestId]       UNIQUEIDENTIFIER NOT NULL DEFAULT NEWID(),
        [UserId]          UNIQUEIDENTIFIER NOT NULL,
        [Status]          VARCHAR(20)      NOT NULL DEFAULT 'Pending',
        [IncludeProfile]  BIT              NOT NULL DEFAULT 1,
        [IncludeRecords]  BIT              NOT NULL DEFAULT 1,
        [IncludeRequests] BIT              NOT NULL DEFAULT 1,
        [DownloadToken]   VARCHAR(64)      NULL,
        [StorageKey]      VARCHAR(400)     NULL,
        [SizeBytes]       BIGINT           NULL,
        [ExpiresAt]       DATETIME2        NULL,
        [DownloadCount]   INT              NOT NULL DEFAULT 0,
        [MaxDownloads]    INT              NOT NULL DEFAULT 3,
        [CreatedAt]       DATETIME2        NOT NULL DEFAULT GETUTCDATE(),
        [UpdatedAt]       DATETIME2        NOT NULL DEFAULT GETUTCDATE(),
        [CompletedAt]     DATETIME2        NULL,
        [DeletedAt]       DATETIME2        NULL,
        CONSTRAINT [PK_SoccerDataExportRequests] PRIMARY KEY ([RequestId])
    );
END
