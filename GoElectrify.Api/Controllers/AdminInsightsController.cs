using GoElectrify.BLL.Contracts.Services;
using GoElectrify.BLL.Dto.Insights;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GoElectrify.Api.Controllers
{
    [ApiController]
    [Route("api/v1/admin/insights")]
    [Authorize(Roles = "Admin")]
    public sealed class AdminInsightsController : ControllerBase
    {
        private readonly IInsightsService _svc;
        public AdminInsightsController(IInsightsService svc) => _svc = svc;

        [HttpGet("revenue")]
        public async Task<ActionResult<RevenueSeriesDto>> Revenue(
    [FromQuery] DateTime from, [FromQuery] DateTime to, [FromQuery] int? stationId,
    [FromQuery] string granularity = "day", CancellationToken ct = default)
        {
            if (to <= from) return BadRequest("`to` must be greater than `from`.");

            from = EnsureUtc(from);
            to = EnsureUtc(to);

            var data = await _svc.GetRevenueAsync(from, to, stationId, granularity, ct);
            return Ok(data);

            static DateTime EnsureUtc(DateTime dt) =>
                dt.Kind == DateTimeKind.Utc ? dt :
                dt.Kind == DateTimeKind.Local ? dt.ToUniversalTime() :
                DateTime.SpecifyKind(dt, DateTimeKind.Utc);
        }

        [HttpGet("usage")]
        public async Task<ActionResult<UsageSeriesDto>> Usage(
            [FromQuery] DateTime from,
            [FromQuery] DateTime to,
            [FromQuery] int? stationId,
            [FromQuery] string granularity = "hour",
            CancellationToken ct = default)
        {
            if (to <= from) return BadRequest("`to` must be greater than `from`.");
            var data = await _svc.GetUsageAsync(from, to, stationId, granularity, ct);
            return Ok(data);
        }
    }
}
