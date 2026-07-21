-- 초대코드 6자리 통일 (Design.ClaimFlow — 코드 박스 6칸 SPEC). 발급은 UspCreateSoccerTeamWithRoster·
-- 시드가 6자로 바뀌었고, 기존 미사용(Pending) 코드는 앞 6자로 축소한다 (사용된 코드는 기록이라 유지).
-- 멱등: 이미 6자면 대상 없음. 앞 6자 중복이 생기는 행은 건드리지 않는다 (수동 재발급 대상 — 로그로 확인).
UPDATE i
SET i.[Code] = LEFT(i.[Code], 6)
FROM [dbo].[SoccerPlayerInvites] i
WHERE i.[Status] = 'Pending' AND LEN(i.[Code]) > 6
  AND NOT EXISTS (
      SELECT 1 FROM [dbo].[SoccerPlayerInvites] other
      WHERE other.[InviteId] <> i.[InviteId] AND LEFT(other.[Code], 6) = LEFT(i.[Code], 6));
