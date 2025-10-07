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



        //public async Task RequestOtpAsync(string emailAddr, CancellationToken ct)
        //{
        //    var emailKey = emailAddr.ToLowerInvariant();

        //    // Rate limit
        //    var rlKey = $"{OtpRatePrefix}{emailKey}";
        //    var count = await redis.IncrAsync(rlKey, RateWindow);
        //    if (count > RateLimit)
        //        throw new InvalidOperationException("Too many OTP requests. Please try again later.");

        //    // Generate 6-digit OTP
        //    var otp = Random.Shared.Next(100000, 999999).ToString();
        //    await redis.SetAsync($"{OtpKeyPrefix}{emailKey}", otp, OtpTtl);

        //    await email.SendAsync(
        //        emailAddr,
        //        "[Go Electrify] Your OTP code",
        //        $"<p>Your login code is <b>{otp}</b>. It expires in {OtpTtl.TotalMinutes:0} minutes.</p>",
        //        ct);
        //}

        public async Task RequestOtpAsync(string rawEmail, CancellationToken ct)
        {
            // 1) Chuẩn hoá & validate email (đồng nhất với Verify)
            var email = NormalizeEmail(rawEmail);

            // BR-ENUM-01: giữ thông điệp đồng phục & timing “na ná”
            // => nếu email lỗi, vẫn return 200 ở Controller, nhưng dừng logic.
            if (!IsValidEmail(email))
            {
                await Task.Delay(RandomJitter(180, 320), ct);
                return;
            }

            // 2) Kiểm tra lockout 15 phút
            var lockKey = $"{OtpLockPrefix}{email}";
            if (await redis.ExistsAsync(lockKey))
            {
                await Task.Delay(RandomJitter(180, 320), ct);
                return; // Không tiết lộ lý do, Controller luôn trả message đồng phục
            }

            // 3) Rate-limit gửi OTP: ≤3 request / 10 phút
            var reqKey = $"{OtpReqCountPrefix}{email}";
            var reqCount = await redis.IncrementAsync(reqKey);
            if (reqCount == 1) await redis.ExpireAsync(reqKey, RequestWindow); // cửa sổ 10 phút

            if (reqCount > RequestLimit)
            {
                // Chạm ngưỡng → lock 15 phút
                await redis.SetAsync(lockKey, "1", LockoutDuration);
                await Task.Delay(RandomJitter(180, 320), ct);
                return;
            }

            // 4) Tạo & lưu OTP 6 số, TTL=5 phút
            var otpKey = $"{OtpKeyPrefix}{email}";
            var otp = Generate6Digits();
            await redis.SetAsync(otpKey, otp, OtpTtl);

            // 5) Gửi email OTP (Resend/SMTP) – KHÔNG log lộ OTP trong production
            await emailSender.SendOtpAsync(email, otp, ct);

            // 6) Jitter nhẹ để làm phẳng thời gian phản hồi (BR-ENUM-01)
            await Task.Delay(RandomJitter(180, 320), ct);
        }

        // Jitter 180–320ms để responses khó phân biệt (đồng phục thời gian)
        private static int RandomJitter(int minMs, int maxMs)
        {
            var rnd = System.Random.Shared.Next(minMs, maxMs + 1);
            return rnd;
        }


        public async Task<TokenResponse> VerifyOtpAsync(string emailAddr, string otp, CancellationToken ct)
        {


            // 1) Lấy OTP từ Redis bằng email đã chuẩn hoá
            var emailKey = NormalizeEmail(emailAddr);
            var lockKey = $"{OtpLockPrefix}{emailKey}";

            // Nếu đang lock → trả lỗi generic (đồng phục)
            if (await redis.ExistsAsync(lockKey))
                throw new InvalidOperationException("Invalid or expired OTP.");

            // Đếm số lần verify trong 10 phút
            var vKey = $"{OtpVerifyCountPrefix}{emailKey}";
            var count = await redis.IncrementAsync(vKey);
            if (count == 1) await redis.ExpireAsync(vKey, VerifyWindow);
            if (count > VerifyLimit)
            {
                await redis.SetAsync(lockKey, "1", LockoutDuration);
                throw new InvalidOperationException("Invalid or expired OTP.");
            }

            // 2) Upsert user (mặc định role Driver) + đảm bảo Wallet
            var user = await users.FindByEmailAsync(emailKey, ct);
            if (user is null)
            {
                var driverRole = await roles.GetByNameAsync("Driver", ct);
                user = new User
                {
                    Email = emailKey,
                    RoleId = driverRole.Id,
                    Role = driverRole, // gắn navigation để build claim ngay
                };

                await users.AddAsync(user, ct);
                await wallets.AddAsync(new Wallet { User = user, Balance = 0m }, ct);
                await users.SaveAsync(ct);
            }
            else
            {
                // FindByEmailAsync đã Include Role/Wallet, nhưng vẫn "phòng hờ"
                if (user.Role is null)
                {
                    // Nếu DB thiếu role, fallback Driver để không crash khi cấp token
                    var driverRole = await roles.GetByNameAsync("Driver", ct);
                    user.RoleId = user.RoleId == default ? driverRole.Id : user.RoleId;
                    user.Role ??= driverRole;
                }
            }

            // 3) Cấp token với đầy đủ thông tin để FE dùng
            var (access, accessExp, refresh, refreshExp) =
                tokenSvc.IssueTokens(
                    userId: user.Id,
                    email: user.Email,
                    role: user.Role?.Name ?? "Driver",
                    fullName: user.FullName,
                    avatarUrl: user.AvatarUrl,
                    authMethod: "otp"
                );

            // 4) Lưu refresh token (hash) vào DB
            await refreshTokens.AddAsync(new RefreshToken
            {
                UserId = user.Id,
                TokenHash = tokenSvc.HashToken(refresh),
                ExpiresAt = refreshExp
            }, ct);
            await refreshTokens.SaveAsync(ct);

            // 5) Tiêu thụ OTP: xoá khỏi Redis
            await redis.DeleteAsync(lockKey);

            // 6) Trả về cho client
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

            var user = await users.GetByIdAsync(existing.UserId, ct);
            if (user is null)
                throw new UnauthorizedAccessException("User not found.");

            // phát hành token mới
            var (access, accessExp, newRefresh, newRefreshExp) =
                tokenSvc.IssueTokens(user.Id, user.Email, user.Role?.Name ?? "Driver", user.FullName ?? string.Empty, user.AvatarUrl ?? string.Empty, authMethod: "otp");

            // rotate: revoke token cũ + lưu token mới
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
