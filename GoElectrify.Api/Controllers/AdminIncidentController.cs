using GoElectrify.BLL.Contracts.Services;
using GoElectrify.BLL.Dto.Incidents;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Primitives;

namespace GoElectrify.Api.Controllers
{
    [ApiController]
    [Route("api/v1/admin/incidents")]
    [Authorize(Roles = "Admin")]
    public class AdminIncidentsController : ControllerBase
    {
        private readonly IAdminIncidentService _svc;

        public AdminIncidentsController(IAdminIncidentService svc)
        {
            _svc = svc;
        }

        [HttpGet]
        public async Task<IActionResult> List([FromQuery] AdminIncidentListQueryDto query, CancellationToken ct)
        {

            if (string.IsNullOrWhiteSpace(query.Keyword)
                && Request.Query.TryGetValue("q", out StringValues legacyQ) //legacyQ (legacy query): “dùng để truy vấn dữ liệu hệ thống cũ hoặc logic cũ
                && !StringValues.IsNullOrEmpty(legacyQ))
            {
                query.Keyword = legacyQ.ToString().Trim();
            }

            var includeTotal = Request.Headers.TryGetValue("X-Include-Total", out var v) && v == "1";

            var (items, total) = await _svc.ListAsync(query, includeTotal, ct);

            if (includeTotal && total.HasValue)
                Response.Headers["X-Total-Count"] = total.Value.ToString();

            return Ok(items);
        }

        [HttpGet("{id:int}")]
        public async Task<IActionResult> Get(int id, CancellationToken ct)
        {
            try
            {
                var dto = await _svc.GetAsync(id, ct);
                return Ok(dto); // 200 OK
            }
            catch (KeyNotFoundException)
            {
                return NotFound(new { error = "Incident not found." });
            }
        }
    }
}
