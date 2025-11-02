using GoElectrify.Api.Auth;
using GoElectrify.BLL.Contracts.Services;
using GoElectrify.BLL.Dto.Notification;
using GoElectrify.BLL.Dtos.Notification;
using GoElectrify.BLL.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace GoElectrify.Api.Controllers
{
    [ApiController]
    [Route("api/v1/notifications")]
    [Authorize]
    public class NotificationController : ControllerBase
    {
        private readonly INotificationService _svc;
        public NotificationController(INotificationService svc) => _svc = svc;

        // Lấy role an toàn từ claims, chuẩn hóa về Admin/Staff/Driver
        private string GetRole()
        {
            var raw = User.FindFirst(ClaimTypes.Role)?.Value
                   ?? User.FindFirst("role")?.Value
                   ?? (User.IsInRole("Admin") ? "Admin"
                        : User.IsInRole("Staff") ? "Staff" : "Driver");

            return raw.Equals("Admin", StringComparison.OrdinalIgnoreCase) ? "Admin"
                 : raw.Equals("Staff", StringComparison.OrdinalIgnoreCase) ? "Staff"
                 : "Driver";
        }

        [HttpGet("dashboard")]
        public async Task<IActionResult> Dashboard(CancellationToken ct)
        {
            var userId = User.GetUserId();
            var role = GetRole();

            var list = await _svc.GetDashboardAsync(userId, role, ct);
            var newCount = list.Count(x => x.IsNew);
            var unreadCount = list.Count(x => x.IsUnread);

            return Ok(new { items = list, newCount, unreadCount });
        }

        [HttpPost("seen")]
        [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
        public async Task<IActionResult> Seen(CancellationToken ct)
        {
            await _svc.MarkSeenAsync(User.GetUserId(), ct);
            return Ok(new { ok = true });
        }

        [HttpPost("read-all")]
        [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
        public async Task<IActionResult> ReadAll(CancellationToken ct)
        {
            var userId = User.GetUserId();
            var role = GetRole();
            await _svc.MarkAllReadAsync(userId, role, ct);
            return Ok(new { ok = true });
        }

        [HttpPost("{id}/read")]
        public async Task<IActionResult> ReadOne([FromRoute] string id, CancellationToken ct)
        {
            if (string.IsNullOrWhiteSpace(id))
                return BadRequest(new { ok = false, message = "Thiếu ID." });

            var role = GetRole();
            var ok = await _svc.MarkOneReadAsync(User.GetUserId(), id, role, ct);
            if (!ok) return NotFound(new { ok = false, message = "Notification ID không hợp lệ hoặc ngoài phạm vi của bạn." });

            return Ok(new { ok = true });
        }
    }
}
