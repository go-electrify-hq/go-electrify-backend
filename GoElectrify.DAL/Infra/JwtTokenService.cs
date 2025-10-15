using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using GoElectrify.BLL.Contracts.Services;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace GoElectrify.DAL.Infra
{
    public sealed class TokenService : ITokenService
    {
        private readonly JwtOptions _opt;
        private readonly JwtSecurityTokenHandler _handler = new();
        private readonly byte[] _key;

        public TokenService(IOptions<JwtOptions> opt)
        {
            _opt = opt.Value;
            _key = System.Text.Encoding.UTF8.GetBytes(_opt.Secret);
        }

        public (string AccessToken, DateTime AccessExpiresAt, string RefreshToken, DateTime RefreshExpiresAt)
            IssueTokens(int userId, string? email, string role, string? fullName, string? avatarUrl, string authMethod = "otp")
        {
            var now = DateTime.UtcNow;
            var accessExp = now.AddMinutes(_opt.AccessMinutes);
            var refreshExp = now.AddDays(_opt.RefreshDays);

            var claims = new List<Claim>
            {
                new(ClaimTypes.NameIdentifier, userId.ToString()),
                new(JwtRegisteredClaimNames.Email, email ?? string.Empty),
                new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString("N")),
                new(JwtRegisteredClaimNames.Iat, new DateTimeOffset(now).ToUnixTimeSeconds().ToString(), ClaimValueTypes.Integer64),

                // Role: thêm chuẩn + 'role' cho FE
                new(ClaimTypes.Role, role),
                new("role", role),

                // Audit: phương thức xác thực (otp / oauth-google / password)
                new("amr", authMethod ?? "otp"),

                // Tiện FE parse nhanh
                new("uid", userId.ToString())
            };

            AddIfNotEmpty(claims, "name", fullName);
            AddIfNotEmpty(claims, "avatar", avatarUrl);

            var creds = new SigningCredentials(new SymmetricSecurityKey(_key), SecurityAlgorithms.HmacSha256);
            var jwt = new JwtSecurityToken(
                issuer: _opt.Issuer,
                audience: _opt.Audience,
                claims: claims,
                notBefore: now,
                expires: accessExp,
                signingCredentials: creds
            );

            var access = _handler.WriteToken(jwt);
            var refresh = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));

            return (access, accessExp, refresh, refreshExp);
        }

        public string HashToken(string token)
        {
            using var sha256 = SHA256.Create();
            var bytes = sha256.ComputeHash(System.Text.Encoding.UTF8.GetBytes(token));
            return Convert.ToHexString(bytes);
        }

        private static void AddIfNotEmpty(List<Claim> claims, string type, string? value)
        {
            if (!string.IsNullOrWhiteSpace(value))
                claims.Add(new Claim(type, value));
        }
    }
}
