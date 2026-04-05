using Cfa.ACHInterbank.Application.ACH.Interfaces;
using Cfa.ACHInterbank.Application.DataBase;
using Cfa.ACHInterbank.Domain.Models.ACH;
using Cfa.ACHInterbank.Domain.Models.Configurations;
using Cfa.ACHInterbank.Persistence.DataBase;
using Microsoft.EntityFrameworkCore;

namespace Cfa.ACHInterbank.Persistence.ACH.Services.Implementation;

[Scoped]
public class CatalogTypesService : ICatalogTypesService
{
    private readonly AchDbContext _context;
    private readonly IUnitOfWork _unitOfWork;

    public CatalogTypesService(AchDbContext context, IUnitOfWork unitOfWork)
    {
        _context = context;
        _unitOfWork = unitOfWork;
    }

    public async Task<IReadOnlyList<CatalogTypeItemDto>> GetAllAsync(string catalogType, CancellationToken ct = default)
    {
        var type = ParseCatalogType(catalogType);
        if (type is null)
        {
            throw new ArgumentException("Tipo de catálogo inválido.");
        }

        return await ListAsync(type.Value, ct);
    }

    public async Task<CatalogTypeItemDto> CreateAsync(string catalogType, CatalogTypeUpsertRequest request, CancellationToken ct = default)
    {
        var type = ParseCatalogType(catalogType);
        if (type is null)
        {
            throw new ArgumentException("Tipo de catálogo inválido.");
        }

        var validationError = ValidateRequest(request);
        if (!string.IsNullOrEmpty(validationError))
        {
            throw new ArgumentException(validationError);
        }

        var code = request.Code!.Trim().ToUpperInvariant();
        var exists = await ExistsAsync(type.Value, code, ct);
        if (exists)
        {
            throw new InvalidOperationException("Ya existe un registro con ese código.");
        }

        AddEntity(type.Value, code, request.Name!.Trim(), request.Description?.Trim());
        await _unitOfWork.CommitAsync(ct);

        return new CatalogTypeItemDto { Code = code, Name = request.Name!.Trim(), Description = request.Description?.Trim() };
    }

    public async Task<CatalogTypeItemDto> UpdateAsync(string catalogType, string code, CatalogTypeUpsertRequest request, CancellationToken ct = default)
    {
        var type = ParseCatalogType(catalogType);
        if (type is null)
        {
            throw new ArgumentException("Tipo de catálogo inválido.");
        }

        if (string.IsNullOrWhiteSpace(code))
        {
            throw new ArgumentException("El código es obligatorio.");
        }

        if (string.IsNullOrWhiteSpace(request.Name))
        {
            throw new ArgumentException("El nombre es obligatorio.");
        }

        var normalizedCode = code.Trim().ToUpperInvariant();
        var updated = await UpdateEntityAsync(type.Value, normalizedCode, request.Name.Trim(), request.Description?.Trim(), ct);
        if (!updated)
        {
            throw new KeyNotFoundException("Registro no encontrado.");
        }

        await _unitOfWork.CommitAsync(ct);
        return new CatalogTypeItemDto { Code = normalizedCode, Name = request.Name.Trim(), Description = request.Description?.Trim() };
    }

    public async Task DeleteAsync(string catalogType, string code, CancellationToken ct = default)
    {
        var type = ParseCatalogType(catalogType);
        if (type is null)
        {
            throw new ArgumentException("Tipo de catálogo inválido.");
        }

        if (string.IsNullOrWhiteSpace(code))
        {
            throw new ArgumentException("El código es obligatorio.");
        }

        var normalizedCode = code.Trim().ToUpperInvariant();
        var removed = await RemoveEntityAsync(type.Value, normalizedCode, ct);
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
        if (string.IsNullOrWhiteSpace(request.Code))
        {
            return "El código es obligatorio.";
        }

        if (string.IsNullOrWhiteSpace(request.Name))
        {
            return "El nombre es obligatorio.";
        }

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

    private async Task<List<CatalogTypeItemDto>> ListAsync(CatalogTypeKey key, CancellationToken ct)
    {
        return key switch
        {
            CatalogTypeKey.DocumentTypes => await _context.DocumentTypes.AsNoTracking().OrderBy(x => x.Code)
                .Select(x => new CatalogTypeItemDto { Code = x.Code, Name = x.Name, Description = x.Description }).ToListAsync(ct),
            CatalogTypeKey.GenderTypes => await _context.GenderTypes.AsNoTracking().OrderBy(x => x.Code)
                .Select(x => new CatalogTypeItemDto { Code = x.Code, Name = x.Name, Description = x.Description }).ToListAsync(ct),
            CatalogTypeKey.PersonTypes => await _context.PersonTypes.AsNoTracking().OrderBy(x => x.Code)
                .Select(x => new CatalogTypeItemDto { Code = x.Code, Name = x.Name, Description = x.Description }).ToListAsync(ct),
            CatalogTypeKey.PhoneTypes => await _context.PhoneTypes.AsNoTracking().OrderBy(x => x.Code)
                .Select(x => new CatalogTypeItemDto { Code = x.Code, Name = x.Name, Description = x.Description }).ToListAsync(ct),
            CatalogTypeKey.EmailTypes => await _context.EmailTypes.AsNoTracking().OrderBy(x => x.Code)
                .Select(x => new CatalogTypeItemDto { Code = x.Code, Name = x.Name, Description = x.Description }).ToListAsync(ct),
            CatalogTypeKey.AddressTypes => await _context.AddressTypes.AsNoTracking().OrderBy(x => x.Code)
                .Select(x => new CatalogTypeItemDto { Code = x.Code, Name = x.Name, Description = x.Description }).ToListAsync(ct),
            CatalogTypeKey.TransactionCodes => await _context.TransactionCodes.AsNoTracking().OrderBy(x => x.Code)
                .Select(x => new CatalogTypeItemDto { Code = x.Code, Name = x.Name, Description = x.Description }).ToListAsync(ct),
            _ => []
        };
    }

    private async Task<bool> ExistsAsync(CatalogTypeKey key, string code, CancellationToken ct)
        => key switch
        {
            CatalogTypeKey.DocumentTypes => await _context.DocumentTypes.AnyAsync(x => x.Code == code, ct),
            CatalogTypeKey.GenderTypes => await _context.GenderTypes.AnyAsync(x => x.Code == code, ct),
            CatalogTypeKey.PersonTypes => await _context.PersonTypes.AnyAsync(x => x.Code == code, ct),
            CatalogTypeKey.PhoneTypes => await _context.PhoneTypes.AnyAsync(x => x.Code == code, ct),
            CatalogTypeKey.EmailTypes => await _context.EmailTypes.AnyAsync(x => x.Code == code, ct),
            CatalogTypeKey.AddressTypes => await _context.AddressTypes.AnyAsync(x => x.Code == code, ct),
            CatalogTypeKey.TransactionCodes => await _context.TransactionCodes.AnyAsync(x => x.Code == code, ct),
            _ => false
        };

    private void AddEntity(CatalogTypeKey key, string code, string name, string? description)
    {
        switch (key)
        {
            case CatalogTypeKey.DocumentTypes:
                _context.DocumentTypes.Add(new DocumentTypeCatalog { Code = code, Name = name, Description = description });
                return;
            case CatalogTypeKey.GenderTypes:
                _context.GenderTypes.Add(new GenderCatalog { Code = code, Name = name, Description = description });
                return;
            case CatalogTypeKey.PersonTypes:
                _context.PersonTypes.Add(new PersonTypeCatalog { Code = code, Name = name, Description = description });
                return;
            case CatalogTypeKey.PhoneTypes:
                _context.PhoneTypes.Add(new PhoneTypeCatalog { Code = code, Name = name, Description = description });
                return;
            case CatalogTypeKey.EmailTypes:
                _context.EmailTypes.Add(new EmailTypeCatalog { Code = code, Name = name, Description = description });
                return;
            case CatalogTypeKey.AddressTypes:
                _context.AddressTypes.Add(new AddressTypeCatalog { Code = code, Name = name, Description = description });
                return;
            case CatalogTypeKey.TransactionCodes:
                _context.TransactionCodes.Add(new TransactionCodeCatalog { Code = code, Name = name, Description = description });
                return;
        }
    }

    private async Task<bool> UpdateEntityAsync(CatalogTypeKey key, string code, string name, string? description, CancellationToken ct)
    {
        switch (key)
        {
            case CatalogTypeKey.DocumentTypes:
                var document = await _context.DocumentTypes.FirstOrDefaultAsync(x => x.Code == code, ct);
                if (document is null) return false;
                document.Name = name;
                document.Description = description;
                return true;
            case CatalogTypeKey.GenderTypes:
                var gender = await _context.GenderTypes.FirstOrDefaultAsync(x => x.Code == code, ct);
                if (gender is null) return false;
                gender.Name = name;
                gender.Description = description;
                return true;
            case CatalogTypeKey.PersonTypes:
                var person = await _context.PersonTypes.FirstOrDefaultAsync(x => x.Code == code, ct);
                if (person is null) return false;
                person.Name = name;
                person.Description = description;
                return true;
            case CatalogTypeKey.PhoneTypes:
                var phone = await _context.PhoneTypes.FirstOrDefaultAsync(x => x.Code == code, ct);
                if (phone is null) return false;
                phone.Name = name;
                phone.Description = description;
                return true;
            case CatalogTypeKey.EmailTypes:
                var email = await _context.EmailTypes.FirstOrDefaultAsync(x => x.Code == code, ct);
                if (email is null) return false;
                email.Name = name;
                email.Description = description;
                return true;
            case CatalogTypeKey.AddressTypes:
                var address = await _context.AddressTypes.FirstOrDefaultAsync(x => x.Code == code, ct);
                if (address is null) return false;
                address.Name = name;
                address.Description = description;
                return true;
            case CatalogTypeKey.TransactionCodes:
                var transactionCode = await _context.TransactionCodes.FirstOrDefaultAsync(x => x.Code == code, ct);
                if (transactionCode is null) return false;
                transactionCode.Name = name;
                transactionCode.Description = description;
                return true;
            default:
                return false;
        }
    }

    private async Task<bool> RemoveEntityAsync(CatalogTypeKey key, string code, CancellationToken ct)
    {
        switch (key)
        {
            case CatalogTypeKey.DocumentTypes:
                var document = await _context.DocumentTypes.FirstOrDefaultAsync(x => x.Code == code, ct);
                if (document is null) return false;
                _context.DocumentTypes.Remove(document);
                return true;
            case CatalogTypeKey.GenderTypes:
                var gender = await _context.GenderTypes.FirstOrDefaultAsync(x => x.Code == code, ct);
                if (gender is null) return false;
                _context.GenderTypes.Remove(gender);
                return true;
            case CatalogTypeKey.PersonTypes:
                var person = await _context.PersonTypes.FirstOrDefaultAsync(x => x.Code == code, ct);
                if (person is null) return false;
                _context.PersonTypes.Remove(person);
                return true;
            case CatalogTypeKey.PhoneTypes:
                var phone = await _context.PhoneTypes.FirstOrDefaultAsync(x => x.Code == code, ct);
                if (phone is null) return false;
                _context.PhoneTypes.Remove(phone);
                return true;
            case CatalogTypeKey.EmailTypes:
                var email = await _context.EmailTypes.FirstOrDefaultAsync(x => x.Code == code, ct);
                if (email is null) return false;
                _context.EmailTypes.Remove(email);
                return true;
            case CatalogTypeKey.AddressTypes:
                var address = await _context.AddressTypes.FirstOrDefaultAsync(x => x.Code == code, ct);
                if (address is null) return false;
                _context.AddressTypes.Remove(address);
                return true;
            case CatalogTypeKey.TransactionCodes:
                var transactionCode = await _context.TransactionCodes.FirstOrDefaultAsync(x => x.Code == code, ct);
                if (transactionCode is null) return false;
                _context.TransactionCodes.Remove(transactionCode);
                return true;
            default:
                return false;
        }
    }

    private enum CatalogTypeKey
    {
        DocumentTypes,
        GenderTypes,
        PersonTypes,
        PhoneTypes,
        EmailTypes,
        AddressTypes,
        TransactionCodes
    }
}
