using System.Runtime.Intrinsics.Arm;
using System.Text.Json;
using GoElectrify.BLL.Contracts.Services;
using GoElectrify.BLL.Dto.Charger;
using GoElectrify.BLL.Dtos.ChargerLogs;
using GoElectrify.DAL.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GoElectrify.Api.Controllers
{
    [ApiController]
    [Route("api/v1/chargers")]
    public sealed class ChargersController : ControllerBase
    {
        private readonly IChargerService _svc;
        private readonly AppDbContext _db;
        private readonly IChargerLogService _chargerLogsvc;
        private static readonly JsonSerializerOptions Camel = new(JsonSerializerDefaults.Web);
        public ChargersController(IChargerService svc, AppDbContext db, IChargerLogService chargerLogService)
        {
            _svc = svc;
            _db = db;
            _chargerLogsvc = chargerLogService;
        }

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

        /// <summary>Lấy lịch sử ChargerLog theo trụ (filter & phân trang).</summary>
        [HttpGet("{id:int}/logs")]
        public async Task<IResult> GetChargerLogs(
            [FromRoute] int id,
            [FromQuery] DateTime? from,
            [FromQuery] DateTime? to,
            [FromQuery] string? state,
            [FromQuery] string? errorCode,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 50,
            [FromQuery] string? order = "desc",
            CancellationToken ct = default)
        {
            var exists = await _db.Chargers.AsNoTracking().AnyAsync(c => c.Id == id, ct);
            if (!exists)
                return Results.Json(new { ok = false, error = "charger_not_found" }, options: Camel, statusCode: 404);

            bool asc = string.Equals(order, "asc", StringComparison.OrdinalIgnoreCase);

            var q = new ChargerLogQueryDto(
                Page: Math.Max(1, page),
                PageSize: Math.Clamp(pageSize, 1, 200),
                From: from,
                To: to,
                States: SplitCsvUpper(state),
                ErrorCodes: SplitCsvUpper(errorCode),
                Ascending: asc
            );

            var data = await _chargerLogsvc.GetLogsAsync(id, q, ct);
            return Results.Json(new { ok = true, data }, options: Camel);
        }

        private static string[] SplitCsvUpper(string? s) =>
            string.IsNullOrWhiteSpace(s)
                ? Array.Empty<string>()
                : s.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                   .Select(x => x.ToUpperInvariant())
                   .ToArray();


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
