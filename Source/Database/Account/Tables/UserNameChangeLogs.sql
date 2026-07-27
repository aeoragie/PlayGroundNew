-- 이름(DisplayName) 변경 로그 (Design.SettingsFlows ①). 운영 추적 + 30일 2회 제한 판정 근거.
-- **Account DB에 둔다** — DisplayName이 Account 소유라 변경·로그·제한 판정을 한 트랜잭션으로 원자 처리한다
--   (Handoff는 SoccerNameChangeLogs로 명명했으나, 신원 도메인 데이터이고 Cross-DB면 제한 판정이 원자적이지
--    않아 Account 규약대로 무프리픽스 UserNameChangeLogs로 둔다).
-- 소프트 삭제 없음 — 이력은 지우지 않는다(운영 추적). 제한 판정은 최근 30일 행 수로 파생.
CREATE TABLE [dbo].[UserNameChangeLogs]
(
    [NameChangeLogId] UNIQUEIDENTIFIER NOT NULL DEFAULT NEWID(),
    [UserId]          UNIQUEIDENTIFIER NOT NULL,          -- Users.UserId
    [PreviousName]    VARCHAR(300)     NOT NULL,          -- UTF-8 이전 이름
    [NewName]         VARCHAR(300)     NOT NULL,          -- UTF-8 이후 이름
    [ChangedAt]       DATETIME2        NOT NULL DEFAULT GETUTCDATE(),

    CONSTRAINT [PK_UserNameChangeLogs] PRIMARY KEY ([NameChangeLogId])
);
