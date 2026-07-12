-- 소셜 로그인 연동. (Provider, ProviderUserId)로 사용자 식별 → Users.UserId 연결(앱 계층 참조).
CREATE TABLE [dbo].[SocialAccounts]
(
    [SocialAccountId] UNIQUEIDENTIFIER NOT NULL DEFAULT NEWID(),
    [UserId]          UNIQUEIDENTIFIER NOT NULL,      -- Account.Users.UserId
    [Provider]        VARCHAR(20)      NOT NULL,      -- 'Google','Kakao'
    [ProviderUserId]  VARCHAR(255)     NOT NULL,      -- provider의 고유 사용자 id
    [Email]           VARCHAR(255)     NULL,
    [CreatedAt]       DATETIME2        NOT NULL DEFAULT GETUTCDATE(),

    CONSTRAINT [PK_SocialAccounts] PRIMARY KEY ([SocialAccountId]),
    CONSTRAINT [UQ_SocialAccounts_Provider] UNIQUE ([Provider], [ProviderUserId])
);
