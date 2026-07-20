-- B6 상태 3종 확인용 — **주최측(대회 운영 서비스)이 채우는 값을 흉내 낸다.**
-- PlayGround에는 심사 API가 없으므로(설계 결정 6·7) 반영/반려 화면은 이렇게 심어서만 볼 수 있다.
-- 이 스크립트가 하는 일이 곧 "우리가 만들지 않기로 한 것"의 범위다.
SET NOCOUNT ON;

DECLARE @Manager UNIQUEIDENTIFIER = 'A0000000-0000-0000-0000-000000000C11'; -- 광주광주FCU15 관리자
DECLARE @TeamId UNIQUEIDENTIFIER = (SELECT TOP 1 TeamId FROM SoccerTeams WHERE ManagerUserId = @Manager AND DeletedAt IS NULL);

-- 이 팀의 공식 경기 3건을 잡아 각각 접수/반영/반려 상태로 만든다
DECLARE @M1 UNIQUEIDENTIFIER, @M2 UNIQUEIDENTIFIER, @M3 UNIQUEIDENTIFIER;

;WITH Official AS (
    SELECT MatchId, ROW_NUMBER() OVER (ORDER BY MatchedAt DESC) AS Seq
    FROM SoccerMatches WITH (NOLOCK)
    WHERE MatchType = 'Official' AND DeletedAt IS NULL
      AND (HomeTeamId = @TeamId OR AwayTeamId = @TeamId))
SELECT
    @M1 = MAX(CASE WHEN Seq = 1 THEN MatchId END),
    @M2 = MAX(CASE WHEN Seq = 2 THEN MatchId END),
    @M3 = MAX(CASE WHEN Seq = 3 THEN MatchId END)
FROM Official;

DELETE FROM SoccerRecordCorrections WHERE RequestedByUserId = @Manager AND Description LIKE '[B6]%';

INSERT INTO SoccerRecordCorrections
    (MatchId, TeamId, FieldType, CurrentValue, RequestedValue, Description,
     Status, RejectReason, RequestedByUserId, RequestedByRole, ReviewedAt, CreatedAt)
VALUES
    (@M1, @TeamId, 'Score', '2:2', '3:2', '[B6] 후반 39분 득점이 빠졌어요',
     'Pending', NULL, @Manager, 'TeamAdmin', NULL, DATEADD(day, -1, GETUTCDATE())),
    (@M2, @TeamId, 'GoalAssist', '김민준', '박지호', '[B6] 득점자가 뒤바뀌었어요',
     'Accepted', NULL, @Manager, 'TeamAdmin', DATEADD(day, -2, GETUTCDATE()), DATEADD(day, -5, GETUTCDATE())),
    (@M3, @TeamId, 'Appearance', '기록 없음', '박지호 출전 추가', '[B6] 후반 교체 출전이 누락됐어요',
     'Rejected', '경기 감독관 기록지와 대조한 결과 출전 기록이 확인되지 않았습니다.',
     @Manager, 'TeamAdmin', DATEADD(day, -3, GETUTCDATE()), DATEADD(day, -8, GETUTCDATE()));

SELECT Status, FieldType, CurrentValue + ' -> ' + RequestedValue AS Change,
       CASE WHEN RejectReason IS NULL THEN '(없음)' ELSE '사유 있음' END AS Reason
FROM SoccerRecordCorrections
WHERE RequestedByUserId = @Manager AND DeletedAt IS NULL
ORDER BY CASE WHEN Status = 'Pending' THEN 0 ELSE 1 END, CreatedAt DESC;
