using System.Security.Claims;
using GoElectrify.BLL.Contracts.Services;
using GoElectrify.BLL.Dto.Auth;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace go_electrify_backend.Controllers
{
    [ApiController]
    [Route("api/v1/auth")]
    public class AuthController(IAuthService auth) : ControllerBase
    {
        [HttpPost("request-otp")]
        [AllowAnonymous]
        public async Task<IActionResult> RequestOtp([FromBody] RequestOtpDto dto, CancellationToken ct)
        {
            await auth.RequestOtpAsync(dto.Email, ct);
            return Ok(new { message = "If the email is valid, an OTP has been sent(valid for 5 minutes)." });
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
