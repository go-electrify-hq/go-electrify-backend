using System.Security.Claims;
using GoElectrify.BLL.Contracts.Services;
using GoElectrify.BLL.Dto.Users;
using GoElectrify.BLL.Dtos.Users;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GoElectrify.Api.Controllers
{
    [ApiController]
    [Route("api/v1/users")]
    [Authorize(Roles = "Admin,Staff")]
    public class UserController : ControllerBase
    {
        private readonly IUserService _svc;

        public UserController(IUserService svc)
        {
            _svc = svc;
        }

        [HttpGet]
        [ProducesResponseType(typeof(UserListPageDto), 200)]
        public async Task<IActionResult> List([FromQuery] UserListQueryDto query, CancellationToken ct)
        {
            // Nếu là Staff -> chỉ xem Driver
            if (User.IsInRole("Staff"))
            {
                query.Role = "Driver";
            }

            var data = await _svc.ListAsync(query, ct);
            return Ok(data);
        }

        [HttpPut("{id:int}/role")]
        [Authorize(Roles = "Admin")]
        [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(object), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(object), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(object), StatusCodes.Status409Conflict)]
        public async Task<IActionResult> UpdateRole(
            int id,
            [FromBody] UpdateUserRoleRequest req,
            CancellationToken ct)
        {
            if (req is null || string.IsNullOrWhiteSpace(req.Role))
                return BadRequest(new { ok = false, error = "invalid_role" });

            var uid =
                User.FindFirstValue(ClaimTypes.NameIdentifier) ??
                User.FindFirstValue("uid") ??
                User.FindFirstValue("sub") ??
                User.FindFirstValue("userid");

            if (string.IsNullOrWhiteSpace(uid) || !int.TryParse(uid, out var actingUserId))
                return Unauthorized(new { ok = false, error = "unauthorized" });

            try
            {
                var result = await _svc.UpdateRoleAsync(actingUserId, id, req.Role, req.ForceSignOut, ct);
                return Ok(new { ok = true, data = result });
            }
            catch (InvalidOperationException ex) when (ex.Message == "user_not_found")
            {
                return NotFound(new { ok = false, error = "user_not_found" });
            }
            catch (InvalidOperationException ex) when (ex.Message == "role_not_found" || ex.Message == "invalid_role")
            {
                return BadRequest(new { ok = false, error = "invalid_role" });
            }
            catch (InvalidOperationException ex) when (ex.Message == "cannot_change_own_role")
            {
                return BadRequest(new { ok = false, error = "cannot_change_own_role" });
            }
            catch (InvalidOperationException ex) when (ex.Message == "cannot_remove_last_admin")
            {
                return Conflict(new { ok = false, error = "cannot_remove_last_admin" });
            }
        }
    }
}
