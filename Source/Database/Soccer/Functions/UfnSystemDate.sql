-- 이 시스템의 "지금" — **UTC**. 프로시저는 내장 시간 함수를 직접 부르지 않고 이 함수만 쓴다
-- (`TimeBaselineGuardTests`가 강제한다).
--
-- 감싸는 이유는 **시간 이동 테스트**다. 만료·마감·재신청 제한처럼 "며칠 뒤"를 봐야 하는 로직을
-- 실제로 며칠 기다리지 않고 확인하려면 시계를 옮길 수 있어야 한다.
--
-- **이 파일은 운영본이고 테이블을 읽지 않는다.** 오프셋을 적용하는 본문은
-- `Debug/UfnSystemDate.Debug.sql`이 로컬에서만 덮어쓴다 — 운영에는 그 파일을 배포하지 않으므로
-- 오프셋 테이블이 없어도 되고, 조회 비용도 0이다.
--
-- 호출 규약: **프로시저 안에서는 반드시 변수로 받는다.**
--     DECLARE @Now DATETIME2(7) = dbo.UfnSystemDate();
-- 스칼라 UDF는 인라인되지 않아(시간 의존 함수를 부르는 UDF는 인라인 대상에서 제외) WHERE에
-- 직접 쓰면 행마다 호출된다. 변수로 받으면 1회로 끝나고, 한 프로시저 안의 시각도 일관돼진다.
CREATE FUNCTION [dbo].[UfnSystemDate]()
RETURNS DATETIME2(7)
WITH SCHEMABINDING
AS
BEGIN
    RETURN SYSUTCDATETIME();
END
