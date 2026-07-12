-- 사용자 계정. 로그인 시 General 역할 자동 부여. 소셜 전용 계정은 PasswordHash NULL.
CREATE TABLE [dbo].[Users]
(
    [UserId]          UNIQUEIDENTIFIER NOT NULL DEFAULT NEWID(),
    [Email]           VARCHAR(255)     NOT NULL,
    [EmailConfirmed]  BIT              NOT NULL DEFAULT 0,
    [PasswordHash]    VARCHAR(255)     NULL,                       -- 소셜 전용은 NULL
    [AuthProvider]    VARCHAR(20)      NOT NULL DEFAULT 'Local',   -- 'Local','Google','Kakao'
    [DisplayName]     NVARCHAR(100)    NOT NULL,
    [ProfileImageUrl] VARCHAR(2048)    NULL,
    [UserRole]        VARCHAR(20)      NOT NULL DEFAULT 'General', -- 'General','Player','TeamAdmin' 등
    [UserStatus]      VARCHAR(20)      NOT NULL DEFAULT 'Active',
    [CreatedAt]       DATETIME2        NOT NULL DEFAULT GETUTCDATE(),
    [UpdatedAt]       DATETIME2        NOT NULL DEFAULT GETUTCDATE(),
    [DeletedAt]       DATETIME2        NULL,

    CONSTRAINT [PK_Users] PRIMARY KEY ([UserId]),
    CONSTRAINT [UQ_Users_Email] UNIQUE ([Email])
);
