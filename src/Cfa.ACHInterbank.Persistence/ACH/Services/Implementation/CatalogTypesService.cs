using Cfa.ACHInterbank.Application.ACH.Interfaces.Repositories;
using Cfa.ACHInterbank.Application.ACH.Models;
using Cfa.ACHInterbank.Domain.Models.ACH;
using Cfa.ACHInterbank.Domain.Models.Configurations;
using Cfa.ACHInterbank.Persistence.DataBase;
using Microsoft.EntityFrameworkCore;

namespace Cfa.ACHInterbank.Persistence.ACH.Services.Implementation;

[Scoped]
public class CatalogTypesRepository : ICatalogTypesRepository
{
    private readonly AchDbContext _context;

    public CatalogTypesRepository(AchDbContext context)
    {
        _context = context;
    }

    public async Task<IReadOnlyList<CatalogTypeItemDto>> ListAsync(CatalogTypeKey key, CancellationToken ct = default)
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

    public async Task<bool> ExistsAsync(CatalogTypeKey key, string code, CancellationToken ct = default)
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

    public Task AddAsync(CatalogTypeKey key, string code, string name, string? description, CancellationToken ct = default)
    {
        switch (key)
        {
            case CatalogTypeKey.DocumentTypes:
                _context.DocumentTypes.Add(new DocumentTypeCatalog { Code = code, Name = name, Description = description });
                break;
            case CatalogTypeKey.GenderTypes:
                _context.GenderTypes.Add(new GenderCatalog { Code = code, Name = name, Description = description });
                break;
            case CatalogTypeKey.PersonTypes:
                _context.PersonTypes.Add(new PersonTypeCatalog { Code = code, Name = name, Description = description });
                break;
            case CatalogTypeKey.PhoneTypes:
                _context.PhoneTypes.Add(new PhoneTypeCatalog { Code = code, Name = name, Description = description });
                break;
            case CatalogTypeKey.EmailTypes:
                _context.EmailTypes.Add(new EmailTypeCatalog { Code = code, Name = name, Description = description });
                break;
            case CatalogTypeKey.AddressTypes:
                _context.AddressTypes.Add(new AddressTypeCatalog { Code = code, Name = name, Description = description });
                break;
            case CatalogTypeKey.TransactionCodes:
                _context.TransactionCodes.Add(new TransactionCodeCatalog { Code = code, Name = name, Description = description });
                break;
        }

        return Task.CompletedTask;
    }

    public async Task<bool> UpdateAsync(CatalogTypeKey key, string code, string name, string? description, CancellationToken ct = default)
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

    public async Task<bool> RemoveAsync(CatalogTypeKey key, string code, CancellationToken ct = default)
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
}
