using Cfa.ACHInterbank.Application.ACH.Interfaces;
using Cfa.ACHInterbank.Domain.Entities.Ach.Dtos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Cfa.ACHInterbank.Api.Controllers;

[ApiController]
[Route("clearing-house-cycle-configs")]
[Authorize]
public class ClearingHouseCycleConfigsController : ControllerBase
{
    private readonly IClearingHouseCycleConfigService _service;

    public ClearingHouseCycleConfigsController(IClearingHouseCycleConfigService service)
    {
        _service = service;
    }

    [HttpGet]
    [Authorize(Policy = "CanManageAch")]
    public async Task<IActionResult> GetByClearingHouse(
        [FromQuery] int clearingHouseId,
        [FromQuery] DateTime? effectiveAt,
        CancellationToken ct = default)
        => Ok(await _service.GetByClearingHouseAsync(clearingHouseId, effectiveAt, ct));

    [HttpGet("current")]
    [Authorize(Policy = "CanManageAch")]
    public async Task<IActionResult> GetCurrentByClearingHouse(
        [FromQuery] int clearingHouseId,
        [FromQuery] DateTime? effectiveAt,
        CancellationToken ct = default)
        => Ok(await _service.GetCurrentByClearingHouseAsync(clearingHouseId, effectiveAt, ct));

    [HttpPost]
    [Authorize(Policy = "CanManageAch")]
    public async Task<IActionResult> CreateVersion([FromBody] UpsertClearingHouseCycleConfigDto dto, CancellationToken ct = default)
        => Ok(await _service.CreateVersionAsync(dto, ct));

    [HttpPost("{id:int}/inactivate")]
    [Authorize(Policy = "CanManageAch")]
    public async Task<IActionResult> Inactivate(int id, [FromBody] InactivateClearingHouseCycleConfigDto dto, CancellationToken ct = default)
        => Ok(await _service.InactivateAsync(id, dto.EffectiveTo, ct));
}
