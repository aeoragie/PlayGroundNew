-- **디버그 전용** — 시간 이동 테스트용 시계 오프셋. 운영에는 배포하지 않는다.
--
-- 만료·마감·재신청 제한처럼 "며칠 뒤"를 봐야 하는 로직을 실제로 기다리지 않고 확인하려고 둔다.
-- 한 행만 유지한다(PK가 상수라 두 번째 행이 들어갈 수 없다).
--
--   -- 3일 뒤로 이동
--   UPDATE dbo.SystemClockOffset SET OffsetSeconds = 3 * 24 * 60 * 60;
--   -- 원래대로
--   UPDATE dbo.SystemClockOffset SET OffsetSeconds = 0;
--
-- 앱(C#)도 이 값을 읽어 `SystemTime`에 같은 오프셋을 적용한다 — 한쪽만 옮기면
-- 앱이 넘긴 시각과 DB의 "지금"이 어긋나 테스트가 거짓 결과를 낸다.
CREATE TABLE [dbo].[SystemClockOffset]
(
    [Id]            TINYINT  NOT NULL CONSTRAINT [PK_SystemClockOffset] PRIMARY KEY
                             CONSTRAINT [CK_SystemClockOffset_Single] CHECK ([Id] = 1),
    [OffsetSeconds] INT      NOT NULL CONSTRAINT [DF_SystemClockOffset_Zero] DEFAULT 0
);
GO

IF NOT EXISTS (SELECT 1 FROM [dbo].[SystemClockOffset])
BEGIN
    INSERT INTO [dbo].[SystemClockOffset] ([Id], [OffsetSeconds]) VALUES (1, 0);
END
GO
