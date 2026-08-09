namespace PlayGround.Domain.Soccer
{
    public static class SoccerPlayerProfileFieldExtensions
    {
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
