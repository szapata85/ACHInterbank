using Cfa.ACHInterbank.Application.ACH.Interfaces.Repositories;
using Cfa.ACHInterbank.Domain.Models.ACH;
using Cfa.ACHInterbank.Domain.Models.Configurations;
using Cfa.ACHInterbank.Persistence.DataBase;
using Microsoft.EntityFrameworkCore;

namespace Cfa.ACHInterbank.Persistence.ACH.Repositories.Implementation;

[Scoped]
public class AchCustomerRepository : IAchCustomerRepository
{
    private readonly AchDbContext _context;

    public AchCustomerRepository(AchDbContext context)
    {
        _context = context;
    }

    public Task<string> ResolveDocumentTypeCodeAsync(string preferredCode, CancellationToken ct = default)
        => ResolveCatalogCodeAsync(_context.DocumentTypes, preferredCode, ct);

    public Task<string> ResolvePersonTypeCodeAsync(string preferredCode, CancellationToken ct = default)
        => ResolveCatalogCodeAsync(_context.PersonTypes, preferredCode, ct);

    public Task<Customer?> GetByDocumentAsync(string documentType, string documentNumber, CancellationToken ct = default)
    {
        return _context.Customers
            .Include(c => c.Accounts)
            .FirstOrDefaultAsync(c => c.DocumentType == documentType && c.DocumentNumber == documentNumber, ct);
    }

    public async Task<IReadOnlyList<Customer>> GetByDocumentNumberAsync(string documentNumber, CancellationToken ct = default)
    {
        return await _context.Customers
            .Include(c => c.Accounts)
            .Where(c => c.DocumentNumber == documentNumber)
            .ToListAsync(ct);
    }

    public Task AddAsync(Customer customer, CancellationToken ct = default)
    {
        _context.Customers.Add(customer);
        return Task.CompletedTask;
    }

    public async Task<Customer?> FindBySourceAccountNumberAsync(string sourceAccountNumber, CancellationToken ct = default)
    {
        var normalized = (sourceAccountNumber ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(normalized))
        {
            return null;
        }

        var tracked = _context.Customers.Local.FirstOrDefault(c => c.Accounts.Any(a => a.AccountNumber == normalized));
        if (tracked is not null)
        {
            return tracked;
        }

        return await _context.Customers
            .Include(c => c.Accounts)
            .FirstOrDefaultAsync(c => c.Accounts.Any(a => a.AccountNumber == normalized), ct);
    }

    private static async Task<string> ResolveCatalogCodeAsync<TCatalog>(IQueryable<TCatalog> source, string preferredCode, CancellationToken ct)
        where TCatalog : class
    {
        var normalized = (preferredCode ?? string.Empty).Trim().ToUpperInvariant();
        if (!string.IsNullOrWhiteSpace(normalized))
        {
            var exact = await source
                .Where(x => EF.Property<string>(x, "Code") == normalized)
                .Select(x => EF.Property<string>(x, "Code"))
                .FirstOrDefaultAsync(ct);
            if (!string.IsNullOrWhiteSpace(exact))
            {
                return exact;
            }
        }

        var fallback = await source
            .OrderBy(x => EF.Property<string>(x, "Code"))
            .Select(x => EF.Property<string>(x, "Code"))
            .FirstOrDefaultAsync(ct);

        return fallback
            ?? throw new InvalidOperationException("No hay catálogos configurados para resolver el tipo requerido.");
    }
}
