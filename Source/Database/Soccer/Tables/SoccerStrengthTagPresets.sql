-- 강점 태그 프리셋 (Design.StrengthTags) — 포지션별 추천 태그. 서버 상수(운영자 편집 가능).
-- 선수 대시보드 편집 화면이 포지션에 맞는 프리셋 칩을 제안한다. 저장값과 무관(제안일 뿐).
CREATE TABLE [dbo].[SoccerStrengthTagPresets]
(
    [PresetId]  UNIQUEIDENTIFIER NOT NULL DEFAULT NEWID(),
    [Position]  VARCHAR(10)      NOT NULL,          -- 'GK','DF','MF','FW'
    [Tag]       VARCHAR(60)      NOT NULL,          -- UTF-8 (한글 12자 이내)
    [SortOrder] INT              NOT NULL DEFAULT 0, -- 포지션 내 표시 순서

    CONSTRAINT [PK_SoccerStrengthTagPresets] PRIMARY KEY ([PresetId])
);
