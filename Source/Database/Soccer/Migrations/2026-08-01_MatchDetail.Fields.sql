-- 공식 경기 상세(Records 내 화면, Design.Records) 데이터 계층 — 대회 서비스 SingleIdx 모델 선반영.
-- Matches(전후반·주심·순번·감독) · Events(카드·등번) · Appearances(이름·등번·포지션·주장, 외부 선수 허용).
-- 멱등. **다른 PC 필수 실행.**

--.// SoccerMatches — 상세 헤더 필드
IF COL_LENGTH('dbo.SoccerMatches', 'FirstHalfHomeScore') IS NULL
    ALTER TABLE [dbo].[SoccerMatches] ADD [FirstHalfHomeScore] INT NULL;
IF COL_LENGTH('dbo.SoccerMatches', 'FirstHalfAwayScore') IS NULL
    ALTER TABLE [dbo].[SoccerMatches] ADD [FirstHalfAwayScore] INT NULL;
IF COL_LENGTH('dbo.SoccerMatches', 'RefereeName') IS NULL
    ALTER TABLE [dbo].[SoccerMatches] ADD [RefereeName] VARCHAR(90) NULL;
IF COL_LENGTH('dbo.SoccerMatches', 'MatchSequence') IS NULL
    ALTER TABLE [dbo].[SoccerMatches] ADD [MatchSequence] INT NULL;
IF COL_LENGTH('dbo.SoccerMatches', 'HomeCoachName') IS NULL
    ALTER TABLE [dbo].[SoccerMatches] ADD [HomeCoachName] VARCHAR(90) NULL;
IF COL_LENGTH('dbo.SoccerMatches', 'AwayCoachName') IS NULL
    ALTER TABLE [dbo].[SoccerMatches] ADD [AwayCoachName] VARCHAR(90) NULL;

--.// SoccerMatchEvents — 카드 이벤트 + 등번호 (EventType 값 확장은 데이터 컨벤션 — DDL 주석만 갱신)
IF COL_LENGTH('dbo.SoccerMatchEvents', 'JerseyNumber') IS NULL
    ALTER TABLE [dbo].[SoccerMatchEvents] ADD [JerseyNumber] INT NULL;

--.// SoccerMatchAppearances — 외부 선수 허용 + 라인업 표시 필드
IF COL_LENGTH('dbo.SoccerMatchAppearances', 'TeamName') IS NULL
    ALTER TABLE [dbo].[SoccerMatchAppearances] ADD [TeamName] VARCHAR(300) NOT NULL DEFAULT '';
IF COL_LENGTH('dbo.SoccerMatchAppearances', 'PlayerName') IS NULL
    ALTER TABLE [dbo].[SoccerMatchAppearances] ADD [PlayerName] VARCHAR(150) NOT NULL DEFAULT '';
IF COL_LENGTH('dbo.SoccerMatchAppearances', 'JerseyNumber') IS NULL
    ALTER TABLE [dbo].[SoccerMatchAppearances] ADD [JerseyNumber] INT NULL;
IF COL_LENGTH('dbo.SoccerMatchAppearances', 'Position') IS NULL
    ALTER TABLE [dbo].[SoccerMatchAppearances] ADD [Position] VARCHAR(10) NULL;
IF COL_LENGTH('dbo.SoccerMatchAppearances', 'IsCaptain') IS NULL
    ALTER TABLE [dbo].[SoccerMatchAppearances] ADD [IsCaptain] BIT NOT NULL DEFAULT 0;

-- 외부 선수(원정 라인업) 허용을 위해 TeamId·PlayerId NULL 허용으로 완화 (멱등 — 이미 NULL이면 무해)
IF EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.SoccerMatchAppearances') AND name = 'TeamId' AND is_nullable = 0)
    ALTER TABLE [dbo].[SoccerMatchAppearances] ALTER COLUMN [TeamId] UNIQUEIDENTIFIER NULL;
IF EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.SoccerMatchAppearances') AND name = 'PlayerId' AND is_nullable = 0)
    ALTER TABLE [dbo].[SoccerMatchAppearances] ALTER COLUMN [PlayerId] UNIQUEIDENTIFIER NULL;
