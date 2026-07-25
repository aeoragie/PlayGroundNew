-- 강점 태그 프리셋 시드 (Design.StrengthTags) — 포지션별 8개. 멱등(이미 있으면 스킵).
-- 신규 DB·다른 PC에서 실행. 태그 문구는 운영자가 편집 가능.
SET NOCOUNT ON;

IF NOT EXISTS (SELECT 1 FROM [dbo].[SoccerStrengthTagPresets])
BEGIN
    INSERT INTO [dbo].[SoccerStrengthTagPresets] ([Position], [Tag], [SortOrder])
    VALUES
        -- GK
        ('GK', N'반사신경', 1), ('GK', N'선방', 2), ('GK', N'발밑 처리', 3), ('GK', N'공중볼 장악', 4),
        ('GK', N'일대일', 5), ('GK', N'빌드업', 6), ('GK', N'커맨딩', 7), ('GK', N'페널티 세이브', 8),
        -- DF
        ('DF', N'대인수비', 1), ('DF', N'헤더', 2), ('DF', N'태클', 3), ('DF', N'커버 플레이', 4),
        ('DF', N'빌드업', 5), ('DF', N'몸싸움', 6), ('DF', N'라인 조율', 7), ('DF', N'예측', 8),
        -- MF
        ('MF', N'패스', 1), ('MF', N'시야', 2), ('MF', N'탈압박', 3), ('MF', N'중거리 슛', 4),
        ('MF', N'활동량', 5), ('MF', N'경기 조율', 6), ('MF', N'압박', 7), ('MF', N'연계 플레이', 8),
        -- FW
        ('FW', N'결정력', 1), ('FW', N'침투', 2), ('FW', N'스피드', 3), ('FW', N'드리블', 4),
        ('FW', N'오프 더 볼', 5), ('FW', N'마무리', 6), ('FW', N'포스트 플레이', 7), ('FW', N'공중볼', 8);
END
