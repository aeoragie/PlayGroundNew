-- 연결 요청의 InviteId를 NULL 허용으로 바꾼다 — 코드 없이(공개 선수 프로필 경유) 올리는 요청 지원.
-- 코드 기반 요청은 여전히 InviteId를 채우고, 프로필 경유 요청은 NULL이다(승인 시 코드 소진 없이 직접 연결).
-- 멱등: 이미 NULL 허용이면 아무것도 안 한다.
IF EXISTS (
    SELECT 1 FROM sys.columns
    WHERE object_id = OBJECT_ID('dbo.SoccerPlayerClaimRequests')
      AND name = 'InviteId' AND is_nullable = 0)
BEGIN
    ALTER TABLE [dbo].[SoccerPlayerClaimRequests] ALTER COLUMN [InviteId] UNIQUEIDENTIFIER NULL;
END
