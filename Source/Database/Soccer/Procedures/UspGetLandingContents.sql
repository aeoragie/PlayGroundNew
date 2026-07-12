-- @entity: SoccerLandingContentRecord
-- @source: join
-- @join: SoccerLandingContents AS lc (Section, Icon, Title, Body)
-- 활성 랜딩 콘텐츠 조회 (Section별 정렬). 파라미터 없음.
CREATE PROCEDURE [dbo].[UspGetLandingContents]
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        lc.[Section],
        lc.[Icon],
        lc.[Title],
        lc.[Body]
    FROM
        [dbo].[SoccerLandingContents] lc WITH (NOLOCK)
    WHERE
        lc.[IsActive] = 1
    ORDER BY
        lc.[Section], lc.[DisplayOrder];
END
