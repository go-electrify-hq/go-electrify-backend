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
    [Authorize(Roles = "Admin,Staff")]
    public class NotificationController : ControllerBase
    {
        private readonly INotificationService _service;
        public NotificationController(INotificationService service) { _service = service; }

        // GET: /api/v1/notifications
        [HttpGet]
        public async Task<IActionResult> Get([FromQuery] NotificationQueryDto query, CancellationToken cancellationToken)
        {
            // Lấy userId từ JWT: ưu tiên "userId", fallback "uid"
            string? idStr = User.FindFirst("userId")?.Value;
            if (string.IsNullOrWhiteSpace(idStr))
                idStr = User.FindFirst("uid")?.Value;

            int userId;
            bool ok = int.TryParse(idStr, out userId);
            if (!ok)
                return Unauthorized(new { error = "Thiếu hoặc sai mã người dùng (userId) trong token." });

            string role = User.FindFirst(ClaimTypes.Role)?.Value ?? "User";

            var data = await _service.GetDashboardAsync(query, userId, role, cancellationToken);
            return Ok(data);
        }

        // POST: /api/v1/notifications/mark-all-read
        [HttpPost("mark-all-read")]
        public async Task<IActionResult> MarkAllRead(CancellationToken cancellationToken)
        {
            string? idStr = User.FindFirst("userId")?.Value;
            if (string.IsNullOrWhiteSpace(idStr))
                idStr = User.FindFirst("uid")?.Value;

            int userId;
            bool ok = int.TryParse(idStr, out userId);
            if (!ok)
                return Unauthorized(new { error = "Thiếu hoặc sai mã người dùng (userId) trong token." });

            DateTime lastSeen = await _service.MarkAllReadNowAsync(userId, cancellationToken);
            return Ok(new { message = "Đã đánh dấu tất cả thông báo là đã đọc.", lastSeen });
        }
    }
}
