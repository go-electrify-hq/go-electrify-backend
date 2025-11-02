using GoElectrify.Api.Auth;
using GoElectrify.BLL.Contracts.Services;
using GoElectrify.BLL.Dto.Incidents;
using GoElectrify.BLL.Dtos.Incidents;
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

        [HttpPatch("{id:int}/status")]
        public async Task<IActionResult> ModerateStatus(int id, [FromBody] AdminIncidentStatusUpdateDto body, CancellationToken ct)
        {
            try
            {
                // Lấy admin user id từ token (phục vụ audit nếu bạn có field)
                var adminUserId = User.TryGetUserId(out var uid) ? uid : 0;

                // Gọi service (repo sẽ validate flow & lưu DB)
                var dto = await _svc.UpdateStatusAsync(id, body.Status, adminUserId, body.Note, ct);

                return Ok(dto); // trả luôn DTO mới nhất cho FE
            }
            catch (KeyNotFoundException)
            {
                return NotFound(new { error = "Incident not found." });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { error = ex.Message }); // sai flow trạng thái
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { error = ex.Message }); // status mục tiêu không hợp lệ
            }
        }
    }
}
