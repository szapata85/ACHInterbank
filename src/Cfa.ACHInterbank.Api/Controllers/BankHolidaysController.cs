using Cfa.ACHInterbank.Application.ACH.Interfaces;
using Cfa.ACHInterbank.Domain.Entities.Ach.Dtos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Cfa.ACHInterbank.Api.Controllers;

[ApiController]
[Route("bank-holidays")]
[Authorize]
public class BankHolidaysController : ControllerBase
{
    private readonly IBankHolidayAdminService _service;
    private readonly IBankHolidayProvisioningService _provisioning;

    public BankHolidaysController(
        IBankHolidayAdminService service,
        IBankHolidayProvisioningService provisioning)
    {
        _service = service;
        _provisioning = provisioning;
    }
    /// <summary>
    /// Endpoint de la API ACH Interbank.
    /// </summary>

    [HttpGet]
    [Authorize(Policy = "CanManageAch")]
    public async Task<IActionResult> GetAll([FromQuery] int? year, CancellationToken ct = default)
        => Ok(await _service.GetAllAsync(year, ct));
    /// <summary>
    /// Endpoint de la API ACH Interbank.
    /// </summary>

    [HttpPost]
    [Authorize(Policy = "CanManageAch")]
    public async Task<IActionResult> Create([FromBody] BankHolidayDto dto, CancellationToken ct = default)
        => Ok(await _service.CreateAsync(dto, ct));
    /// <summary>
    /// Endpoint de la API ACH Interbank.
    /// </summary>

    [HttpPut("{id}")]
    [Authorize(Policy = "CanManageAch")]
    public async Task<IActionResult> Update(int id, [FromBody] BankHolidayDto dto, CancellationToken ct = default)
    {
        if (id != dto.Id) return BadRequest();
        return Ok(await _service.UpdateAsync(dto, ct));
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

    [HttpPost("ensure")]
    [Authorize(Policy = "CanManageAch")]
    public async Task<IActionResult> Ensure([FromBody] BankHolidayEnsureRequest request, CancellationToken ct = default)
        => Ok(await _provisioning.EnsureYearsAsync(request.Years, ct));
}

public sealed record BankHolidayEnsureRequest(IReadOnlyList<int> Years);
