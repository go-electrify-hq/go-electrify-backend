using System.Net.Mail;
using System.Security.Claims;
using GoElectrify.BLL.Contracts.Services;
using GoElectrify.BLL.Dto.Auth;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using GoElectrify.BLL.Exceptions;

namespace go_electrify_backend.Controllers
{
    [ApiController]
    [Route("api/v1/auth")]
    public class AuthController(IAuthService auth) : ControllerBase
    {
        [HttpPost("request-otp")]
        [AllowAnonymous]
        public async Task<IActionResult> RequestOtp([FromBody] RequestOtpDto dto,
                                            [FromServices] IAuthService auth,
                                            CancellationToken ct)
        {
            // Chỉ che khi email không hợp lệ
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

        [HttpPost("logout")]
        [Authorize]
        public async Task<IActionResult> Logout([FromBody] string refreshToken, CancellationToken ct)
        {
            var uid = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            await auth.LogoutAsync(uid, refreshToken, ct); return Ok();
        }


        [HttpGet("whoami")]
        [Authorize]
        public IActionResult WhoAmI()
        {
            return Ok(new
            {
                nameid = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value,
                sub = User.FindFirst("sub")?.Value,
                role = User.FindFirst(System.Security.Claims.ClaimTypes.Role)?.Value,
                iss = User.FindFirst("iss")?.Value,
                aud = User.FindFirst("aud")?.Value,
                exp = User.FindFirst("exp")?.Value
            });
        }

        [HttpPost("refreshToken")]
        [AllowAnonymous]
        public async Task<IActionResult> Refresh([FromBody] RefreshRequest req, CancellationToken ct)
        {
            try
            {
                var tokens = await auth.RefreshAsync(req.RefreshToken, ct);
                return Ok(tokens);
            }
            catch (UnauthorizedAccessException ex)
            {
                return Unauthorized(new { error = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }
    }
}
