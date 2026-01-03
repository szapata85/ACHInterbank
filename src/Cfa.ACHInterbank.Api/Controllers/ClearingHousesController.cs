using Cfa.ACHInterbank.Application.ACH.Interfaces;
using Cfa.ACHInterbank.Domain.Models.Configurations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Cfa.ACHInterbank.Api.Controllers;

[ApiController]
[Route("clearing-houses")]
[Authorize]
public class ClearingHousesController : ControllerBase
{
    private readonly IClearingHouseService _service;
    private readonly IAchCycleAppService _cycleService;

    public ClearingHousesController(IClearingHouseService service, IAchCycleAppService cycleService)
    {
        _service = service;
        _cycleService = cycleService;
    }

    [HttpGet]
    [Authorize(Policy = "CanReadAch")]
    public async Task<IActionResult> Get([FromQuery] PaginationRequest request, CancellationToken ct)
    {
        var result = await _service.GetAsync(request, ct);
        return Ok(result);
    }

    [HttpGet("{id:int}")]
    [Authorize(Policy = "CanReadAch")]
    public async Task<IActionResult> GetById(int id, CancellationToken ct)
    {
        var result = await _service.GetByIdAsync(id, ct);
        return result is null ? NotFound() : Ok(result);
    }

    [HttpGet("{id:int}/cycles")]
    [Authorize(Policy = "CanReadAch")]
    public async Task<IActionResult> GetCyclesForClearingHouse(int id, [FromQuery] DateTime? processingDate, CancellationToken ct)
    {
        var cycles = await _cycleService.GetAsync(id, processingDate, processingDate, ct);
        return Ok(cycles);
    }
}
