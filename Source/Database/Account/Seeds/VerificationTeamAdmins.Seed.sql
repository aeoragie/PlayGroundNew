-- 로컬 검증용 팀 관리자 계정 6종 (U12 3팀 · U15 3팀) — 비밀번호는 전부 'password123!'.
-- PasswordHash는 동일 값 재사용 (ASP.NET Identity 해시 — salt 내장이라 복제 가능).
-- UserId는 Soccer 시드(VerificationLeagueTeams.Seed.sql)의 ManagerUserId와 맞춘 고정 GUID.
-- 재실행 안전: 이메일 기준 삭제 후 삽입. 로컬 개발 DB 전용 — 운영 배포 금지.
DECLARE @Hash VARCHAR(255) = 'AQAAAAIAAYagAAAAEJzWUF0xvuIiMlaBopf5Np7aOJ8n4cseTx8BHeQMX4OnCIXUfErv9xub2VA1NwWRug==';

DELETE FROM [dbo].[Users] WHERE [Email] LIKE 'verify-u1_-_@test.local';

INSERT INTO [dbo].[Users]
    ([UserId], [Email], [EmailConfirmed], [PasswordHash], [AuthProvider], [DisplayName], [UserRole])
VALUES
    ('A0000000-0000-0000-0000-000000000C01', 'verify-u12-1@test.local', 1, @Hash, 'Local', '서울신답FCU12 관리자', 'TeamAdmin'),
    ('A0000000-0000-0000-0000-000000000C02', 'verify-u12-2@test.local', 1, @Hash, 'Local', '서울K리거강용FC 관리자', 'TeamAdmin'),
    ('A0000000-0000-0000-0000-000000000C03', 'verify-u12-3@test.local', 1, @Hash, 'Local', '전남순천중앙초 관리자', 'TeamAdmin'),
    ('A0000000-0000-0000-0000-000000000C11', 'verify-u15-1@test.local', 1, @Hash, 'Local', '광주광주FCU15 관리자', 'TeamAdmin'),
    ('A0000000-0000-0000-0000-000000000C12', 'verify-u15-2@test.local', 1, @Hash, 'Local', '부산아이파크U15낙동중 관리자', 'TeamAdmin'),
    ('A0000000-0000-0000-0000-000000000C13', 'verify-u15-3@test.local', 1, @Hash, 'Local', '전북U15군산시민축구단 관리자', 'TeamAdmin');
