using Cfa.ACHInterbank.Application.ACH.Interfaces;
using Cfa.ACHInterbank.Application.ACH.Models;
using Cfa.ACHInterbank.Application.DataBase;
using Cfa.ACHInterbank.Domain.Models.ACH;
using Cfa.ACHInterbank.Domain.Models.Configurations;
using Cfa.ACHInterbank.Persistence.DataBase;
using Microsoft.EntityFrameworkCore;

namespace Cfa.ACHInterbank.Persistence.ACH.Services.Implementation;

[Scoped]
public class CompanyEntryDescriptionsService : ICompanyEntryDescriptionsService
{
    private readonly AchDbContext _context;
    private readonly IUnitOfWork _unitOfWork;

    public CompanyEntryDescriptionsService(AchDbContext context, IUnitOfWork unitOfWork)
    {
        _context = context;
        _unitOfWork = unitOfWork;
    }

    public async Task<IReadOnlyList<CompanyEntryDescriptionAdminDto>> GetAllAsync(CancellationToken ct = default)
    {
        return await _context.CompanyEntryDescriptionCatalogs
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
    }

    public async Task<CompanyEntryDescriptionAdminDto> CreateAsync(CompanyEntryDescriptionUpsertRequest request, CancellationToken ct = default)
    {
        var validation = ValidateRequest(request);
        if (!string.IsNullOrEmpty(validation))
        {
            throw new ArgumentException(validation);
        }

        var term = request.Term!.Trim().ToUpperInvariant();
        var sec = request.StandardEntryClassCode!.Trim().ToUpperInvariant();

        var exists = await _context.CompanyEntryDescriptionCatalogs.AnyAsync(x => x.Term == term, ct);
        if (exists)
        {
            throw new InvalidOperationException("Ya existe un concepto con ese término.");
        }

        var entity = new CompanyEntryDescriptionCatalog
        {
            Term = term,
            Description = request.Description!.Trim(),
            StandardEntryClassCode = sec,
            IsActive = request.IsActive
        };

        _context.CompanyEntryDescriptionCatalogs.Add(entity);
        await _unitOfWork.CommitAsync(ct);

        return Map(entity);
    }

    public async Task<CompanyEntryDescriptionAdminDto> UpdateAsync(int id, CompanyEntryDescriptionUpsertRequest request, CancellationToken ct = default)
    {
        var validation = ValidateRequest(request);
        if (!string.IsNullOrEmpty(validation))
        {
            throw new ArgumentException(validation);
        }

        var entity = await _context.CompanyEntryDescriptionCatalogs.FirstOrDefaultAsync(x => x.Id == id, ct);
        if (entity is null)
        {
            throw new KeyNotFoundException("Registro no encontrado.");
        }

        var term = request.Term!.Trim().ToUpperInvariant();
        var sec = request.StandardEntryClassCode!.Trim().ToUpperInvariant();

        var exists = await _context.CompanyEntryDescriptionCatalogs.AnyAsync(x => x.Id != id && x.Term == term, ct);
        if (exists)
        {
            throw new InvalidOperationException("Ya existe un concepto con ese término.");
        }

        entity.Term = term;
        entity.Description = request.Description!.Trim();
        entity.StandardEntryClassCode = sec;
        entity.IsActive = request.IsActive;

        await _unitOfWork.CommitAsync(ct);
        return Map(entity);
    }

    public async Task DeleteAsync(int id, CancellationToken ct = default)
    {
        var entity = await _context.CompanyEntryDescriptionCatalogs.FirstOrDefaultAsync(x => x.Id == id, ct);
        if (entity is null)
        {
            throw new KeyNotFoundException("Registro no encontrado.");
        }

        _context.CompanyEntryDescriptionCatalogs.Remove(entity);

        try
        {
            await _unitOfWork.CommitAsync(ct);
        }
        catch (DbUpdateException ex)
        {
            throw new InvalidOperationException("No se puede eliminar el registro porque está siendo utilizado.", ex);
        }
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
