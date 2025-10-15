using GoElectrify.BLL.Contracts.Services;
using GoElectrify.BLL.Dto;
using GoElectrify.BLL.Dto.Station;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace go_electrify_backend.Controllers;
[ApiController]
[Route("api/v1/stations")]
public class StationController : ControllerBase
{
    private readonly IStationService _service;

    public StationController(IStationService service)
    {
        _service = service;
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
}