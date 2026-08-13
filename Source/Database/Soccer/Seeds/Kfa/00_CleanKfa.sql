SET NOCOUNT ON;
DELETE FROM [dbo].[SoccerMatchEvents] WHERE [MatchId] IN (SELECT [MatchId] FROM [dbo].[SoccerMatches] WHERE [DataSource] = 'KfaApi');
DELETE FROM [dbo].[SoccerMatchAppearances] WHERE [MatchId] IN (SELECT [MatchId] FROM [dbo].[SoccerMatches] WHERE [DataSource] = 'KfaApi');
DELETE FROM [dbo].[SoccerMatches] WHERE [DataSource] = 'KfaApi';
DELETE FROM [dbo].[SoccerTournamentStandings] WHERE [DataSource] = 'KfaApi';
DELETE FROM [dbo].[SoccerTournaments] WHERE [DataSource] = 'KfaApi';
DELETE FROM [dbo].[SoccerTeamPlayers] WHERE [PlayerId] IN (SELECT [PlayerId] FROM [dbo].[SoccerPlayers] WHERE [DataSource] = 'KfaApi');
DELETE FROM [dbo].[SoccerPlayers] WHERE [DataSource] = 'KfaApi';
DELETE FROM [dbo].[SoccerTeams] WHERE [DataSource] = 'KfaApi';
GO
