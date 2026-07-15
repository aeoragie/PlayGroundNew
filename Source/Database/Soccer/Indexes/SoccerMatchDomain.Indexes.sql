-- 경기 도메인 인덱스 (MatchSchemaDesign.md §4 — 실시간 집계 조회 대비)
-- 필터드 인덱스는 쓰지 않는다: 있으면 해당 테이블 DML에 QUOTED_IDENTIFIER ON이 강제되어
-- sqlcmd(기본 OFF) 기반 시드·운영 스크립트가 전부 깨진다. 이 규모에선 일반 인덱스로 충분.
CREATE NONCLUSTERED INDEX [IX_SoccerMatches_TournamentId] ON [dbo].[SoccerMatches] ([TournamentId]);
CREATE NONCLUSTERED INDEX [IX_SoccerMatches_HomeTeamId] ON [dbo].[SoccerMatches] ([HomeTeamId]);
CREATE NONCLUSTERED INDEX [IX_SoccerMatches_AwayTeamId] ON [dbo].[SoccerMatches] ([AwayTeamId]);
CREATE NONCLUSTERED INDEX [IX_SoccerMatchEvents_MatchId] ON [dbo].[SoccerMatchEvents] ([MatchId]);
CREATE NONCLUSTERED INDEX [IX_SoccerMatchEvents_PlayerId] ON [dbo].[SoccerMatchEvents] ([PlayerId]);
CREATE NONCLUSTERED INDEX [IX_SoccerMatchEvents_AssistPlayerId] ON [dbo].[SoccerMatchEvents] ([AssistPlayerId]);
CREATE NONCLUSTERED INDEX [IX_SoccerMatchAppearances_PlayerId] ON [dbo].[SoccerMatchAppearances] ([PlayerId]);
CREATE NONCLUSTERED INDEX [IX_SoccerMatchAppearances_MatchId] ON [dbo].[SoccerMatchAppearances] ([MatchId]);
CREATE NONCLUSTERED INDEX [IX_SoccerTournamentStandings_TournamentId] ON [dbo].[SoccerTournamentStandings] ([TournamentId]);
CREATE NONCLUSTERED INDEX [IX_SoccerTournaments_SeasonYear] ON [dbo].[SoccerTournaments] ([SeasonYear]);
