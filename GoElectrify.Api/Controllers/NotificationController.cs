using GoElectrify.BLL.Contracts.Services;
using GoElectrify.BLL.Dto.Notification;
using GoElectrify.BLL.Dtos.Notification;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace GoElectrify.Api.Controllers
{
    [ApiController]
    [Route("api/v1/notifications")]
    [Authorize(Roles = "Admin,Staff,Driver")]
    public class NotificationController : ControllerBase
    {
        private readonly INotificationService _service;
        public NotificationController(INotificationService service) => _service = service;

        [HttpGet]
        public async Task<IActionResult> Get([FromQuery] NotificationQueryDto query, CancellationToken ct)
        {
            var role = User.FindFirst(ClaimTypes.Role)?.Value
                       ?? User.FindFirst("role")?.Value
                       ?? "Driver";

            var userIdStr = User.FindFirst("userId")?.Value
                         ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (!int.TryParse(userIdStr, out var userId))
                return Unauthorized(new { error = "Thiếu hoặc sai userId trong token." });

            var items = await _service.GetDashboardAsync(query, userId, role, ct);
            return Ok(items);
        }

        [HttpPost("read-all")]
        public async Task<IActionResult> MarkAllRead(CancellationToken ct)
        {
            var userIdStr = User.FindFirst("userId")?.Value
                         ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (!int.TryParse(userIdStr, out var userId))
                return Unauthorized(new { error = "Thiếu hoặc sai userId trong token." });

            await _service.MarkAllReadNowAsync(userId, ct);
            return NoContent();
        }

        [HttpPost("{id}/read")]
        public async Task<IActionResult> MarkOneRead([FromRoute] string id, CancellationToken ct)
        {
            if (string.IsNullOrWhiteSpace(id))
                return BadRequest(new { error = "Thiếu id thông báo." });

            var userIdStr = User.FindFirst("userId")?.Value
                         ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (!int.TryParse(userIdStr, out var userId))
                return Unauthorized(new { error = "Thiếu hoặc sai userId trong token." });

            await _service.MarkOneReadAsync(userId, id.Trim(), ct);
            return NoContent();
        }
    }
}
