namespace PlayGround.Client.Models
{
    /// <summary>
    /// 사용자 역할 — 서버가 JWT·AuthUserDto.Role로 주는 문자열의 클라이언트 해석.
    /// 서버 원본 문자열(Users.UserRole 컬럼 값)과 이름이 일치해야 한다. 에이전트 축은 선반영.
    /// </summary>
    public enum UserRole
    {
        General,
        Player,
        TeamAdmin,
        Agent,
        AgencyAdmin,
    }

    public static class UserRoleExtensions
    {
        /// <summary>서버 역할 문자열을 해석. null·빈 값·미지원 값은 General(역할 미선택).</summary>
        public static UserRole Parse(string? role)
        {
            return Enum.TryParse(role, ignoreCase: true, out UserRole parsed) ? parsed : UserRole.General;
        }
    }
}
