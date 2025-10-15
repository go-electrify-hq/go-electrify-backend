using System.Security.Claims;
using GoElectrify.BLL.Contracts.Services;
using GoElectrify.BLL.Dto.Incidents;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GoElectrify.Api.Controllers
{
    [ApiController]
    [Route("api/v1/stations/{stationId:int}/incidents")]
    [Authorize(Roles = "Staff")]
    public class IncidentController : ControllerBase
    {
        private readonly IIncidentService _svc;
        public IncidentController(IIncidentService svc) => _svc = svc;

        [HttpGet]
        public async Task<IActionResult> List(int stationId, [FromQuery] IncidentListQueryDto query, CancellationToken ct)
        {
            try { return Ok(await _svc.ListAsync(stationId, query, ct)); }
            catch (KeyNotFoundException) { return NotFound(new { error = "Station not found." }); }
        }

        [HttpGet("{incidentId:int}")]
        public async Task<IActionResult> GetById(int stationId, int incidentId, CancellationToken ct)
        {
            try { return Ok(await _svc.GetAsync(stationId, incidentId, ct)); }
            catch (KeyNotFoundException) { return NotFound(new { error = "Incident not found." }); }
        }

        [HttpPost]
        public async Task<IActionResult> Create(int stationId, [FromBody] IncidentCreateDto dto, CancellationToken ct)
        {
            try
            {
                var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");
                if (!int.TryParse(userIdStr, out var userId))
                    return Unauthorized(new { error = "Invalid user id in token." });

                var result = await _svc.CreateAsync(stationId, userId, dto, ct);
                return CreatedAtAction(nameof(GetById), new { stationId, incidentId = result.Id }, result);
            }
            catch (KeyNotFoundException ex) { return NotFound(new { error = ex.Message }); }
            catch (InvalidOperationException ex) { return StatusCode(403, new { error = ex.Message }); }
            catch (ArgumentException ex) { return BadRequest(new { error = ex.Message }); }
        }

        [HttpPatch("{incidentId:int}/status")]
        public async Task<IActionResult> UpdateStatus(int stationId, int incidentId, [FromBody] IncidentUpdateStatusDto dto, CancellationToken ct)
        {
            try
            {
                var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");
                if (!int.TryParse(userIdStr, out var userId))
                    return Unauthorized(new { error = "Invalid user id in token." });

                var result = await _svc.UpdateStatusAsync(stationId, incidentId, userId, dto, ct);
                return Ok(result);
            }
            catch (KeyNotFoundException ex) { return NotFound(new { error = ex.Message }); }
            catch (InvalidOperationException ex) { return StatusCode(403, new { error = ex.Message }); }
            catch (ArgumentException ex) { return BadRequest(new { error = ex.Message }); }
        }
    }
}
