using PlayGround.Domain.Account;
namespace PlayGround.Contracts.Auth
{
    public class LoginByEmailRequest
    {
        public string Email { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
    }

    public class AuthResult
    {
        public string AccessToken { get; set; } = string.Empty;
        public AuthUserDto User { get; set; } = new();
    }

    public class AuthUserDto
    {
        public Guid UserId { get; set; }
        public string Email { get; set; } = string.Empty;
        public string DisplayName { get; set; } = string.Empty;
        public AccountRole Role { get; set; }
        public string? ProfileImageUrl { get; set; }
    }
}
