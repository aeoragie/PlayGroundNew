-- 에이전트 열람 요청 자격 판정 (Design.AgentDashboard) — **만료·거절 쿨다운·차단 판정은 PlayGround 단독 수행.**
-- 에이전트 서비스가 요청 생성 전에 이 판정을 조회해 강제한다(요청 생성 자체는 에이전트 서비스 몫 — 여기선 판정만).
-- 호출자(에이전트) 본인 판정만: @RequesterUserId → SoccerAgentProfiles.UserId로 @AgentId 해석(남의 자격 조회 불가).
-- 결과(스칼라 2개): RS1 상태 · RS2 쿨다운 해제일.
--   'NotAgent'(에이전트 아님) / 'Blocked'(보호자 차단) / 'Active'(승인·미만료 열람 진행 중) /
--   'Cooldown'(최근 30일 내 거절 — CooldownUntil까지 재요청 불가) / 'Allowed'(가능).
-- 만료(Approved인데 ExpiresAt 경과)는 Active가 아니다 → 재요청 허용(연장 없음, 새 요청). 판정 기준은 저장 ExpiresAt.
CREATE PROCEDURE [dbo].[UspGetSoccerAgentRequestEligibility]
    @RequesterUserId UNIQUEIDENTIFIER,
    @PlayerId        UNIQUEIDENTIFIER,
    @GuardianUserId  UNIQUEIDENTIFIER
AS
BEGIN
    SET NOCOUNT ON;

    -- 시각은 dbo.UfnSystemDate()로만 얻는다. 변수로 한 번 받는 이유는 두 가지다 —
    -- 스칼라 UDF는 인라인되지 않아 WHERE에 직접 쓰면 행마다 호출되고,
    -- 한 프로시저 안의 "지금"이 호출마다 달라지는 것도 막는다.
    DECLARE @Now DATETIME2(7) = dbo.UfnSystemDate();

    DECLARE @Status VARCHAR(20);
    DECLARE @CooldownUntil DATETIME2 = NULL;

    DECLARE @AgentId UNIQUEIDENTIFIER = (
        SELECT TOP 1 [AgentId] FROM [dbo].[SoccerAgentProfiles] WITH (NOLOCK)
        WHERE [UserId] = @RequesterUserId AND [DeletedAt] IS NULL);

    IF @AgentId IS NULL
    BEGIN
        SET @Status = 'NotAgent';
    END
    ELSE IF EXISTS (
        SELECT 1 FROM [dbo].[SoccerAgentBlocks] WITH (NOLOCK)
        WHERE [GuardianUserId] = @GuardianUserId AND [AgentId] = @AgentId)
    BEGIN
        SET @Status = 'Blocked';
    END
    ELSE IF EXISTS (
        SELECT 1 FROM [dbo].[SoccerAgentViewRequests] WITH (NOLOCK)
        WHERE [AgentId] = @AgentId AND [PlayerId] = @PlayerId AND [GuardianUserId] = @GuardianUserId
          AND [Status] = 'Approved' AND [DeletedAt] IS NULL
          AND [ExpiresAt] IS NOT NULL AND [ExpiresAt] > @Now)
    BEGIN
        SET @Status = 'Active';
    END
    ELSE
    BEGIN
        -- 최근 30일 내 거절이 있으면 쿨다운 (가장 최근 거절 시각 + 30일까지)
        DECLARE @LatestDeny DATETIME2 = (
            SELECT MAX([ReviewedAt]) FROM [dbo].[SoccerAgentViewRequests] WITH (NOLOCK)
            WHERE [AgentId] = @AgentId AND [PlayerId] = @PlayerId AND [GuardianUserId] = @GuardianUserId
              AND [Status] = 'Denied' AND [ReviewedAt] IS NOT NULL);

        IF @LatestDeny IS NOT NULL AND @LatestDeny > DATEADD(DAY, -30, @Now)
        BEGIN
            SET @Status = 'Cooldown';
            SET @CooldownUntil = DATEADD(DAY, 30, @LatestDeny);
        END
        ELSE
        BEGIN
            SET @Status = 'Allowed';
        END
    END

    SELECT @Status AS [Status];
    SELECT @CooldownUntil AS [CooldownUntil];
END
