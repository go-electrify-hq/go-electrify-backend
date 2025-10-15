namespace GoElectrify.BLL.Contracts.Services
{
    public interface ITokenService
    {
        ///// <summary>Cấp access/refresh token cho user.</summary>
        //(string AccessToken, DateTime AccessExpiresAt, string RefreshToken, DateTime RefreshExpiresAt)
        //IssueTokens(User user, string authMethod = "otp", IEnumerable<Claim>? extraClaims = null);

        (string AccessToken, DateTime AccessExpiresAt, string RefreshToken, DateTime RefreshExpiresAt)
        IssueTokens(int userId, string? email, string role, string? fullName, string? avatarUrl, string authMethod = "otp");
        /// <summary>Hash (SHA-256) để lưu refresh token an toàn.</summary>
        string HashToken(string raw);
    }
}
