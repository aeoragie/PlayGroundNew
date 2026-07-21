-- 리뷰 작성·수정 겸용 (@ReviewId 빈 GUID = 신규, B3 규약).
-- **재원 판정이 이 프로시저의 본체다**: 작성자의 보호자 연결 자녀가 이 팀에 Active 소속이어야 한다.
-- 자격 없음·미존재·남의 리뷰는 빈 결과 (존재 여부 미노출). 계정당 팀 하나에 1건 — 중복 신규는 거부.
CREATE PROCEDURE [dbo].[UspSaveSoccerTeamReview]
    @AuthorUserId UNIQUEIDENTIFIER,
    @TeamSlug VARCHAR(100),
    @ReviewId UNIQUEIDENTIFIER,
    @Rating INT,
    @Body VARCHAR(1500)
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @Applied INT = 0;
    DECLARE @TeamId UNIQUEIDENTIFIER = (
        SELECT TOP 1 [TeamId]
        FROM [dbo].[SoccerTeams]
        WHERE [Slug] = @TeamSlug AND [IsPublicProfile] = 1 AND [DeletedAt] IS NULL
        ORDER BY [CreatedAt] DESC);

    -- 재원 근거 자녀 — 보호자 연결 + 이 팀 Active 소속 (첫 자녀)
    DECLARE @PlayerId UNIQUEIDENTIFIER = (
        SELECT TOP 1 f.[PlayerId]
        FROM [dbo].[SoccerPlayerFamilyLinks] f
        JOIN [dbo].[SoccerTeamPlayers] t
            ON t.[PlayerId] = f.[PlayerId] AND t.[TeamId] = @TeamId
            AND t.[Status] = 'Active' AND t.[DeletedAt] IS NULL
        WHERE f.[UserId] = @AuthorUserId AND f.[Role] = 'Guardian' AND f.[DeletedAt] IS NULL
        ORDER BY f.[CreatedAt]);

    IF @PlayerId IS NOT NULL
    BEGIN
        IF @ReviewId = CAST(0x0 AS UNIQUEIDENTIFIER)
        BEGIN
            -- 계정당 1건 — 이미 쓴 리뷰가 있으면 신규를 거부한다 (수정 경로로 유도)
            IF NOT EXISTS (SELECT 1 FROM [dbo].[SoccerTeamReviews]
                           WHERE [TeamId] = @TeamId AND [AuthorUserId] = @AuthorUserId AND [DeletedAt] IS NULL)
            BEGIN
                SET @ReviewId = NEWID();

                INSERT INTO [dbo].[SoccerTeamReviews]
                    ([ReviewId], [TeamId], [AuthorUserId], [PlayerId], [Rating], [Body])
                VALUES (@ReviewId, @TeamId, @AuthorUserId, @PlayerId, @Rating, @Body);

                SET @Applied = 1;
            END
        END
        ELSE
        BEGIN
            UPDATE [dbo].[SoccerTeamReviews]
            SET [Rating] = @Rating, [Body] = @Body, [UpdatedAt] = GETUTCDATE()
            WHERE [ReviewId] = @ReviewId AND [TeamId] = @TeamId
              AND [AuthorUserId] = @AuthorUserId AND [DeletedAt] IS NULL;

            SET @Applied = @@ROWCOUNT;
        END
    END

    SELECT
        r.[ReviewId], r.[TeamId], r.[AuthorUserId], r.[PlayerId], r.[Rating], r.[Body],
        r.[CreatedAt], r.[UpdatedAt], r.[DeletedAt]
    FROM [dbo].[SoccerTeamReviews] r WITH (NOLOCK)
    WHERE r.[ReviewId] = @ReviewId AND @Applied = 1;
END
