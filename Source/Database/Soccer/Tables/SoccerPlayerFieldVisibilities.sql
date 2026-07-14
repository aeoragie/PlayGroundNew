-- 선수 프로필 항목별 공개 설정. 행이 없는 필드는 기본값 적용 (키·몸무게·주발 공개 / 학교·보호자 연락처 비공개 — 앱 계층).
-- 쓰기는 관리 주체(SoccerPlayers.UserId 연결 계정 = 보호자)만 — 유즈케이스에서 검증.
CREATE TABLE [dbo].[SoccerPlayerFieldVisibilities]
(
    [VisibilityId]  UNIQUEIDENTIFIER NOT NULL DEFAULT NEWID(),
    [PlayerId]      UNIQUEIDENTIFIER NOT NULL,          -- SoccerPlayers.PlayerId (앱 계층 참조)
    [FieldName]     VARCHAR(30)      NOT NULL,          -- 'Height','Weight','PreferredFoot','School','GuardianPhone'
    [IsPublic]      BIT              NOT NULL,

    [CreatedAt]     DATETIME2        NOT NULL DEFAULT GETUTCDATE(),
    [UpdatedAt]     DATETIME2        NOT NULL DEFAULT GETUTCDATE(),

    CONSTRAINT [PK_SoccerPlayerFieldVisibilities] PRIMARY KEY ([VisibilityId]),
    CONSTRAINT [UQ_SoccerPlayerFieldVisibilities_PlayerField] UNIQUE ([PlayerId], [FieldName])
);
