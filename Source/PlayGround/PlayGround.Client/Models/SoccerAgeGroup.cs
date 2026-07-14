namespace PlayGround.Client.Models
{
    /// <summary>유소년 연령 그룹. 화면 표기는 이름 그대로 (U12·U15·U18).</summary>
    public enum SoccerAgeGroup
    {
        U12,
        U15,
        U18,
    }

    public static class SoccerAgeGroupExtensions
    {
        /// <summary>서버 문자열 → enum. 미지정·알 수 없는 값은 null.</summary>
        public static SoccerAgeGroup? ParseAgeGroupOrNull(string? value)
        {
            return Enum.TryParse(value, out SoccerAgeGroup ageGroup) ? ageGroup : null;
        }
    }
}
