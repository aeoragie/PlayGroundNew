SET NOCOUNT ON;
DECLARE @pg UNIQUEIDENTIFIER = (SELECT TOP 1 [TeamId] FROM [dbo].[SoccerTeams] WHERE [TeamName] IN ('검증fc','플레이그라운드FC') AND [DataSource] <> 'KfaApi' AND [DeletedAt] IS NULL);
-- 테스트 팀이 없는 DB(운영 등)에서는 플레이그라운드FC를 새로 만들어 검증fc 자리를 대체한다
IF @pg IS NULL
BEGIN
    SET @pg = '5240BCC1-5AE8-904E-FB41-60EED7326651';
    INSERT INTO [dbo].[SoccerTeams] ([TeamId],[TeamName],[TeamType],[AgeGroup],[Slug],[IsPublicProfile],[DataSource],[ExternalId])
    VALUES (@pg, '플레이그라운드FC', '클럽', 'U12', 'playgroundfc', 1, 'Seed', 'playgroundfc');
END
UPDATE [dbo].[SoccerTeams] SET [TeamName] = '플레이그라운드FC', [UpdatedAt] = GETUTCDATE() WHERE [TeamId] = @pg;
UPDATE [dbo].[SoccerMatches] SET [HomeTeamName] = '플레이그라운드FC' WHERE [HomeTeamId] = @pg AND [HomeTeamName] <> '플레이그라운드FC';
UPDATE [dbo].[SoccerMatches] SET [AwayTeamName] = '플레이그라운드FC' WHERE [AwayTeamId] = @pg AND [AwayTeamName] <> '플레이그라운드FC';
UPDATE [dbo].[SoccerMatchEvents] SET [TeamName] = '플레이그라운드FC' WHERE [TeamId] = @pg AND [TeamName] <> '플레이그라운드FC';
UPDATE [dbo].[SoccerMatchAppearances] SET [TeamName] = '플레이그라운드FC' WHERE [TeamId] = @pg AND [TeamName] <> '플레이그라운드FC';
UPDATE [dbo].[SoccerTournamentStandings] SET [TeamName] = '플레이그라운드FC' WHERE [TeamId] = @pg AND [TeamName] <> '플레이그라운드FC';
UPDATE [dbo].[SoccerMatches] SET [HomeTeamId] = @pg WHERE [HomeTeamId] = '0C64A6FF-595B-A569-43E3-D12268232EE8';
UPDATE [dbo].[SoccerMatches] SET [AwayTeamId] = @pg WHERE [AwayTeamId] = '0C64A6FF-595B-A569-43E3-D12268232EE8';
UPDATE [dbo].[SoccerMatchEvents] SET [TeamId] = @pg WHERE [TeamId] = '0C64A6FF-595B-A569-43E3-D12268232EE8';
UPDATE [dbo].[SoccerMatchAppearances] SET [TeamId] = @pg WHERE [TeamId] = '0C64A6FF-595B-A569-43E3-D12268232EE8';
UPDATE [dbo].[SoccerTournamentStandings] SET [TeamId] = @pg WHERE [TeamId] = '0C64A6FF-595B-A569-43E3-D12268232EE8';
UPDATE [dbo].[SoccerPlayers] SET [TeamId] = @pg WHERE [TeamId] = '0C64A6FF-595B-A569-43E3-D12268232EE8';
UPDATE [dbo].[SoccerTeamPlayers] SET [TeamId] = @pg WHERE [TeamId] = '0C64A6FF-595B-A569-43E3-D12268232EE8';
GO
