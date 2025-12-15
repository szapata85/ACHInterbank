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

    [HttpGet]
    [Authorize(Policy = "CanManageAch")]
    public async Task<IActionResult> GetAll(CancellationToken ct = default)
        => Ok(await _service.GetAllAsync(ct));

    [HttpPost]
    [Authorize(Policy = "CanManageAch")]
    public async Task<IActionResult> Create([FromBody] InstitutionClearingHousePreferenceDto dto, CancellationToken ct = default)
        => Ok(await _service.CreateAsync(dto, ct));

    [HttpPut("{id}")]
    [Authorize(Policy = "CanManageAch")]
    public async Task<IActionResult> Update(int id, [FromBody] InstitutionClearingHousePreferenceDto dto, CancellationToken ct = default)
    {
        if (id != dto.Id) return BadRequest();
        return Ok(await _service.UpdateAsync(dto, ct));
    }

    [HttpDelete("{id}")]
    [Authorize(Policy = "CanManageAch")]
    public async Task<IActionResult> Delete(int id, CancellationToken ct = default)
    {
        await _service.DeleteAsync(id, ct);
        return NoContent();
    }
}
