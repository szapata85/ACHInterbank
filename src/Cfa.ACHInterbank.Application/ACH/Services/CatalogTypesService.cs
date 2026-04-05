using Cfa.ACHInterbank.Application.ACH.Interfaces;
using Cfa.ACHInterbank.Application.ACH.Interfaces.Repositories;
using Cfa.ACHInterbank.Application.DataBase;
using Cfa.ACHInterbank.Domain.Models.ACH;
using Cfa.ACHInterbank.Domain.Models.Configurations;
using Microsoft.EntityFrameworkCore;

namespace Cfa.ACHInterbank.Application.ACH.Services;

[Scoped]
public class CatalogTypesService : ICatalogTypesService
{
    private readonly ICatalogTypesRepository _repository;
    private readonly IUnitOfWork _unitOfWork;

    public CatalogTypesService(ICatalogTypesRepository repository, IUnitOfWork unitOfWork)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
    }

    public async Task<IReadOnlyList<CatalogTypeItemDto>> GetAllAsync(string catalogType, CancellationToken ct = default)
    {
        var type = ParseCatalogType(catalogType);
        if (type is null)
        {
            throw new ArgumentException("Tipo de catálogo inválido.");
        }

        return await _repository.ListAsync(type.Value, ct);
    }

    public async Task<CatalogTypeItemDto> CreateAsync(string catalogType, CatalogTypeUpsertRequest request, CancellationToken ct = default)
    {
        var type = ParseCatalogType(catalogType) ?? throw new ArgumentException("Tipo de catálogo inválido.");

        var validationError = ValidateRequest(request);
        if (!string.IsNullOrEmpty(validationError))
        {
            throw new ArgumentException(validationError);
        }

        var code = request.Code!.Trim().ToUpperInvariant();
        if (await _repository.ExistsAsync(type.Value, code, ct))
        {
            throw new InvalidOperationException("Ya existe un registro con ese código.");
        }

        await _repository.AddAsync(type.Value, code, request.Name!.Trim(), request.Description?.Trim(), ct);
        await _unitOfWork.CommitAsync(ct);

        return new CatalogTypeItemDto { Code = code, Name = request.Name!.Trim(), Description = request.Description?.Trim() };
    }

    public async Task<CatalogTypeItemDto> UpdateAsync(string catalogType, string code, CatalogTypeUpsertRequest request, CancellationToken ct = default)
    {
        var type = ParseCatalogType(catalogType) ?? throw new ArgumentException("Tipo de catálogo inválido.");

        if (string.IsNullOrWhiteSpace(code))
        {
            throw new ArgumentException("El código es obligatorio.");
        }

        if (string.IsNullOrWhiteSpace(request.Name))
        {
            throw new ArgumentException("El nombre es obligatorio.");
        }

        var normalizedCode = code.Trim().ToUpperInvariant();
        var updated = await _repository.UpdateAsync(type.Value, normalizedCode, request.Name.Trim(), request.Description?.Trim(), ct);
        if (!updated)
        {
            throw new KeyNotFoundException("Registro no encontrado.");
        }

        await _unitOfWork.CommitAsync(ct);
        return new CatalogTypeItemDto { Code = normalizedCode, Name = request.Name.Trim(), Description = request.Description?.Trim() };
    }

    public async Task DeleteAsync(string catalogType, string code, CancellationToken ct = default)
    {
        var type = ParseCatalogType(catalogType) ?? throw new ArgumentException("Tipo de catálogo inválido.");

        if (string.IsNullOrWhiteSpace(code))
        {
            throw new ArgumentException("El código es obligatorio.");
        }

        var normalizedCode = code.Trim().ToUpperInvariant();
        var removed = await _repository.RemoveAsync(type.Value, normalizedCode, ct);
        if (!removed)
        {
            throw new KeyNotFoundException("Registro no encontrado.");
        }

        try
        {
            await _unitOfWork.CommitAsync(ct);
        }
        catch (DbUpdateException ex)
        {
            throw new InvalidOperationException("No se puede eliminar el registro porque está siendo utilizado.", ex);
        }
    }

    private static string? ValidateRequest(CatalogTypeUpsertRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Code)) return "El código es obligatorio.";
        if (string.IsNullOrWhiteSpace(request.Name)) return "El nombre es obligatorio.";
        return null;
    }

    private static CatalogTypeKey? ParseCatalogType(string value)
        => value.Trim().ToLowerInvariant() switch
        {
            "document-types" => CatalogTypeKey.DocumentTypes,
            "gender-types" => CatalogTypeKey.GenderTypes,
            "person-types" => CatalogTypeKey.PersonTypes,
            "phone-types" => CatalogTypeKey.PhoneTypes,
            "email-types" => CatalogTypeKey.EmailTypes,
            "address-types" => CatalogTypeKey.AddressTypes,
            "transaction-codes" => CatalogTypeKey.TransactionCodes,
            _ => null
        };
}
