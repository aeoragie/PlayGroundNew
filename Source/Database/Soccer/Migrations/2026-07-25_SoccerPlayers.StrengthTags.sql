-- SoccerPlayers.StrengthTags 추가 — 강점 태그(Design.StrengthTags). 순서 보존 JSON 배열.
-- 최대 5개·각 12자라 400바이트면 충분(UTF-8 한글 12자×5 + 구분자). 멱등.
-- **다른 PC 필수 실행.**
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.SoccerPlayers') AND name = 'StrengthTags')
BEGIN
    ALTER TABLE [dbo].[SoccerPlayers] ADD [StrengthTags] VARCHAR(400) NULL;
END
