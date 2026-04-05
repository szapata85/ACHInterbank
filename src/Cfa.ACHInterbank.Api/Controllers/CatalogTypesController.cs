using Cfa.ACHInterbank.Application.ACH.Interfaces;
using Cfa.ACHInterbank.Domain.Models.ACH;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Cfa.ACHInterbank.Api.Controllers;

[ApiController]
[Route("catalog-types")]
[Authorize]
public class CatalogTypesController : ControllerBase
{
    private readonly ICatalogTypesService _service;

    public CatalogTypesController(ICatalogTypesService service)
    {
        _service = service;
    }

    [HttpGet("{catalogType}")]
    [Authorize(Policy = "CanManageAch")]
    public async Task<IActionResult> GetAll(string catalogType, CancellationToken ct = default)
    {
        try
        {
            var items = await _service.GetAllAsync(catalogType, ct);
            return Ok(items);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ex.Message);
        }
    }

    [HttpPost("{catalogType}")]
    [Authorize(Policy = "CanManageAch")]
    public async Task<IActionResult> Create(string catalogType, [FromBody] CatalogTypeUpsertRequest request, CancellationToken ct = default)
    {
        try
        {
            var created = await _service.CreateAsync(catalogType, request, ct);
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

    [HttpPut("{catalogType}/{code}")]
    [Authorize(Policy = "CanManageAch")]
    public async Task<IActionResult> Update(string catalogType, string code, [FromBody] CatalogTypeUpsertRequest request, CancellationToken ct = default)
    {
        try
        {
            var updated = await _service.UpdateAsync(catalogType, code, request, ct);
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
    }

    [HttpDelete("{catalogType}/{code}")]
    [Authorize(Policy = "CanManageAch")]
    public async Task<IActionResult> Delete(string catalogType, string code, CancellationToken ct = default)
    {
        try
        {
            await _service.DeleteAsync(catalogType, code, ct);
            return NoContent();
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
}
