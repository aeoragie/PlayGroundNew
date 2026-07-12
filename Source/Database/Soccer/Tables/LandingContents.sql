-- 랜딩 페이지 콘텐츠(문구). Section으로 '핵심 기능(Feature)' / '작동 방식 3스텝(HowStep)' 구분.
-- 운영이 DB에서 카피를 편집하면 랜딩에 바로 반영된다.
CREATE TABLE [dbo].[LandingContents]
(
    [LandingContentId] UNIQUEIDENTIFIER NOT NULL DEFAULT NEWID(),
    [Section]          VARCHAR(20)      NOT NULL,           -- 'Feature','HowStep'
    [DisplayOrder]     INT              NOT NULL DEFAULT 0,
    [Icon]             NVARCHAR(20)     NOT NULL,           -- 이모지(🏠) 또는 스텝 번호('1')
    [Title]            NVARCHAR(100)    NOT NULL,
    [Body]             NVARCHAR(500)    NOT NULL,
    [IsActive]         BIT              NOT NULL DEFAULT 1,
    [CreatedAt]        DATETIME2        NOT NULL DEFAULT GETUTCDATE(),
    [UpdatedAt]        DATETIME2        NOT NULL DEFAULT GETUTCDATE(),

    CONSTRAINT [PK_LandingContents] PRIMARY KEY ([LandingContentId])
);
