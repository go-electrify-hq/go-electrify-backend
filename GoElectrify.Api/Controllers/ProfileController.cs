using GoElectrify.Api.Auth;
using GoElectrify.BLL.Contracts.Services;
using GoElectrify.BLL.Dtos.Profile;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace go_electrify_backend.Controllers
{
    [ApiController]
    [Authorize]
    [Route("api/v1/profile")]
    public class ProfileController(IProfileService profile) : ControllerBase
    {
        [HttpGet]
        [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
        public async Task<IActionResult> Me(CancellationToken ct)
        {
            var uid = User.GetUserId();
            var data = await profile.GetMeAsync(uid, ct);
            return Ok(data);
        }

        [HttpPut]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        public async Task<IActionResult> Update([FromBody] UpdateProfileDto dto, CancellationToken ct)
        {
            var uid = User.GetUserId();
            await profile.UpdateProfileAsync(uid, dto.FullName, dto.AvatarUrl, ct);
            return NoContent();
        }

        /// <summary>Cập nhật full name cho user hiện tại.</summary>
        [HttpPut("name")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        public async Task<IActionResult> UpdateName([FromBody] UpdateFullNameRequest req, CancellationToken ct)
        {
            await profile.UpdateFullNameAsync(User.GetUserId(), req?.FullName, ct);
            return NoContent();
        }

        /// <summary>Cập nhật avatar URL cho user hiện tại.</summary>
        [HttpPut("avatar")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        public async Task<IActionResult> UpdateAvatar([FromBody] UpdateAvatarRequest req, CancellationToken ct)
        {
            await profile.UpdateAvatarAsync(User.GetUserId(), req?.AvatarUrl, ct);
            return NoContent();
        }
    }
}
