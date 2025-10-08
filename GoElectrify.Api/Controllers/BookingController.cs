using GoElectrify.Api.Auth;
using GoElectrify.BLL.Contracts.Services;
using GoElectrify.BLL.Dto.Booking;
using Microsoft.AspNetCore.Mvc;

namespace GoElectrify.Api.Controllers
{
    [ApiController]
    [Route("api/v1/bookings")]
    public sealed class BookingController : ControllerBase
    {
        private readonly IBookingService _svc;
        private readonly IAuthService _auth; // nếu bạn đã có cách lấy userId khác thì thay

        public BookingController(IBookingService svc, IAuthService auth)
        {
            _svc = svc;
            _auth = auth;
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateBookingDto dto, CancellationToken ct)
        {
            var userId = User.GetUserId();
            var data = await _svc.CreateAsync(userId, dto, ct);
            return CreatedAtAction(nameof(GetById), new { id = data.Id }, new { ok = true, data });
        }

        [HttpGet("my")]
        public async Task<IActionResult> My([FromQuery] MyBookingQueryDto q, CancellationToken ct)
        {
            var userId = User.GetUserId();
            var data = await _svc.GetMyAsync(userId, q, ct);
            return Ok(new { ok = true, data });
        }

        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetById([FromRoute] int id, CancellationToken ct)
        {
            var userId = User.GetUserId();
            var data = await _svc.GetAsync(userId, id, ct);
            if (data is null) return NotFound();
            return Ok(new { ok = true, data });
        }

        [HttpPost("{id:int}/cancel")]
        public async Task<IActionResult> Cancel([FromRoute] int id, [FromBody] CancelBookingDto body, CancellationToken ct)
        {
            var userId = User.GetUserId();
            var ok = await _svc.CancelAsync(userId, id, body?.Reason, ct);
            if (!ok) return NotFound();
            return Ok(new { ok = true });
        }
    }
}
