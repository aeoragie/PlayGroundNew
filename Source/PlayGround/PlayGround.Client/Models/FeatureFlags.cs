namespace PlayGround.Client.Models
{
    /// <summary>기능 플래그 (설계 결정 4 — 에이전트 축 UI는 플래그 뒤에 숨긴다).
    /// 값은 컴파일 타임 상수가 아니라 readonly — off 분기가 unreachable 경고 없이 남는다.</summary>
    public static class FeatureFlags
    {
        /// <summary>에이전트 열람 승인 (Design.AgentViewApproval). **기본 off** —
        /// 요청 생산자(에이전트 서비스)가 아직 없어 켜도 도달할 데이터가 없다.
        /// 켜면: 알림 센터 "열람 요청" 행 + /approvals/agent/{id} 심사 화면이 활성화된다.</summary>
        public static readonly bool AgentApproval = false;
    }
}
