namespace PlayGround.Client.Models
{
    /// <summary>경기영상 유형 (필터·pill).</summary>
    public enum SoccerVideoType
    {
        Highlight,
        FullMatch,
        Training,
    }

    public static class SoccerVideoTypeExtensions
    {
        public static string ToLabel(this SoccerVideoType type)
        {
            return type switch
            {
                SoccerVideoType.Highlight => "하이라이트",
                SoccerVideoType.FullMatch => "풀경기",
                _ => "훈련",
            };
        }
    }
}
