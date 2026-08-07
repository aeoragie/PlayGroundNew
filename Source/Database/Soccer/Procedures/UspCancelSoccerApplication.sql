-- 지원 취소 (보호자, 소프트 삭제). 내가 올린 지원이고 아직 대기(Pending)일 때만.
-- 검토중·수락·종료된 지원은 취소할 수 없다(빈 결과 → Command가 Forbidden).
-- 반환은 취소된 행(ApplicationId, Status) — 취소 대상이 없으면 빈 결과.
-- @entity는 UspCreateSoccerApplication이 SoccerApplicationCreateRecord로 선언했으므로 여기서는 마커를 두지 않는다.
CREATE PROCEDURE [dbo].[UspCancelSoccerApplication]
    @GuardianUserId  UNIQUEIDENTIFIER,
    @ApplicationId   UNIQUEIDENTIFIER
AS
BEGIN
    SET NOCOUNT ON;

    -- 시각은 dbo.UfnSystemDate()로만 얻는다. 변수로 한 번 받는 이유는 두 가지다 —
    -- 스칼라 UDF는 인라인되지 않아 WHERE에 직접 쓰면 행마다 호출되고,
    -- 한 프로시저 안의 "지금"이 호출마다 달라지는 것도 막는다.
    DECLARE @Now DATETIME2(7) = dbo.UfnSystemDate();

    UPDATE [dbo].[SoccerApplications]
    SET [DeletedAt] = @Now, [UpdatedAt] = @Now
    WHERE [ApplicationId] = @ApplicationId AND [GuardianUserId] = @GuardianUserId
      AND [Status] = 'Pending' AND [DeletedAt] IS NULL;

    DECLARE @Affected INT = @@ROWCOUNT;

    SELECT a.[Status], a.[ApplicationId]
    FROM [dbo].[SoccerApplications] a WITH (NOLOCK)
    WHERE a.[ApplicationId] = @ApplicationId AND a.[GuardianUserId] = @GuardianUserId
      AND @Affected = 1;
END
