-- 공개 선수 프로필 (Design.PlayerPublicProfile) — /player/{slug} URL용 Slug 신설.
-- 기존 행은 이름 기반으로 채운다 (동명이인은 생성 순서대로 -2, -3 …). 멱등.
-- 소프트 삭제된 행도 채운다 — UNIQUE 제약은 전체 행 대상이라 비워 둘 수 없다.
SET NOCOUNT ON;

IF COL_LENGTH('dbo.SoccerPlayers', 'Slug') IS NULL
BEGIN
    ALTER TABLE [dbo].[SoccerPlayers] ADD [Slug] VARCHAR(150) NULL;
END
GO

-- 이름 → slug (공백만 하이픈으로 — 한글 slug는 팀과 동일 정책), 동명이인은 -N
;WITH numbered AS (
    SELECT
        [PlayerId],
        REPLACE(LTRIM(RTRIM([Name])), ' ', '-') AS [Base],
        ROW_NUMBER() OVER (PARTITION BY [Name] ORDER BY [CreatedAt], [PlayerId]) AS [Rn]
    FROM [dbo].[SoccerPlayers]
    WHERE [Slug] IS NULL
)
UPDATE p
SET p.[Slug] = CASE WHEN n.[Rn] = 1 THEN n.[Base]
                    ELSE LEFT(n.[Base], 140) + '-' + CAST(n.[Rn] AS VARCHAR(10)) END
FROM [dbo].[SoccerPlayers] p
JOIN numbered n ON n.[PlayerId] = p.[PlayerId];
GO

IF EXISTS (SELECT 1 FROM sys.columns
           WHERE object_id = OBJECT_ID('dbo.SoccerPlayers') AND name = 'Slug' AND is_nullable = 1)
BEGIN
    ALTER TABLE [dbo].[SoccerPlayers] ALTER COLUMN [Slug] VARCHAR(150) NOT NULL;
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.key_constraints
               WHERE name = 'UQ_SoccerPlayers_Slug' AND parent_object_id = OBJECT_ID('dbo.SoccerPlayers'))
BEGIN
    ALTER TABLE [dbo].[SoccerPlayers] ADD CONSTRAINT [UQ_SoccerPlayers_Slug] UNIQUE ([Slug]);
END
GO

SELECT COUNT(*) AS [TotalPlayers], COUNT([Slug]) AS [WithSlug] FROM [dbo].[SoccerPlayers];
