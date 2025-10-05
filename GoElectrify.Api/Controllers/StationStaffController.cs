using GoElectrify.BLL.Contracts.Services;
using GoElectrify.BLL.Dto.StationStaff;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GoElectrify.Api.Controllers
{
    [ApiController]
    [Route("api/v1/stations/{stationId:int}/staff")]
    [Authorize(Roles = "Admin")]
    public class StationStaffController : ControllerBase
    {
        private readonly IStationStaffService _svc;
        public StationStaffController(IStationStaffService svc) => _svc = svc;

        // GET: chỉ trả staff ACTIVE (RevokedAt == null)
        [HttpGet]
        public async Task<IActionResult> List(int stationId, CancellationToken ct)
        {
            try
            {
                var result = await _svc.ListAsync(stationId, ct);
                return Ok(result);
            }
            catch (KeyNotFoundException)
            {
                return NotFound(new { error = "Station not found." });
            }
        }

        // POST: Assign (nếu đã từng revoke => re-activate)
        [HttpPost]
        public async Task<IActionResult> Assign(int stationId, [FromBody] AssignStaffRequestDto req, CancellationToken ct)
        {
            try
            {
                var dto = await _svc.AssignAsync(stationId, req, ct);
                return Ok(dto);
            }
            catch (KeyNotFoundException ex) { return NotFound(new { error = ex.Message }); }
            catch (InvalidOperationException ex) { return Conflict(new { error = ex.Message }); }
            catch (ArgumentException ex) { return BadRequest(new { error = ex.Message }); }
        }

        // DELETE: Soft-revoke (bắt buộc lý do). Ưu tiên body, fallback query.
        [HttpDelete("{userId:int}")]
        public async Task<IActionResult> Revoke(
            int stationId,
            int userId,
            [FromBody] RevokeStaffRequestDto? body,
            [FromQuery] string? reason,
            CancellationToken ct = default)
        {
            var msg = (body?.Reason ?? reason ?? string.Empty).Trim();
            if (msg.Length < 3)
                return BadRequest(new { error = "Revoke reason must be at least 3 characters." });

            try
            {
                var result = await _svc.DeleteAsync(stationId, userId, msg, ct);
                return Ok(result); // 200 OK + body JSON
            }
            catch (KeyNotFoundException)
            {
                return NotFound(new { error = "Assignment not found." });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }
    }
}
