using PlayGround.Contracts.Soccer;

namespace PlayGround.Application.Player
{
    public static class PlayerProfileFieldDefaults
    {
        /// <summary>항목 기본 공개값 — 프로필·키·몸무게·주발·강점 태그 공개, 학교·보호자 연락처 비공개 (SPEC.PLAYERDASHBOARD §1).</summary>
        public static bool DefaultIsPublic(this SoccerPlayerProfileField field)
        {
            return field is SoccerPlayerProfileField.Profile
                or SoccerPlayerProfileField.Height
                or SoccerPlayerProfileField.Weight
                or SoccerPlayerProfileField.PreferredFoot
                or SoccerPlayerProfileField.StrengthTags;
        }
    }
}
