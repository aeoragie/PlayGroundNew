-- 게시판 글 저장 — 신규·수정 겸용 (@PostId 빈 GUID = 신규, B3 규약).
-- 소유 판정은 팀 ManagerUserId — 거부·미존재는 빈 결과(존재 여부 미노출). 삭제된 글은 수정할 수 없다.
-- 고정(IsPinned)은 여기서 건드리지 않는다 — 전용 프로시저(UspSetSoccerTeamPostPinned)가 최대 2개를 강제한다.
-- 수정 시 EditedAt를 찍는다("수정됨"). 첨부는 통째 교체(기존 파일 삭제 후 @FilesJson 재삽입).
-- 알림(공지 발행)은 Application 후처리다 — 신규 Notice일 때만, 여기서는 발송하지 않는다(설정 필터·DB간 조인 회피).
CREATE PROCEDURE [dbo].[UspSaveSoccerTeamPost]
    @ManagerUserId UNIQUEIDENTIFIER,
    @PostId        UNIQUEIDENTIFIER,
    @Type          VARCHAR(20),
    @Title         VARCHAR(300),
    @Body          VARCHAR(6000),
    @IsPublic      BIT,
    @AuthorName    VARCHAR(300) = NULL,
    @FilesJson     VARCHAR(MAX) = NULL   -- [{ "url":..., "name":..., "sizeBytes":... }]
AS
BEGIN
    SET NOCOUNT ON;

    -- 시각은 dbo.UfnSystemDate()로만 얻는다. 변수로 한 번 받는 이유는 두 가지다 —
    -- 스칼라 UDF는 인라인되지 않아 WHERE에 직접 쓰면 행마다 호출되고,
    -- 한 프로시저 안의 "지금"이 호출마다 달라지는 것도 막는다.
    DECLARE @Now DATETIME2(7) = dbo.UfnSystemDate();

    DECLARE @Applied INT = 0;
    DECLARE @TeamId UNIQUEIDENTIFIER = (
        SELECT TOP 1 [TeamId] FROM [dbo].[SoccerTeams]
        WHERE [ManagerUserId] = @ManagerUserId AND [DeletedAt] IS NULL
        ORDER BY [CreatedAt] DESC);

    IF @TeamId IS NOT NULL
    BEGIN
        BEGIN TRY
            BEGIN TRANSACTION;

            IF @PostId = CAST(0x0 AS UNIQUEIDENTIFIER)
            BEGIN
                SET @PostId = NEWID();

                INSERT INTO [dbo].[SoccerTeamPosts]
                    ([PostId], [TeamId], [Type], [Title], [Body], [IsPublic], [AuthorId], [AuthorName])
                VALUES (@PostId, @TeamId, @Type, @Title, @Body, @IsPublic, @ManagerUserId, @AuthorName);

                SET @Applied = 1;
            END
            ELSE
            BEGIN
                UPDATE [dbo].[SoccerTeamPosts]
                SET [Type] = @Type, [Title] = @Title, [Body] = @Body, [IsPublic] = @IsPublic,
                    [EditedAt] = @Now, [UpdatedAt] = @Now
                WHERE [PostId] = @PostId AND [TeamId] = @TeamId AND [DeletedAt] IS NULL;

                SET @Applied = @@ROWCOUNT;
            END

            IF @Applied = 1
            BEGIN
                -- 첨부 통째 교체
                DELETE FROM [dbo].[SoccerTeamPostFiles] WHERE [PostId] = @PostId;

                IF @FilesJson IS NOT NULL AND LEN(@FilesJson) > 2
                BEGIN
                    INSERT INTO [dbo].[SoccerTeamPostFiles] ([PostId], [FileUrl], [FileName], [SizeBytes], [DisplayOrder])
                    SELECT @PostId, j.[url], j.[name], ISNULL(j.[sizeBytes], 0), j.[ord]
                    FROM OPENJSON(@FilesJson)
                    WITH (
                        [url]       VARCHAR(400) '$.url',
                        [name]      VARCHAR(300) '$.name',
                        [sizeBytes] BIGINT       '$.sizeBytes',
                        [ord]       INT          '$.ord'
                    ) j
                    WHERE j.[url] IS NOT NULL AND j.[name] IS NOT NULL;
                END
            END

            COMMIT TRANSACTION;
        END TRY
        BEGIN CATCH
            IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
            THROW;
        END CATCH
    END

    -- RS1: 저장된 글 (@Applied=1일 때만 — 실패·거부는 빈 결과)
    SELECT
        po.[PostId], po.[TeamId], po.[Type], po.[Title], po.[Body], po.[IsPinned], po.[IsPublic],
        po.[AuthorId], po.[AuthorName], po.[EditedAt], po.[CreatedAt], po.[UpdatedAt], po.[DeletedAt]
    FROM [dbo].[SoccerTeamPosts] po WITH (NOLOCK)
    WHERE po.[PostId] = @PostId AND @Applied = 1;

    -- RS2: 저장된 글의 첨부
    SELECT f.[FileId], f.[PostId], f.[FileUrl], f.[FileName], f.[SizeBytes], f.[DisplayOrder], f.[CreatedAt]
    FROM [dbo].[SoccerTeamPostFiles] f WITH (NOLOCK)
    WHERE f.[PostId] = @PostId AND @Applied = 1
    ORDER BY f.[DisplayOrder], f.[CreatedAt];
END
