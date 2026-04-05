using Cfa.ACHInterbank.Application.ACH.Interfaces;
using Cfa.ACHInterbank.Application.ACH.Interfaces.Repositories;
using Cfa.ACHInterbank.Application.ACH.Models;
using Cfa.ACHInterbank.Application.DataBase;
using Cfa.ACHInterbank.Domain.Models.ACH;
using Cfa.ACHInterbank.Domain.Models.Configurations;

namespace Cfa.ACHInterbank.Application.ACH.Services;

[Scoped]
public class CompanyEntryDescriptionsService : ICompanyEntryDescriptionsService
{
    private readonly ICompanyEntryDescriptionsRepository _repository;
    private readonly IUnitOfWork _unitOfWork;

    public CompanyEntryDescriptionsService(ICompanyEntryDescriptionsRepository repository, IUnitOfWork unitOfWork)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
    }

    public Task<IReadOnlyList<CompanyEntryDescriptionAdminDto>> GetAllAsync(CancellationToken ct = default)
        => _repository.GetAllAsync(ct);

    public async Task<CompanyEntryDescriptionAdminDto> CreateAsync(CompanyEntryDescriptionUpsertRequest request, CancellationToken ct = default)
    {
        var validation = ValidateRequest(request);
        if (!string.IsNullOrEmpty(validation)) throw new ArgumentException(validation);

        var term = request.Term!.Trim().ToUpperInvariant();
        var sec = request.StandardEntryClassCode!.Trim().ToUpperInvariant();

        if (await _repository.ExistsByTermAsync(term, null, ct))
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

        await _repository.AddAsync(entity, ct);
        await _unitOfWork.CommitAsync(ct);

        return Map(entity);
    }

    public async Task<CompanyEntryDescriptionAdminDto> UpdateAsync(int id, CompanyEntryDescriptionUpsertRequest request, CancellationToken ct = default)
    {
        var validation = ValidateRequest(request);
        if (!string.IsNullOrEmpty(validation)) throw new ArgumentException(validation);

        var entity = await _repository.GetByIdAsync(id, ct)
            ?? throw new KeyNotFoundException("Registro no encontrado.");

        var term = request.Term!.Trim().ToUpperInvariant();
        var sec = request.StandardEntryClassCode!.Trim().ToUpperInvariant();

        if (await _repository.ExistsByTermAsync(term, id, ct))
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
        var entity = await _repository.GetByIdAsync(id, ct)
            ?? throw new KeyNotFoundException("Registro no encontrado.");

        await _repository.RemoveAsync(entity, ct);

        try
        {
            await _unitOfWork.CommitAsync(ct);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException("No se puede eliminar el registro porque está siendo utilizado.", ex);
        }
    }

    private static string? ValidateRequest(CompanyEntryDescriptionUpsertRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Term)) return "El término es obligatorio.";
        if (request.Term.Trim().Length > 12) return "El término no puede superar 12 caracteres.";
        if (string.IsNullOrWhiteSpace(request.Description)) return "La descripción es obligatoria.";
        if (request.Description.Trim().Length > 255) return "La descripción no puede superar 255 caracteres.";
        if (string.IsNullOrWhiteSpace(request.StandardEntryClassCode)) return "El código SEC es obligatorio.";

        var sec = request.StandardEntryClassCode.Trim().ToUpperInvariant();
        return sec != "PPD" && sec != "CCD" ? "El código SEC debe ser PPD o CCD." : null;
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
