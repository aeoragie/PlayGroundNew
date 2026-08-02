using PlayGround.Client.Localization;

namespace PlayGround.Client.Models
{
    /// <summary>주발. 멤버 이름 = 서버 저장 문자열 (SoccerPlayers.PreferredFoot).</summary>
    public enum SoccerPreferredFoot
    {
        Left,
        Right,
        Both,
    }

    public static class SoccerPreferredFootExtensions
    {
        /// <summary>화면 표기 라벨.</summary>
        public static string ToLabel(this SoccerPreferredFoot foot)
        {
            return foot switch
            {
                SoccerPreferredFoot.Left => AppText.Enums.FootLeft,
                SoccerPreferredFoot.Right => AppText.Enums.FootRight,
                _ => AppText.Enums.FootBoth,
            };
        }

        /// <summary>서버 문자열 → 라벨. 미지정·알 수 없는 값은 null.</summary>
        public static string? ParseLabelOrNull(string? value)
        {
            return Enum.TryParse(value, out SoccerPreferredFoot foot) ? foot.ToLabel() : null;
        }
    }
}
