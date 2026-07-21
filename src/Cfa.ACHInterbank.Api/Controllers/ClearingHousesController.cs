using Cfa.ACHInterbank.Application.ACH.Interfaces;
using Cfa.ACHInterbank.Application.ACH.Models;
using Cfa.ACHInterbank.Application.Security;
using Cfa.ACHInterbank.Domain.Entities.Ach.Dtos;
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
    [Authorize(Policy = FineGrainedPermissions.ClearingHouses.View)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> Get([FromQuery] ClearingHouseAdminQuery request, CancellationToken ct)
        => Ok(await _service.GetAsync(request, ct));

    [HttpGet("operational")]
    [Authorize(Policy = "CanReadAch")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetOperational(CancellationToken ct)
        => Ok(await _service.GetOperationalAsync(ct));

    [HttpGet("nacha-profiles")]
    [Authorize(Policy = FineGrainedPermissions.ClearingHouses.View)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetNachaProfiles([FromQuery] string? clearingHouseCode, CancellationToken ct)
        => Ok(await _service.GetNachaProfilesAsync(clearingHouseCode, ct));

    [HttpGet("{id:int}")]
    [Authorize(Policy = FineGrainedPermissions.ClearingHouses.View)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(int id, CancellationToken ct)
    {
        var result = await _service.GetByIdAsync(id, ct);
        return result is null ? NotFoundProblem() : Ok(result);
    }

    [HttpGet("{id:int}/readiness")]
    [Authorize(Policy = FineGrainedPermissions.ClearingHouses.View)]
    public async Task<IActionResult> GetReadiness(int id, CancellationToken ct)
    {
        try { return Ok(await _service.GetReadinessAsync(id, ct)); }
        catch (ClearingHouseNotFoundException) { return NotFoundProblem(); }
    }

    [HttpPost]
    [Authorize(Policy = FineGrainedPermissions.ClearingHouses.Create)]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Create([FromBody] CreateClearingHouseRequest request, CancellationToken ct)
    {
        try
        {
            var created = await _service.CreateAsync(request, ct);
            return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
        }
        catch (ClearingHouseValidationException ex) { return ValidationProblem(ex); }
        catch (ClearingHouseConflictException ex) { return ConflictProblem(ex.Message); }
    }

    [HttpPut("{id:int}")]
    [Authorize(Policy = FineGrainedPermissions.ClearingHouses.Update)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateClearingHouseRequest request, CancellationToken ct)
    {
        try { return Ok(await _service.UpdateAsync(id, request, ct)); }
        catch (ClearingHouseValidationException ex) { return ValidationProblem(ex); }
        catch (ClearingHouseConflictException ex) { return ConflictProblem(ex.Message); }
        catch (ClearingHouseNotFoundException) { return NotFoundProblem(); }
    }

    [HttpPatch("{id:int}/status")]
    [Authorize(Policy = FineGrainedPermissions.ClearingHouses.ChangeStatus)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ChangeStatus(int id, [FromBody] ChangeClearingHouseStatusRequest request, CancellationToken ct)
    {
        try { return Ok(await _service.ChangeStatusAsync(id, request.IsActive, ct)); }
        catch (ClearingHouseValidationException ex) { return ValidationProblem(ex); }
        catch (ClearingHouseNotFoundException) { return NotFoundProblem(); }
    }

    [HttpGet("{id:int}/cycles")]
    [Authorize(Policy = FineGrainedPermissions.ClearingHouses.View)]
    public async Task<IActionResult> GetCyclesForClearingHouse(
        int id,
        [FromQuery] DateTime? processingDate,
        CancellationToken ct)
        => Ok(await _cycleService.GetAsync(id, processingDate, processingDate, ct));

    private ObjectResult ValidationProblem(ClearingHouseValidationException exception)
        => Problem(
            statusCode: StatusCodes.Status400BadRequest,
            title: "Configuración incompleta o inválida",
            detail: exception.Message,
            extensions: new Dictionary<string, object?> { ["missingRequirements"] = exception.MissingRequirements });

    private ObjectResult ConflictProblem(string detail)
        => Problem(statusCode: StatusCodes.Status409Conflict, title: "Conflicto de cámara compensadora", detail: detail);

    private ObjectResult NotFoundProblem()
        => Problem(statusCode: StatusCodes.Status404NotFound, title: "Cámara no encontrada", detail: "La cámara compensadora no existe.");
}
