-- **디버그 전용 덮어쓰기** — 운영에는 배포하지 않는다.
--
-- `Functions/UfnSystemDate.sql`(운영본)은 테이블을 읽지 않고 실제 UTC만 돌려준다.
-- 이 파일을 로컬에 적용하면 같은 함수가 `SystemClockOffset`을 읽어 시계를 옮긴다.
--
-- **운영과 디버그를 파일로 가른 이유**: 조건 분기를 함수 안에 두면 운영에서도 매 호출마다
-- 테이블을 읽어야 하고, 그 테이블이 운영 스키마에 존재해야 한다. 실수로 값이 들어가면
-- 운영 시계가 통째로 밀린다. 배포 대상에서 빼는 편이 안전하고 빠르다.
--
-- SCHEMABINDING을 빼는 이유는 테이블을 읽기 때문이다(바인딩되면 테이블 변경이 막힌다).
CREATE OR ALTER FUNCTION [dbo].[UfnSystemDate]()
RETURNS DATETIME2(7)
AS
BEGIN
    RETURN DATEADD(SECOND,
        ISNULL((SELECT TOP 1 [OffsetSeconds] FROM [dbo].[SystemClockOffset]), 0),
        SYSUTCDATETIME());
END
