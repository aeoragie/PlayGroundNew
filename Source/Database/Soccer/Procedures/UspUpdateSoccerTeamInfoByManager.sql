-- @entity: SoccerUpdatedTeamRecord
-- @source: join
-- @join: SoccerTeams AS t (TeamId, Slug)
-- 팀 정보 수정 (팀 대시보드 "정보 수정"). 관리자 본인 팀만 대상.
-- 기본 정보 + 핵심가치 + 코칭스태프를 한 트랜잭션으로 저장한다.
-- 가치·코치는 "통째로 교체"(소프트 삭제 후 재삽입) — 순서 변경·삭제를 한 번에 반영하는 가장 단순한 방법이고,
-- 목록이 작아(각 3~5행) 비용도 무시할 수 있다.
--
-- 이미지 URL은 이미 업로드가 끝난 뒤의 공개 경로만 받는다(업로드 자체는 별도 엔드포인트).
-- NULL = 변경 없음이 아니라 "지움"이다 — 클라이언트가 항상 현재 값을 실어 보낸다.
--
-- 주의: 파라미터 줄에 꼬리 주석을 달면 제너레이터가 그 파라미터를 누락한다(기본값 없는 경우).
--       @ValuesJson  : [{"Title":"...","Description":"..."}]
--       @CoachesJson : [{"Name":"...","Role":"...","Career":"...","Certification":"...","Quote":"...",
--                        "Achievements":"[\"칩1\"]","InstagramUrl":"...","YoutubeUrl":"..."}]
-- 결과셋 1개: TeamId·Slug (권한 없으면 빈 결과셋 → 호출자가 NotFound로 변환).
CREATE PROCEDURE [dbo].[UspUpdateSoccerTeamInfoByManager]
    @ManagerUserId  UNIQUEIDENTIFIER,
    @TeamName       VARCHAR(300) = NULL,
    @Description    VARCHAR(3000) = NULL,
    @Region         VARCHAR(300) = NULL,
    @FoundedYear    INT = NULL,
    @LogoUrl        VARCHAR(2048) = NULL,
    @CoverImageUrl  VARCHAR(2048) = NULL,
    @ValuesJson     VARCHAR(MAX) = NULL,
    @CoachesJson    VARCHAR(MAX) = NULL
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @TeamId UNIQUEIDENTIFIER = (
        SELECT TOP 1 [TeamId]
        FROM [dbo].[SoccerTeams] WITH (NOLOCK)
        WHERE [ManagerUserId] = @ManagerUserId AND [DeletedAt] IS NULL
        ORDER BY [CreatedAt] DESC);

    IF @TeamId IS NULL
    BEGIN
        RETURN;   -- 관리하는 팀이 없다
    END

    IF @TeamName IS NULL OR LEN(LTRIM(RTRIM(@TeamName))) = 0
    BEGIN
        RETURN;   -- 팀명은 필수 (형식 검증은 Application에서 이미 끝났다)
    END

    BEGIN TRY
        BEGIN TRANSACTION;

        UPDATE [dbo].[SoccerTeams]
        SET [TeamName]      = LTRIM(RTRIM(@TeamName)),
            [Description]   = NULLIF(LTRIM(RTRIM(ISNULL(@Description, ''))), ''),
            [Region]        = NULLIF(LTRIM(RTRIM(ISNULL(@Region, ''))), ''),
            [FoundedYear]   = @FoundedYear,
            [LogoUrl]       = NULLIF(@LogoUrl, ''),
            [CoverImageUrl] = NULLIF(@CoverImageUrl, ''),
            [UpdatedAt]     = GETUTCDATE()
        WHERE [TeamId] = @TeamId;

        --.// 핵심가치 — 통째로 교체
        UPDATE [dbo].[SoccerTeamValues]
        SET [DeletedAt] = GETUTCDATE()
        WHERE [TeamId] = @TeamId AND [DeletedAt] IS NULL;

        IF @ValuesJson IS NOT NULL AND LEN(@ValuesJson) > 2
        BEGIN
            INSERT INTO [dbo].[SoccerTeamValues] ([TeamId], [Title], [Description], [DisplayOrder])
            SELECT
                @TeamId,
                LTRIM(RTRIM(v.[Title])),
                LTRIM(RTRIM(ISNULL(v.[Description], ''))),
                ROW_NUMBER() OVER (ORDER BY v.[Ordinal])
            FROM OPENJSON(@ValuesJson)
            WITH (
                [Ordinal]     INT          '$.DisplayOrder',
                [Title]       VARCHAR(150) '$.Title',
                [Description] VARCHAR(600) '$.Description'
            ) v
            WHERE v.[Title] IS NOT NULL AND LEN(LTRIM(RTRIM(v.[Title]))) > 0;
        END

        --.// 코칭스태프 — 통째로 교체
        UPDATE [dbo].[SoccerTeamCoaches]
        SET [DeletedAt] = GETUTCDATE()
        WHERE [TeamId] = @TeamId AND [DeletedAt] IS NULL;

        IF @CoachesJson IS NOT NULL AND LEN(@CoachesJson) > 2
        BEGIN
            INSERT INTO [dbo].[SoccerTeamCoaches]
                ([TeamId], [Name], [Role], [Career], [Certification], [Quote], [Achievements],
                 [InstagramUrl], [YoutubeUrl], [DisplayOrder])
            SELECT
                @TeamId,
                LTRIM(RTRIM(c.[Name])),
                LTRIM(RTRIM(ISNULL(c.[Role], ''))),
                NULLIF(LTRIM(RTRIM(ISNULL(c.[Career], ''))), ''),
                NULLIF(LTRIM(RTRIM(ISNULL(c.[Certification], ''))), ''),
                NULLIF(LTRIM(RTRIM(ISNULL(c.[Quote], ''))), ''),
                NULLIF(LTRIM(RTRIM(ISNULL(c.[Achievements], ''))), ''),
                NULLIF(LTRIM(RTRIM(ISNULL(c.[InstagramUrl], ''))), ''),
                NULLIF(LTRIM(RTRIM(ISNULL(c.[YoutubeUrl], ''))), ''),
                ROW_NUMBER() OVER (ORDER BY c.[Ordinal])
            FROM OPENJSON(@CoachesJson)
            WITH (
                [Ordinal]       INT           '$.DisplayOrder',
                [Name]          VARCHAR(150)  '$.Name',
                [Role]          VARCHAR(60)   '$.Role',
                [Career]        VARCHAR(300)  '$.Career',
                [Certification] VARCHAR(100)  '$.Certification',
                [Quote]         VARCHAR(600)  '$.Quote',
                [Achievements]  VARCHAR(900)  '$.Achievements',
                [InstagramUrl]  VARCHAR(2048) '$.InstagramUrl',
                [YoutubeUrl]    VARCHAR(2048) '$.YoutubeUrl'
            ) c
            WHERE c.[Name] IS NOT NULL AND LEN(LTRIM(RTRIM(c.[Name]))) > 0;
        END

        COMMIT TRANSACTION;
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0
        BEGIN
            ROLLBACK TRANSACTION;
        END;

        THROW;   -- THROW 앞 문장은 반드시 세미콜론으로 끝나야 한다
    END CATCH

    SELECT [TeamId], [Slug]
    FROM [dbo].[SoccerTeams] WITH (NOLOCK)
    WHERE [TeamId] = @TeamId;
END
