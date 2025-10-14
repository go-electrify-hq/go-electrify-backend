using GoElectrify.BLL.Contracts.Services;
using GoElectrify.BLL.Dto.ConnectorTypes;
using GoElectrify.BLL.Dtos.ConnectorTypes;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GoElectrify.Api.Controllers
{
    [ApiController]
    [Route("api/v1/connector-types")]
    public class ConnectorTypeController(IConnectorTypeService svc) : ControllerBase
    {
        [HttpGet]
        //[AllowAnonymous]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> List([FromQuery] string? search, CancellationToken ct)
           => Ok(await svc.ListAsync(search, ct));

        [HttpGet("{id:int}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Get(int id, CancellationToken ct)
            => (await svc.GetAsync(id, ct)) is { } dto ? Ok(dto) : NotFound();

        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Create([FromBody] CreateConnectorTypeDto dto, CancellationToken ct)
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
        public async Task<IActionResult> Update(int id, [FromBody] UpdateConnectorTypeDto dto, CancellationToken ct)
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
            catch (InvalidOperationException ex) // đang bị bookings/chargers tham chiếu
            {
                return Conflict(new { error = ex.Message });
            }
        }

        [HttpDelete("batch")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> BatchDelete([FromBody] DeleteConnectorTypeDto req, CancellationToken ct)
        {
            if (!ModelState.IsValid) return ValidationProblem(ModelState);
            if (req?.Ids == null || req.Ids.Count == 0)
                return BadRequest(new { error = "The Id list cannot be empty" });

            var result = await svc.DeleteManyWithReportAsync(req.Ids, ct);

            // Có id không tồn tại hoặc đang bị tham chiếu -> 409, không xóa gì cả
            if (result.BlockedIds.Count > 0 || result.NotFoundIds.Count > 0)
                return Conflict(result);

            // Xóa thành công toàn bộ
            return Ok(result); // { deleted, deletedIds, blockedIds:[], notFoundIds:[] }
        }
    }
}
