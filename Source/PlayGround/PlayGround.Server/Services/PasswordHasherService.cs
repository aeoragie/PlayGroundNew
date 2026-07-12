using Microsoft.AspNetCore.Identity;
using PlayGround.Application.Interfaces;

namespace PlayGround.Server.Services
{
    /// <summary>표준 PasswordHasher(PBKDF2) 기반 비밀번호 해시/검증.</summary>
    public sealed class PasswordHasherService : IPasswordHasher
    {
        private static readonly PasswordHasher<object> Hasher = new();
        private static readonly object Subject = new();

        public string Hash(string password)
        {
            return Hasher.HashPassword(Subject, password);
        }

        public bool Verify(string hash, string password)
        {
            PasswordVerificationResult result = Hasher.VerifyHashedPassword(Subject, hash, password);
            return result is PasswordVerificationResult.Success or PasswordVerificationResult.SuccessRehashNeeded;
        }
    }
}
