-- 시즌(연도) 기준 대회/리그 목록 조회 (Records 목록·아카이브 공용, 공개 화면).
-- 결과셋 3개: ①시즌 대회 전체(테이블 엔티티) → ②우승팀(아카이브 '우승' 뱃지용, 시즌 스코프)
--            → ③기록이 있는 연도 목록(아카이브 연도 칩). MultiQueryReader로 소비.
-- 정렬(진행중→예정→종료)·세그먼트(대회|리그)·연령 그룹핑은 클라이언트/Application 몫.
CREATE PROCEDURE [dbo].[UspGetSoccerTournamentsBySeason]
    @SeasonYear INT
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        t.[TournamentId], t.[SeasonYear], t.[Name], t.[SeriesSlug], t.[Format], t.[Scope],
        t.[AgeGroup], t.[RegionGroup], t.[Status], t.[StartDate], t.[EndDate], t.[TeamCount],
        t.[HostName], t.[MethodText], t.[MatchTimeText], t.[VenueText], t.[TiebreakText],
        t.[RegulationPdfUrl], t.[SourceName], t.[SourceUrl], t.[OrganizerUserId], t.[OrganizerType],
        t.[DataSource], t.[ExternalId], t.[SyncStatus], t.[CreatedAt], t.[UpdatedAt], t.[DeletedAt]
    FROM [dbo].[SoccerTournaments] t WITH (NOLOCK)
    WHERE t.[SeasonYear] = @SeasonYear AND t.[DeletedAt] IS NULL
    ORDER BY t.[AgeGroup], t.[Name];

    SELECT
        a.[AwardId], a.[TournamentId], a.[AwardType], a.[TeamId], a.[TeamName], a.[DisplayOrder],
        a.[CreatedAt], a.[UpdatedAt], a.[DeletedAt]
    FROM [dbo].[SoccerTournamentAwards] a WITH (NOLOCK)
    JOIN [dbo].[SoccerTournaments] t WITH (NOLOCK)
        ON t.[TournamentId] = a.[TournamentId] AND t.[SeasonYear] = @SeasonYear AND t.[DeletedAt] IS NULL
    WHERE a.[AwardType] = 'Champion' AND a.[DeletedAt] IS NULL;

    SELECT DISTINCT t.[SeasonYear]
    FROM [dbo].[SoccerTournaments] t WITH (NOLOCK)
    WHERE t.[DeletedAt] IS NULL
    ORDER BY t.[SeasonYear] DESC;
END
