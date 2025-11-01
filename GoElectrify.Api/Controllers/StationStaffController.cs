using GoElectrify.BLL.Contracts.Services;
using GoElectrify.BLL.Dto.StationStaff;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Net;

namespace GoElectrify.Api.Controllers
{
    [ApiController]
    [Route("api/v1/stations/{stationId:int}/staff")]
    [Authorize(Roles = "Admin")]
    public class StationStaffController : ControllerBase
    {
        private readonly IStationStaffService _svc;
        public StationStaffController(IStationStaffService svc) => _svc = svc;

        // GET: chỉ staff ACTIVE
        [HttpGet]
        public async Task<IActionResult> List(int stationId, CancellationToken ct)
        {
            try
            {
                var data = await _svc.ListAsync(stationId, ct);
                return Ok(new { ok = true, data });
            }
            catch (KeyNotFoundException)
            {
                return StatusCode((int)HttpStatusCode.NotFound, new
                {
                    ok = false,
                    error = new { code = "StationNotFound", message = "Station not found." }
                });
            }
        }

        // POST: Assign / Reactivate
        [HttpPost]
        public async Task<IActionResult> Assign(int stationId, [FromBody] AssignStaffRequestDto? req, CancellationToken ct)
        {
            // Validate body sớm để tránh NullRef & giúp FE
            if (req == null || req.UserId <= 0)
            {
                return StatusCode((int)HttpStatusCode.BadRequest, new
                {
                    ok = false,
                    error = new { code = "InvalidArgument", message = "Invalid request body. 'userId' is required and must be > 0." }
                });
            }

            try
            {
                var data = await _svc.AssignAsync(stationId, req, ct);
                return Ok(new { ok = true, data });
            }
            catch (KeyNotFoundException ex) when (ex.Message.Contains("Station", StringComparison.OrdinalIgnoreCase))
            {
                return StatusCode((int)HttpStatusCode.NotFound, new
                {
                    ok = false,
                    error = new { code = "StationNotFound", message = "Station not found." }
                });
            }
            catch (KeyNotFoundException ex) when (ex.Message.Contains("User", StringComparison.OrdinalIgnoreCase))
            {
                return StatusCode((int)HttpStatusCode.NotFound, new
                {
                    ok = false,
                    error = new { code = "UserNotFound", message = "User not found." }
                });
            }
            // Không phải nhân viên
            catch (InvalidOperationException ex) when (ex.Message == "UserNotStaff")
            {
                return StatusCode((int)HttpStatusCode.Conflict, new
                {
                    ok = false,
                    error = new
                    {
                        code = "UserNotStaff",
                        message = "Only users with role 'staff' can be assigned to a station.",
                        data = new { userId = req.UserId }
                    }
                });
            }
            // Đã được gán vào chính trạm này và đang active
            catch (InvalidOperationException ex) when (ex.Message == "StaffAlreadyAssigned")
            {
                return StatusCode((int)HttpStatusCode.Conflict, new
                {
                    ok = false,
                    error = new
                    {
                        code = "StaffAlreadyAssigned",
                        message = "Staff is already assigned to this station.",
                        data = new { stationId, userId = req.UserId }
                    }
                });
            }
            // Đang active ở trạm khác
            catch (InvalidOperationException ex) when (ex.Message.StartsWith("StaffAssignedToOtherStation:", StringComparison.Ordinal))
            {
                var parts = ex.Message.Split(':', 2);
                int.TryParse(parts.Length == 2 ? parts[1] : "0", out var currentStationId);

                return StatusCode((int)HttpStatusCode.Conflict, new
                {
                    ok = false,
                    error = new
                    {
                        code = "StaffAssignedToOtherStation",
                        message = "Staff is already assigned to another station.",
                        data = new { currentStationId, userId = req.UserId }
                    }
                });
            }
            catch (ArgumentException ex)
            {
                return StatusCode((int)HttpStatusCode.BadRequest, new
                {
                    ok = false,
                    error = new { code = "InvalidArgument", message = ex.Message }
                });
            }
        }

        // DELETE: Soft-revoke (body preferred; fallback query)
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
            {
                return StatusCode((int)HttpStatusCode.BadRequest, new
                {
                    ok = false,
                    error = new { code = "InvalidRevokeReason", message = "Revoke reason must be at least 3 characters." }
                });
            }

            try
            {
                var data = await _svc.DeleteAsync(stationId, userId, msg, ct);
                // action: "revoked" | "noop_already_revoked"
                return Ok(new { ok = true, data });
            }
            catch (KeyNotFoundException)
            {
                return StatusCode((int)HttpStatusCode.NotFound, new
                {
                    ok = false,
                    error = new { code = "AssignmentNotFound", message = "Assignment not found." }
                });
            }
            catch (ArgumentException ex)
            {
                return StatusCode((int)HttpStatusCode.BadRequest, new
                {
                    ok = false,
                    error = new { code = "InvalidArgument", message = ex.Message }
                });
            }
        }
    }
}
