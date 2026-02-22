using Cfa.ACHInterbank.Application.ACH.Interfaces;
using Cfa.ACHInterbank.Domain.Entities.Ach.Dtos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Cfa.ACHInterbank.Api.Controllers;

[ApiController]
[Route("institution-clearing-house-preferences")]
[Authorize]
public class InstitutionClearingHousePreferencesController : ControllerBase
{
    private readonly IInstitutionClearingHousePreferenceService _service;

    public InstitutionClearingHousePreferencesController(IInstitutionClearingHousePreferenceService service)
    {
        _service = service;
    }
    /// <summary>
    /// Endpoint de la API ACH Interbank.
    /// </summary>

    [HttpGet]
    [Authorize(Policy = "CanManageAch")]
    public async Task<IActionResult> GetAll(CancellationToken ct = default)
        => Ok(await _service.GetAllAsync(ct));
    /// <summary>
    /// Endpoint de la API ACH Interbank.
    /// </summary>

    [HttpPost]
    [Authorize(Policy = "CanManageAch")]
    public async Task<IActionResult> Create([FromBody] InstitutionClearingHousePreferenceDto dto, CancellationToken ct = default)
        => Ok(await _service.CreateAsync(dto, ct));
    /// <summary>
    /// Endpoint de la API ACH Interbank.
    /// </summary>

    [HttpPut("{id}")]
    [Authorize(Policy = "CanManageAch")]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateInstitutionClearingHousePreferenceDto? dto, CancellationToken ct = default)
    {
        dto ??= new UpdateInstitutionClearingHousePreferenceDto();
        return Ok(await _service.UpdateAsync(id, dto, ct));
    }
    /// <summary>
    /// Endpoint de la API ACH Interbank.
    /// </summary>

    [HttpDelete("{id}")]
    [Authorize(Policy = "CanManageAch")]
    public async Task<IActionResult> Delete(int id, CancellationToken ct = default)
    {
        await _service.DeleteAsync(id, ct);
        return NoContent();
    }
}
