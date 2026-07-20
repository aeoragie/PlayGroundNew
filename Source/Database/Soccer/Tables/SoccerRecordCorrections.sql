-- 공식 경기 기록 수정 신청. 공식 기록의 주체는 주최측이므로(설계 결정 7) 팀·보호자는
-- 직접 고치지 않고 신청을 올리고, 주최측(대회 운영 서비스)이 심사해 반영한다.
--
-- **PlayGround는 생성·조회·취소만 한다.** Status를 Accepted/Rejected로 바꾸고 RejectReason·
-- ReviewedAt·ReviewedByUserId를 채우는 것은 대회 운영 서비스의 몫이다(DB 공유 — 설계 결정 6).
-- 그 쪽 컬럼을 여기서 쓰는 코드를 만들면 결정 7이 무너진다.
--
-- 1건 1항목 — 여러 오류는 신청을 여러 건 올린다(심사 단위를 명확히 하기 위해).
CREATE TABLE [dbo].[SoccerRecordCorrections]
(
    [CorrectionId]     UNIQUEIDENTIFIER NOT NULL DEFAULT NEWID(),
    [MatchId]          UNIQUEIDENTIFIER NOT NULL,          -- SoccerMatches.MatchId (앱 계층 참조)
    [TeamId]           UNIQUEIDENTIFIER NULL,              -- 신청 시점의 우리 팀 (목록 필터용)

    [FieldType]        VARCHAR(20)      NOT NULL,          -- 'Score','GoalAssist','Appearance','Other'
    [CurrentValue]     VARCHAR(300)     NULL,              -- UTF-8 (한글 100자) 신청 시점의 기록 — 심사 시 대조용
    [RequestedValue]   VARCHAR(300)     NOT NULL,          -- UTF-8 (한글 100자) 올바르다고 주장하는 기록
    [Description]      VARCHAR(1500)    NULL,              -- UTF-8 (한글 500자) 근거 설명 (선택)

    [Status]           VARCHAR(20)      NOT NULL DEFAULT 'Pending', -- 'Pending','Accepted','Rejected'
    [RejectReason]     VARCHAR(1500)    NULL,              -- UTF-8 (한글 500자) 반려 시 필수 — 주최측이 채운다

    [RequestedByUserId] UNIQUEIDENTIFIER NOT NULL,         -- Account.Users.UserId (앱 계층 참조)
    [RequestedByRole]  VARCHAR(20)      NOT NULL,          -- 'TeamAdmin','Guardian' — 신청 주체 구분

    -- 주최측(대회 운영 서비스) 기록 영역 — PlayGround는 읽기만 한다
    [ReviewedByUserId] UNIQUEIDENTIFIER NULL,
    [ReviewedAt]       DATETIME2        NULL,

    [CreatedAt]        DATETIME2        NOT NULL DEFAULT GETUTCDATE(),
    [UpdatedAt]        DATETIME2        NOT NULL DEFAULT GETUTCDATE(),
    [DeletedAt]        DATETIME2        NULL,              -- 신청 취소 = 소프트 삭제

    CONSTRAINT [PK_SoccerRecordCorrections] PRIMARY KEY ([CorrectionId])
);
