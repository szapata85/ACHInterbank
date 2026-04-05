using Cfa.ACHInterbank.Application.ACH.Interfaces;
using Cfa.ACHInterbank.Application.ACH.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Cfa.ACHInterbank.Api.Controllers;

[ApiController]
[Route("company-entry-descriptions")]
[Authorize]
public class CompanyEntryDescriptionsController : ControllerBase
{
    private readonly ICompanyEntryDescriptionsService _service;

    public CompanyEntryDescriptionsController(ICompanyEntryDescriptionsService service)
    {
        _service = service;
    }

    [HttpGet]
    [Authorize(Policy = "CanManageAch")]
    public async Task<IActionResult> GetAll(CancellationToken ct = default)
    {
        var rows = await _service.GetAllAsync(ct);
        return Ok(rows);
    }

    [HttpPost]
    [Authorize(Policy = "CanManageAch")]
    public async Task<IActionResult> Create([FromBody] CompanyEntryDescriptionUpsertRequest request, CancellationToken ct = default)
    {
        try
        {
            var created = await _service.CreateAsync(request, ct);
            return Ok(created);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ex.Message);
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(ex.Message);
        }
    }

    [HttpPut("{id:int}")]
    [Authorize(Policy = "CanManageAch")]
    public async Task<IActionResult> Update(int id, [FromBody] CompanyEntryDescriptionUpsertRequest request, CancellationToken ct = default)
    {
        try
        {
            var updated = await _service.UpdateAsync(id, request, ct);
            return Ok(updated);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ex.Message);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(ex.Message);
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(ex.Message);
        }
    }

    [HttpDelete("{id:int}")]
    [Authorize(Policy = "CanManageAch")]
    public async Task<IActionResult> Delete(int id, CancellationToken ct = default)
    {
        try
        {
            await _service.DeleteAsync(id, ct);
            return NoContent();
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(ex.Message);
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(ex.Message);
        }
    }
}
