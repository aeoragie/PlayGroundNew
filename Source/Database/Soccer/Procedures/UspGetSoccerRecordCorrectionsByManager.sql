-- @entity: SoccerCorrectionRecord
-- @source: join
-- @join: SoccerRecordCorrections AS c (CorrectionId, MatchId, TeamId, FieldType, CurrentValue, RequestedValue, Description, Status, RejectReason, CreatedAt, ReviewedAt)
-- @join: SoccerMatches AS m (HomeTeamId, HomeTeamName, AwayTeamName, MatchedAt)
-- @join: SoccerTournaments AS t (Name)
-- 내가 올린 기록 수정 신청 목록 (팀 관리자). 요약 문구("리그 12R 스코어 3:1 → 3:2")와
-- 날짜 포맷은 클라이언트가 조립한다 — 여기서는 원자료만 준다.
-- 취소(소프트 삭제)된 신청은 제외. 진행 중인 것이 위로 오도록 Pending 우선 정렬.
CREATE PROCEDURE [dbo].[UspGetSoccerRecordCorrectionsByManager]
    @ManagerUserId UNIQUEIDENTIFIER
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        c.[CorrectionId], c.[MatchId], c.[TeamId], c.[FieldType], c.[CurrentValue], c.[RequestedValue],
        c.[Description], c.[Status], c.[RejectReason], c.[CreatedAt], c.[ReviewedAt],
        m.[HomeTeamId], m.[HomeTeamName], m.[AwayTeamName], m.[MatchedAt],
        t.[Name]
    FROM [dbo].[SoccerRecordCorrections] c WITH (NOLOCK)
    INNER JOIN [dbo].[SoccerMatches] m WITH (NOLOCK)
        ON m.[MatchId] = c.[MatchId] AND m.[DeletedAt] IS NULL
    LEFT JOIN [dbo].[SoccerTournaments] t WITH (NOLOCK)
        ON t.[TournamentId] = m.[TournamentId] AND t.[DeletedAt] IS NULL
    WHERE c.[RequestedByUserId] = @ManagerUserId AND c.[DeletedAt] IS NULL
    ORDER BY
        CASE WHEN c.[Status] = 'Pending' THEN 0 ELSE 1 END,
        c.[CreatedAt] DESC;
END
