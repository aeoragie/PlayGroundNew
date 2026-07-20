-- 기존 DB에 SoccerMatches.MatchType을 추가하고 기존 행을 분류한다 (Design.FriendlyMatch / 설계 결정 7).
-- Tables/SoccerMatches.sql은 CREATE 문이라 이미 만들어진 DB에는 적용되지 않는다 — 그 간극을 메우는 스크립트다.
-- 새로 만든 DB(Tables 폴더를 처음부터 적용)에는 실행할 필요가 없다. 재실행해도 안전하다.
--
-- 분류 규칙:
--   1) DataSource = 'User'  → Friendly. 팀이 직접 입력한 경기는 전부 친선이다(B1 저장분 — 설계 결정 7).
--   2) TournamentId IS NULL → Friendly. 대회에 속하지 않는 경기는 기존부터 친선으로 다뤄 왔다.
--   3) 그 외                → Official.  주최측이 기록한 대회·리그 경기.

SET NOCOUNT ON;

IF COL_LENGTH('dbo.SoccerMatches', 'MatchType') IS NULL
BEGIN
    ALTER TABLE [dbo].[SoccerMatches]
        ADD [MatchType] VARCHAR(20) NOT NULL CONSTRAINT [DF_SoccerMatches_MatchType] DEFAULT 'Official';

    PRINT 'MatchType column added.';
END
ELSE
BEGIN
    PRINT 'MatchType column already exists — skipping ALTER.';
END
GO

-- 기존 행 분류 (기본값 'Official'로 채워진 상태에서 친선만 되돌린다)
UPDATE [dbo].[SoccerMatches]
SET [MatchType] = 'Friendly', [UpdatedAt] = GETUTCDATE()
WHERE [MatchType] <> 'Friendly'
  AND ([DataSource] = 'User' OR [TournamentId] IS NULL);

PRINT CONCAT('Rows migrated to Friendly: ', @@ROWCOUNT);
GO

SELECT [MatchType], COUNT(*) AS [Matches]
FROM [dbo].[SoccerMatches] WITH (NOLOCK)
WHERE [DeletedAt] IS NULL
GROUP BY [MatchType];
GO
