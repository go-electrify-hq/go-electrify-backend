using GoElectrify.BLL.Contracts.Services;
using GoElectrify.BLL.Dto;
using GoElectrify.BLL.Dto.Station;
using Microsoft.AspNetCore.Mvc;

namespace go_electrify_backend.Controllers;
[ApiController]
[Route("api/[controller]")]
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

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id)
    {
        var station = await _service.GetStationByIdAsync(id);
        if (station == null) return NotFound();
        return Ok(station);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] StationCreateDto request)
    {
        var station = await _service.CreateStationAsync(request);
        return CreatedAtAction(nameof(GetById), new { id = station.Id }, station);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(int id, [FromBody] StationUpdateDto request)
    {
        var station = await _service.UpdateStationAsync(id, request);
        if (station == null) return NotFound();
        return Ok(station);
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        var deleted = await _service.DeleteStationAsync(id);
        if (!deleted) return NotFound();
        return NoContent();
    }
}