namespace PlayGround.Domain.Soccer
{
    /// <summary>
    /// 선수 프로필 공개 설정 대상 항목. 멤버 이름 = DB 저장 문자열 (SoccerPlayerFieldVisibilities.FieldName).
    /// 기본 공개값은 <see cref="SoccerPlayerProfileFieldExtensions.DefaultIsPublic"/> 참조.
    /// </summary>
    public enum SoccerPlayerProfileField
    {
        Height,
        Weight,
        PreferredFoot,
        School,
        GuardianPhone,
    }

    public static class SoccerPlayerProfileFieldExtensions
    {
        /// <summary>항목 기본 공개값 — 키·몸무게·주발 공개, 학교·보호자 연락처 비공개 (SPEC.PLAYERDASHBOARD §1).</summary>
        public static bool DefaultIsPublic(this SoccerPlayerProfileField field)
        {
            return field is SoccerPlayerProfileField.Height
                or SoccerPlayerProfileField.Weight
                or SoccerPlayerProfileField.PreferredFoot;
        }
    }
}
