using GoElectrify.Api.Auth;
using GoElectrify.BLL.Contracts.Repositories;
using GoElectrify.BLL.Contracts.Services;
using GoElectrify.BLL.Dtos.Booking;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GoElectrify.Api.Controllers
{
    [ApiController]
    [Route("api/v1/admin/booking-fee")]
    public class BookingFeeController : ControllerBase
    {
        private readonly ISystemSettingRepository _repo;
        private readonly IBookingFeeService _fee;

        public BookingFeeController(ISystemSettingRepository repo, IBookingFeeService fee)
        {
            _repo = repo;
            _fee = fee;
        }

        [HttpGet]
        public async Task<IActionResult> Get(CancellationToken ct)
        {
            var (type, value) = await _fee.GetAsync(ct);
            return Ok(new { ok = true, data = new { type, value } });
        }

        [Authorize(Roles = "Admin")]
        [HttpPut]
        public async Task<IActionResult> Put([FromBody] BookingFeeSettingDto dto, CancellationToken ct)
        {
            var t = (dto.Type ?? "FLAT").Trim().ToUpperInvariant();
            if (t != "FLAT" && t != "PERCENT")
                return BadRequest(new { ok = false, message = "Type must be FLAT or PERCENT." });

            if (dto.Value < 0)
                return BadRequest(new { ok = false, message = "Value must be >= 0." });

            // VND-only: nếu FLAT => ép nguyên đồng
            var val = t == "FLAT"
                ? Math.Round(dto.Value, 0, MidpointRounding.AwayFromZero)
                : dto.Value;

            var adminId = User?.Identity?.IsAuthenticated == true ? (int?)User.GetUserId() : null;

            await _repo.UpsertAsync("BOOKING_FEE_TYPE", t, adminId, ct);
            await _repo.UpsertAsync("BOOKING_FEE_VALUE", val.ToString(), adminId, ct);

            var (type, value) = await _fee.GetAsync(ct);
            return Ok(new { ok = true, data = new { type, value } });
        }
    }
}
