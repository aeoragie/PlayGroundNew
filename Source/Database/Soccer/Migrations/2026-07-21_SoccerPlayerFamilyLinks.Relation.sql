-- 가족 연결에 관계(어머니/아버지/기타 보호자) 추가 — Claim 플로우(Design.ClaimFlow) 관계 선택 저장.
-- 멱등: 컬럼이 이미 있으면 아무것도 하지 않는다. 기존 행은 NULL(미지정) 유지.
IF NOT EXISTS (
    SELECT 1 FROM sys.columns
    WHERE object_id = OBJECT_ID('dbo.SoccerPlayerFamilyLinks') AND name = 'Relation')
BEGIN
    ALTER TABLE [dbo].[SoccerPlayerFamilyLinks]
    ADD [Relation] VARCHAR(20) NULL; -- 'Mother','Father','Guardian'
END
