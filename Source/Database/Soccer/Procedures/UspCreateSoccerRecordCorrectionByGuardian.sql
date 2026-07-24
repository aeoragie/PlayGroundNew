-- 결과셋은 팀 경로와 같은 모양(CorrectionId 1컬럼) — @entity는 팀 create 프로시저가 이미 선언했으므로
-- 여기서는 마커를 두지 않는다(같은 @entity 이름 중복 생성 방지). 리포지토리가 SoccerCorrectionCreatedRecord를 재사용.
-- 기록 수정 신청 생성 (보호자) — 내 자녀 관련 공식 경기 기록만. 팀 관리자 경로(UspCreateSoccerRecordCorrection)의
-- 보호자판이다. 거부는 전부 빈 결과셋(사유를 구분해 흘리지 않는다 — 남의 경기 존재 여부 떠보기 방지).
--
-- 거부하는 경우:
--   1) 요청값이 비었다
--   2) 내가 관리하는(소유한) 그 자녀가 없다
--   3) 그 경기에 자녀의 출전 기록이 없다 (내 자녀와 무관한 경기)
--   4) 친선경기다 — 직접 고칠 수 있으므로 신청 대상이 아니다(설계 결정 7)
--   5) 같은 경기에 내 미처리(Pending) 신청이 이미 있다 — 1건 1항목, 중복 차단
-- 주의: 파라미터 줄에 꼬리 주석을 달면 제너레이터가 그 파라미터를 누락한다.
CREATE PROCEDURE [dbo].[UspCreateSoccerRecordCorrectionByGuardian]
    @UserId           UNIQUEIDENTIFIER,
    @TargetPlayerId   UNIQUEIDENTIFIER,
    @MatchId          UNIQUEIDENTIFIER,
    @FieldType        VARCHAR(20),
    @CurrentValue     VARCHAR(300) = NULL,
    @RequestedValue   VARCHAR(300) = NULL,
    @Description      VARCHAR(1500) = NULL
AS
BEGIN
    SET NOCOUNT ON;

    IF @RequestedValue IS NULL OR LEN(LTRIM(RTRIM(@RequestedValue))) = 0
    BEGIN
        SELECT c.[CorrectionId] FROM [dbo].[SoccerRecordCorrections] c WITH (NOLOCK) WHERE 1 = 0;
        RETURN;
    END

    -- 소유 검증을 해석에 내장 — 내 자녀가 아니면 0행이라 별도 권한 분기가 필요 없다
    DECLARE @PlayerId UNIQUEIDENTIFIER = (
        SELECT TOP 1 [PlayerId]
        FROM [dbo].[SoccerPlayers] WITH (NOLOCK)
        WHERE [UserId] = @UserId AND [DeletedAt] IS NULL
          AND (@TargetPlayerId IS NULL OR [PlayerId] = @TargetPlayerId)
        ORDER BY [CreatedAt]);

    IF @PlayerId IS NULL
    BEGIN
        SELECT c.[CorrectionId] FROM [dbo].[SoccerRecordCorrections] c WITH (NOLOCK) WHERE 1 = 0;
        RETURN;
    END

    -- 자녀가 그 공식 경기에 출전한 기록이 있는지 (내 자녀와 무관한 경기 차단). 신청 시점 소속팀도 함께.
    DECLARE @TeamId UNIQUEIDENTIFIER = (
        SELECT TOP 1 a.[TeamId]
        FROM [dbo].[SoccerMatchAppearances] a WITH (NOLOCK)
        JOIN [dbo].[SoccerMatches] m WITH (NOLOCK)
            ON m.[MatchId] = a.[MatchId] AND m.[DeletedAt] IS NULL AND m.[MatchType] = 'Official'
        WHERE a.[MatchId] = @MatchId AND a.[PlayerId] = @PlayerId AND a.[DeletedAt] IS NULL);

    IF @TeamId IS NULL
    BEGIN
        SELECT c.[CorrectionId] FROM [dbo].[SoccerRecordCorrections] c WITH (NOLOCK) WHERE 1 = 0;
        RETURN;
    END

    -- 같은 경기에 내 미처리 신청이 있으면 중복 차단
    IF EXISTS (
        SELECT 1
        FROM [dbo].[SoccerRecordCorrections] WITH (NOLOCK)
        WHERE [MatchId] = @MatchId AND [RequestedByUserId] = @UserId
          AND [Status] = 'Pending' AND [DeletedAt] IS NULL)
    BEGIN
        SELECT c.[CorrectionId] FROM [dbo].[SoccerRecordCorrections] c WITH (NOLOCK) WHERE 1 = 0;
        RETURN;
    END

    DECLARE @CorrectionId UNIQUEIDENTIFIER = NEWID();

    INSERT INTO [dbo].[SoccerRecordCorrections]
        ([CorrectionId], [MatchId], [TeamId], [FieldType], [CurrentValue], [RequestedValue],
         [Description], [Status], [RequestedByUserId], [RequestedByRole])
    VALUES
        (@CorrectionId, @MatchId, @TeamId, @FieldType, @CurrentValue, @RequestedValue,
         @Description, 'Pending', @UserId, 'Guardian');

    SELECT c.[CorrectionId]
    FROM [dbo].[SoccerRecordCorrections] c WITH (NOLOCK)
    WHERE c.[CorrectionId] = @CorrectionId;
END
