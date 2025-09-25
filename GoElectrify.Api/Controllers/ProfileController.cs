using GoElectrify.BLL.Auth;
using GoElectrify.BLL.Entities;
using GoElectrify.BLL.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using GoElectrify.BLL.Contracts.Services;

namespace go_electrify_backend.Controllers
{
    [ApiController]
    [Authorize]
    [Route("api/v1/profile")]
    public class ProfileController(IProfileService profile) : ControllerBase
    {
        [HttpGet]
        public async Task<IActionResult> Me(CancellationToken ct)
        {
            var uid = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var data = await profile.GetMeAsync(uid, ct);
            return Ok(data);
        }

        [HttpPut]
        public async Task<IActionResult> Update([FromBody] UpdateProfileDto dto, CancellationToken ct)
        {
            var uid = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            await profile.UpdateFullNameAsync(uid, dto.FullName, ct);
            return NoContent();
        }
    }
}
