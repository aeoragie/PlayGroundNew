namespace PlayGround.Contracts.Player
{
    /// <summary>선수 온보딩 프로필 생성 요청. UserId는 본문이 아니라 인증 토큰(sub)에서 읽는다.</summary>
    public class CreatePlayerProfileRequest
    {
        public string Name { get; set; } = string.Empty;
        public string? BirthDate { get; set; }   // "YYYY.MM.DD" — 서버에서 파싱
        public string? AgeGroup { get; set; }     // 'U12' | 'U15' | 'U18'
        public string? Region { get; set; }
    }

    /// <summary>생성된 선수 프로필 요약.</summary>
    public class CreatePlayerProfileResponse
    {
        public Guid PlayerId { get; set; }
    }
}
