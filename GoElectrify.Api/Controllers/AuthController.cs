using GoElectrify.BLL.Auth;
using GoElectrify.BLL.Contracts.Services;
using GoElectrify.BLL.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

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
            return Ok(new { message = "OTP sent if email exists." });
        }

        [HttpPost("verify-otp")]
        [AllowAnonymous]
        public async Task<IActionResult> VerifyOtp([FromBody] VerifyOtpDto dto, CancellationToken ct)
        {
            var tokens = await auth.VerifyOtpAsync(dto.Email, dto.Otp, ct);
            return Ok(tokens);
        }

        [HttpPost("logout")]
        [Authorize]
        public async Task<IActionResult> Logout([FromBody] string refreshToken, CancellationToken ct)
        {
            var uid = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            await auth.LogoutAsync(uid, refreshToken, ct);
            return Ok();
        }
    }
}
