-- 선수 가족 계정 연결. 보호자 = 관리(Guardian) / 본인 = 열람(Self, 만 14세 이상 시 권한 이전 예정).
-- UserId NULL = 계정 미연결 구성원 (표시만). MemberName은 표시용 (Account와 앱 계층 정합성).
CREATE TABLE [dbo].[SoccerPlayerFamilyLinks]
(
    [FamilyLinkId]  UNIQUEIDENTIFIER NOT NULL DEFAULT NEWID(),
    [PlayerId]      UNIQUEIDENTIFIER NOT NULL,          -- SoccerPlayers.PlayerId (앱 계층 참조)
    [UserId]        UNIQUEIDENTIFIER NULL,              -- Account.Users.UserId (앱 계층 참조)
    [MemberName]    VARCHAR(300)     NOT NULL,          -- UTF-8 (한글 100자) 표시 이름
    [Role]          VARCHAR(20)      NOT NULL,          -- 'Guardian','Self'
    [DisplayOrder]  INT              NOT NULL DEFAULT 0,

    [CreatedAt]     DATETIME2        NOT NULL DEFAULT GETUTCDATE(),
    [UpdatedAt]     DATETIME2        NOT NULL DEFAULT GETUTCDATE(),
    [DeletedAt]     DATETIME2        NULL,

    CONSTRAINT [PK_SoccerPlayerFamilyLinks] PRIMARY KEY ([FamilyLinkId])
);
