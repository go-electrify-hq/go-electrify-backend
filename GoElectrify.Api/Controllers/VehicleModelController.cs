using GoElectrify.BLL.Contracts.Services;
using GoElectrify.BLL.Dto.VehicleModels;
using GoElectrify.BLL.Dtos.VehicleModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Runtime.Intrinsics.Arm;

namespace GoElectrify.Api.Controllers
{
    [ApiController]
    [Route("api/v1/vehicle-models")]
    public class VehicleModelController(IVehicleModelService svc) : ControllerBase
    {
        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> List([FromQuery] string? search, CancellationToken ct)
           => Ok(await svc.ListAsync(search, ct));

        [HttpGet("{id:int}")]
        [AllowAnonymous]
        public async Task<IActionResult> Get(int id, CancellationToken ct)
            => (await svc.GetAsync(id, ct)) is { } dto ? Ok(dto) : NotFound();

        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Create([FromBody] CreateVehicleModelDto dto, CancellationToken ct)
        {
            try { var id = await svc.CreateAsync(dto, ct); return CreatedAtAction(nameof(Get), new { id }, new { id }); }
            catch (InvalidOperationException ex)
            {
                if (ex.Message.Contains("exists", StringComparison.OrdinalIgnoreCase))
                    return Conflict(new { error = ex.Message });
                return BadRequest(new { error = ex.Message });
            }
        }

        [HttpPut("{id:int}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Update(int id, [FromBody] UpdateVehicleModelDto dto, CancellationToken ct)
        {
            try { await svc.UpdateAsync(id, dto, ct); return NoContent(); }
            catch (KeyNotFoundException) { return NotFound(); }
            catch (InvalidOperationException ex)
            {
                if (ex.Message.Contains("exists", StringComparison.OrdinalIgnoreCase))
                    return Conflict(new { error = ex.Message });
                return BadRequest(new { error = ex.Message });
            }
        }

        [HttpDelete("{id:int}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Delete(int id, CancellationToken ct)
        {
            try
            {
                await svc.DeleteAsync(id, ct);
                return NoContent();
            }
            catch (KeyNotFoundException)
            {
                return NotFound();
            }
            catch (InvalidOperationException ex) // đang bị bookings tham chiếu
            {
                return Conflict(new { error = ex.Message });
            }
        }

        // DELETE: /api/v1/vehicle-models/batch
        [HttpDelete("batch")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> BatchDelete([FromBody] DeleteVehicleModelDto req, CancellationToken ct)
        {
            if (!ModelState.IsValid) return ValidationProblem(ModelState);
            if (req?.Ids == null || req.Ids.Count == 0)
                return BadRequest(new { error = "The Id list cannot be empty" });

            var result = await svc.DeleteManyWithReportAsync(req.Ids, ct);
            // Luôn trả 200: FE hiển thị deleted vs blocked rõ ràng
            if (result.BlockedIds.Count > 0)
                return Conflict(result); // 409 + { deleted:0, deletedIds:[], blockedIds:[...] }

            return Ok(result); // { deleted:n, deletedIds:[...], blockedIds:[] }
        }
    }
}
