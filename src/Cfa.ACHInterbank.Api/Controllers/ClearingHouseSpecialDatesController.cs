using Cfa.ACHInterbank.Application.ACH.Interfaces;
using Cfa.ACHInterbank.Application.Security;
using Cfa.ACHInterbank.Domain.Entities.Ach.Dtos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Cfa.ACHInterbank.Api.Controllers;

[ApiController]
[Route("clearing-house-special-dates")]
[Authorize]
public class ClearingHouseSpecialDatesController : ControllerBase
{
    private readonly IClearingHouseSpecialDateService _service;

    public ClearingHouseSpecialDatesController(IClearingHouseSpecialDateService service)
    {
        _service = service;
    }
    /// <summary>
    /// Endpoint de la API ACH Interbank.
    /// </summary>

    [HttpGet]
    [Authorize(Policy = FineGrainedPermissions.ClearingHouses.View)]
    public async Task<IActionResult> GetAll([FromQuery] int? year, [FromQuery] int? clearingHouseId, CancellationToken ct = default)
        => Ok(await _service.GetAllAsync(year, clearingHouseId, ct));
    /// <summary>
    /// Endpoint de la API ACH Interbank.
    /// </summary>

    [HttpPost]
    [Authorize(Policy = FineGrainedPermissions.ClearingHouses.ManageSpecialDates)]
    public async Task<IActionResult> Create([FromBody] ClearingHouseSpecialDateDto dto, CancellationToken ct = default)
        => Ok(await _service.CreateAsync(dto, ct));
    /// <summary>
    /// Endpoint de la API ACH Interbank.
    /// </summary>

    [HttpPut("{id}")]
    [Authorize(Policy = FineGrainedPermissions.ClearingHouses.ManageSpecialDates)]
    public async Task<IActionResult> Update(int id, [FromBody] ClearingHouseSpecialDateDto dto, CancellationToken ct = default)
    {
        if (id != dto.Id) return BadRequest();
        return Ok(await _service.UpdateAsync(dto, ct));
    }
    /// <summary>
    /// Endpoint de la API ACH Interbank.
    /// </summary>

    [HttpPatch("{id:int}/status")]
    [Authorize(Policy = FineGrainedPermissions.ClearingHouses.ManageSpecialDates)]
    public async Task<IActionResult> ChangeStatus(int id, [FromBody] ClearingHouseSpecialDateStatusDto dto, CancellationToken ct = default)
        => Ok(await _service.ChangeStatusAsync(id, dto.IsActive, ct));
}

public sealed class ClearingHouseSpecialDateStatusDto
{
    public bool IsActive { get; set; }
}
