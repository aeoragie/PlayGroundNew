using PlayGround.Contracts.Common;

namespace PlayGround.Client.Models
{
    public static class AccountRoles
    {
        /// <summary>JWT 클레임의 역할 문자열 해석. null·빈 값·미지원 값은 General(역할 미선택).</summary>
        public static AccountRole Parse(string? role)
        {
            return Enum.TryParse(role, ignoreCase: true, out AccountRole parsed) && parsed != AccountRole.Unknown
                ? parsed
                : AccountRole.General;
        }
    }
}
