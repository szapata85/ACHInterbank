using Cfa.ACHInterbank.Application.ACH.Interfaces.Repositories;
using Cfa.ACHInterbank.Application.ACH.Models;
using Cfa.ACHInterbank.Domain.Models.ACH;
using Cfa.ACHInterbank.Domain.Models.Configurations;
using Cfa.ACHInterbank.Persistence.DataBase;
using Microsoft.EntityFrameworkCore;

namespace Cfa.ACHInterbank.Persistence.ACH.Services.Implementation;

[Scoped]
public class CompanyEntryDescriptionsRepository : ICompanyEntryDescriptionsRepository
{
    private readonly AchDbContext _context;

    public CompanyEntryDescriptionsRepository(AchDbContext context)
    {
        _context = context;
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

    public Task<bool> ExistsByTermAsync(string term, int? excludingId = null, CancellationToken ct = default)
    {
        return excludingId.HasValue
            ? _context.CompanyEntryDescriptionCatalogs.AnyAsync(x => x.Id != excludingId.Value && x.Term == term, ct)
            : _context.CompanyEntryDescriptionCatalogs.AnyAsync(x => x.Term == term, ct);
    }

    public Task<CompanyEntryDescriptionCatalog?> GetByIdAsync(int id, CancellationToken ct = default)
        => _context.CompanyEntryDescriptionCatalogs.FirstOrDefaultAsync(x => x.Id == id, ct);

    public Task AddAsync(CompanyEntryDescriptionCatalog entity, CancellationToken ct = default)
    {
        _context.CompanyEntryDescriptionCatalogs.Add(entity);
        return Task.CompletedTask;
    }

    public Task RemoveAsync(CompanyEntryDescriptionCatalog entity, CancellationToken ct = default)
    {
        _context.CompanyEntryDescriptionCatalogs.Remove(entity);
        return Task.CompletedTask;
    }
}
