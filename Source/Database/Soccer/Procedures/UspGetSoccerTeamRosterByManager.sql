-- @entity: SoccerTeamRosterRecord
-- @source: join
-- @join: SoccerTeamPlayers AS tp (TeamPlayerId, JerseyNumber, Position, Grade)
-- @join: SoccerPlayers AS p (PlayerId, Name, Slug, PhotoUrl, AgeGroup, UserId, StrengthTags)
-- @join: SoccerPlayerInvites AS inv (Code)
-- 팀 관리자 기준 선수단(로스터) 조회 (대시보드 선수단 섹션). 단일 결과셋.
-- Claim 상태는 C#에서 계산 (UserId 연결 = Claimed) — 여기서는 원본 컬럼만 내려준다.
-- Code = 유효한 Pending 초대코드 (Unclaimed 선수의 '초대코드 보내기'용, 관리자 전용 API에서만 노출).
-- 정렬: 등번호 숫자순 (숫자 아님·미입력은 뒤로, 이름순 보조).
CREATE PROCEDURE [dbo].[UspGetSoccerTeamRosterByManager]
    @ManagerUserId UNIQUEIDENTIFIER
AS
BEGIN
    SET NOCOUNT ON;

    -- 시각은 dbo.UfnSystemDate()로만 얻는다. 변수로 한 번 받는 이유는 두 가지다 —
    -- 스칼라 UDF는 인라인되지 않아 WHERE에 직접 쓰면 행마다 호출되고,
    -- 한 프로시저 안의 "지금"이 호출마다 달라지는 것도 막는다.
    DECLARE @Now DATETIME2(7) = dbo.UfnSystemDate();

    DECLARE @TeamId UNIQUEIDENTIFIER = (
        SELECT TOP 1 [TeamId]
        FROM [dbo].[SoccerTeams] WITH (NOLOCK)
        WHERE [ManagerUserId] = @ManagerUserId AND [DeletedAt] IS NULL
        ORDER BY [CreatedAt] DESC);

    -- 강점 태그는 선수별 공개 설정을 SQL에서 건다 (행 없으면 기본 공개 — IsPublic=0 명시일 때만 숨김)
    SELECT
        tp.[TeamPlayerId], tp.[JerseyNumber], tp.[Position], tp.[Grade],
        p.[PlayerId], p.[Name], p.[Slug], p.[PhotoUrl], p.[AgeGroup], p.[UserId],
        CASE WHEN fvs.[IsPublic] = 0 THEN NULL ELSE p.[StrengthTags] END AS [StrengthTags],
        inv.[Code]
    FROM [dbo].[SoccerTeamPlayers] tp WITH (NOLOCK)
    JOIN [dbo].[SoccerPlayers] p WITH (NOLOCK) ON p.[PlayerId] = tp.[PlayerId]
    LEFT JOIN [dbo].[SoccerPlayerFieldVisibilities] fvs WITH (NOLOCK)
        ON fvs.[PlayerId] = p.[PlayerId] AND fvs.[FieldName] = 'StrengthTags'
    OUTER APPLY (
        SELECT TOP 1 i.[Code]
        FROM [dbo].[SoccerPlayerInvites] i WITH (NOLOCK)
        WHERE i.[PlayerId] = tp.[PlayerId] AND i.[TeamId] = tp.[TeamId]
          AND i.[Status] = 'Pending'
          AND (i.[ExpiresAt] IS NULL OR i.[ExpiresAt] > @Now)
        ORDER BY i.[CreatedAt] DESC) inv
    WHERE tp.[TeamId] = @TeamId
      AND tp.[Status] = 'Active' AND tp.[DeletedAt] IS NULL
      AND p.[DeletedAt] IS NULL
    ORDER BY
        CASE WHEN TRY_CAST(tp.[JerseyNumber] AS INT) IS NULL THEN 1 ELSE 0 END,
        TRY_CAST(tp.[JerseyNumber] AS INT),
        p.[Name];
END
