-- 모집 공고에 지원(Application) 통합용 컬럼 추가 (Design.Application, E5).
-- 새 공고 테이블(SoccerRecruitPosts)을 만들지 않고 기존 SoccerTeamRecruitments로 통일(사용자 확정).
-- 전부 NULL 허용이라 기존 모집 코드는 영향 없음. 멱등. **다른 PC 필수 실행.**
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.SoccerTeamRecruitments') AND name = 'AgeGroup')
    ALTER TABLE [dbo].[SoccerTeamRecruitments] ADD [AgeGroup] VARCHAR(20) NULL;

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.SoccerTeamRecruitments') AND name = 'PositionsJson')
    ALTER TABLE [dbo].[SoccerTeamRecruitments] ADD [PositionsJson] VARCHAR(200) NULL;

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.SoccerTeamRecruitments') AND name = 'Capacity')
    ALTER TABLE [dbo].[SoccerTeamRecruitments] ADD [Capacity] INT NULL;
