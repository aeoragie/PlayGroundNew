-- 경기 도메인 인덱스 (MatchSchemaDesign.md §4 — 실시간 집계 조회 대비)
-- 필터드 인덱스는 쓰지 않는다: 있으면 해당 테이블 DML에 QUOTED_IDENTIFIER ON이 강제되어
-- sqlcmd(기본 OFF) 기반 시드·운영 스크립트가 전부 깨진다. 이 규모에선 일반 인덱스로 충분.
-- SSDT는 일괄 처리당 문 하나만 허용하므로 GO로 끊는다 (sqlcmd 적용에도 그대로 쓴다).
CREATE NONCLUSTERED INDEX [IX_SoccerMatches_TournamentId] ON [dbo].[SoccerMatches] ([TournamentId]);
GO
CREATE NONCLUSTERED INDEX [IX_SoccerMatches_HomeTeamId] ON [dbo].[SoccerMatches] ([HomeTeamId]);
GO
CREATE NONCLUSTERED INDEX [IX_SoccerMatches_AwayTeamId] ON [dbo].[SoccerMatches] ([AwayTeamId]);
GO
CREATE NONCLUSTERED INDEX [IX_SoccerMatchEvents_MatchId] ON [dbo].[SoccerMatchEvents] ([MatchId]);
GO
CREATE NONCLUSTERED INDEX [IX_SoccerMatchEvents_PlayerId] ON [dbo].[SoccerMatchEvents] ([PlayerId]);
GO
CREATE NONCLUSTERED INDEX [IX_SoccerMatchEvents_AssistPlayerId] ON [dbo].[SoccerMatchEvents] ([AssistPlayerId]);
GO
CREATE NONCLUSTERED INDEX [IX_SoccerMatchAppearances_PlayerId] ON [dbo].[SoccerMatchAppearances] ([PlayerId]);
GO
CREATE NONCLUSTERED INDEX [IX_SoccerMatchAppearances_MatchId] ON [dbo].[SoccerMatchAppearances] ([MatchId]);
GO
CREATE NONCLUSTERED INDEX [IX_SoccerTournamentStandings_TournamentId] ON [dbo].[SoccerTournamentStandings] ([TournamentId]);
GO
CREATE NONCLUSTERED INDEX [IX_SoccerTournaments_SeasonYear] ON [dbo].[SoccerTournaments] ([SeasonYear]);
GO
