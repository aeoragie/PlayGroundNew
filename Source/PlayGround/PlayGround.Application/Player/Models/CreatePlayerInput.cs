namespace PlayGround.Application.Player.Models
{
    /// <summary>선수 프로필 생성 입력(검증·정규화 완료). 유즈케이스 → 포트 전달용.</summary>
    public class CreatePlayerInput
    {
        public Guid UserId { get; set; }
        public string Name { get; set; } = string.Empty;
        public DateOnly? BirthDate { get; set; }
        public string? AgeGroup { get; set; }
        public string? Region { get; set; }
    }
}
