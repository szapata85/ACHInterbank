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
                ClearingHouseId = f.ClearingHouseId,
                IsDefaultSource = f.IsDefaultSource
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
                ClearingHouseId = f.ClearingHouseId,
                IsDefaultSource = f.IsDefaultSource
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

        // Si se marca como predeterminado, desmarcar a los demás
        if (dto.IsDefaultSource)
        {
            var all = await _context.FinancialInstitutions.ToListAsync();
            foreach (var fi in all)
                fi.IsDefaultSource = false;
        }
        else
        {
            // Garantizar que siempre exista al menos un default
            bool hasDefault = await _context.FinancialInstitutions.AnyAsync(fi => fi.IsDefaultSource);
            if (!hasDefault)
                dto.IsDefaultSource = true;
        }

        var entity = new FinancialInstitution
        {
            Name = dto.Name,
            Code = dto.Code,
            ClearingHouseId = dto.ClearingHouseId,
            IsDefaultSource = dto.IsDefaultSource
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

        if (dto.IsDefaultSource)
        {
            // Desmarcar a todos los demás
            var all = await _context.FinancialInstitutions.ToListAsync();
            foreach (var fi in all)
                fi.IsDefaultSource = fi.Id == id;
        }
        else
        {
            // No permitir que queden todas en false
            bool otherDefault = await _context.FinancialInstitutions
                .AnyAsync(fi => fi.Id != id && fi.IsDefaultSource);
            if (!otherDefault)
                return BadRequest("Debe existir al menos una entidad financiera por defecto.");
            entity.IsDefaultSource = false;
        }

        await _context.SaveChangesAsync();
        return NoContent();
    }

    // DELETE: api/FinancialInstitutions/5
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        var entity = await _context.FinancialInstitutions.FindAsync(id);
        if (entity == null) return NotFound();

        // Evitar eliminar la última default
        if (entity.IsDefaultSource)
        {
            bool otherDefault = await _context.FinancialInstitutions
                .AnyAsync(fi => fi.Id != id && fi.IsDefaultSource);
            if (!otherDefault)
                return BadRequest("No se puede eliminar la única entidad predeterminada.");
        }

        _context.FinancialInstitutions.Remove(entity);
        await _context.SaveChangesAsync();
        return NoContent();
    }
}

