-- 학부모 리뷰 (Design.TeamPublicHome ⑥ 리뷰). 재원 이력이 확인된 보호자 계정만 작성한다 — 판정은 저장 프로시저.
-- 계정당 팀 하나에 1건 (재작성은 수정으로). 팀은 삭제할 수 없다 — 관리자용 삭제 경로 자체가 없다(캡션 규칙).
-- 팀 답글(TeamReply)은 dc에 렌더 명세가 없어 컬럼도 두지 않았다 — 답글 지시가 오면 마이그레이션과 함께.
CREATE TABLE [dbo].[SoccerTeamReviews]
(
    [ReviewId]      UNIQUEIDENTIFIER NOT NULL DEFAULT NEWID(),
    [TeamId]        UNIQUEIDENTIFIER NOT NULL,          -- SoccerTeams.TeamId (앱 계층 참조)
    [AuthorUserId]  UNIQUEIDENTIFIER NOT NULL,          -- 작성 보호자 (Account.Users.UserId, 앱 계층 참조)
    [PlayerId]      UNIQUEIDENTIFIER NOT NULL,          -- 재원 근거 자녀 — 메타(연령·재원기간) 파생용
    [Rating]        INT              NOT NULL,          -- 1~5 (서버 검증)
    [Body]          VARCHAR(1500)    NOT NULL,          -- UTF-8 (한글 500자)

    [CreatedAt]     DATETIME2        NOT NULL DEFAULT GETUTCDATE(),
    [UpdatedAt]     DATETIME2        NOT NULL DEFAULT GETUTCDATE(),
    [DeletedAt]     DATETIME2        NULL,              -- 작성자 본인 소프트 삭제 (실행취소 지원)

    CONSTRAINT [PK_SoccerTeamReviews] PRIMARY KEY ([ReviewId])
);
