using Cfa.ACHInterbank.Application.ACH.Interfaces;
using Cfa.ACHInterbank.Application.ACH.Interfaces.Repositories;
using Cfa.ACHInterbank.Application.ACH.Models;
using Cfa.ACHInterbank.Application.DataBase;
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
    private readonly IUnitOfWork _unitOfWork;
    private readonly IAchCustomerRepository _customerRepository;
    private readonly IBankHoliday _holidayService;
    private readonly ITransactionValidator _transactionValidator;
    private readonly IBatchResolver _batchResolver;
    private readonly ITransactionPersister _transactionPersister;
    private readonly IPrenotificationHandler _prenotificationHandler;
    private readonly ITransactionPolicyService? _transactionPolicyService;
    private readonly IContrapartidaDispatchPersistenceService _contrapartidaDispatchPersistenceService;
    private readonly ICenitCycleQueueService? _cenitCycleQueueService;

    public AchTransactionService(
        AchDbContext context,
        IUnitOfWork unitOfWork,
        IAchCustomerRepository customerRepository,
        IBankHoliday holidayService,
        ITransactionValidator transactionValidator,
        IBatchResolver batchResolver,
        ITransactionPersister transactionPersister,
        IPrenotificationHandler prenotificationHandler,
        IContrapartidaDispatchPersistenceService contrapartidaDispatchPersistenceService,
        ICenitCycleQueueService? cenitCycleQueueService = null,
        ITransactionPolicyService? transactionPolicyService = null)
    {
        _context = context;
        _unitOfWork = unitOfWork;
        _customerRepository = customerRepository;
        _holidayService = holidayService;
        _transactionValidator = transactionValidator;
        _batchResolver = batchResolver;
        _transactionPersister = transactionPersister;
        _prenotificationHandler = prenotificationHandler;
        _contrapartidaDispatchPersistenceService = contrapartidaDispatchPersistenceService;
        _cenitCycleQueueService = cenitCycleQueueService;
        _transactionPolicyService = transactionPolicyService;
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
        int companyEntryDescriptionId,
        string? sourcePersonType = null,
        string? recipientPersonType = null,
        string? recipientIdNumber = null,
        string? recipientName = null,
        string? transactionExternalId = null,
        bool requiresIdentityValidation = false,
        IEnumerable<AddendaDto>? addendas = null,
        CancellationToken ct = default)
    {
        var request = new AchTransactionRequestData
        {
            Amount = amount,
            TransactionExternalId = transactionExternalId,
            Reference = reference,
            Type = type,
            AccountType = accountType,
            IsPrenotification = isPrenotification,
            DestinationInstitutionId = destinationInstitutionId,
            SourceAccountNumber = sourceAccountNumber,
            DestinationAccountNumber = destinationAccountNumber,
            CompanyName = companyName,
            CompanyIdentification = companyIdentification,
            CompanyEntryDescriptionId = companyEntryDescriptionId,
            SourcePersonType = sourcePersonType,
            RecipientPersonType = recipientPersonType,
            RecipientIdNumber = recipientIdNumber,
            RecipientName = recipientName,
            RequiresIdentityValidation = requiresIdentityValidation,
            Addendas = addendas?.ToList()
        };

        _transactionValidator.ValidateRequest(request);
        if (_transactionPolicyService is not null)
        {
            var preview = await _transactionPolicyService.PreviewAsync(new TransactionPolicyPreviewRequest(
                request.Amount,
                request.TransactionExternalId,
                request.Reference,
                request.Type,
                request.AccountType,
                request.IsPrenotification,
                request.DestinationInstitutionId,
                request.SourceAccountNumber,
                request.DestinationAccountNumber,
                request.CompanyIdentification,
                request.RecipientIdNumber), ct);

            if (!preview.CanSubmit)
            {
                throw new InvalidOperationException(preview.Message ?? "La transacción incumple las políticas operativas ACH.");
            }
        }

        var executionStrategy = _context.Database.CreateExecutionStrategy();
        AchTransaction? registeredTransaction = null;

        await executionStrategy.ExecuteAsync(async () =>
        {
            await using var dbTransaction = await _context.Database.BeginTransactionAsync(ct);

            await EnsureCustomerAndAccountsAsync(request, ct);

            var batchContext = await _batchResolver.ResolveAsync(request, ct);
            var persisted = await _transactionPersister.PersistAsync(request, batchContext, ct);

            if (batchContext.MustQueueForTargetCycle && _cenitCycleQueueService is not null)
            {
                await _cenitCycleQueueService.EnqueueAsync(
                    persisted.Transaction,
                    DateTime.UtcNow,
                    batchContext.QueueReason,
                    ct);
            }

            if (isPrenotification)
            {
                await _prenotificationHandler.HandleAsync(request, persisted.Transaction, ct);
            }

            await _transactionPersister.UpdateBatchTotalsAsync(persisted.Batch, ct);
            await _transactionPersister.UpdateBatchServiceClassCodeAsync(persisted.Batch, ct);
            await _contrapartidaDispatchPersistenceService.EnsurePendingDispatchAsync(
                persisted.Transaction,
                batchContext.ClearingHouseId,
                ct);

            await _unitOfWork.CommitAsync(ct);
            await dbTransaction.CommitAsync(ct);
            registeredTransaction = persisted.Transaction;
        });

        return registeredTransaction
            ?? throw new InvalidOperationException("No fue posible registrar la transacción ACH.");
    }


    public async Task<IReadOnlyList<CompanyEntryDescriptionDto>> GetCompanyEntryDescriptionsAsync(CancellationToken ct = default)
    {
        return await _context.CompanyEntryDescriptionCatalogs
            .AsNoTracking()
            .Where(item => item.IsActive)
            .OrderBy(item => item.Term)
            .Select(item => new CompanyEntryDescriptionDto
            {
                Id = item.Id,
                Term = item.Term,
                Description = item.Description,
                StandardEntryClassCode = item.StandardEntryClassCode
            })
            .ToListAsync(ct);
    }

    private async Task EnsureCustomerAndAccountsAsync(AchTransactionRequestData request, CancellationToken ct)
    {
        await EnsureCustomerWithAccountAsync(
            documentNumber: request.CompanyIdentification,
            preferredName: request.CompanyName,
            accountNumber: request.SourceAccountNumber,
            defaultPersonType: ResolvePersonTypeCode(request.SourcePersonType, "PJ"),
            defaultDocumentType: "NIT",
            ct: ct);

        if (!string.IsNullOrWhiteSpace(request.RecipientIdNumber))
        {
            await EnsureCustomerWithAccountAsync(
                documentNumber: request.RecipientIdNumber,
                preferredName: request.RecipientName,
                accountNumber: request.DestinationAccountNumber,
                defaultPersonType: ResolvePersonTypeCode(request.RecipientPersonType, "PN"),
                defaultDocumentType: "CC",
                ct: ct);
        }
    }

    private async Task EnsureCustomerWithAccountAsync(
        string? documentNumber,
        string? preferredName,
        string? accountNumber,
        string defaultPersonType,
        string defaultDocumentType,
        CancellationToken ct)
    {
        var normalizedDocument = (documentNumber ?? string.Empty).Trim();
        var normalizedAccount = (accountNumber ?? string.Empty).Trim();

        if (string.IsNullOrWhiteSpace(normalizedDocument) || string.IsNullOrWhiteSpace(normalizedAccount))
        {
            return;
        }

        var resolvedDocumentType = await _customerRepository.ResolveDocumentTypeCodeAsync(defaultDocumentType, ct);
        var resolvedPersonType = await _customerRepository.ResolvePersonTypeCodeAsync(defaultPersonType, ct);

        var customer = await _customerRepository.GetByDocumentAsync(resolvedDocumentType, normalizedDocument, ct);

        if (customer is null)
        {
            var legacyCustomers = await _customerRepository.GetByDocumentNumberAsync(normalizedDocument, ct);

            if (legacyCustomers.Count == 1)
            {
                customer = legacyCustomers[0];
                if (customer.DocumentType == "OTRO" && customer.DocumentType != resolvedDocumentType)
                {
                    customer.DocumentType = resolvedDocumentType;
                }
            }
        }

        if (customer is null)
        {
            var autoProfile = BuildAutoProfile(defaultPersonType, preferredName);

            customer = new Customer
            {
                PersonType = resolvedPersonType,
                DocumentType = resolvedDocumentType,
                DocumentNumber = normalizedDocument,
                CompanyName = autoProfile.CompanyName,
                FirstName = autoProfile.FirstName,
                LastName = autoProfile.LastName
            };

            customer.Accounts.Add(new CustomerAccount { AccountNumber = normalizedAccount });
            await _customerRepository.AddAsync(customer, ct);
            return;
        }

        if (!string.Equals(customer.PersonType, resolvedPersonType, StringComparison.OrdinalIgnoreCase))
        {
            customer.PersonType = resolvedPersonType;
        }

        if (!string.Equals(customer.DocumentType, resolvedDocumentType, StringComparison.OrdinalIgnoreCase)
            && (string.Equals(customer.DocumentType, "OTRO", StringComparison.OrdinalIgnoreCase)
                || string.IsNullOrWhiteSpace(customer.DocumentType)))
        {
            customer.DocumentType = resolvedDocumentType;
        }

        RefreshAutoProfileIfNeeded(customer, defaultPersonType, preferredName);

        if (!customer.Accounts.Any(a => a.AccountNumber == normalizedAccount))
        {
            customer.Accounts.Add(new CustomerAccount { AccountNumber = normalizedAccount });
        }
    }

    private static string ResolvePersonTypeCode(string? requestedPersonType, string fallback)
    {
        var normalized = (requestedPersonType ?? string.Empty).Trim().ToUpperInvariant();
        return normalized is "PN" or "PJ" ? normalized : fallback;
    }

    private static (string FirstName, string LastName, string? CompanyName) BuildAutoProfile(
        string personType,
        string? preferredName)
    {
        if (personType == "PJ")
        {
            var companyName = NormalizeText(preferredName, 200, "EMPRESA NO IDENTIFICADA");
            return (
                FirstName: NormalizeText(companyName, 100, "EMPRESA"),
                LastName: "N/A",
                CompanyName: companyName);
        }

        var naturalName = IsLikelyIdentifier(preferredName)
            ? string.Empty
            : NormalizeText(preferredName, 100, "CLIENTE");

        return (
            FirstName: string.IsNullOrWhiteSpace(naturalName) ? "CLIENTE" : naturalName,
            LastName: "NO IDENTIFICADO",
            CompanyName: null);
    }

    private static void RefreshAutoProfileIfNeeded(Customer customer, string personType, string? preferredName)
    {
        var canUsePreferredName = !string.IsNullOrWhiteSpace(preferredName) && !IsLikelyIdentifier(preferredName);
        var normalizedName = canUsePreferredName ? NormalizeText(preferredName, 200) : string.Empty;

        if (personType == "PJ")
        {
            var effectiveCompanyName = string.IsNullOrWhiteSpace(normalizedName)
                ? NormalizeText(customer.CompanyName, 200, "EMPRESA NO IDENTIFICADA")
                : normalizedName;

            if (LooksAutoGenerated(customer.CompanyName) || string.IsNullOrWhiteSpace(customer.CompanyName))
            {
                customer.CompanyName = effectiveCompanyName;
            }

            if (LooksAutoGenerated(customer.FirstName) || string.IsNullOrWhiteSpace(customer.FirstName))
            {
                customer.FirstName = NormalizeText(effectiveCompanyName, 100, "EMPRESA");
            }

            if (LooksAutoGenerated(customer.LastName) || string.IsNullOrWhiteSpace(customer.LastName))
            {
                customer.LastName = "N/A";
            }

            return;
        }

        var effectiveFirstName = string.IsNullOrWhiteSpace(normalizedName)
            ? NormalizeText(customer.FirstName, 100, "CLIENTE")
            : NormalizeText(normalizedName, 100, "CLIENTE");

        if (LooksAutoGenerated(customer.FirstName) || string.IsNullOrWhiteSpace(customer.FirstName))
        {
            customer.FirstName = effectiveFirstName;
        }

        if (LooksAutoGenerated(customer.LastName) || string.IsNullOrWhiteSpace(customer.LastName))
        {
            customer.LastName = "NO IDENTIFICADO";
        }

        if (LooksAutoGenerated(customer.CompanyName))
        {
            customer.CompanyName = null;
        }
    }

    private static bool LooksAutoGenerated(string? value)
    {
        var normalized = (value ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(normalized))
        {
            return true;
        }

        return normalized.Equals("N/A", StringComparison.OrdinalIgnoreCase)
            || normalized.Equals("CLIENTE", StringComparison.OrdinalIgnoreCase)
            || normalized.Equals("NO IDENTIFICADO", StringComparison.OrdinalIgnoreCase)
            || normalized.Equals("EMPRESA", StringComparison.OrdinalIgnoreCase)
            || normalized.Equals("EMPRESA NO IDENTIFICADA", StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsLikelyIdentifier(string? value)
    {
        var normalized = (value ?? string.Empty).Trim();
        return !string.IsNullOrWhiteSpace(normalized) && normalized.All(char.IsDigit);
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
                TransactionExternalId = t.TransactionExternalId,
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
