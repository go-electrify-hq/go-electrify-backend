using GoElectrify.BLL.Contracts.Services;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

namespace GoElectrify.DAL.Infra
{
    public class JwtTokenService(IOptions<JwtOptions> opt) : ITokenService
    {
        private readonly JwtOptions _o = opt.Value;

        public (string accessToken, DateTime accessExpires, string refreshToken, DateTime refreshExpires)
            IssueTokens(int userId, string email, string? role)
        {
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_o.Secret));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var now = DateTime.UtcNow;
            var exp = now.AddMinutes(_o.AccessMinutes);

            var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, userId.ToString()),
            new(JwtRegisteredClaimNames.Email, email),
            new(ClaimTypes.NameIdentifier, userId.ToString())
        };
            if (!string.IsNullOrWhiteSpace(role))
                claims.Add(new(ClaimTypes.Role, role!));

            var token = new JwtSecurityToken(_o.Issuer, _o.Audience, claims, now, exp, creds);
            var access = new JwtSecurityTokenHandler().WriteToken(token);

            Span<byte> buf = stackalloc byte[64];
            RandomNumberGenerator.Fill(buf);
            var refresh = Convert.ToBase64String(buf);
            var refreshExp = now.AddDays(_o.RefreshDays);

            return (access, exp, refresh, refreshExp);
        }

        public string HashToken(string raw)
        {
            using var sha = SHA256.Create();
            return Convert.ToHexString(sha.ComputeHash(Encoding.UTF8.GetBytes(raw)));
        }
    }
}
