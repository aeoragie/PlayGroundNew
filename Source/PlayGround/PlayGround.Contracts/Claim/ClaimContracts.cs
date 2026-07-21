using System;

namespace PlayGround.Contracts.Claim
{
    /// <summary>스텝 ① → ②: 초대코드로 조회한 선수 카드 (코드는 아직 소진되지 않는다).</summary>
    public class ClaimInviteCardResponse
    {
        public Guid PlayerId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Position { get; set; }
        public string? JerseyNumber { get; set; }
        public int? BirthYear { get; set; }
        public string? AgeGroup { get; set; }
        public string TeamName { get; set; } = string.Empty;
    }

    /// <summary>스텝 ② → ③: 연결 요청 생성. Relation은 SoccerClaimRelation 멤버 이름 문자열.</summary>
    public class CreateClaimRequestRequest
    {
        public string Code { get; set; } = string.Empty;
        public string Relation { get; set; } = string.Empty;
    }

    /// <summary>내 연결 요청 요약 — 대기 화면·재방문 복원. Status: 'Pending','Approved','Rejected'.</summary>
    public class ClaimRequestSummaryResponse
    {
        public Guid RequestId { get; set; }
        public string Status { get; set; } = string.Empty;
        public string Relation { get; set; } = string.Empty;
        public string PlayerName { get; set; } = string.Empty;
        public string TeamName { get; set; } = string.Empty;
        public DateTime RequestedAt { get; set; }
    }

    /// <summary>팀 관리자 승인/거절 요청.</summary>
    public class ReviewClaimRequestRequest
    {
        public Guid RequestId { get; set; }
        public bool Approve { get; set; }
    }

    /// <summary>승인/거절 처리 결과.</summary>
    public class ReviewClaimResponse
    {
        public Guid RequestId { get; set; }
        public string Status { get; set; } = string.Empty;
        public string PlayerName { get; set; } = string.Empty;
    }
}
