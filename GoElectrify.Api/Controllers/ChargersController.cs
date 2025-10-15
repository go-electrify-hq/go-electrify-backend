using System.Runtime.Intrinsics.Arm;
using GoElectrify.BLL.Contracts.Services;
using GoElectrify.BLL.Dto.Charger;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GoElectrify.Api.Controllers
{
    [ApiController]
    [Route("api/v1/chargers")]
    public sealed class ChargersController : ControllerBase
    {
        private readonly IChargerService _svc;
        public ChargersController(IChargerService svc) => _svc = svc;

        // READ: ai cũng xem (đổi thành [Authorize] nếu cần)
        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> GetAll(CancellationToken ct) =>
            Ok(await _svc.GetAllAsync(ct));

        [HttpGet("{id:int}")]
        [AllowAnonymous]
        public async Task<IActionResult> Get(int id, CancellationToken ct)
        {
            var x = await _svc.GetByIdAsync(id, ct);
            return x is null ? NotFound() : Ok(x);
        }

        //[HttpGet("~/api/v1/stations/{stationId:int}/chargers")]
        //[AllowAnonymous]
        //public async Task<IActionResult> ListByStation([FromRoute] int stationId, CancellationToken ct)
        //{
        //    var items = await _svc.GetByStationAsync(stationId, ct);
        //    return Ok(new { ok = true, data = items });
        //}

        // CREATE: Admin|Staff
        [HttpPost]
        [Authorize(Roles = "Admin,Staff")]
        public async Task<IActionResult> Create([FromBody] ChargerCreateDto dto, CancellationToken ct)
        {
            var x = await _svc.CreateAsync(dto, ct);
            return CreatedAtAction(nameof(Get), new { id = x.Id }, x);
        }

        // UPDATE: Admin|Staff
        [HttpPut("{id:int}")]
        [Authorize(Roles = "Admin,Staff")]
        public async Task<IActionResult> Update(int id, [FromBody] ChargerUpdateDto dto, CancellationToken ct)
        {
            var x = await _svc.UpdateAsync(id, dto, ct);
            return x is null ? NotFound() : Ok(x);
        }

        // DELETE: Admin|Staff
        [HttpDelete("{id:int}")]
        [Authorize(Roles = "Admin,Staff")]
        public async Task<IActionResult> Delete(int id, CancellationToken ct)
        {
            var ok = await _svc.DeleteAsync(id, ct);
            return ok ? NoContent() : NotFound();
        }
    }
}
