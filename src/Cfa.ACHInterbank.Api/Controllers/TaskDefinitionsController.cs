using Cfa.ACHInterbank.Application.Scheduler.Interfaces;
using Cfa.ACHInterbank.Domain.Entities.SchedulerTask.Dtos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Cfa.ACHInterbank.Api.Controllers;

[ApiController]
[Route("[controller]")]
[Authorize]
public class TaskDefinitionsController : ControllerBase
{
    private readonly ITaskDefinitionAppService _service;

    public TaskDefinitionsController(ITaskDefinitionAppService service)
    {
        _service = service;
    }
    /// <summary>
    /// Endpoint de la API ACH Interbank.
    /// </summary>

    [HttpGet]
    [Authorize(Policy = "CanReadAch")]
    public async Task<ActionResult<IEnumerable<TaskDefinitionDto>>> Get(CancellationToken ct)
    {
        var items = await _service.GetAllAsync(ct);
        return Ok(items);
    }
    /// <summary>
    /// Endpoint de la API ACH Interbank.
    /// </summary>

    [HttpGet("{id}")]
    [Authorize(Policy = "CanReadAch")]
    public async Task<ActionResult<TaskDefinitionDto>> Get(int id, CancellationToken ct)
    {
        var task = await _service.GetByIdAsync(id, ct);
        if (task is null) return NotFound();
        return Ok(task);
    }
    /// <summary>
    /// Endpoint de la API ACH Interbank.
    /// </summary>

    [HttpPost]
    [Authorize(Policy = "CanManageAch")]
    public async Task<ActionResult<TaskDefinitionDto>> Post([FromBody] TaskDefinitionDto task, CancellationToken ct)
    {
        var created = await _service.CreateAsync(task, ct);
        return CreatedAtAction(nameof(Get), new { id = created.Id }, created);
    }
    /// <summary>
    /// Endpoint de la API ACH Interbank.
    /// </summary>

    [HttpPut("{id}")]
    [Authorize(Policy = "CanManageAch")]
    public async Task<IActionResult> Put(int id, [FromBody] TaskDefinitionDto task, CancellationToken ct)
    {
        if (id != task.Id) return BadRequest();
        var updated = await _service.UpdateAsync(id, task, ct);
        return updated is null ? NotFound() : Ok(updated);
    }
    /// <summary>
    /// Endpoint de la API ACH Interbank.
    /// </summary>

    [HttpDelete("{id}")]
    [Authorize(Policy = "CanManageAch")]
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
    {
        var deleted = await _service.DeleteAsync(id, ct);
        return deleted ? NoContent() : NotFound();
    }
}
