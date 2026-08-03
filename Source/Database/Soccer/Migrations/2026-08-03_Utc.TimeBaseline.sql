-- 시각 기준을 UTC 하나로 통일 (ReleasePlan H7, 2026-08-03)
--
-- 지금까지 두 기준이 섞여 있었다:
--   * 감사 컬럼(CreatedAt·ExpiresAt 등)은 GETUTCDATE() = UTC
--   * 사용자가 입력한 일시(StartsAt·MatchedAt)는 한국 벽시계 그대로
--   * 마감일(DeadlineDate)은 한국 달력 날짜인데 CAST(GETUTCDATE() AS DATE)와 비교
-- 그래서 UTC 서버에서 "지금"과 비교하는 판정이 전부 9시간 어긋났다
-- (00~09시 KST에 어제 마감된 모집이 살아 있고, 지난 일정이 예정으로 보였다).
--
-- 이 스크립트는 **저장을 전부 UTC 순간으로 맞춘다.** 이후 SQL은 GETUTCDATE()로만 비교하고,
-- 한국 시각 변환은 앱(KoreanTime) 한 곳에서만 한다.
--
-- **멱등하지 않다** — 데이터 시프트(-9h)는 두 번 돌리면 두 번 빠진다.
-- 아래 마커 테이블로 1회만 적용되게 막는다.

SET NOCOUNT ON;
GO

--.// 1회 적용 마커
IF OBJECT_ID('dbo.SoccerSchemaMigrations', 'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[SoccerSchemaMigrations]
    (
        [MigrationName] VARCHAR(200)  NOT NULL PRIMARY KEY,   -- UTF-8 (한글 66자)
        [AppliedAt]     DATETIME2     NOT NULL DEFAULT GETUTCDATE()
    );
END
GO

--.// 1. 모집 마감일 — DATE(한국 달력) → DATETIME2(UTC 순간)
-- "8/10 마감"은 8/10 23:59:59.999 KST까지 유효하다. 그 순간을 UTC로 넣으면
-- 프로시저가 [DeadlineAt] > GETUTCDATE() 하나로 판정한다(SQL에 9시간 상수가 안 들어간다).
IF COL_LENGTH('dbo.SoccerTeamRecruitments', 'DeadlineAt') IS NULL
BEGIN
    ALTER TABLE [dbo].[SoccerTeamRecruitments] ADD [DeadlineAt] DATETIME2 NULL;
END
GO

IF NOT EXISTS (SELECT 1 FROM [dbo].[SoccerSchemaMigrations] WHERE [MigrationName] = '2026-08-03_Utc.TimeBaseline')
   AND COL_LENGTH('dbo.SoccerTeamRecruitments', 'DeadlineDate') IS NOT NULL
BEGIN
    -- 한국 날짜의 끝(23:59:59.9999999 KST) → UTC = 날짜 + 1일 - 1틱 - 9시간
    UPDATE [dbo].[SoccerTeamRecruitments]
    SET [DeadlineAt] = DATEADD(HOUR, -9, DATEADD(NANOSECOND, -100, DATEADD(DAY, 1, CAST([DeadlineDate] AS DATETIME2))))
    WHERE [DeadlineDate] IS NOT NULL AND [DeadlineAt] IS NULL;
END
GO

--.// 2. 사용자 입력 일시 — 한국 벽시계 → UTC (-9h)
-- 일정 시작(StartsAt)과 경기 일시(MatchedAt)는 실제로 "그 순간에 일어나는 일"이라
-- UTC 순간으로 저장한다. 표시는 클라이언트가 브라우저 시간대로 되돌린다.
IF NOT EXISTS (SELECT 1 FROM [dbo].[SoccerSchemaMigrations] WHERE [MigrationName] = '2026-08-03_Utc.TimeBaseline')
BEGIN
    UPDATE [dbo].[SoccerSchedules] SET [StartsAt] = DATEADD(HOUR, -9, [StartsAt]);
    UPDATE [dbo].[SoccerMatches]   SET [MatchedAt] = DATEADD(HOUR, -9, [MatchedAt]) WHERE [MatchedAt] IS NOT NULL;

    INSERT INTO [dbo].[SoccerSchemaMigrations] ([MigrationName]) VALUES ('2026-08-03_Utc.TimeBaseline');
END
GO

--.// 3. 구 컬럼 제거 — 프로시저를 먼저 재배포한 뒤 이 블록을 돌린다
-- (프로시저가 아직 DeadlineDate를 참조하는 상태에서 지우면 런타임 오류가 난다)
IF COL_LENGTH('dbo.SoccerTeamRecruitments', 'DeadlineDate') IS NOT NULL
   AND EXISTS (SELECT 1 FROM [dbo].[SoccerSchemaMigrations] WHERE [MigrationName] = '2026-08-03_Utc.TimeBaseline')
BEGIN
    ALTER TABLE [dbo].[SoccerTeamRecruitments] DROP COLUMN [DeadlineDate];
END
GO
