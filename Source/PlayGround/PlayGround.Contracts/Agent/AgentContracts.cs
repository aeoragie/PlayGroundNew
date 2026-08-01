using System;
using System.Collections.Generic;

namespace PlayGround.Contracts.Agent
{
    /// <summary>에이전트 열람 요청 심사 화면 묶음 (보호자 측 — Design.AgentViewApproval).</summary>
    public class AgentViewRequestResponse
    {
        public Guid RequestId { get; set; }

        /// <summary>'Pending','Approved','Denied','Revoked' — 화면은 Revoked를 거절과 동급으로 그린다.</summary>
        public string Status { get; set; } = string.Empty;

        public string Message { get; set; } = string.Empty;
        public DateTime RequestedAt { get; set; }
        public DateTime? ExpiresAt { get; set; }

        /// <summary>승인됐지만 30일이 지났다 — 서버 파생 (권한 뷰 접근 차단 기준과 동일).</summary>
        public bool IsExpired { get; set; }

        public Guid PlayerId { get; set; }
        public string PlayerName { get; set; } = string.Empty;
        public string? PlayerAgeGroup { get; set; }
        public string? PlayerPosition { get; set; }

        public AgentProfileDto Agent { get; set; } = new();
        public List<AgentViewLogDto> Logs { get; set; } = new();
    }

    /// <summary>에이전트 신원 카드 — 데이터는 에이전트 서비스가 채운다(읽기 전용).</summary>
    public class AgentProfileDto
    {
        public string Name { get; set; } = string.Empty;
        public string? AgencyName { get; set; }
        public int? RegisteredYear { get; set; }
        public bool IsVerified { get; set; }
        public int BrokerageCount { get; set; }
        public decimal? Rating { get; set; }
        public string? ActiveRegions { get; set; }
    }

    /// <summary>열람 기록 한 건 — 문구는 클라이언트가 EventType으로 조립.</summary>
    public class AgentViewLogDto
    {
        public string EventType { get; set; } = string.Empty; // 'Approved','ProfileView','RecordView'
        public DateTime CreatedAt { get; set; }
    }

    /// <summary>심사 요청 — Action은 SoccerAgentReviewAction 멤버 이름 문자열.</summary>
    public class ReviewAgentViewRequestRequest
    {
        public Guid RequestId { get; set; }
        public string Action { get; set; } = string.Empty;
    }

    /// <summary>열람 요청 자격 판정 (Design.AgentDashboard) — 만료·거절 쿨다운·차단은 PlayGround 단독 판정.
    /// 에이전트 서비스가 요청 생성 전에 조회한다(생성 자체는 에이전트 서비스 몫). 호출자 본인(에이전트) 것만.</summary>
    public class AgentRequestEligibilityResponse
    {
        /// <summary>'NotAgent' | 'Blocked' | 'Active'(승인·미만료) | 'Cooldown' | 'Allowed'.</summary>
        public string Status { get; set; } = string.Empty;

        /// <summary>Cooldown일 때만 값 — 이 시각 이후 재요청 가능(최근 거절 + 30일).</summary>
        public DateTime? CooldownUntil { get; set; }

        /// <summary>요청 생성 가능 여부 — Status == 'Allowed'.</summary>
        public bool CanRequest => Status == "Allowed";
    }
}
