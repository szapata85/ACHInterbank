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
    /// <summary>
    /// Pendiente de documentación.
    /// </summary>

    [HttpGet]
    [Authorize(Policy = "CanReadAch")]
    public async Task<IActionResult> Get(
        [FromQuery] int? clearingHouseId,
        [FromQuery] DateTime? startDate,
        [FromQuery] DateTime? endDate,
        [FromQuery] DateTime? processingDate,
        CancellationToken ct)
    {
        var effectiveStart = startDate ?? processingDate;
        var effectiveEnd = endDate ?? processingDate;

        var cycles = await _service.GetAsync(clearingHouseId, effectiveStart, effectiveEnd, ct);
        return Ok(cycles);
    }
    /// <summary>
    /// Pendiente de documentación.
    /// </summary>

    [HttpGet("exportable")]
    [Authorize(Policy = "CanReadAch")]
    public async Task<IActionResult> GetExportable(
        [FromQuery] int? clearingHouseId,
        [FromQuery] DateTime? startDate,
        [FromQuery] DateTime? endDate,
        CancellationToken ct)
    {
        var cycles = await _service.GetExecutedWithTransactionsAsync(clearingHouseId, startDate, endDate, ct);
        return Ok(cycles);
    }
    /// <summary>
    /// Pendiente de documentación.
    /// </summary>

    [HttpGet("{id}")]
    [Authorize(Policy = "CanReadAch")]
    public async Task<IActionResult> GetById(string id, CancellationToken ct)
    {
        var cycle = await _service.GetByIdAsync(id, ct);
        return cycle is null ? NotFound() : Ok(cycle);
    }
    /// <summary>
    /// Pendiente de documentación.
    /// </summary>

    [HttpPost]
    [Authorize(Policy = "CanManageAch")]
    public async Task<IActionResult> Create([FromBody] AchCycleRequest request, CancellationToken ct)
    {
        var cycle = await _service.CreateAsync(request, ct);
        return CreatedAtAction(nameof(GetById), new { id = cycle.Id }, cycle);
    }
    /// <summary>
    /// Pendiente de documentación.
    /// </summary>

    [HttpPut("{id}")]
    [Authorize(Policy = "CanManageAch")]
    public async Task<IActionResult> Update(string id, [FromBody] AchCycleRequest request, CancellationToken ct)
    {
        var cycle = await _service.UpdateAsync(id, request, ct);
        return Ok(cycle);
    }
    /// <summary>
    /// Pendiente de documentación.
    /// </summary>

    [HttpDelete("{id}")]
    [Authorize(Policy = "CanManageAch")]
    public async Task<IActionResult> Delete(string id, CancellationToken ct)
    {
        await _service.DeleteAsync(id, ct);
        return NoContent();
    }
}
