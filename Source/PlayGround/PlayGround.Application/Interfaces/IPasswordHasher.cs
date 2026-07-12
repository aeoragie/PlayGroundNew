namespace PlayGround.Application.Interfaces
{
    /// <summary>비밀번호 해시/검증 포트 (Server에서 구현 — 표준 PasswordHasher).</summary>
    public interface IPasswordHasher
    {
        string Hash(string password);

        bool Verify(string hash, string password);
    }
}
