using GoElectrify.BLL.Contracts.Services;
using Microsoft.AspNetCore.Mvc;

namespace GoElectrify.Api.Controllers
{
    [ApiController]
    [Route("api/v1/notifications")]
    public class NotificationController : ControllerBase
    {
        private readonly INotificationService _service;
        public NotificationController(INotificationService service) => _service = service;

        [HttpGet]
        public async Task<IActionResult> GetDashboardNotifications()
        {
            var list = await _service.GetDashboardNotificationsAsync();
            return Ok(list);
        }
    }
}
