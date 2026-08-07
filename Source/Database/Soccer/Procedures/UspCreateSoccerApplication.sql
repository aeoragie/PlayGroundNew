-- @entity: SoccerApplicationCreateRecord
-- @source: join
-- @join: SoccerApplications AS a (Status, ApplicationId)
-- 선수 모집 지원 생성 (보호자) — 내 자녀(소유)로 공개 공고에 지원한다.
-- 결과셋은 상태 신호 1행: Status(문자열) + ApplicationId. Command가 상태별로 매핑한다
-- ('Ok'→성공 / 'Duplicate'→중복 인라인 / 'Closed'·'Full'·'Cooldown'·'Forbidden'→해당 오류).
-- Status·ApplicationId 둘 다 SoccerApplications 실컬럼이라 제너레이터가 매핑한다(계산 컬럼 회피).
-- ApplicationId는 Ok가 아니면 빈 GUID(NULL을 비널 Guid에 매핑하면 Dapper가 던진다).
--
-- 거부 판정 순서(먼저 걸리는 사유를 돌려준다):
--   Forbidden = 내 자녀가 아니다 / 공고가 없다
--   Closed    = 공고가 마감(Closed) 또는 마감일 경과
--   Full      = 정원(Capacity) 충족 — 수락 수 도달
--   Duplicate = 같은 자녀·공고에 진행 중(Pending/Reviewing/Accepted) 지원이 이미 있다
--   Cooldown  = 같은 자녀·공고에 30일 이내 보류(Rejected) 이력이 있다
-- 주의: 파라미터 줄에 꼬리 주석을 달면 제너레이터가 그 파라미터를 누락한다.
CREATE PROCEDURE [dbo].[UspCreateSoccerApplication]
    @GuardianUserId   UNIQUEIDENTIFIER,
    @RecruitmentId    UNIQUEIDENTIFIER,
    @PlayerId         UNIQUEIDENTIFIER,
    @DesiredPosition  VARCHAR(20) = NULL,
    @Introduction     VARCHAR(1500) = NULL
AS
BEGIN
    SET NOCOUNT ON;

    -- 시각은 dbo.UfnSystemDate()로만 얻는다. 변수로 한 번 받는 이유는 두 가지다 —
    -- 스칼라 UDF는 인라인되지 않아 WHERE에 직접 쓰면 행마다 호출되고,
    -- 한 프로시저 안의 "지금"이 호출마다 달라지는 것도 막는다.
    DECLARE @Now DATETIME2(7) = dbo.UfnSystemDate();

    IF @DesiredPosition = '' SET @DesiredPosition = NULL;
    IF @Introduction = '' SET @Introduction = NULL;

    DECLARE @Status VARCHAR(20) = NULL;
    DECLARE @ApplicationId UNIQUEIDENTIFIER = NULL;

    -- (a) 내 자녀인가 — 관리 계정(SoccerPlayers.UserId) 또는 보호자 가족 연결
    DECLARE @OwnsPlayer BIT = CASE WHEN EXISTS (
        SELECT 1 FROM [dbo].[SoccerPlayers] p WITH (NOLOCK)
        WHERE p.[PlayerId] = @PlayerId AND p.[DeletedAt] IS NULL AND p.[UserId] = @GuardianUserId)
      OR EXISTS (
        SELECT 1 FROM [dbo].[SoccerPlayerFamilyLinks] fl WITH (NOLOCK)
        WHERE fl.[PlayerId] = @PlayerId AND fl.[UserId] = @GuardianUserId
          AND fl.[Role] = 'Guardian' AND fl.[DeletedAt] IS NULL)
      THEN 1 ELSE 0 END;

    -- (b) 공고 존재 + 모집중 판정 원자료
    DECLARE @Capacity INT = NULL;
    DECLARE @RecruitmentExists BIT = 0;
    DECLARE @IsOpen BIT = 0;

    SELECT
        @RecruitmentExists = 1,
        @Capacity = r.[Capacity],
        @IsOpen = CASE WHEN r.[Status] = 'Open'
                        AND (r.[DeadlineAt] IS NULL OR r.[DeadlineAt] > @Now)
                       THEN 1 ELSE 0 END
    FROM [dbo].[SoccerTeamRecruitments] r WITH (NOLOCK)
    WHERE r.[RecruitmentId] = @RecruitmentId AND r.[DeletedAt] IS NULL;

    IF @OwnsPlayer = 0 OR @RecruitmentExists = 0
    BEGIN
        SET @Status = 'Forbidden';
    END
    ELSE IF @IsOpen = 0
    BEGIN
        SET @Status = 'Closed';
    END
    ELSE IF @Capacity IS NOT NULL AND (
        SELECT COUNT(*) FROM [dbo].[SoccerApplications] a WITH (NOLOCK)
        WHERE a.[RecruitmentId] = @RecruitmentId AND a.[Status] = 'Accepted' AND a.[DeletedAt] IS NULL
    ) >= @Capacity
    BEGIN
        SET @Status = 'Full';
    END
    ELSE IF EXISTS (
        SELECT 1 FROM [dbo].[SoccerApplications] a WITH (NOLOCK)
        WHERE a.[RecruitmentId] = @RecruitmentId AND a.[PlayerId] = @PlayerId
          AND a.[Status] IN ('Pending', 'Reviewing', 'Accepted') AND a.[DeletedAt] IS NULL)
    BEGIN
        SET @Status = 'Duplicate';
    END
    ELSE IF EXISTS (
        SELECT 1 FROM [dbo].[SoccerApplications] a WITH (NOLOCK)
        WHERE a.[RecruitmentId] = @RecruitmentId AND a.[PlayerId] = @PlayerId
          AND a.[Status] = 'Rejected' AND a.[RejectedAt] IS NOT NULL
          AND a.[RejectedAt] >= DATEADD(DAY, -30, @Now))
    BEGIN
        SET @Status = 'Cooldown';
    END
    ELSE
    BEGIN
        SET @ApplicationId = NEWID();
        INSERT INTO [dbo].[SoccerApplications]
            ([ApplicationId], [RecruitmentId], [PlayerId], [GuardianUserId],
             [DesiredPosition], [Introduction], [Status], [Route])
        VALUES
            (@ApplicationId, @RecruitmentId, @PlayerId, @GuardianUserId,
             @DesiredPosition, @Introduction, 'Pending', 'Direct');
        SET @Status = 'Ok';
    END

    SELECT
        @Status AS [Status],
        ISNULL(@ApplicationId, CAST(0x0 AS UNIQUEIDENTIFIER)) AS [ApplicationId];
END
