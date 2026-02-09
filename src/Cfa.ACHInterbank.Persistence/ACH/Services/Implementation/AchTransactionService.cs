using Cfa.ACHInterbank.Application.ACH.Interfaces;
using Cfa.ACHInterbank.Application.ACH.Models;
using Cfa.ACHInterbank.Domain.Entities.Transactions.Dtos;
using Cfa.ACHInterbank.Domain.Entities.Transactions.Enums;
using Cfa.ACHInterbank.Domain.Models.ACH;
using Cfa.ACHInterbank.Domain.Models.Configurations;
using Cfa.ACHInterbank.Persistence.DataBase;
using Microsoft.EntityFrameworkCore;

namespace Cfa.ACHInterbank.Persistence.ACH.Services.Implementation;

[Scoped]
public class AchTransactionService : IAchTransactionService
{
    private readonly AchDbContext _context;
    private readonly IBankHoliday _holidayService;
    private readonly ITransactionValidator _transactionValidator;
    private readonly IBatchResolver _batchResolver;
    private readonly ITransactionPersister _transactionPersister;
    private readonly IPrenotificationHandler _prenotificationHandler;

    public AchTransactionService(
        AchDbContext context,
        IBankHoliday holidayService,
        ITransactionValidator transactionValidator,
        IBatchResolver batchResolver,
        ITransactionPersister transactionPersister,
        IPrenotificationHandler prenotificationHandler)
    {
        _context = context;
        _holidayService = holidayService;
        _transactionValidator = transactionValidator;
        _batchResolver = batchResolver;
        _transactionPersister = transactionPersister;
        _prenotificationHandler = prenotificationHandler;
    }

    public async Task<AchTransaction> RegisterTransactionAsync(
        decimal amount,
        string reference,
        TransactionTypeEnum type,
        AccountTypeEnum accountType,
        bool isPrenotification,
        int destinationInstitutionId,
        string sourceAccountNumber,
        string destinationAccountNumber,
        string companyName,
        string companyIdentification,
        string companyEntryDescription = "PAGOS",
        string? recipientIdNumber = null,
        bool requiresIdentityValidation = false,
        IEnumerable<AddendaDto>? addendas = null,
        CancellationToken ct = default)
    {
        var request = new AchTransactionRequestData
        {
            Amount = amount,
            Reference = reference,
            Type = type,
            AccountType = accountType,
            IsPrenotification = isPrenotification,
            DestinationInstitutionId = destinationInstitutionId,
            SourceAccountNumber = sourceAccountNumber,
            DestinationAccountNumber = destinationAccountNumber,
            CompanyName = companyName,
            CompanyIdentification = companyIdentification,
            CompanyEntryDescription = companyEntryDescription,
            RecipientIdNumber = recipientIdNumber,
            RequiresIdentityValidation = requiresIdentityValidation,
            Addendas = addendas
        };

        _transactionValidator.ValidateRequest(request);

        await using var dbTransaction = await _context.Database.BeginTransactionAsync(ct);

        await EnsureCustomerAndAccountsAsync(request, ct);

        var batchContext = await _batchResolver.ResolveAsync(request, ct);
        var persisted = await _transactionPersister.PersistAsync(request, batchContext, ct);

        if (isPrenotification)
        {
            await _prenotificationHandler.HandleAsync(request, persisted.Transaction, ct);
        }

        await _transactionPersister.UpdateBatchTotalsAsync(persisted.Batch, ct);
        await _transactionPersister.UpdateBatchServiceClassCodeAsync(persisted.Batch, ct);

        await dbTransaction.CommitAsync(ct);

        return persisted.Transaction;
    }


    private async Task EnsureCustomerAndAccountsAsync(AchTransactionRequestData request, CancellationToken ct)
    {
        await EnsureCustomerWithAccountAsync(
            documentNumber: request.CompanyIdentification,
            preferredName: request.CompanyName,
            accountNumber: request.SourceAccountNumber,
            defaultPersonType: "PJ",
            ct: ct);

        if (!string.IsNullOrWhiteSpace(request.RecipientIdNumber))
        {
            await EnsureCustomerWithAccountAsync(
                documentNumber: request.RecipientIdNumber,
                preferredName: request.RecipientIdNumber,
                accountNumber: request.DestinationAccountNumber,
                defaultPersonType: "PN",
                ct: ct);
        }
    }

    private async Task EnsureCustomerWithAccountAsync(
        string? documentNumber,
        string? preferredName,
        string? accountNumber,
        string defaultPersonType,
        CancellationToken ct)
    {
        var normalizedDocument = (documentNumber ?? string.Empty).Trim();
        var normalizedAccount = (accountNumber ?? string.Empty).Trim();

        if (string.IsNullOrWhiteSpace(normalizedDocument) || string.IsNullOrWhiteSpace(normalizedAccount))
        {
            return;
        }

        var customer = await _context.Customers
            .Include(c => c.Accounts)
            .FirstOrDefaultAsync(c => c.DocumentNumber == normalizedDocument, ct);

        if (customer is null)
        {
            customer = new Customer
            {
                PersonType = await ResolveCatalogCodeAsync(_context.PersonTypes, defaultPersonType, ct),
                DocumentType = await ResolveCatalogCodeAsync(_context.DocumentTypes, "OTRO", ct),
                DocumentNumber = normalizedDocument,
                CompanyName = NormalizeText(preferredName, 200),
                FirstName = NormalizeText(preferredName, 100, "N/A"),
                LastName = "N/A"
            };

            customer.Accounts.Add(new CustomerAccount { AccountNumber = normalizedAccount });
            _context.Customers.Add(customer);
            await _context.SaveChangesAsync(ct);
            return;
        }

        if (!customer.Accounts.Any(a => a.AccountNumber == normalizedAccount))
        {
            customer.Accounts.Add(new CustomerAccount { AccountNumber = normalizedAccount });
            await _context.SaveChangesAsync(ct);
        }
    }

    private static string NormalizeText(string? value, int maxLength, string fallback = "")
    {
        var normalized = (value ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(normalized))
        {
            normalized = fallback;
        }

        return normalized.Length <= maxLength ? normalized : normalized[..maxLength];
    }

    private static async Task<string> ResolveCatalogCodeAsync<TCatalog>(
        DbSet<TCatalog> dbSet,
        string preferredCode,
        CancellationToken ct)
        where TCatalog : class
    {
        var codeProperty = typeof(TCatalog).GetProperty("Code")
            ?? throw new InvalidOperationException($"La entidad {typeof(TCatalog).Name} no define la propiedad Code.");

        var preferred = await dbSet
            .AsNoTracking()
            .FirstOrDefaultAsync(x => EF.Property<string>(x, "Code") == preferredCode, ct);

        if (preferred is not null)
        {
            return (string)(codeProperty.GetValue(preferred) ?? preferredCode);
        }

        var fallback = await dbSet
            .AsNoTracking()
            .Select(x => EF.Property<string>(x, "Code"))
            .FirstOrDefaultAsync(ct);

        if (string.IsNullOrWhiteSpace(fallback))
        {
            throw new InvalidOperationException($"No existen registros en el catálogo {typeof(TCatalog).Name}.");
        }

        return fallback;
    }

    public Task<DateTime> GetNextBusinessDayAsync(DateTime baseDate, CancellationToken ct = default)
    {
        var date = baseDate.Date;
        var currentYear = date.Year;
        var holidays = _holidayService.GetHolidays(currentYear)
            .Select(h => h.Date)
            .ToHashSet();

        while (true)
        {
            if (date.Year != currentYear)
            {
                currentYear = date.Year;
                holidays = _holidayService.GetHolidays(currentYear)
                    .Select(h => h.Date)
                    .ToHashSet();
            }

            bool weekend = date.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday;
            bool holiday = holidays.Contains(DateOnly.FromDateTime(date));

            if (!weekend && !holiday)
                return Task.FromResult(date);

            date = date.AddDays(1);
        }
    }


    public async Task<IReadOnlyList<AchTransaction>> GetTransactionsByCycleAsync(
        string achCycleId, bool includeRelations = false, CancellationToken ct = default)
    {
        IQueryable<AchTransaction> q = _context.AchTransactions.AsNoTracking()
            .Where(t => t.AchCycleId == achCycleId);

        if (includeRelations)
        {
            q = q
                .Include(t => t.SourceInstitution)
                .Include(t => t.DestinationInstitution)
                .Include(t => t.Addendas)
                .Include(t => t.AchBatch);
        }

        return await q.OrderBy(t => t.Id).ToListAsync(ct);
    }

    public async Task<IReadOnlyList<AchTransactionListDto>> GetAllAsync(
        string? achCycleId = default,
        string? achCycleName = default,
        DateTime? effectiveDate = default,
        int? clearingHouseId = default,
        CancellationToken ct = default)
    {
        var query = _context.AchTransactions
            .AsNoTracking()
            .Include(t => t.AchBatch)
            .Include(t => t.AchCycle)
            .ThenInclude(cycle => cycle.ClearingHouse)
            .Include(t => t.SourceInstitution)
            .Include(t => t.DestinationInstitution)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(achCycleId))
        {
            query = query.Where(t => t.AchCycleId == achCycleId);
        }

        if (!string.IsNullOrWhiteSpace(achCycleName))
        {
            var normalizedName = achCycleName.Trim();
            query = query.Where(t => t.AchCycle.CycleName == normalizedName);
        }

        if (effectiveDate.HasValue)
        {
            var targetDate = effectiveDate.Value.Date;
            query = query.Where(t => t.EffectiveEntryDate.Date == targetDate);
        }

        if (clearingHouseId.HasValue)
        {
            query = query.Where(t => t.AchCycle.ClearingHouseId == clearingHouseId.Value);
        }

        return await query
            .OrderByDescending(t => t.CreatedAt)
            .Select(t => new AchTransactionListDto
            {
                Id = t.Id,
                Amount = t.Amount,
                Reference = t.Reference,
                Type = t.Type,
                TraceNumber = t.TraceNumber,
                EffectiveEntryDate = t.EffectiveEntryDate,
                CreatedAt = t.CreatedAt,
                SourceAccountNumber = t.SourceAccountNumber,
                DestinationAccountNumber = t.DestinationAccountNumber,
                SourceInstitutionName = t.SourceInstitution.Name,
                DestinationInstitutionName = t.DestinationInstitution.Name,
                IsPrenotification = t.IsPrenotification,
                TransactionCode = t.TransactionCode,
                AchBatchId = t.AchBatchId,
                BatchSequenceNumber = t.AchBatch.BatchSequenceNumber,
                BatchCompanyName = t.AchBatch.CompanyName,
                BatchEffectiveEntryDate = t.AchBatch.EffectiveEntryDate,
                AchCycleId = t.AchCycleId,
                AchCycleName = t.AchCycle.CycleName,
                ClearingHouseName = t.AchCycle.ClearingHouse!.Name
            })
            .ToListAsync(ct);
    }

    public async Task<AchTransaction?> GetTransactionByIdAsync(int transactionId, CancellationToken ct = default)
    {
        return await _context.AchTransactions
            .Include(t => t.SourceInstitution)
            .Include(t => t.DestinationInstitution)
            .Include(t => t.Addendas)
            .Include(t => t.AchBatch)
            .Include(t => t.AchCycle)
            .FirstOrDefaultAsync(t => t.Id == transactionId, ct);
    }
}
