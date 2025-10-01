using GoElectrify.BLL.Contracts.Services;
using GoElectrify.BLL.Dto.Subscription;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GoElectrify.Api.Controllers
{
    [ApiController]
    [Route("api/v1/subscriptions")]
    public sealed class SubscriptionsController : ControllerBase
    {
        private readonly ISubscriptionService _svc;
        public SubscriptionsController(ISubscriptionService svc) => _svc = svc;

        // READ: ai cũng xem (tuỳ bạn có thể RequireAuthorization)
        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> GetAll(CancellationToken ct) =>
            Ok(await _svc.GetAllAsync(ct));

        [HttpGet("{id:int}")]
        [AllowAnonymous]
        public async Task<IActionResult> Get(int id, CancellationToken ct)
        {
            var s = await _svc.GetByIdAsync(id, ct);
            return s is null ? NotFound() : Ok(s);
        }

        // CREATE: Admin only
        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Create([FromBody] SubscriptionCreateDto dto, CancellationToken ct)
        {
            var s = await _svc.CreateAsync(dto, ct);
            return CreatedAtAction(nameof(Get), new { id = s.Id }, s);
        }

        // UPDATE: Admin only
        [HttpPut("{id:int}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Update(int id, [FromBody] SubscriptionUpdateDto dto, CancellationToken ct)
        {
            var s = await _svc.UpdateAsync(id, dto, ct);
            return s is null ? NotFound() : Ok(s);
        }

        // DELETE: Admin only
        [HttpDelete("{id:int}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Delete(int id, CancellationToken ct)
        {
            var ok = await _svc.DeleteAsync(id, ct);
            return ok ? NoContent() : NotFound();
        }
    }
}
