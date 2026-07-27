-- 데이터 내려받기 요청 (Design.SettingsFlows ③). 요청 접수 → 백그라운드 잡이 파일 생성 → 완료 알림.
-- 상태 Pending(준비 중)/Ready(준비 완료)/Failed(실패). 동시 1건 · 재요청 쿨다운 24h(생성 프로시저가 판정).
-- 다운로드는 **서명 URL** = 추측 불가한 DownloadToken(엔드포인트가 Ready·미만료·횟수<상한을 원자 검증).
-- 파일은 정적 서빙 밖(비공개 경로)에 두고 StorageKey로 참조한다 — UseStaticFiles로 우회 못 하게.
-- 취소 = 소프트 삭제(Pending만). 자녀 데이터는 연결된 자녀분만 포함(잡이 소유로 판정).
CREATE TABLE [dbo].[SoccerDataExportRequests]
(
    [RequestId]       UNIQUEIDENTIFIER NOT NULL DEFAULT NEWID(),
    [UserId]          UNIQUEIDENTIFIER NOT NULL,          -- Account.Users.UserId (요청자)
    [Status]          VARCHAR(20)      NOT NULL DEFAULT 'Pending', -- 'Pending','Ready','Failed'

    -- 포함 항목 3종 (요청 모달 체크)
    [IncludeProfile]  BIT              NOT NULL DEFAULT 1, -- 계정·프로필
    [IncludeRecords]  BIT              NOT NULL DEFAULT 1, -- 경기 기록·커리어
    [IncludeRequests] BIT              NOT NULL DEFAULT 1, -- 요청·신청 내역

    [DownloadToken]   VARCHAR(64)      NULL,              -- 추측 불가 서명 URL 토큰 (Ready 시 발급)
    [StorageKey]      VARCHAR(400)     NULL,              -- 비공개 파일 경로/키 (정적 서빙 밖)
    [SizeBytes]       BIGINT           NULL,              -- 완료 파일 크기
    [ExpiresAt]       DATETIME2        NULL,              -- Ready + 7일 (만료 후 다운로드 불가)
    [DownloadCount]   INT              NOT NULL DEFAULT 0,
    [MaxDownloads]    INT              NOT NULL DEFAULT 3, -- 다운로드 3회 상한

    [CreatedAt]       DATETIME2        NOT NULL DEFAULT GETUTCDATE(),
    [UpdatedAt]       DATETIME2        NOT NULL DEFAULT GETUTCDATE(),
    [CompletedAt]     DATETIME2        NULL,              -- Ready/Failed 전환 시각
    [DeletedAt]       DATETIME2        NULL,              -- 취소 = 소프트 삭제

    CONSTRAINT [PK_SoccerDataExportRequests] PRIMARY KEY ([RequestId])
);
