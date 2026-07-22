using Cfa.ACHInterbank.Application.ACH.Interfaces;
using Cfa.ACHInterbank.Domain.Entities.Ach.Dtos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Cfa.ACHInterbank.Api.Controllers;

[ApiController]
[Route("ach-cycles")]
[Route("api/ach-cycles")]
[Authorize]
public class AchCyclesController : ControllerBase
{
    private readonly IAchCycleAppService _service;

    public AchCyclesController(IAchCycleAppService service)
    {
        _service = service;
    }
    /// <summary>
    /// Endpoint de la API ACH Interbank.
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
    /// Endpoint de la API ACH Interbank.
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
    /// Endpoint de la API ACH Interbank.
    /// </summary>

    [HttpGet("{id}")]
    [Authorize(Policy = "CanReadAch")]
    public async Task<IActionResult> GetById(string id, CancellationToken ct)
    {
        var cycle = await _service.GetByIdAsync(id, ct);
        return cycle is null ? NotFound() : Ok(cycle);
    }
    /// <summary>
    /// Endpoint de la API ACH Interbank.
    /// </summary>

    [HttpPost]
    [Authorize(Policy = "CanManageAch")]
    public async Task<IActionResult> Create([FromBody] AchCycleRequest request, CancellationToken ct)
    {
        try
        {
            var cycle = await _service.CreateAsync(request, ct);
            return CreatedAtAction(nameof(GetById), new { id = cycle.Id }, cycle);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(Problem("Ciclo ACH inválido", ex.Message, StatusCodes.Status400BadRequest));
        }
    }
    /// <summary>
    /// Endpoint de la API ACH Interbank.
    /// </summary>

    [HttpPut("{id}")]
    [Authorize(Policy = "CanManageAch")]
    public async Task<IActionResult> Update(string id, [FromBody] AchCycleRequest request, CancellationToken ct)
    {
        try
        {
            var cycle = await _service.UpdateAsync(id, request, ct);
            return Ok(cycle);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(Problem("Ciclo ACH no encontrado", ex.Message, StatusCodes.Status404NotFound));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(Problem("Ciclo ACH inválido", ex.Message, StatusCodes.Status400BadRequest));
        }
    }

    [HttpPost("repair-configuration-links")]
    [Authorize(Policy = "CanManageAch")]
    public async Task<IActionResult> RepairConfigurationLinks(CancellationToken ct)
    {
        var result = await _service.RepairConfigurationLinksAsync(ct);
        return result.Completed
            ? Ok(result)
            : Conflict(Problem(
                "Reparación bloqueada por asociaciones ambiguas",
                "No se modificó ningún ciclo. Revise los identificadores reportados y corrija las configuraciones superpuestas.",
                StatusCodes.Status409Conflict,
                result));
    }
    /// <summary>
    /// Endpoint de la API ACH Interbank.
    /// </summary>

    [HttpDelete("{id}")]
    [Authorize(Policy = "CanManageAch")]
    public async Task<IActionResult> Delete(string id, CancellationToken ct)
    {
        await _service.DeleteAsync(id, ct);
        return NoContent();
    }

    private static ProblemDetails Problem(string title, string detail, int status, object? result = null)
    {
        var problem = new ProblemDetails { Title = title, Detail = detail, Status = status };
        if (result is not null)
        {
            problem.Extensions["repairResult"] = result;
        }
        return problem;
    }
}
