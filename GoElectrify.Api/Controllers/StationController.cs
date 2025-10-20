using GoElectrify.Api.Auth;
using GoElectrify.BLL.Contracts.Repositories;
using GoElectrify.BLL.Contracts.Services;
using GoElectrify.BLL.Dto.Charger;
using GoElectrify.BLL.Dto.Station;
using GoElectrify.BLL.Dtos.Booking;
using GoElectrify.BLL.Dtos.ChargingSession;
using GoElectrify.BLL.Dtos.Station;
using GoElectrify.BLL.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace go_electrify_backend.Controllers;
[ApiController]
[Route("api/v1/stations")]
public class StationController : ControllerBase
{
    private readonly IStationService _service;
    private readonly IStationStaffRepository _staffRepo;
    public StationController(IStationService service, IStationStaffRepository staffRepo)
    {
        _service = service;
        _staffRepo = staffRepo;
    }

    [Authorize(Roles = "Staff")]
    [HttpGet("me")]
    public async Task<IActionResult> GetMyStation()
    {
        var userId = User.GetUserId();
        var station = await _service.GetMyStationAsync(userId, CancellationToken.None);
        if (station == null)
            return NotFound(new { error = "You are not assigned to any active station." });

        var dto = new StationDto
        {
            Id = station.Id,
            Name = station.Name,
            Address = station.Address,
            Status = station.Status.ToString(),
            Latitude = (decimal)station.Latitude,
            Longitude = (decimal)station.Longitude,
            Chargers = station.Chargers.Select(c => new ChargerDto
            {
                Id = c.Id,
                StationId = c.StationId,
                ConnectorTypeId = c.ConnectorTypeId,
                Code = c.Code,
                PowerKw = c.PowerKw,
                Status = c.Status,
                PricePerKwh = c.PricePerKwh,
                CreatedAt = c.CreatedAt,
                UpdatedAt = c.UpdatedAt
            }).ToList()
        };

        return Ok(dto);
    }
    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var stations = await _service.GetAllStationsAsync();
        return Ok(stations);
    }

    [HttpGet("{stationId:int}/chargers")]
    public async Task<IActionResult> GetChargersByStation(
    int stationId,
    [FromServices] IStationService stations,    // inject cục bộ, không đổi constructor
    [FromServices] IChargerService chargers,    // inject cục bộ, không đổi constructor
    CancellationToken ct = default)
    {

        var list = await chargers.GetByStationAsync(stationId, ct);

        // Trả đúng pattern của project: { ok, data }
        return Ok(new { ok = true, data = list });
    }

    [Authorize(Roles = "Admin,Staff")]
    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id)
    {
        var station = await _service.GetStationByIdAsync(id);
        if (station == null) return NotFound();
        return Ok(station);
    }

    [Authorize(Roles = "Admin")]
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] StationCreateDto request)
    {
        var station = await _service.CreateStationAsync(request);
        return CreatedAtAction(nameof(GetById), new { id = station.Id }, station);
    }

    [Authorize(Roles = "Admin")]
    [HttpPut("{id}")]
    public async Task<IActionResult> Update(int id, [FromBody] StationUpdateDto request)
    {
        var station = await _service.UpdateStationAsync(id, request);
        if (station == null) return NotFound();
        return Ok(station);
    }

    [Authorize(Roles = "Admin")]
    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        var deleted = await _service.DeleteStationAsync(id);
        if (!deleted) return NotFound();
        return NoContent();
    }

    [HttpPost("{id}/image")]
    [Authorize(Roles = "Admin,Staff")]
    public async Task<IActionResult> UploadImage(int id, IFormFile file)
    {
        if (file == null)
            return BadRequest(new { error = "No file uploaded." });

        try
        {
            var request = HttpContext.Request;
            var baseUrl = $"{request.Scheme}://{request.Host}";

            var imageUrl = await _service.UploadStationImageAsync(id, file, baseUrl);

            return Ok(new { imageUrl });
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { error = ex.Message });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = "Internal server error", detail = ex.Message });
        }
    }

    [HttpGet("nearby")]
    public async Task<IActionResult> Nearby(
    [FromQuery] double lat,
    [FromQuery] double lng,
    [FromQuery] double radiusKm = 10,
    [FromQuery] int limit = 20,
    CancellationToken ct = default)
    {
        var data = await _service.GetNearbyAsync(lat, lng, radiusKm, limit, ct);
        return Ok(new { ok = true, data });
    }

    [HttpGet("{stationId:int}/bookings")]
    [Authorize(Roles = "Admin,Staff")]
    public async Task<IActionResult> GetBookingsByStation(
       int stationId,
       [FromQuery] StationBookingQueryDto q,
       [FromServices] IBookingService bookings,
       [FromServices] IStationService stationsSvc,
       CancellationToken ct = default)
    {
        if (!await stationsSvc.ExistsAsync(stationId, ct))
            return NotFound(new { ok = false, error = "Station not found" });

        var data = await bookings.GetByStationAsync(stationId, q, ct);
        return Ok(new { ok = true, data });
    }

    [HttpGet("{stationId:int}/sessions")]
    [Authorize(Roles = "Admin,Staff")]
    public async Task<IActionResult> GetSessionsByStation(
        int stationId,
        [FromQuery] StationSessionQueryDto q,
        [FromServices] IChargingSessionService sessions,
        [FromServices] IStationService stations,
        CancellationToken ct = default)
    {
        if (!await stations.ExistsAsync(stationId, ct))
            return NotFound(new { ok = false, error = "Station not found" });

        var data = await sessions.GetByStationAsync(stationId, q, ct);
        return Ok(new { ok = true, data });
    }
}