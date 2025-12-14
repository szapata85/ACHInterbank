using Cfa.ACHInterbank.Application.ACH.Interfaces;
using Cfa.ACHInterbank.Domain.Entities.Ach.Dtos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Cfa.ACHInterbank.Api.Controllers;

[ApiController]
[Route("ach-cycles")]
[Authorize]
public class AchCyclesController : ControllerBase
{
    private readonly IAchCycleAppService _service;

    public AchCyclesController(IAchCycleAppService service)
    {
        _service = service;
    }

    [HttpGet]
    [Authorize(Policy = "CanReadAch")]
    public async Task<IActionResult> Get([FromQuery] int? clearingHouseId, [FromQuery] DateTime? processingDate, CancellationToken ct)
    {
        var cycles = await _service.GetAsync(clearingHouseId, processingDate, ct);
        return Ok(cycles);
    }

    [HttpGet("exportable")]
    [Authorize(Policy = "CanReadAch")]
    public async Task<IActionResult> GetExportable(CancellationToken ct)
    {
        var cycles = await _service.GetExecutedWithTransactionsAsync(ct);
        return Ok(cycles);
    }

    [HttpGet("{id}")]
    [Authorize(Policy = "CanReadAch")]
    public async Task<IActionResult> GetById(string id, CancellationToken ct)
    {
        var cycle = await _service.GetByIdAsync(id, ct);
        return cycle is null ? NotFound() : Ok(cycle);
    }

    [HttpPost]
    [Authorize(Policy = "CanManageAch")]
    public async Task<IActionResult> Create([FromBody] AchCycleRequest request, CancellationToken ct)
    {
        var cycle = await _service.CreateAsync(request, ct);
        return CreatedAtAction(nameof(GetById), new { id = cycle.Id }, cycle);
    }

    [HttpPut("{id}")]
    [Authorize(Policy = "CanManageAch")]
    public async Task<IActionResult> Update(string id, [FromBody] AchCycleRequest request, CancellationToken ct)
    {
        var cycle = await _service.UpdateAsync(id, request, ct);
        return Ok(cycle);
    }

    [HttpDelete("{id}")]
    [Authorize(Policy = "CanManageAch")]
    public async Task<IActionResult> Delete(string id, CancellationToken ct)
    {
        await _service.DeleteAsync(id, ct);
        return NoContent();
    }
}
