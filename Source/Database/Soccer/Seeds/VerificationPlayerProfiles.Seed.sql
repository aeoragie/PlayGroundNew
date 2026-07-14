-- 로컬 검증용 선수 프로필 보강 — 검증 선수 계정(D01·D11)이 연결된 선수에 대시보드 프로필 데이터 주입.
-- 선행: VerificationPlayerLinks.Seed.sql (계정↔선수 UserId 연결). PlayerId가 아니라 고정 UserId로
-- 선수를 해석하므로 리그 시드 재실행 후에도 이 스크립트만 다시 돌리면 된다.
-- 김정현은 공개 설정을 2행만 넣어 기본값 병합(키·몸무게·주발 공개 / 학교·연락처 비공개) 경로도 검증.
-- 재실행 안전(UPDATE + 삭제 후 삽입). 로컬 개발 DB 전용 — 운영 배포 금지.

DECLARE @U12PlayerId UNIQUEIDENTIFIER =
    (SELECT TOP 1 [PlayerId] FROM [dbo].[SoccerPlayers]
     WHERE [UserId] = 'A0000000-0000-0000-0000-000000000D01' AND [DeletedAt] IS NULL);
DECLARE @U15PlayerId UNIQUEIDENTIFIER =
    (SELECT TOP 1 [PlayerId] FROM [dbo].[SoccerPlayers]
     WHERE [UserId] = 'A0000000-0000-0000-0000-000000000D11' AND [DeletedAt] IS NULL);

IF @U12PlayerId IS NULL OR @U15PlayerId IS NULL
BEGIN
    RAISERROR('Verification players not found. Run VerificationPlayerLinks.Seed.sql first.', 16, 1);
    RETURN;
END

--.// 프로필 확장 컬럼

UPDATE [dbo].[SoccerPlayers]
SET [HeightCm] = 152, [WeightKg] = 43, [PreferredFoot] = 'Right',
    [SchoolName] = '서울신답초등학교', [GuardianPhone] = '010-1234-5678',
    [BirthDate] = '2015-04-12', [IsGuardianManaged] = 1, [UpdatedAt] = GETUTCDATE()
WHERE [PlayerId] = @U12PlayerId;

UPDATE [dbo].[SoccerPlayers]
SET [HeightCm] = 171, [WeightKg] = 58, [PreferredFoot] = 'Left',
    [SchoolName] = '광주북중학교', [GuardianPhone] = '010-2345-6789',
    [BirthDate] = '2012-09-03', [IsGuardianManaged] = 1, [UpdatedAt] = GETUTCDATE()
WHERE [PlayerId] = @U15PlayerId;

--.// 항목별 공개 설정 (신준우 = 5행 전부 / 김정현 = 2행만 — 기본값 병합 검증)

DELETE FROM [dbo].[SoccerPlayerFieldVisibilities] WHERE [PlayerId] IN (@U12PlayerId, @U15PlayerId);

INSERT INTO [dbo].[SoccerPlayerFieldVisibilities] ([PlayerId], [FieldName], [IsPublic]) VALUES
(@U12PlayerId, 'Height', 1),
(@U12PlayerId, 'Weight', 1),
(@U12PlayerId, 'PreferredFoot', 1),
(@U12PlayerId, 'School', 0),
(@U12PlayerId, 'GuardianPhone', 0),
(@U15PlayerId, 'Weight', 0),          -- 기본값(공개)과 다른 저장값
(@U15PlayerId, 'School', 1);          -- 기본값(비공개)과 다른 저장값

--.// 가족 계정 (보호자 = 검증 계정, 본인 = 계정 미연결)

DELETE FROM [dbo].[SoccerPlayerFamilyLinks] WHERE [PlayerId] IN (@U12PlayerId, @U15PlayerId);

INSERT INTO [dbo].[SoccerPlayerFamilyLinks] ([PlayerId], [UserId], [MemberName], [Role], [DisplayOrder]) VALUES
(@U12PlayerId, 'A0000000-0000-0000-0000-000000000D01', '신OO', 'Guardian', 1),
(@U12PlayerId, NULL, '신준우', 'Self', 2),
(@U15PlayerId, 'A0000000-0000-0000-0000-000000000D11', '김OO', 'Guardian', 1),
(@U15PlayerId, NULL, '김정현', 'Self', 2);
