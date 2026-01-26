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
            RecipientIdNumber = recipientIdNumber,
            RequiresIdentityValidation = requiresIdentityValidation,
            Addendas = addendas
        };

        _transactionValidator.ValidateRequest(request);

        var batchContext = await _batchResolver.ResolveAsync(request, ct);
        var persisted = await _transactionPersister.PersistAsync(request, batchContext, ct);

        if (isPrenotification)
        {
            await _prenotificationHandler.HandleAsync(request, persisted.Transaction, ct);
        }

        await _transactionPersister.UpdateBatchTotalsAsync(persisted.Batch, ct);
        await _transactionPersister.UpdateBatchServiceClassCodeAsync(persisted.Batch, ct);

        return persisted.Transaction;
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
