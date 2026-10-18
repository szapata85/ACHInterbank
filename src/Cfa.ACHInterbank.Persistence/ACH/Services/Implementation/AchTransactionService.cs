using Cfa.ACHInterbank.Application.ACH.Interfaces;
using Cfa.ACHInterbank.Application.ACH.Interfaces.Repositories;
using Cfa.ACHInterbank.Application.ACH.Models;
using Cfa.ACHInterbank.Application.ACH.Services;
using Cfa.ACHInterbank.Application.DataBase;
using Cfa.ACHInterbank.Domain.Entities.Transactions.Dtos;
using Cfa.ACHInterbank.Domain.Entities.Transactions.Enums;
using Cfa.ACHInterbank.Domain.Models.ACH;
using Cfa.ACHInterbank.Domain.Models.ACH.Enums;
using Cfa.ACHInterbank.Domain.Models.Configurations;
using Cfa.ACHInterbank.Persistence.DataBase;
using System.Diagnostics.Metrics;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

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
    private readonly IThirdPartyOnboardingService _thirdPartyOnboardingService;
    private readonly IClearingHouseValidationPolicyService _clearingHouseValidationPolicyService;
    private readonly IThirdPartyAccountEligibilityService _thirdPartyAccountEligibilityService;
    private readonly ITransactionPrerequisitePolicyService _transactionPrerequisitePolicyService;
    private readonly IThirdPartyAccountValidationService? _thirdPartyAccountValidationService;
    private readonly ITransactionPolicyService? _transactionPolicyService;
    private readonly IContrapartidaDispatchPersistenceService _contrapartidaDispatchPersistenceService;
    private readonly ICenitCycleQueueService? _cenitCycleQueueService;
    private readonly IAchRegulatoryCatalogService? _catalogService;
    private readonly IAchTransactionClassificationPolicy _classificationPolicy;
    private readonly ILogger<AchTransactionService>? _logger;

    private static readonly Meter OperationalMeter = new("Cfa.ACHInterbank.ACH.Single");
    private static readonly Counter<long> OperationCounter = OperationalMeter.CreateCounter<long>(
        "ach_single_operations_total",
        description: "Count of SINGLE ACH registration terminal outcomes.");
    private static readonly Counter<long> RetryCounter = OperationalMeter.CreateCounter<long>(
        "ach_single_retries_total",
        description: "Count of bounded SINGLE ACH registration retries.");
    private static readonly Counter<long> RollbackCounter = OperationalMeter.CreateCounter<long>(
        "ach_single_rollbacks_total",
        description: "Count of SINGLE ACH database transaction rollbacks.");

    public AchTransactionService(
        AchDbContext context,
        IUnitOfWork unitOfWork,
        IAchCustomerRepository customerRepository,
        IBankHoliday holidayService,
        ITransactionValidator transactionValidator,
        IBatchResolver batchResolver,
        ITransactionPersister transactionPersister,
        IPrenotificationHandler prenotificationHandler,
        IThirdPartyOnboardingService thirdPartyOnboardingService,
        IClearingHouseValidationPolicyService clearingHouseValidationPolicyService,
        IThirdPartyAccountEligibilityService thirdPartyAccountEligibilityService,
        ITransactionPrerequisitePolicyService transactionPrerequisitePolicyService,
        IContrapartidaDispatchPersistenceService contrapartidaDispatchPersistenceService,
        ICenitCycleQueueService? cenitCycleQueueService = null,
        IAchRegulatoryCatalogService? catalogService = null,
        ITransactionPolicyService? transactionPolicyService = null,
        IAchTransactionClassificationPolicy? classificationPolicy = null,
        IThirdPartyAccountValidationService? thirdPartyAccountValidationService = null,
        ILogger<AchTransactionService>? logger = null)
    {
        _context = context;
        _unitOfWork = unitOfWork;
        _customerRepository = customerRepository;
        _holidayService = holidayService;
        _transactionValidator = transactionValidator;
        _batchResolver = batchResolver;
        _transactionPersister = transactionPersister;
        _prenotificationHandler = prenotificationHandler;
        _thirdPartyOnboardingService = thirdPartyOnboardingService;
        _clearingHouseValidationPolicyService = clearingHouseValidationPolicyService;
        _thirdPartyAccountEligibilityService = thirdPartyAccountEligibilityService;
        _transactionPrerequisitePolicyService = transactionPrerequisitePolicyService;
        _thirdPartyAccountValidationService = thirdPartyAccountValidationService;
        _contrapartidaDispatchPersistenceService = contrapartidaDispatchPersistenceService;
        _cenitCycleQueueService = cenitCycleQueueService;
        _catalogService = catalogService;
        _transactionPolicyService = transactionPolicyService;
        _classificationPolicy = classificationPolicy ?? new AchTransactionClassificationPolicy();
        _logger = logger;
    }

    public Task<AchTransaction> RegisterTransactionAsync(
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
        => RegisterTransactionCoreAsync(
            amount, reference, type, accountType, isPrenotification, destinationInstitutionId,
            sourceAccountNumber, destinationAccountNumber, companyName, companyIdentification,
            companyEntryDescriptionId, sourcePersonType, recipientPersonType, recipientIdNumber,
            recipientName, transactionExternalId, requiresIdentityValidation, addendas, null, ct);

    public Task<AchTransaction> RegisterCanonicalPrenotificationAsync(
        CanonicalPrenotificationTransactionRequest request,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        var transactionType = request.TransactionNature == TransactionNature.Debit
            ? TransactionTypeEnum.Debit
            : TransactionTypeEnum.Credit;
        var reference = $"TPA5-{Guid.NewGuid():N}"[..30];

        return RegisterTransactionCoreAsync(
            0m,
            reference,
            transactionType,
            request.AccountType,
            true,
            request.DestinationInstitutionId,
            request.SourceAccountNumber,
            request.DestinationAccountNumber,
            request.CompanyName,
            request.CompanyIdentification,
            request.CompanyEntryDescriptionId,
            request.SourcePersonType,
            request.RecipientPersonType,
            request.RecipientIdNumber,
            request.RecipientName,
            null,
            false,
            null,
            new AchTransactionRegistrationOptions(
                request.ThirdPartyAccountId,
                request.ExpectedClearingHouseId,
                request.ExpectedEffectiveDate,
                request.ValidationIdempotencyKey),
            ct);
    }

    private async Task<AchTransaction> RegisterTransactionCoreAsync(
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
        string? sourcePersonType,
        string? recipientPersonType,
        string? recipientIdNumber,
        string? recipientName,
        string? transactionExternalId,
        bool requiresIdentityValidation,
        IEnumerable<AddendaDto>? addendas,
        AchTransactionRegistrationOptions? registrationOptions,
        CancellationToken ct)
    {
        var terminalOutcomeRecorded = false;
        try
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

        ct.ThrowIfCancellationRequested();
        _transactionValidator.ValidateRequest(request);
        if (_catalogService is not null)
        {
            await _catalogService.IsPrenotificationRequiredAsync(request.Type, ct);
        }
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
                RecordTerminalOutcome("rejected", "transaction_policy");
                terminalOutcomeRecorded = true;
                throw new InvalidOperationException(preview.Message ?? "La transacción incumple las políticas operativas ACH.");
            }
        }

        ct.ThrowIfCancellationRequested();

        var executionStrategy = _context.Database.CreateExecutionStrategy();
        AchTransaction? registeredTransaction = null;
        const int maxUniqueConflictRetries = 1;
        var uniqueConflictRetries = 0;

        await executionStrategy.ExecuteAsync(async () =>
        {
            while (true)
            {
                ct.ThrowIfCancellationRequested();
                var transactionStarted = false;

                try
                {
                    await using var dbTransaction = await _context.Database.BeginTransactionAsync(ct);
                    transactionStarted = true;

                    var customerContext = await EnsureCustomerAndAccountsAsync(request, ct);
                    await _context.SaveChangesAsync(ct);

                    ThirdPartyAccountRef? canonicalAccount = null;
                    if (registrationOptions?.CanonicalThirdPartyAccountId is long canonicalAccountId)
                    {
                        var canonical = await _context.ThirdPartyAccounts
                            .AsNoTracking()
                            .Where(x => x.Id == canonicalAccountId)
                            .Select(x => new { x.Id, x.ThirdPartyId })
                            .FirstOrDefaultAsync(ct)
                            ?? throw new InvalidOperationException("La cuenta canónica indicada para la prenotificación no existe.");
                        canonicalAccount = new ThirdPartyAccountRef(canonical.ThirdPartyId, canonical.Id, false, false, false);
                    }
                    else
                    {
                        var onboardingRequest = ThirdPartyOnboardingRequestMapper.TryCreate(
                            request,
                            customerContext.SourceCustomer,
                            customerContext.RecipientCustomer,
                            ThirdPartyAccountOriginSource.SingleTransaction);
                        if (onboardingRequest is not null)
                        {
                            canonicalAccount = await _thirdPartyOnboardingService.EnsureAccountAsync(onboardingRequest, ct);
                        }
                    }

                    var batchContext = await _batchResolver.ResolveAsync(request, ct);
                    if (registrationOptions?.ExpectedClearingHouseId is int expectedClearingHouseId
                        && batchContext.ClearingHouseId != expectedClearingHouseId)
                    {
                        throw new ThirdPartyAdministrationConflictException(
                            "CLEARING_HOUSE_ROUTE_MISMATCH",
                            "La cámara solicitada no coincide con la cámara resuelta por el enrutamiento vigente.");
                    }
                    if (registrationOptions?.ExpectedEffectiveDate is DateTime expectedEffectiveDate
                        && batchContext.EffectiveEntryDate.Date != expectedEffectiveDate.Date)
                    {
                        throw new ThirdPartyAdministrationConflictException(
                            "EFFECTIVE_DATE_ROUTE_MISMATCH",
                            "La fecha efectiva solicitada no coincide con la fecha del ciclo operacional resuelto.");
                    }

                    var eligibility = await ValidateCanonicalTransactionPrerequisitesAsync(
                        request,
                        canonicalAccount,
                        batchContext,
                        ct,
                        stage =>
                        {
                            RecordTerminalOutcome("rejected", stage);
                            terminalOutcomeRecorded = true;
                        });

                    var classification = _classificationPolicy.Classify(new AchTransactionClassificationRequest(
                        request.Type,
                        request.IsPrenotification,
                        batchContext.SourceInstitutionIsDefault,
                        batchContext.DestinationInstitutionIsDefault));
                    if (!classification.CanCreate)
                    {
                        RecordTerminalOutcome("rejected", "classification");
                        terminalOutcomeRecorded = true;
                        throw new InvalidOperationException(classification.RejectionMessage
                            ?? "La combinación seleccionada no corresponde a un flujo ACH válido.");
                    }

                    batchContext = batchContext with { Classification = classification };
                    var persisted = await _transactionPersister.PersistAsync(request, batchContext, ct);

                    if (!request.IsPrenotification
                        && canonicalAccount is not null
                        && eligibility?.ValidationId is long validationId
                        && _thirdPartyAccountValidationService is not null)
                    {
                        await _thirdPartyAccountValidationService.RecordMonetaryUseAsync(
                            new ThirdPartyAccountMonetaryUseRequest(
                                canonicalAccount.ThirdPartyAccountId,
                                validationId,
                                new DateTimeOffset(DateTime.SpecifyKind(persisted.Transaction.StateChangedAtUtc, DateTimeKind.Utc))),
                            ct);
                    }

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
                        if (canonicalAccount is not null && _thirdPartyAccountValidationService is not null)
                        {
                            await _context.SaveChangesAsync(ct);
                            var prenotificationType = NachaTransactionCodeTaxonomy.ResolvePrenotificationDirection(persisted.Transaction.TransactionCode);
                            var nature = prenotificationType.HasValue
                                ? TransactionPrerequisitePolicyService.ResolveNature(prenotificationType.Value)
                                : null;
                            if (nature.HasValue)
                            {
                                var validationStart = await _thirdPartyAccountValidationService.RegisterPrenotificationAttemptAsync(
                                    new PrenotificationValidationStartRequest(
                                        canonicalAccount.ThirdPartyAccountId,
                                        persisted.Transaction.Id,
                                        batchContext.ClearingHouseId,
                                        nature.Value,
                                        persisted.Transaction.EffectiveEntryDate,
                                        new DateTimeOffset(DateTime.SpecifyKind(persisted.Transaction.StateChangedAtUtc, DateTimeKind.Utc)),
                                        batchContext.AchCycleId,
                                        registrationOptions?.PrenotificationValidationIdempotencyKey
                                            ?? $"prenotification-transaction:{persisted.Transaction.Id}",
                                        ServiceType: batchContext.SourceServiceClassCode),
                                    ct);
                                if (validationStart.ReusedExistingAttempt
                                    && validationStart.AchTransactionId.HasValue
                                    && validationStart.AchTransactionId.Value != persisted.Transaction.Id
                                    && !string.IsNullOrWhiteSpace(registrationOptions?.PrenotificationValidationIdempotencyKey))
                                {
                                    throw new PrenotificationIdempotencyConflictException(
                                        registrationOptions.PrenotificationValidationIdempotencyKey!);
                                }
                            }
                        }

                        await _prenotificationHandler.HandleAsync(request, persisted.Transaction, ct);
                    }

                    await _transactionPersister.UpdateBatchTotalsAsync(persisted.Batch, ct);
                    await _transactionPersister.UpdateBatchServiceClassCodeAsync(persisted.Batch, ct);
                    if (classification.MonetaryIntegrationRoute == AchMonetaryIntegrationRoute.ProcContrapartidas)
                    {
                        await _contrapartidaDispatchPersistenceService.EnsurePendingDispatchAsync(
                            persisted.Transaction,
                            batchContext.ClearingHouseId,
                            ct);
                    }

                    ct.ThrowIfCancellationRequested();
                    await _unitOfWork.CommitAsync(ct);
                    ct.ThrowIfCancellationRequested();
                    await dbTransaction.CommitAsync(ct);
                    transactionStarted = false;
                    registeredTransaction = persisted.Transaction;
                    return;
                }
                catch (DbUpdateException ex) when (
                    uniqueConflictRetries < maxUniqueConflictRetries
                    && RelationalDatabaseExceptionClassifier.IsUniqueViolation(ex))
                {
                    uniqueConflictRetries++;
                    if (transactionStarted)
                    {
                        RecordRollback("unique_conflict");
                    }
                    RecordRetry("unique_conflict");
                    _context.ChangeTracker.Clear();
                    ct.ThrowIfCancellationRequested();
                }
                catch (OperationCanceledException)
                {
                    if (transactionStarted)
                    {
                        RecordRollback("canceled");
                    }

                    _context.ChangeTracker.Clear();
                    throw;
                }
                catch
                {
                    if (transactionStarted)
                    {
                        RecordRollback("failed");
                    }

                    // A failed single registration owns its transaction-scoped unit of work. Clearing the
                    // tracker after rollback prevents ghost Added/Modified state from leaking into a caller
                    // retry or an execution-strategy retry on the same scoped DbContext.
                    _context.ChangeTracker.Clear();
                    throw;
                }
            }
        });

            var result = registeredTransaction
                ?? throw new InvalidOperationException("No fue posible registrar la transacción ACH.");

            RecordTerminalOutcome("accepted", "committed");
            terminalOutcomeRecorded = true;
            return result;
        }
        catch (OperationCanceledException ex)
        {
            if (!terminalOutcomeRecorded)
            {
                RecordTerminalOutcome("canceled", "cancellation", ex.GetType());
            }

            throw;
        }
        catch (ArgumentException ex)
        {
            if (!terminalOutcomeRecorded)
            {
                RecordTerminalOutcome("rejected", "request_validation", ex.GetType());
            }

            throw;
        }
        catch (Exception ex)
        {
            if (!terminalOutcomeRecorded)
            {
                RecordTerminalOutcome("failed", "unexpected", ex.GetType());
            }

            throw;
        }
    }


    private async Task<ThirdPartyAccountEligibilityResult?> ValidateCanonicalTransactionPrerequisitesAsync(
        AchTransactionRequestData request,
        ThirdPartyAccountRef? canonicalAccount,
        TransactionBatchContext batchContext,
        CancellationToken ct,
        Action<string>? onRejected = null)
    {
        if (request.IsPrenotification)
        {
            return null;
        }

        var nature = TransactionPrerequisitePolicyService.ResolveNature(request.Type);
        if (!nature.HasValue)
        {
            return null;
        }

        var policy = await _clearingHouseValidationPolicyService.ResolveAsync(
            batchContext.ClearingHouseId,
            nature.Value,
            batchContext.EffectiveEntryDate,
            ct);

        ThirdPartyAccountEligibilityResult? eligibility = null;
        if (canonicalAccount is not null)
        {
            eligibility = await _thirdPartyAccountEligibilityService.EvaluateAsync(
                new ThirdPartyAccountEligibilityRequest(
                    canonicalAccount.ThirdPartyAccountId,
                    batchContext.ClearingHouseId,
                    nature.Value,
                    batchContext.EffectiveEntryDate,
                    batchContext.SourceServiceClassCode),
                ct);
        }

        var prerequisite = _transactionPrerequisitePolicyService.ValidateForRegistration(
            new TransactionRegistrationPrerequisiteRequest(
                request.IsPrenotification,
                policy.Rule,
                eligibility));
        if (!prerequisite.IsValid)
        {
            onRejected?.Invoke("canonical_prerequisite");
            throw new InvalidOperationException($"{prerequisite.Code}: {prerequisite.Message}");
        }

        return eligibility;
    }


    private void RecordTerminalOutcome(string outcome, string stage, Type? exceptionType = null)
    {
        TryObserve(() => OperationCounter.Add(1,
            new KeyValuePair<string, object?>("outcome", outcome),
            new KeyValuePair<string, object?>("stage", stage)));

        TryObserve(() =>
        {
            if (exceptionType is null)
            {
                _logger?.LogInformation(
                    "ACH SINGLE registration outcome {Outcome} at stage {Stage}.",
                    outcome,
                    stage);
                return;
            }

            _logger?.LogWarning(
                "ACH SINGLE registration outcome {Outcome} at stage {Stage}; failure type {ExceptionType}.",
                outcome,
                stage,
                exceptionType.Name);
        });
    }

    private void RecordRetry(string reason)
    {
        TryObserve(() => RetryCounter.Add(1, new KeyValuePair<string, object?>("reason", reason)));
        TryObserve(() => _logger?.LogInformation(
            "ACH SINGLE retry scheduled for reason {RetryReason}.",
            reason));
    }

    private void RecordRollback(string reason)
    {
        TryObserve(() => RollbackCounter.Add(1, new KeyValuePair<string, object?>("reason", reason)));
        TryObserve(() => _logger?.LogWarning(
            "ACH SINGLE database transaction rollback observed for reason {RollbackReason}.",
            reason));
    }

    private static void TryObserve(Action observe)
    {
        try
        {
            observe();
        }
        catch
        {
            // Operational telemetry is best-effort and must never alter ACH transaction behavior.
        }
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

    private async Task<CustomerOnboardingContext> EnsureCustomerAndAccountsAsync(AchTransactionRequestData request, CancellationToken ct)
    {
        var sourceCustomer = await EnsureCustomerWithAccountAsync(
            documentNumber: request.CompanyIdentification,
            preferredName: request.CompanyName,
            accountNumber: request.SourceAccountNumber,
            defaultPersonType: ResolvePersonTypeCode(request.SourcePersonType, "PJ"),
            defaultDocumentType: "NIT",
            ct: ct);

        Customer? recipientCustomer = null;
        if (!string.IsNullOrWhiteSpace(request.RecipientIdNumber))
        {
            recipientCustomer = await EnsureCustomerWithAccountAsync(
                documentNumber: request.RecipientIdNumber,
                preferredName: request.RecipientName,
                accountNumber: request.DestinationAccountNumber,
                defaultPersonType: ResolvePersonTypeCode(request.RecipientPersonType, "PN"),
                defaultDocumentType: "CC",
                ct: ct);
        }

        return new CustomerOnboardingContext(sourceCustomer, recipientCustomer);
    }

    private async Task<Customer?> EnsureCustomerWithAccountAsync(
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
            return null;
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
            return customer;
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

        return customer;
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

    private sealed record CustomerOnboardingContext(Customer? SourceCustomer, Customer? RecipientCustomer);

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
