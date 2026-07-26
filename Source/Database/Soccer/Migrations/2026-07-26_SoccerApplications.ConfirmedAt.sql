-- 선수단 초대 확인(로스터 편입) 시각 컬럼 추가 (Design.Application, E5 최종 단계).
-- 수락(Accepted) 이후 보호자가 초대를 확인하면 SoccerTeamPlayers에 편입하고 이 시각을 찍는다 —
-- 전용 상태 컬럼 대신 편입 완료 마커로 쓴다(지원 상태는 Accepted에서 고정, 결정 7).
-- NULL 허용이라 기존 지원 코드는 영향 없음. 멱등. **다른 PC 필수 실행.**
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.SoccerApplications') AND name = 'ConfirmedAt')
    ALTER TABLE [dbo].[SoccerApplications] ADD [ConfirmedAt] DATETIME2 NULL;
