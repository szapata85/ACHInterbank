using Cfa.ACHInterbank.Domain.Entities.Transactions.Dtos;
using Cfa.ACHInterbank.Domain.Models.ACH;
using Cfa.ACHInterbank.Persistence.DataBase;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Cfa.ACHInterbank.Api.Controllers;

[ApiController]
[Route("[controller]")]
public class FinancialInstitutionsController : Controller
{
    private readonly AchDbContext _context;

    public FinancialInstitutionsController(AchDbContext context)
    {
        _context = context;
    }

    // GET: api/FinancialInstitutions
    [HttpGet]
    public async Task<ActionResult<IEnumerable<FinancialInstitutionDto>>> GetAll()
    {
        var items = await _context.FinancialInstitutions
            .Select(f => new FinancialInstitutionDto
            {
                Id = f.Id,
                Name = f.Name,
                Code = f.Code,
                ClearingHouseId = f.ClearingHouseId
            })
            .ToListAsync();

        return Ok(items);
    }

    // GET: api/FinancialInstitutions/5
    [HttpGet("{id:int}")]
    public async Task<ActionResult<FinancialInstitutionDto>> GetById(int id)
    {
        var f = await _context.FinancialInstitutions
            .Where(x => x.Id == id)
            .Select(f => new FinancialInstitutionDto
            {
                Id = f.Id,
                Name = f.Name,
                Code = f.Code,
                ClearingHouseId = f.ClearingHouseId
            })
            .FirstOrDefaultAsync();

        if (f == null) return NotFound();
        return Ok(f);
    }

    // POST: api/FinancialInstitutions
    [HttpPost]
    public async Task<ActionResult<FinancialInstitutionDto>> Create([FromBody] FinancialInstitutionDto dto)
    {
        // Validar que la cámara existe
        bool exists = await _context.ClearingHouses.AnyAsync(ch => ch.Id == dto.ClearingHouseId);
        if (!exists) return BadRequest("ClearingHouse no encontrado.");

        var entity = new FinancialInstitution
        {
            Name = dto.Name,
            Code = dto.Code,
            ClearingHouseId = dto.ClearingHouseId
        };

        _context.FinancialInstitutions.Add(entity);
        await _context.SaveChangesAsync();

        dto.Id = entity.Id;
        return CreatedAtAction(nameof(GetById), new { id = dto.Id }, dto);
    }

    // PUT: api/FinancialInstitutions/5
    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, [FromBody] FinancialInstitutionDto dto)
    {
        if (id != dto.Id) return BadRequest();

        var entity = await _context.FinancialInstitutions.FindAsync(id);
        if (entity == null) return NotFound();

        entity.Name = dto.Name;
        entity.Code = dto.Code;
        entity.ClearingHouseId = dto.ClearingHouseId;

        await _context.SaveChangesAsync();
        return NoContent();
    }

    // DELETE: api/FinancialInstitutions/5
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        var entity = await _context.FinancialInstitutions.FindAsync(id);
        if (entity == null) return NotFound();

        _context.FinancialInstitutions.Remove(entity);
        await _context.SaveChangesAsync();
        return NoContent();
    }
}
