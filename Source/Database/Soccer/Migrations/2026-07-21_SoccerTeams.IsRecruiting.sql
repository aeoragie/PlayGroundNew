-- SoccerTeams.IsRecruiting 추가 (팀 탐색 모집중 뱃지·필터 — 모집 공고 스키마 도입 전 단순 플래그).
-- 멱등 — 기존 DB에 반복 실행 안전. 다른 PC에서는 이 스크립트를 반드시 실행할 것 (CREATE TABLE만으로는 미적용).
IF COL_LENGTH('dbo.SoccerTeams', 'IsRecruiting') IS NULL
BEGIN
    ALTER TABLE [dbo].[SoccerTeams]
    ADD [IsRecruiting] BIT NOT NULL CONSTRAINT [DF_SoccerTeams_IsRecruiting] DEFAULT 0;
END
