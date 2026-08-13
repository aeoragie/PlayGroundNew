-- Soccer DB 데이터 초기화 — 모든 사용자 테이블을 비운다 (스키마·프로시저는 유지).
-- 동적 SQL이라 테이블이 늘어도 목록 관리가 필요 없다. FK가 없어 순서도 무관하다.
-- 디버그 시계(SystemClockOffset)는 비우면 UfnSystemDate가 깨지므로 남긴다.
-- 초기화 후에는 마스터 시드(Seeds/*.sql — 랜딩 콘텐츠·강점 태그)를 다시 적용해야 한다.
SET NOCOUNT ON;

DECLARE @sql NVARCHAR(MAX) = N'';  -- sp_executesql 계약상 NVARCHAR (컬럼 VARCHAR 규칙과 별개)
SELECT @sql += N'DELETE FROM ' + QUOTENAME(s.name) + N'.' + QUOTENAME(t.name) + N';' + NCHAR(10)
FROM sys.tables t
JOIN sys.schemas s ON s.schema_id = t.schema_id
WHERE t.is_ms_shipped = 0 AND t.name <> 'SystemClockOffset';

EXEC sp_executesql @sql;
PRINT 'Soccer 데이터 초기화 완료';
