using GoElectrify.BLL.Contracts.Services;
using GoElectrify.BLL.Dto.StationStaff;
using Microsoft.AspNetCore.Mvc;

namespace GoElectrify.Api.Controllers
{
    [ApiController]
    [Route("api/v1/stations/{stationId:int}/staff")]
    public class StationStaffController(IStationStaffService svc) : ControllerBase
    {
        [HttpGet]
        public async Task<IActionResult> List(int stationId, CancellationToken ct)
        {
            try { return Ok(await svc.ListAsync(stationId, ct)); }
            catch (KeyNotFoundException) { return NotFound(new { error = "Station not found." }); }
        }

        [HttpPost]
        public async Task<IActionResult> Assign(int stationId, [FromBody] AssignStaffRequestDto req, CancellationToken ct)
        {
            try { return Ok(await svc.AssignAsync(stationId, req, ct)); }
            catch (KeyNotFoundException ex) { return NotFound(new { error = ex.Message }); }
            catch (InvalidOperationException ex) { return Conflict(new { error = ex.Message }); }
            catch (ArgumentException ex) { return BadRequest(new { error = ex.Message }); }
        }

        // DELETE with reason (body preferred)
        [HttpDelete("{userId:int}")]
        public async Task<IActionResult> Revoke(
            int stationId,
            int userId,
            [FromBody] RevokeStaffRequestDto? body,     // body optional
            [FromQuery] string? reason,                 // fallback via query
            CancellationToken ct = default)
        {
            try
            {
                var msg = body?.Reason ?? reason ?? string.Empty;
                await svc.DeleteAsync(stationId, userId, msg, ct);
                return NoContent();
            }
            catch (KeyNotFoundException) { return NotFound(new { error = "Assignment not found." }); }
            catch (ArgumentException ex) { return BadRequest(new { error = ex.Message }); }
        }
    }
}
