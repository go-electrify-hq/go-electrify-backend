using System.Security.Claims;
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
            var uid = UserId();
            var data = await profile.GetMeAsync(uid, ct);
            return Ok(data);
        }

        [HttpPut]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        public async Task<IActionResult> Update([FromBody] UpdateProfileDto dto, CancellationToken ct)
        {
            var uid = UserId();
            await profile.UpdateProfileAsync(uid, dto.FullName, dto.AvatarUrl, ct);
            return NoContent();
        }

        private int UserId()
        {
            var uid =
                User.FindFirstValue(ClaimTypes.NameIdentifier) ??
                User.FindFirstValue("uid") ??
                User.FindFirstValue("sub") ??
                User.FindFirstValue("userid");

            if (string.IsNullOrWhiteSpace(uid) || !int.TryParse(uid, out var id))
                throw new InvalidOperationException("Missing or invalid user id in token.");

            return id;
        }

        /// <summary>Cập nhật full name cho user hiện tại.</summary>
        [HttpPut("name")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        public async Task<IActionResult> UpdateName([FromBody] UpdateFullNameRequest req, CancellationToken ct)
        {
            await profile.UpdateFullNameAsync(UserId(), req?.FullName, ct);
            return NoContent();
        }

        /// <summary>Cập nhật avatar URL cho user hiện tại.</summary>
        [HttpPut("avatar")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        public async Task<IActionResult> UpdateAvatar([FromBody] UpdateAvatarRequest req, CancellationToken ct)
        {
            await profile.UpdateAvatarAsync(UserId(), req?.AvatarUrl, ct);
            return NoContent();
        }
    }
}
