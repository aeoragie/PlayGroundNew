-- 기존 슬러그(한글) → 로마자(ASCII) 일괄 재생성. dbo.UfnRomanizeKoreanSlug 기준.
--   · 선수(SoccerPlayers)·팀(SoccerTeams) 둘 다. 한글 슬러그가 URL에서 인코딩돼야 하고
--     미성년 이름 노출 문제가 있어 영문 로마자로 통일한다(강민→gangmin, 테스트유나이티드→teseuteu…).
--   · Slug UNIQUE 제약은 전역이다(필터드 인덱스 금지 규칙) — 소프트 삭제 행까지 포함해 전체를
--     한 번에 재슬러그하고, 활성 행이 깨끗한 base(-접미사 없음)를 갖도록 정렬한다.
--   · 이름(Name)에서 파생하므로 재실행해도 결과가 같다(멱등). **다른 PC 필수 실행.**
SET NOCOUNT ON;

PRINT '=== 재슬러그 전 (한글 슬러그 표본) ===';
SELECT TOP 5 Name, Slug FROM SoccerPlayers WHERE DeletedAt IS NULL ORDER BY CreatedAt DESC;

--.// 선수 — 이름 로마자 base + 전역 동일 base 순번(-2, -3 …). 활성 우선 → 깨끗한 base.
;WITH b AS (
    SELECT p.[PlayerId], p.[DeletedAt], p.[CreatedAt],
           CASE WHEN dbo.UfnRomanizeKoreanSlug(p.[Name]) = '' THEN 'player'
                ELSE dbo.UfnRomanizeKoreanSlug(p.[Name]) END AS [Base]
    FROM [dbo].[SoccerPlayers] p
),
s AS (
    SELECT [PlayerId], [Base],
           ROW_NUMBER() OVER (
               PARTITION BY [Base]
               ORDER BY CASE WHEN [DeletedAt] IS NULL THEN 0 ELSE 1 END, [CreatedAt], [PlayerId]) AS [rn]
    FROM b
)
UPDATE p
SET p.[Slug] = CASE WHEN s.[rn] = 1 THEN s.[Base]
                    ELSE LEFT(s.[Base], 140) + '-' + CAST(s.[rn] AS VARCHAR(10)) END,
    p.[UpdatedAt] = GETUTCDATE()
FROM [dbo].[SoccerPlayers] p
JOIN s ON s.[PlayerId] = p.[PlayerId];

--.// 팀 — 팀명 로마자 base + 전역 동일 base 순번.
;WITH b AS (
    SELECT t.[TeamId], t.[DeletedAt], t.[CreatedAt],
           CASE WHEN dbo.UfnRomanizeKoreanSlug(t.[TeamName]) = '' THEN 'team'
                ELSE dbo.UfnRomanizeKoreanSlug(t.[TeamName]) END AS [Base]
    FROM [dbo].[SoccerTeams] t
),
s AS (
    SELECT [TeamId], [Base],
           ROW_NUMBER() OVER (
               PARTITION BY [Base]
               ORDER BY CASE WHEN [DeletedAt] IS NULL THEN 0 ELSE 1 END, [CreatedAt], [TeamId]) AS [rn]
    FROM b
)
UPDATE t
SET t.[Slug] = CASE WHEN s.[rn] = 1 THEN s.[Base]
                    ELSE LEFT(s.[Base], 90) + '-' + CAST(s.[rn] AS VARCHAR(10)) END,
    t.[UpdatedAt] = GETUTCDATE()
FROM [dbo].[SoccerTeams] t
JOIN s ON s.[TeamId] = t.[TeamId];

PRINT '=== 재슬러그 후 (표본) ===';
SELECT TOP 5 Name, Slug FROM SoccerPlayers WHERE DeletedAt IS NULL ORDER BY CreatedAt DESC;
SELECT TOP 5 TeamName, Slug FROM SoccerTeams WHERE DeletedAt IS NULL ORDER BY CreatedAt DESC;
