-- 미완료(Pending) 데이터 내려받기 요청 목록 (서버 재기동 시 재개용). 백그라운드 잡은 비내구성이라
-- 생성 중 재기동되면 Pending이 남는다 — 워커가 기동 시 이 목록을 큐에 다시 넣어 이어서 처리한다.
-- RequestId만 내린다(잡이 상세를 다시 읽는다).
CREATE PROCEDURE [dbo].[UspGetPendingSoccerDataExports]
AS
BEGIN
    SET NOCOUNT ON;

    SELECT [RequestId]
    FROM [dbo].[SoccerDataExportRequests] WITH (NOLOCK)
    WHERE [Status] = 'Pending' AND [DeletedAt] IS NULL
    ORDER BY [CreatedAt];
END
