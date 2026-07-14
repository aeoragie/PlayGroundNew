-- 로컬 검증용 선수 계정 2종 (U12 1명 · U15 1명) — 비밀번호는 전부 'password123!'.
-- PasswordHash는 팀 관리자 시드와 동일 값 재사용 (ASP.NET Identity 해시 — salt 내장이라 복제 가능).
-- UserId는 Soccer 시드(VerificationPlayerLinks.Seed.sql)가 리그 팀 선수 행과 연결하는 고정 GUID.
--   D01 = 서울신답FCU12 신준우(U12), D11 = 광주광주FCU15 김정현(U15).
-- 재실행 안전: 이메일 기준 삭제 후 삽입. 로컬 개발 DB 전용 — 운영 배포 금지.
DECLARE @Hash VARCHAR(255) = 'AQAAAAIAAYagAAAAEJzWUF0xvuIiMlaBopf5Np7aOJ8n4cseTx8BHeQMX4OnCIXUfErv9xub2VA1NwWRug==';

DELETE FROM [dbo].[Users] WHERE [Email] IN ('verify-player-u12@test.local', 'verify-player-u15@test.local');

INSERT INTO [dbo].[Users]
    ([UserId], [Email], [EmailConfirmed], [PasswordHash], [AuthProvider], [DisplayName], [UserRole])
VALUES
    ('A0000000-0000-0000-0000-000000000D01', 'verify-player-u12@test.local', 1, @Hash, 'Local', '신준우', 'Player'),
    ('A0000000-0000-0000-0000-000000000D11', 'verify-player-u15@test.local', 1, @Hash, 'Local', '김정현', 'Player');
