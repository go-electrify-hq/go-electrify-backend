using GoElectrify.BLL.Contracts.Repositories;
using GoElectrify.BLL.Contracts.Services;
using GoElectrify.BLL.Dto.Auth;
using GoElectrify.BLL.Entities;

namespace GoElectrify.BLL.Services
{
    public sealed class AuthService(
    IUserRepository users,
    IRoleRepository roles,
    IWalletRepository wallets,
    IRefreshTokenRepository refreshTokens,
    IRedisCache redis,
    IEmailSender email,
    ITokenService tokenSvc
) : IAuthService
    {
        private const string OtpKeyPrefix = "otp:";
        private const string OtpRatePrefix = "otp:rl:";
        private static readonly TimeSpan OtpTtl = TimeSpan.FromMinutes(5);
        private static readonly TimeSpan RateWindow = TimeSpan.FromMinutes(15);
        private const int RateLimit = 5;

        public async Task RequestOtpAsync(string emailAddr, CancellationToken ct)
        {
            var emailKey = emailAddr.ToLowerInvariant();

            // Rate limit
            var rlKey = $"{OtpRatePrefix}{emailKey}";
            var count = await redis.IncrAsync(rlKey, RateWindow);
            if (count > RateLimit)
                throw new InvalidOperationException("Too many OTP requests. Please try again later.");

            // Generate 6-digit OTP
            var otp = Random.Shared.Next(100000, 999999).ToString();
            await redis.SetAsync($"{OtpKeyPrefix}{emailKey}", otp, OtpTtl);

            await email.SendAsync(
                emailAddr,
                "[Go Electrify] Your OTP code",
                $"<p>Your login code is <b>{otp}</b>. It expires in {OtpTtl.TotalMinutes:0} minutes.</p>",
                ct);
        }

        public async Task<TokenResponse> VerifyOtpAsync(string emailAddr, string otp, CancellationToken ct)
        {
            var emailKey = emailAddr.ToLowerInvariant();
            var key = $"{OtpKeyPrefix}{emailKey}";
            var cached = await redis.GetAsync(key);

            if (cached is null || cached != otp)
                throw new InvalidOperationException("Invalid or expired OTP.");

            // Upsert user
            var user = await users.FindByEmailAsync(emailAddr, ct);
            if (user is null)
            {
                var role = await roles.GetByNameAsync("Driver", ct);
                user = new User { Email = emailAddr, RoleId = role.Id };
                await users.AddAsync(user, ct);
                await wallets.AddAsync(new Wallet { User = user, Balance = 0m }, ct);
                await users.SaveAsync(ct);
            }

            var (access, accessExp, refresh, refreshExp) =
                tokenSvc.IssueTokens(user.Id, user.Email, user.Role?.Name ?? "Driver");

            await refreshTokens.AddAsync(new RefreshToken
            {
                UserId = user.Id,
                TokenHash = tokenSvc.HashToken(refresh),
                ExpiresAt = refreshExp
            }, ct);
            await refreshTokens.SaveAsync(ct);

            // Consume OTP
            await redis.DeleteAsync(key);

            return new TokenResponse(access, accessExp, refresh, refreshExp);
        }

        public async Task LogoutAsync(int userId, string refreshToken, CancellationToken ct)
        {
            var hash = tokenSvc.HashToken(refreshToken);
            var rt = await refreshTokens.FindActiveAsync(userId, hash, ct);
            if (rt is null) return;

            rt.RevokedAt = DateTime.UtcNow;
            await refreshTokens.SaveAsync(ct);
        }

        //public async Task<bool> LogoutAsync(int userId, string refreshToken, CancellationToken ct)
        //{
        //    if (string.IsNullOrWhiteSpace(refreshToken)) return false;

        //    var hash = tokenSvc.HashToken(refreshToken);
        //    var rt = await refreshTokens.FindActiveAsync(userId, hash, ct);
        //    if (rt is null) return false;

        //    rt.RevokedAt = DateTime.UtcNow;
        //    await refreshTokens.SaveAsync(ct);
        //    return true;
        //}
    }
}
