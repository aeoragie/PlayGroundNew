-- 랜딩 콘텐츠 시드. 카피는 SPEC.LANDING.md 기준 (한 글자도 변경 금지).
-- 재실행 안전: Section별로 지우고 다시 삽입.
DELETE FROM [dbo].[SoccerLandingContents] WHERE [Section] IN ('Feature', 'HowStep');

INSERT INTO [dbo].[SoccerLandingContents] ([Section], [DisplayOrder], [Icon], [Title], [Body]) VALUES
('Feature', 1, N'🏠', N'팀 홈페이지 자동 생성', N'소개·선수단·시즌성적·모집까지, 대시보드에 입력한 데이터가 그대로 공개 홈페이지가 됩니다. 별도 제작 비용 없이.'),
('Feature', 2, N'📈', N'선수 이력 · 기록 관리', N'커리어 타임라인, 시즌 통계, 대표 영상 포트폴리오. 경기 결과가 입력되면 선수 기록이 자동으로 쌓입니다.'),
('Feature', 3, N'📊', N'경기기록 무료 공개', N'대회 결과, 순위표, 일정을 로그인 없이 누구나 조회. 자녀와 관심팀의 기록을 언제든 확인하세요.'),
('HowStep', 1, N'1', N'팀 등록', N'팀 정보를 입력하면 공개 팀 홈페이지가 자동으로 생성됩니다. 엠블럼, 소개, 코칭스태프까지.'),
('HowStep', 2, N'2', N'선수단 등록', N'이름·포지션·등번호만 입력하면 선수 프로필이 함께 만들어지고, 학부모가 초대코드로 연결합니다.'),
('HowStep', 3, N'3', N'기록 자동 집계', N'경기 결과를 입력하면 팀 성적과 선수 개인 기록이 자동으로 쌓이고, 경기기록에 공개됩니다.');
