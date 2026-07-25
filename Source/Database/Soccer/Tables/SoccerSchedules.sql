-- 팀 일정 (Design.Schedule). 팀 대시보드에서 작성하고, 공개 홈 Schedule 탭·허브 "다음 경기"가 열람한다.
-- **상태 컬럼 없음** — 진행/종료는 StartsAt 경과로 파생한다(수동 전환 없음). 결과 입력은 MatchId로 연결.
-- 공개(IsPublic) 일정만 공개 홈·iCal 피드에 노출. 훈련은 기본 비공개(작성 시 스위치 기본 끔).
CREATE TABLE [dbo].[SoccerSchedules]
(
    [ScheduleId]    UNIQUEIDENTIFIER NOT NULL DEFAULT NEWID(),
    [TeamId]        UNIQUEIDENTIFIER NOT NULL,          -- SoccerTeams.TeamId (앱 계층 참조)
    [Type]          VARCHAR(20)      NOT NULL,          -- 'Match','Tournament','Training' (enum 멤버 이름)
    [Title]         VARCHAR(300)     NULL,              -- UTF-8 (한글 100자) 대회·훈련만 (경기는 'vs {상대}' 파생)
    [OpponentName]  VARCHAR(300)     NULL,              -- UTF-8 (한글 100자) 경기·대회만 (훈련은 NULL)
    [StartsAt]      DATETIME2        NOT NULL,          -- 시작 일시 — 상태 파생 기준
    [Venue]         VARCHAR(300)     NOT NULL,          -- UTF-8 (한글 100자) 장소 (필수)
    [IsPublic]      BIT              NOT NULL DEFAULT 1, -- 공개 홈 노출 여부 (훈련은 작성 시 기본 0)
    [MatchId]       UNIQUEIDENTIFIER NULL,              -- 결과 입력 연결 (SoccerMatches.MatchId — "종료 · 결과 입력됨")
    [CreatedBy]     UNIQUEIDENTIFIER NOT NULL,          -- 작성 관리자 (Account.Users.UserId, 앱 계층 참조)

    [CreatedAt]     DATETIME2        NOT NULL DEFAULT GETUTCDATE(),
    [UpdatedAt]     DATETIME2        NOT NULL DEFAULT GETUTCDATE(),
    [DeletedAt]     DATETIME2        NULL,              -- 삭제 = 소프트 (실행취소 지원)

    CONSTRAINT [PK_SoccerSchedules] PRIMARY KEY ([ScheduleId])
);
