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
        /// <summary>액션형 — 보호자 수신, 지원 수락 후 선수단 초대. 인라인 [초대 확인]으로 로스터 편입
        /// (RefId = ApplicationId, TargetPlayerId = 자녀). 처리 여부는 SoccerApplications.ConfirmedAt로 라이브 파생.</summary>
        RosterInvite,
        /// <summary>이동형 — 보호자 수신, 팀 공지 발행 (RefId = PostId, TargetPlayerId = 자녀). 딥링크 → 자녀 팀 소식.
        /// 자료(Material)는 발송하지 않는다 — 공지(Notice)만. 스냅샷: TeamName + 글 제목(MetaText).</summary>
        TeamNotice,
        /// <summary>이동형 — 본인 수신, 데이터 내려받기 파일 준비 완료 (RefId = 내보내기 RequestId). 딥링크 → 설정 계정 탭.
        /// 완료 알림은 알림 센터 + 이메일 두 채널(Design.SettingsFlows ③). 스냅샷 없음(문구는 클라 고정).</summary>
        ExportReady,
        /// <summary>이동형 — 보호자 수신, 에이전트 열람 승인 만료 임박(3일 전, RefId = 열람 RequestId). 딥링크 → 심사 화면.
        /// 조회 시점 지연 생성(Design.AgentDashboard). 에이전트 요소라 flag OFF면 클라가 숨긴다(ViewRequest와 동일).</summary>
        AgentGrantExpiring,
    }
}
