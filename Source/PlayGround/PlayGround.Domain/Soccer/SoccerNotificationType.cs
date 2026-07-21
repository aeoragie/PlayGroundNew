namespace PlayGround.Domain.Soccer
{
    /// <summary>알림 유형. 멤버 이름 = DB 저장 문자열 (SoccerNotifications.NotificationType).
    /// 표시 문구·딥링크는 클라이언트가 유형+스냅샷으로 조립한다.</summary>
    public enum SoccerNotificationType
    {
        /// <summary>액션형 — 팀 관리자 수신, 인라인 승인/거절 (RefId = RequestId).</summary>
        ClaimRequest,
        /// <summary>보호자 수신 — 연결 승인 (RefId = RequestId, TargetPlayerId로 딥링크).</summary>
        ClaimApproved,
        /// <summary>보호자 수신 — 연결 거절 (RefId = RequestId).</summary>
        ClaimRejected,
        /// <summary>보호자 수신 — 친선경기 결과 반영 (RefId = MatchId).</summary>
        MatchResult,
        /// <summary>신청자 수신 — 기록 수정 신청 심사 결과 (RefId = CorrectionId, 조회 시점 지연 생성).</summary>
        CorrectionReviewed,
        /// <summary>보호자 수신 — 에이전트 상세 정보 열람 요청 (RefId = RequestId, 지연 생성 — 딥링크 심사 화면).</summary>
        ViewRequest,
    }
}
