-- 알림 (Design.ClaimFlow 알림 센터). 표시 문구는 클라이언트가 유형+스냅샷으로 조립한다 (카피는 클라 소유).
-- 스냅샷 컬럼을 두는 이유: 알림은 과거 시점의 불변 기록이다 — 선수 이름이 나중에 바뀌어도 "그때 문구"가 맞다.
-- 라우트 문자열은 저장하지 않는다 — 딥링크는 클라이언트가 NotificationType + TargetPlayerId로 조립 (Routes.cs 단일 관리).
-- 유형: 'ClaimRequest'(액션형 — 승인/거절 인라인, RefId=RequestId) / 'ClaimApproved','ClaimRejected'(RefId=RequestId)
--       / 'MatchResult'(RefId=MatchId) / 'CorrectionReviewed'(RefId=CorrectionId — 주최측이 DB만 바꾸므로 조회 시점 지연 생성)
CREATE TABLE [dbo].[SoccerNotifications]
(
    [NotificationId]    UNIQUEIDENTIFIER NOT NULL DEFAULT NEWID(),
    [RecipientUserId]   UNIQUEIDENTIFIER NOT NULL,          -- Account.Users.UserId (앱 계층 참조)
    [NotificationType]  VARCHAR(30)      NOT NULL,          -- 위 유형 5종
    [RefId]             UNIQUEIDENTIFIER NOT NULL,          -- 유형별 주 참조 (요청·경기·신청)
    [TargetPlayerId]    UNIQUEIDENTIFIER NULL,              -- 딥링크용 자녀 (playerId 쿼리 조립)

    -- 표시 스냅샷 (유형별 사용 — 조립은 클라이언트)
    [ActorName]         VARCHAR(300)     NULL,              -- UTF-8 요청자 이름 / 상대팀 이름
    [PlayerName]        VARCHAR(300)     NULL,              -- UTF-8 대상 선수 이름
    [TeamName]          VARCHAR(300)     NULL,              -- UTF-8 팀 이름
    [MetaText]          VARCHAR(300)     NULL,              -- UTF-8 부가 1 (포지션·등번호·연령 / 스코어 / 항목)
    [SubText]           VARCHAR(300)     NULL,              -- UTF-8 부가 2 (사용 코드 / 심사 상태)
    [Relation]          VARCHAR(20)      NULL,              -- 'Mother','Father','Guardian'

    [IsRead]            BIT              NOT NULL DEFAULT 0,
    [ReadAt]            DATETIME2        NULL,
    [CreatedAt]         DATETIME2        NOT NULL DEFAULT GETUTCDATE(),

    CONSTRAINT [PK_SoccerNotifications] PRIMARY KEY ([NotificationId])
);
