using System.Net.Mail;
using System.Security.Claims;
using GoElectrify.BLL.Contracts.Services;
using GoElectrify.BLL.Dto.Auth;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using GoElectrify.BLL.Exceptions;
using Microsoft.AspNetCore.Authentication;

namespace go_electrify_backend.Controllers
{
    [ApiController]
    [Route("api/v1/auth")]
    public class AuthController : ControllerBase
    {
        private readonly IConfiguration _cfg;
        private readonly IAuthService auth;
        public AuthController(IAuthService auth, IConfiguration cfg)
        {
            this.auth = auth;
            _cfg = cfg;
        }

        [HttpPost("request-otp")]
        [AllowAnonymous]
        public async Task<IActionResult> RequestOtp([FromBody] RequestOtpDto dto,
                                            [FromServices] IAuthService auth,
                                            CancellationToken ct)
        {
            // Che khi email không hợp lệ
            if (!IsValidEmail(dto.Email))
                return Ok(new { message = "If the email is valid, an OTP has been sent(valid for 5 minutes)." });

            try
            {
                await auth.RequestOtpAsync(dto.Email, ct);
                return Ok(new { message = "If the email is valid, an OTP has been sent(valid for 5 minutes)." });
            }
            catch (OtpLockedException ex)
            {
                if (ex.RetryAfterSeconds is not null)
                    Response.Headers.Append("Retry-After", ex.RetryAfterSeconds.Value.ToString());
                return StatusCode(423, new { ok = false, error = "Locked. Try again later." });
            }
            catch (OtpRateLimitedException ex)
            {
                Response.Headers.Append("Retry-After", ex.RetryAfterSeconds.ToString());
                return StatusCode(429, new { ok = false, error = "Too many OTP requests. Try again later." });
            }
            catch (Exception)
            {
                // Cho các lỗi khác: 500 (SMTP/Redis, v.v.)
                return StatusCode(500, new { ok = false, error = "Internal server error." });
            }

            static bool IsValidEmail(string? email)
            {
                if (string.IsNullOrWhiteSpace(email)) return false;
                try { _ = new MailAddress(email); return true; }
                catch { return false; }
            }
        }

        [HttpPost("verify-otp")]
        [AllowAnonymous]
        public async Task<IActionResult> VerifyOtp([FromBody] VerifyOtpDto dto, CancellationToken ct)
        {
            try
            {
                var tokens = await auth.VerifyOtpAsync(dto.Email, dto.Otp, ct);
                return Ok(tokens);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }

        [AllowAnonymous]
        [HttpPost("logout")]
        public async Task<IActionResult> Logout([FromBody] LogoutRequest? body, CancellationToken ct)
        {
            // Lấy refresh token: ưu tiên body, fallback cookie
            var rt = body?.RefreshToken;
            if (string.IsNullOrWhiteSpace(rt))
                rt = Request.Cookies["refreshToken"];

            // Revoke trong DB (nếu có token)
            if (!string.IsNullOrWhiteSpace(rt))
            {
                try
                {
                    await auth.RevokeRefreshTokenAsync(rt!, ct);
                }
                catch
                {
                    // không throw: vẫn tiếp tục clear cookie để đảm bảo client logged out
                }
            }

            // Clear cookies trên domain chung để FE/API tách subdomain vẫn xoá được
            var expired = DateTime.UnixEpoch;

            Response.Cookies.Append("refreshToken", ".", new CookieOptions
            {
                HttpOnly = true,
                Secure = true,
                SameSite = SameSiteMode.None,
                Path = "/",
                Domain = ".go-electrify.com",
                Expires = expired
            });

            // Nếu bạn đang set accessToken ở cookie cho FE đọc, xoá luôn cho chắc
            Response.Cookies.Append("accessToken", ".", new CookieOptions
            {
                HttpOnly = false,
                Secure = true,
                SameSite = SameSiteMode.None,
                Path = "/",
                Domain = ".go-electrify.com",
                Expires = expired
            });

            return Ok(new { ok = true });
        }
        public sealed record LogoutRequest(string? RefreshToken);


        [HttpGet("whoami")]
        [Authorize]
        public IActionResult WhoAmI()
        {
            return Ok(new
            {
                nameid = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value,
                email = User.FindFirstValue(System.Security.Claims.ClaimTypes.Email),
                sub = User.FindFirst("sub")?.Value,
                role = User.FindFirst(System.Security.Claims.ClaimTypes.Role)?.Value,
                iss = User.FindFirst("iss")?.Value,
                aud = User.FindFirst("aud")?.Value,
                exp = User.FindFirst("exp")?.Value
            });
        }

        [AllowAnonymous]
        [HttpPost("refreshToken")]
        public async Task<IActionResult> RefreshToken([FromBody] RefreshRequest? body, CancellationToken ct)
        {
            // Ưu tiên body, fallback cookie
            var incoming = body?.RefreshToken;
            if (string.IsNullOrWhiteSpace(incoming))
                incoming = Request.Cookies["refreshToken"];

            if (string.IsNullOrWhiteSpace(incoming))
                return Unauthorized(new { ok = false, error = "missing_refresh_token" });

            try
            {
                // AuthService sẽ ném exception nếu token sai/hết hạn
                var tokens = await auth.RefreshAsync(incoming!, ct);

                // Rotate refreshToken cookie (cross-site: FE và API khác subdomain)
                Response.Cookies.Append(
                    "refreshToken",
                    tokens.RefreshToken,
                    new CookieOptions
                    {
                        HttpOnly = true,
                        Secure = true,
                        SameSite = SameSiteMode.None,
                        Expires = tokens.RefreshExpires,
                        Path = "/",
                        Domain = ".go-electrify.com"
                    });

                var ttlSeconds = (int)Math.Max(
                    1,
                    (tokens.AccessExpires - DateTimeOffset.UtcNow).TotalSeconds
                );

                return Ok(new
                {
                    ok = true,
                    accessToken = tokens.AccessToken,
                    expiresInSeconds = ttlSeconds
                });
            }
            catch (Exception)
            {
                return Unauthorized(new { ok = false, error = "invalid_or_expired_refresh_token" });
            }
        }
        public sealed record RefreshRequest(string? RefreshToken);
        private bool IsAllowedRedirect(string? url)
        {
            if (string.IsNullOrWhiteSpace(url)) return false;
            var allowed = _cfg.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? Array.Empty<string>();
            return allowed.Any(a => url.StartsWith(a, StringComparison.OrdinalIgnoreCase));
        }

        [HttpGet("login/google")]
        [AllowAnonymous]
        public IActionResult LoginWithGoogle([FromQuery] string? returnUrl)
        {
            var cb = Url.Action(nameof(GoogleCallback), "Auth", values: null, protocol: Request.Scheme)!;
            var (scheme, props) = auth.GetGoogleChallenge(cb);

            if (!string.IsNullOrWhiteSpace(returnUrl) && IsAllowedRedirect(returnUrl))
                props.Items["returnUrl"] = returnUrl;

            return Challenge(props, scheme);
        }

        [AllowAnonymous]
        [HttpGet("callback/google")]
        public async Task<IActionResult> GoogleCallback(CancellationToken ct)
        {
            var ext = await HttpContext.AuthenticateAsync("External");
            if (!ext.Succeeded || ext.Principal == null)
            {
                return Unauthorized(new { ok = false, error = "external_auth_failed" });
            }

            try
            {
                var tokens = await auth.SignInWithGoogleAsync(ext.Principal, ct);

                var isHttps = Request.IsHttps;
                Response.Cookies.Append(
                    "refreshToken",
                    tokens.RefreshToken,
                    new CookieOptions
                    {
                        HttpOnly = true,
                        Secure = true,
                        SameSite = SameSiteMode.None,
                        Expires = tokens.RefreshExpires,
                        Path = "/",
                        Domain = ".go-electrify.com"
                    });

                await HttpContext.SignOutAsync("External");

                string? returnUrl = null;
                if (ext.Properties?.Items != null &&
                    ext.Properties.Items.TryGetValue("returnUrl", out var r) &&
                    IsAllowedRedirect(r))
                {
                    returnUrl = r;
                }

                return string.IsNullOrWhiteSpace(returnUrl)
                    ? Redirect("/")
                    : Redirect(returnUrl);
            }
            catch
            {
                await HttpContext.SignOutAsync("External");
                return Unauthorized(new { ok = false, error = "signin_with_google_failed" });
            }
        }
    }
}
