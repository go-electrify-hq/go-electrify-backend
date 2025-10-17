using GoElectrify.BLL.Contracts.Repositories;
using GoElectrify.BLL.Contracts.Services;
using GoElectrify.BLL.Dto.Auth;
using GoElectrify.BLL.Entities;
using GoElectrify.BLL.Exceptions;


namespace GoElectrify.BLL.Services
{
    public sealed class AuthService(
    IUserRepository users,
    IRoleRepository roles,
    IWalletRepository wallets,
    IRefreshTokenRepository refreshTokens,
    IRedisCache redis,
    IEmailSender emailSender,
    ITokenService tokenSvc
) : IAuthService
    {
        private const string OtpKeyPrefix = "otp:";
        private const string OtpReqCountPrefix = "otp:req:";     // ≤3 requests / 10m
        private const string OtpVerifyCountPrefix = "otp:verify:";  // ≤5 verifications / 10m
        private const string OtpLockPrefix = "otp:lock:";    // 15m lockout

        private static readonly TimeSpan OtpTtl = TimeSpan.FromMinutes(5);   // BR-OTP-01
        private static readonly TimeSpan RequestWindow = TimeSpan.FromMinutes(10);  // BR-OTP-01
        private static readonly TimeSpan VerifyWindow = TimeSpan.FromMinutes(10);  // BR-OTP-01
        private static readonly TimeSpan LockoutDuration = TimeSpan.FromMinutes(15);  // BR-OTP-01
        private const int RequestLimit = 3;   // BR-OTP-01
        private const int VerifyLimit = 5;   // BR-OTP-01

        private static string NormalizeEmail(string raw) => raw?.Trim().ToLowerInvariant();

        private static bool IsValidEmail(string email)
        {
            if (string.IsNullOrWhiteSpace(email)) return false;
            try { _ = new System.Net.Mail.MailAddress(email); return true; }
            catch { return false; }
        }

        private static string Generate6Digits()
        {
            Span<byte> buf = stackalloc byte[4];
            System.Security.Cryptography.RandomNumberGenerator.Fill(buf);
            var v = BitConverter.ToUInt32(buf);
            return (v % 1_000_000).ToString("D6");
        }

        public async Task RequestOtpAsync(string rawEmail, CancellationToken ct)
        {
            var email = NormalizeEmail(rawEmail);

            // (Không cần tự validate email ở đây nữa; controller đã làm)

            // 1) Lockout?
            var lockKey = $"{OtpLockPrefix}{email}";
            if (await redis.ExistsAsync(lockKey))
                throw new OtpLockedException((int)LockoutDuration.TotalSeconds);

            // 2) Rate-limit gửi: ≤3 / 10 phút
            var reqKey = $"{OtpReqCountPrefix}{email}";
            var reqCount = await redis.IncrementAsync(reqKey);
            if (reqCount == 1) await redis.ExpireAsync(reqKey, RequestWindow);

            if (reqCount > RequestLimit)
            {
                await redis.SetAsync(lockKey, "1", LockoutDuration);
                throw new OtpRateLimitedException((int)LockoutDuration.TotalSeconds);
            }

            // 3) Sinh & lưu OTP
            var otpKey = $"{OtpKeyPrefix}{email}";
            var otp = Generate6Digits();
            await redis.SetAsync(otpKey, otp, OtpTtl);

            // 4) Gửi email (để lỗi ném ra cho controller mapping)
            await emailSender.SendOtpAsync(email, otp, ct);

            // 5) Log thông tin server-side (đừng log mã OTP)
            //log.LogInformation("OTP sent to {Email}", email);
        }

        // Jitter 180–320ms để responses khó phân biệt (đồng phục thời gian)
        private static int RandomJitter(int minMs, int maxMs)
        {
            var rnd = System.Random.Shared.Next(minMs, maxMs + 1);
            return rnd;
        }


        public async Task<TokenResponse> VerifyOtpAsync(string emailAddr, string otp, CancellationToken ct)
        {
            var emailKey = NormalizeEmail(emailAddr);
            if (!IsValidEmail(emailKey))
                throw new InvalidOperationException("Invalid or expired OTP.");

            var otpKey = $"{OtpKeyPrefix}{emailKey}";
            var lockKey = $"{OtpLockPrefix}{emailKey}";
            var vKey = $"{OtpVerifyCountPrefix}{emailKey}";
            var reqKey = $"{OtpReqCountPrefix}{emailKey}";

            // 0) Đang bị lock?
            if (await redis.ExistsAsync(lockKey))
                throw new InvalidOperationException("Invalid or expired OTP.");

            // 1) Lấy OTP từ Redis
            var cached = await redis.GetAsync(otpKey);
            if (string.IsNullOrEmpty(cached))
                throw new InvalidOperationException("Invalid or expired OTP.");

            // 2) Tăng đếm verify trong 10'
            var count = await redis.IncrementAsync(vKey);
            if (count == 1) await redis.ExpireAsync(vKey, VerifyWindow);
            if (count > VerifyLimit)
            {
                await redis.SetAsync(lockKey, "1", LockoutDuration);
                throw new InvalidOperationException("Invalid or expired OTP.");
            }

            // 3) So sánh OTP người dùng nhập với Redis
            if (!string.Equals(cached, otp, StringComparison.Ordinal))
                throw new InvalidOperationException("Invalid or expired OTP.");

            // 4) Upsert user + đảm bảo Wallet + gán role 
            var user = await users.FindByEmailAsync(emailKey, ct);
            if (user is null)
            {
                var driverRole = await roles.GetByNameAsync("Driver", ct);
                user = new User
                {
                    Email = emailKey,
                    RoleId = driverRole.Id,
                    Role = driverRole,
                };

                await users.AddAsync(user, ct);
                await wallets.AddAsync(new Wallet { User = user, Balance = 0m }, ct);
                await users.SaveAsync(ct);
            }
            else if (user.Role is null)
            {
                var driverRole = await roles.GetByNameAsync("Driver", ct);
                user.RoleId = user.RoleId == default ? driverRole.Id : user.RoleId;
                user.Role ??= driverRole;
            }

            var (access, accessExp, refresh, refreshExp) =
                tokenSvc.IssueTokens(
                    userId: user.Id,
                    email: user.Email,
                    role: user.Role?.Name ?? "Driver",
                    fullName: user.FullName,
                    avatarUrl: user.AvatarUrl,
                    authMethod: "otp"
                );

            await refreshTokens.AddAsync(new RefreshToken
            {
                UserId = user.Id,
                TokenHash = tokenSvc.HashToken(refresh),
                ExpiresAt = refreshExp
            }, ct);
            await refreshTokens.SaveAsync(ct);

            // 5) ĐÃ VERIFY THÀNH CÔNG → XÓA OTP & COUNTERS
            await redis.DeleteAsync(otpKey); // <-- xóa đúng key OTP
            await redis.DeleteAsync(vKey);   // reset đếm verify
            await redis.DeleteAsync(reqKey);
            await redis.DeleteAsync(lockKey);

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


        public async Task<TokenResponse> RefreshAsync(string refreshToken, CancellationToken ct)
        {
            if (string.IsNullOrWhiteSpace(refreshToken))
                throw new InvalidOperationException("Refresh token is required.");

            var hash = tokenSvc.HashToken(refreshToken);
            var existing = await refreshTokens.FindActiveByHashAsync(hash, ct);
            if (existing is null)
                throw new UnauthorizedAccessException("Invalid or expired refresh token.");

            var user = await users.GetByIdWithRoleAsync(existing.UserId, ct);
            if (user is null)
                throw new UnauthorizedAccessException("User not found.");

            var roleName = user.Role?.Name ?? "Driver";

            var (access, accessExp, newRefresh, newRefreshExp) =
                tokenSvc.IssueTokens(
                    user.Id,
                    user.Email,
                    roleName,
                    user.FullName ?? string.Empty,
                    user.AvatarUrl ?? string.Empty,
                    authMethod: "refresh"
                    );

            // Rotate refresh token
            existing.RevokedAt = DateTime.UtcNow;
            await refreshTokens.AddAsync(new RefreshToken
            {
                UserId = user.Id,
                TokenHash = tokenSvc.HashToken(newRefresh),
                ExpiresAt = newRefreshExp
            }, ct);
            await refreshTokens.SaveAsync(ct);

            return new TokenResponse(access, accessExp, newRefresh, newRefreshExp);
        }

    }
}
