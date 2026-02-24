using Cfa.ACHInterbank.Domain.Models.ACH;
using Cfa.ACHInterbank.Persistence.DataBase;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Cfa.ACHInterbank.Api.Controllers;

[ApiController]
[Route("company-entry-descriptions")]
[Authorize]
public class CompanyEntryDescriptionsController : ControllerBase
{
    private readonly AchDbContext _context;

    public CompanyEntryDescriptionsController(AchDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    [Authorize(Policy = "CanManageAch")]
    public async Task<IActionResult> GetAll(CancellationToken ct = default)
    {
        var rows = await _context.CompanyEntryDescriptionCatalogs
            .AsNoTracking()
            .OrderBy(x => x.Term)
            .Select(x => new CompanyEntryDescriptionAdminDto
            {
                Id = x.Id,
                Term = x.Term,
                Description = x.Description,
                StandardEntryClassCode = x.StandardEntryClassCode,
                IsActive = x.IsActive
            })
            .ToListAsync(ct);

        return Ok(rows);
    }

    [HttpPost]
    [Authorize(Policy = "CanManageAch")]
    public async Task<IActionResult> Create([FromBody] CompanyEntryDescriptionUpsertRequest request, CancellationToken ct = default)
    {
        var validation = ValidateRequest(request);
        if (!string.IsNullOrEmpty(validation))
        {
            return BadRequest(validation);
        }

        var term = request.Term!.Trim().ToUpperInvariant();
        var sec = request.StandardEntryClassCode!.Trim().ToUpperInvariant();

        var exists = await _context.CompanyEntryDescriptionCatalogs.AnyAsync(x => x.Term == term, ct);
        if (exists)
        {
            return Conflict("Ya existe un concepto con ese término.");
        }

        var entity = new CompanyEntryDescriptionCatalog
        {
            Term = term,
            Description = request.Description!.Trim(),
            StandardEntryClassCode = sec,
            IsActive = request.IsActive
        };

        _context.CompanyEntryDescriptionCatalogs.Add(entity);
        await _context.SaveChangesAsync(ct);

        return Ok(Map(entity));
    }

    [HttpPut("{id:int}")]
    [Authorize(Policy = "CanManageAch")]
    public async Task<IActionResult> Update(int id, [FromBody] CompanyEntryDescriptionUpsertRequest request, CancellationToken ct = default)
    {
        var validation = ValidateRequest(request);
        if (!string.IsNullOrEmpty(validation))
        {
            return BadRequest(validation);
        }

        var entity = await _context.CompanyEntryDescriptionCatalogs.FirstOrDefaultAsync(x => x.Id == id, ct);
        if (entity is null)
        {
            return NotFound("Registro no encontrado.");
        }

        var term = request.Term!.Trim().ToUpperInvariant();
        var sec = request.StandardEntryClassCode!.Trim().ToUpperInvariant();

        var exists = await _context.CompanyEntryDescriptionCatalogs.AnyAsync(x => x.Id != id && x.Term == term, ct);
        if (exists)
        {
            return Conflict("Ya existe un concepto con ese término.");
        }

        entity.Term = term;
        entity.Description = request.Description!.Trim();
        entity.StandardEntryClassCode = sec;
        entity.IsActive = request.IsActive;

        await _context.SaveChangesAsync(ct);
        return Ok(Map(entity));
    }

    [HttpDelete("{id:int}")]
    [Authorize(Policy = "CanManageAch")]
    public async Task<IActionResult> Delete(int id, CancellationToken ct = default)
    {
        var entity = await _context.CompanyEntryDescriptionCatalogs.FirstOrDefaultAsync(x => x.Id == id, ct);
        if (entity is null)
        {
            return NotFound("Registro no encontrado.");
        }

        _context.CompanyEntryDescriptionCatalogs.Remove(entity);

        try
        {
            await _context.SaveChangesAsync(ct);
        }
        catch (DbUpdateException)
        {
            return Conflict("No se puede eliminar el registro porque está siendo utilizado.");
        }

        return NoContent();
    }

    private static string? ValidateRequest(CompanyEntryDescriptionUpsertRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Term))
        {
            return "El término es obligatorio.";
        }

        if (request.Term.Trim().Length > 12)
        {
            return "El término no puede superar 12 caracteres.";
        }

        if (string.IsNullOrWhiteSpace(request.Description))
        {
            return "La descripción es obligatoria.";
        }

        if (request.Description.Trim().Length > 255)
        {
            return "La descripción no puede superar 255 caracteres.";
        }

        if (string.IsNullOrWhiteSpace(request.StandardEntryClassCode))
        {
            return "El código SEC es obligatorio.";
        }

        var sec = request.StandardEntryClassCode.Trim().ToUpperInvariant();
        if (sec != "PPD" && sec != "CCD")
        {
            return "El código SEC debe ser PPD o CCD.";
        }

        return null;
    }

    private static CompanyEntryDescriptionAdminDto Map(CompanyEntryDescriptionCatalog entity)
        => new()
        {
            Id = entity.Id,
            Term = entity.Term,
            Description = entity.Description,
            StandardEntryClassCode = entity.StandardEntryClassCode,
            IsActive = entity.IsActive
        };
}

public class CompanyEntryDescriptionAdminDto
{
    public int Id { get; set; }
    public string Term { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string StandardEntryClassCode { get; set; } = string.Empty;
    public bool IsActive { get; set; }
}

public class CompanyEntryDescriptionUpsertRequest
{
    public string? Term { get; set; }
    public string? Description { get; set; }
    public string? StandardEntryClassCode { get; set; }
    public bool IsActive { get; set; } = true;
}
