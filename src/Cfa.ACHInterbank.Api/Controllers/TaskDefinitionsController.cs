using Cfa.ACHInterbank.Domain.Entities.SchedulerTask;
using Cfa.ACHInterbank.Persistence.DataBase;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Cfa.ACHInterbank.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class TaskDefinitionsController : Controller
{
    private readonly AchDbContext _context;

    public TaskDefinitionsController(AchDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<TaskDefinition>>> Get()
    {
        return await _context.TaskDefinitions
            .Include(t => t.Parameters)
            .ToListAsync();
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<TaskDefinition>> Get(int id)
    {
        var task = await _context.TaskDefinitions
            .Include(t => t.Parameters)
            .FirstOrDefaultAsync(t => t.Id == id);

        if (task == null) return NotFound();
        return task;
    }

    [HttpPost]
    public async Task<ActionResult<TaskDefinition>> Post(TaskDefinition task)
    {
        _context.TaskDefinitions.Add(task);
        await _context.SaveChangesAsync();
        return CreatedAtAction(nameof(Get), new { id = task.Id }, task);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Put(int id, TaskDefinition task)
    {
        if (id != task.Id) return BadRequest();
        _context.Entry(task).State = EntityState.Modified;
        await _context.SaveChangesAsync();
        return NoContent();
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        var task = await _context.TaskDefinitions.FindAsync(id);
        if (task == null) return NotFound();

        _context.TaskDefinitions.Remove(task);
        await _context.SaveChangesAsync();
        return NoContent();
    }
}
