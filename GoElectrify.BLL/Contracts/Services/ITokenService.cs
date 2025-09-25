namespace GoElectrify.BLL.Contracts.Services
{
    public interface ITokenService
    {
        /// <summary>Cấp access/refresh token cho user.</summary>
        (string accessToken, DateTime accessExpires, string refreshToken, DateTime refreshExpires)
            IssueTokens(int userId, string email, string? role);

        /// <summary>Hash (SHA-256) để lưu refresh token an toàn.</summary>
        string HashToken(string raw);
    }
}
