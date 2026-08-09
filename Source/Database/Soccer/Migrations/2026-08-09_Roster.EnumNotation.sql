-- 로스터 Grade를 나이 기준 U표기로 전환하고, enum 멤버 이름이 아닌 값을 정리한다.
-- C#이 SoccerGrade·SoccerPosition enum으로 읽으므로(EnumColumn — 미지 값은 Panic)
-- 이 마이그레이션 없이 옛 데이터를 읽으면 DEBUG에서 죽는다. 멱등 — 재실행 안전.

UPDATE [dbo].[SoccerTeamPlayers] SET [Grade] =
    CASE [Grade]
        WHEN '초1' THEN 'U7'  WHEN '초2' THEN 'U8'  WHEN '초3' THEN 'U9'
        WHEN '초4' THEN 'U10' WHEN '초5' THEN 'U11' WHEN '초6' THEN 'U12'
        WHEN '중1' THEN 'U13' WHEN '중2' THEN 'U14' WHEN '중3' THEN 'U15'
        WHEN '고1' THEN 'U16' WHEN '고2' THEN 'U17' WHEN '고3' THEN 'U18'
    END
WHERE [Grade] IN ('초1','초2','초3','초4','초5','초6','중1','중2','중3','고1','고2','고3');

-- 자유 입력 시절의 비정형 값('-' 포함) — 매핑 불가면 미지정으로
UPDATE [dbo].[SoccerTeamPlayers] SET [Grade] = NULL
WHERE [Grade] IS NOT NULL
  AND [Grade] NOT IN ('U7','U8','U9','U10','U11','U12','U13','U14','U15','U16','U17','U18');

UPDATE [dbo].[SoccerTeamPlayers] SET [Position] = NULL
WHERE [Position] IS NOT NULL
  AND [Position] NOT IN ('GK','DF','MF','FW');
