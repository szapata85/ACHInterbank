using Cfa.ACHInterbank.Application.ACH.Interfaces;
using Cfa.ACHInterbank.Domain.Entities.Ach.Dtos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Cfa.ACHInterbank.Api.Controllers;

[ApiController]
[Route("nacha-layouts")]
[Authorize]
public class NachaRecordLayoutsController : ControllerBase
{
    private readonly INachaRecordLayoutAppService _service;

    public NachaRecordLayoutsController(INachaRecordLayoutAppService service)
    {
        _service = service;
    }

    [HttpGet]
    [Authorize(Policy = "CanReadAch")]
    public async Task<ActionResult<IEnumerable<NachaRecordLayoutDto>>> GetAll(CancellationToken ct)
    {
        var items = await _service.GetAllAsync(ct);
        return Ok(items);
    }

    [HttpGet("{id:int}")]
    [Authorize(Policy = "CanReadAch")]
    public async Task<ActionResult<NachaRecordLayoutDto>> GetById(int id, CancellationToken ct)
    {
        var item = await _service.GetByIdAsync(id, ct);
        return item is null ? NotFound() : Ok(item);
    }

    [HttpPost]
    [Authorize(Policy = "CanManageAch")]
    public async Task<ActionResult<NachaRecordLayoutDto>> Create([FromBody] NachaRecordLayoutDto request, CancellationToken ct)
    {
        var created = await _service.CreateAsync(request, ct);
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
    }

    [HttpPut("{id:int}")]
    [Authorize(Policy = "CanManageAch")]
    public async Task<IActionResult> Update(int id, [FromBody] NachaRecordLayoutDto request, CancellationToken ct)
    {
        if (id != request.Id)
        {
            return BadRequest();
        }

        var updated = await _service.UpdateAsync(id, request, ct);
        return updated is null ? NotFound() : Ok(updated);
    }

    [HttpDelete("{id:int}")]
    [Authorize(Policy = "CanManageAch")]
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
    {
        var deleted = await _service.DeleteAsync(id, ct);
        return deleted ? NoContent() : NotFound();
    }
}
