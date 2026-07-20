-- @entity: SoccerCorrectionCreatedRecord
-- @source: join
-- @join: SoccerRecordCorrections AS c (CorrectionId)
-- 기록 수정 신청 생성 (팀 관리자). 거부 조건은 전부 빈 결과셋으로 응답한다 —
-- 사유를 구분해 흘리면 남의 경기 존재 여부를 떠볼 수 있다.
--
-- 거부하는 경우:
--   1) 관리하는 팀이 없다
--   2) 그 경기에 우리 팀이 없다 (남의 경기에 신청할 수 없다)
--   3) 친선경기다 — 팀이 직접 고칠 수 있으므로 신청 대상이 아니다(설계 결정 7)
--   4) 같은 경기에 내 미처리(Pending) 신청이 이미 있다 — 1건 1항목, 중복 신청 차단
-- 주의: 파라미터 줄에 꼬리 주석을 달면 제너레이터가 그 파라미터를 누락한다.
CREATE PROCEDURE [dbo].[UspCreateSoccerRecordCorrection]
    @ManagerUserId    UNIQUEIDENTIFIER,
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

    DECLARE @TeamId UNIQUEIDENTIFIER = (
        SELECT TOP 1 [TeamId]
        FROM [dbo].[SoccerTeams] WITH (NOLOCK)
        WHERE [ManagerUserId] = @ManagerUserId AND [DeletedAt] IS NULL
        ORDER BY [CreatedAt] DESC);

    IF @TeamId IS NULL
    BEGIN
        SELECT c.[CorrectionId] FROM [dbo].[SoccerRecordCorrections] c WITH (NOLOCK) WHERE 1 = 0;
        RETURN;
    END

    -- 우리 팀 경기이면서 공식인지 (친선은 신청 대상이 아니다)
    IF NOT EXISTS (
        SELECT 1
        FROM [dbo].[SoccerMatches] WITH (NOLOCK)
        WHERE [MatchId] = @MatchId AND [DeletedAt] IS NULL
          AND [MatchType] = 'Official'
          AND ([HomeTeamId] = @TeamId OR [AwayTeamId] = @TeamId))
    BEGIN
        SELECT c.[CorrectionId] FROM [dbo].[SoccerRecordCorrections] c WITH (NOLOCK) WHERE 1 = 0;
        RETURN;
    END

    -- 같은 경기에 내 미처리 신청이 있으면 중복 신청 차단
    IF EXISTS (
        SELECT 1
        FROM [dbo].[SoccerRecordCorrections] WITH (NOLOCK)
        WHERE [MatchId] = @MatchId AND [RequestedByUserId] = @ManagerUserId
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
         @Description, 'Pending', @ManagerUserId, 'TeamAdmin');

    SELECT c.[CorrectionId]
    FROM [dbo].[SoccerRecordCorrections] c WITH (NOLOCK)
    WHERE c.[CorrectionId] = @CorrectionId;
END
