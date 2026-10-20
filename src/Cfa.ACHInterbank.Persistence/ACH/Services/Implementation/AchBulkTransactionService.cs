using Cfa.ACHInterbank.Application.ACH.Interfaces;
using Cfa.ACHInterbank.Application.ACH.Interfaces.Repositories;
using Cfa.ACHInterbank.Application.ACH.Models;
using Cfa.ACHInterbank.Application.ACH.Services;
using Cfa.ACHInterbank.Application.DataBase;
using Cfa.ACHInterbank.Domain.Models.ACH;
using Cfa.ACHInterbank.Domain.Models.ACH.Enums;
using Cfa.ACHInterbank.Domain.Models.Configurations;
using Cfa.ACHInterbank.Persistence.DataBase;
using System.Diagnostics.Metrics;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Cfa.ACHInterbank.Persistence.ACH.Services.Implementation;

[Scoped]
public sealed class AchBulkTransactionService : IAchBulkTransactionService
{
    private const int DefaultChunkSize = 200;
    private const int DefaultMaxItems = 2000;

    private readonly AchDbContext _context;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IAchCustomerRepository _customerRepository;
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
    private readonly IConfiguration _configuration;
    private readonly IContrapartidaDispatchPersistenceService _contrapartidaDispatchPersistenceService;
    private readonly ICenitCycleQueueService? _cenitCycleQueueService;
    private readonly IAchRegulatoryCatalogService? _catalogService;
    private readonly IAchTransactionClassificationPolicy _classificationPolicy;
    private readonly ILogger<AchBulkTransactionService>? _logger;

    private static readonly Meter OperationalMeter = new("Cfa.ACHInterbank.ACH.Bulk");
    private static readonly Counter<long> OperationCounter = OperationalMeter.CreateCounter<long>(
        "ach_bulk_operations_total",
        description: "Count of BULK ACH registration terminal outcomes.");
    private static readonly Counter<long> ItemCounter = OperationalMeter.CreateCounter<long>(
        "ach_bulk_items_total",
        description: "Count of BULK ACH item outcomes.");
    private static readonly Counter<long> RetryCounter = OperationalMeter.CreateCounter<long>(
        "ach_bulk_retries_total",
        description: "Count of bounded BULK ACH item retries.");
    private static readonly Counter<long> RollbackCounter = OperationalMeter.CreateCounter<long>(
        "ach_bulk_rollbacks_total",
        description: "Count of BULK ACH savepoint/chunk rollbacks.");

    public AchBulkTransactionService(
        AchDbContext context,
        IUnitOfWork unitOfWork,
        IAchCustomerRepository customerRepository,
        ITransactionValidator transactionValidator,
        IBatchResolver batchResolver,
        ITransactionPersister transactionPersister,
        IPrenotificationHandler prenotificationHandler,
        IThirdPartyOnboardingService thirdPartyOnboardingService,
        IClearingHouseValidationPolicyService clearingHouseValidationPolicyService,
        IThirdPartyAccountEligibilityService thirdPartyAccountEligibilityService,
        ITransactionPrerequisitePolicyService transactionPrerequisitePolicyService,
        IConfiguration configuration,
        IContrapartidaDispatchPersistenceService contrapartidaDispatchPersistenceService,
        ICenitCycleQueueService? cenitCycleQueueService = null,
        IAchRegulatoryCatalogService? catalogService = null,
        ITransactionPolicyService? transactionPolicyService = null,
        IAchTransactionClassificationPolicy? classificationPolicy = null,
        IThirdPartyAccountValidationService? thirdPartyAccountValidationService = null,
        ILogger<AchBulkTransactionService>? logger = null)
    {
        _context = context;
        _unitOfWork = unitOfWork;
        _customerRepository = customerRepository;
        _transactionValidator = transactionValidator;
        _batchResolver = batchResolver;
        _transactionPersister = transactionPersister;
        _prenotificationHandler = prenotificationHandler;
        _thirdPartyOnboardingService = thirdPartyOnboardingService;
        _clearingHouseValidationPolicyService = clearingHouseValidationPolicyService;
        _thirdPartyAccountEligibilityService = thirdPartyAccountEligibilityService;
        _transactionPrerequisitePolicyService = transactionPrerequisitePolicyService;
        _thirdPartyAccountValidationService = thirdPartyAccountValidationService;
        _configuration = configuration;
        _contrapartidaDispatchPersistenceService = contrapartidaDispatchPersistenceService;
        _cenitCycleQueueService = cenitCycleQueueService;
        _catalogService = catalogService;
        _transactionPolicyService = transactionPolicyService;
        _classificationPolicy = classificationPolicy ?? new AchTransactionClassificationPolicy();
        _logger = logger;
    }

    public async Task<BulkAchTransactionResponse> RegisterBulkAsync(BulkAchTransactionRequest request, CancellationToken ct = default)
    {
        var terminalOutcomeRecorded = false;
        try
        {
            ArgumentNullException.ThrowIfNull(request);
        ct.ThrowIfCancellationRequested();

        var maxItems = _configuration.GetValue<int?>("Transactions:Bulk:MaxItems") ?? DefaultMaxItems;
        var configuredChunkSize = _configuration.GetValue<int?>("Transactions:Bulk:ChunkSize") ?? DefaultChunkSize;
        var chunkSize = Math.Clamp(request.ChunkSize ?? configuredChunkSize, 50, 1000);

        ValidateBatchRequest(request, maxItems);

        var response = new BulkAchTransactionResponse
        {
            BatchReference = request.BatchReference.Trim(),
            TotalReceived = request.Transactions.Count
        };

        var normalizedItems = request.Transactions
            .Select((item, index) => new NormalizedBulkItem(index, item, MapToRequestData(item)))
            .ToList();

        var activeCompanyEntryDescriptionIds = await PreloadActiveCompanyEntryDescriptionIdsAsync(ct);
        var duplicatedOperationalIdsInRequest = GetDuplicateOperationalIds(normalizedItems);
        var existingOperationalIdsInPersistence = await LoadExistingOperationalIdsAsync(normalizedItems, ct);

        var documentTypeByDefault = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["NIT"] = await _customerRepository.ResolveDocumentTypeCodeAsync("NIT", ct),
            ["CC"] = await _customerRepository.ResolveDocumentTypeCodeAsync("CC", ct)
        };
        var personTypeByDefault = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["PJ"] = await _customerRepository.ResolvePersonTypeCodeAsync("PJ", ct),
            ["PN"] = await _customerRepository.ResolvePersonTypeCodeAsync("PN", ct)
        };

        foreach (var chunk in normalizedItems.Chunk(chunkSize))
        {
            ct.ThrowIfCancellationRequested();
            var customerCache = await BuildCustomerCacheAsync(normalizedItems, ct);
            await using var dbTransaction = await _context.Database.BeginTransactionAsync(ct);

            var touchedBatchIds = new HashSet<int>();
            var pendingSuccesses = new List<(BulkAchTransactionItemResult Result, AchTransaction Transaction)>();

            try
            {
                foreach (var record in chunk)
                {
                    ct.ThrowIfCancellationRequested();
                    var itemSavepoint = $"bulk_item_{record.Index}";
                    await dbTransaction.CreateSavepointAsync(itemSavepoint, ct);
                    var itemFailureStage = "request_validation";

                    try
                    {
                        var normalizedReference = record.Data.Reference.Trim();
                        var normalizedOperationalId = ResolveOperationalId(record.Data);

                        if (duplicatedOperationalIdsInRequest.Contains(normalizedOperationalId))
                        {
                            throw new ArgumentException($"El identificador operativo '{normalizedOperationalId}' está duplicado dentro del mismo request.", nameof(record.Data.TransactionExternalId));
                        }

                        if (existingOperationalIdsInPersistence.Contains(normalizedOperationalId))
                        {
                            throw new ArgumentException($"El identificador operativo '{normalizedOperationalId}' ya existe en persistencia.", nameof(record.Data.TransactionExternalId));
                        }

                        _transactionValidator.ValidateRequest(record.Data, activeCompanyEntryDescriptionIds);
                        if (_catalogService is not null)
                        {
                            // Legacy catalog lookup is retained for compatibility/observability only.
                            // Chamber-specific prenotification authority is resolved after routing.
                            await _catalogService.IsPrenotificationRequiredAsync(record.Data.Type, ct);
                        }

                        if (_transactionPolicyService is not null)
                        {
                            itemFailureStage = "transaction_policy";
                            var preview = await _transactionPolicyService.PreviewAsync(new TransactionPolicyPreviewRequest(
                                record.Data.Amount,
                                record.Data.TransactionExternalId,
                                record.Data.Reference,
                                record.Data.Type,
                                record.Data.AccountType,
                                record.Data.IsPrenotification,
                                record.Data.DestinationInstitutionId,
                                record.Data.SourceAccountNumber,
                                record.Data.DestinationAccountNumber,
                                record.Data.CompanyIdentification,
                                record.Data.RecipientIdNumber), ct);

                            if (!preview.CanSubmit)
                            {
                                throw new InvalidOperationException(preview.Message ?? "La transacción incumple políticas operativas ACH.");
                            }
                        }

                        itemFailureStage = "onboarding";
                        CustomerOnboardingContext customerContext;
                        var legacyOnboardingUniqueConflictRetriesRemaining = 1;
                        while (true)
                        {
                            try
                            {
                                customerContext = await EnsureCustomerAndAccountsAsync(
                                    record.Data,
                                    customerCache,
                                    documentTypeByDefault,
                                    personTypeByDefault,
                                    ct);
                                await _context.SaveChangesAsync(ct);
                                break;
                            }
                            catch (DbUpdateException ex) when (
                                legacyOnboardingUniqueConflictRetriesRemaining > 0
                                && RelationalDatabaseExceptionClassifier.IsUniqueViolation(ex))
                            {
                                legacyOnboardingUniqueConflictRetriesRemaining--;

                                // This retry is intentionally scoped to legacy Customer/CustomerAccount
                                // onboarding, before canonical onboarding or transaction persistence runs.
                                // Roll back only this item, clear stale Added entities, re-read the concurrent
                                // winner, and retry the idempotent natural-key ensure exactly once.
                                await dbTransaction.RollbackToSavepointAsync(itemSavepoint, CancellationToken.None);
                                RecordRollback("item_savepoint", "unique_conflict");
                                RecordRetry("unique_conflict");
                                _context.ChangeTracker.Clear();
                                ct.ThrowIfCancellationRequested();
                                customerCache = await BuildCustomerCacheAsync(normalizedItems, ct);
                            }
                        }

                        var onboardingRequest = ThirdPartyOnboardingRequestMapper.TryCreate(
                            record.Data,
                            customerContext.SourceCustomer,
                            customerContext.RecipientCustomer,
                            ThirdPartyAccountOriginSource.BulkTransaction);
                        ThirdPartyAccountRef? canonicalAccount = null;
                        if (onboardingRequest is not null)
                        {
                            canonicalAccount = await _thirdPartyOnboardingService.EnsureAccountAsync(onboardingRequest, ct);
                        }

                        var batchContext = await _batchResolver.ResolveAsync(record.Data, ct);
                        itemFailureStage = "canonical_prerequisite";
                        var eligibility = await ValidateCanonicalTransactionPrerequisitesAsync(
                            record.Data,
                            canonicalAccount,
                            batchContext,
                            ct);

                        itemFailureStage = "classification";
                        var classification = _classificationPolicy.Classify(new AchTransactionClassificationRequest(
                            record.Data.Type,
                            record.Data.IsPrenotification,
                            batchContext.SourceInstitutionIsDefault,
                            batchContext.DestinationInstitutionIsDefault));
                        if (!classification.CanCreate)
                        {
                            throw new InvalidOperationException(classification.RejectionMessage
                                ?? "La combinación seleccionada no corresponde a un flujo ACH válido.");
                        }

                        batchContext = batchContext with { Classification = classification };
                        itemFailureStage = "processing";
                        var persisted = await _transactionPersister.PersistAsync(record.Data, batchContext, ct);

                        if (batchContext.MustQueueForTargetCycle && _cenitCycleQueueService is not null)
                        {
                            await _cenitCycleQueueService.EnqueueAsync(
                                persisted.Transaction,
                                DateTime.UtcNow,
                                batchContext.QueueReason,
                                ct);
                        }

                        if (record.Data.IsPrenotification)
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
                                    await _thirdPartyAccountValidationService.RegisterPrenotificationAttemptAsync(
                                        new PrenotificationValidationStartRequest(
                                            canonicalAccount.ThirdPartyAccountId,
                                            persisted.Transaction.Id,
                                            batchContext.ClearingHouseId,
                                            nature.Value,
                                            persisted.Transaction.EffectiveEntryDate,
                                            new DateTimeOffset(DateTime.SpecifyKind(persisted.Transaction.StateChangedAtUtc, DateTimeKind.Utc)),
                                            batchContext.AchCycleId,
                                            $"prenotification-transaction:{persisted.Transaction.Id}",
                                            ServiceType: batchContext.SourceServiceClassCode),
                                        ct);
                                }
                            }

                            await _prenotificationHandler.HandleAsync(record.Data, persisted.Transaction, ct);
                        }

                        if (!persisted.Transaction.IsPrenotification)
                        {
                            await _contrapartidaDispatchPersistenceService.EnsurePendingDispatchAsync(
                                persisted.Transaction,
                                batchContext.ClearingHouseId,
                                ct);
                        }

                        if (!record.Data.IsPrenotification
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

                        // Flush the successful item inside the still-open chunk transaction so a later
                        // item-level rollback can safely clear/re-hydrate the tracker without losing it.
                        // This is not a database commit; the chunk transaction remains the atomic commit boundary.
                        await _context.SaveChangesAsync(ct);
                        touchedBatchIds.Add(persisted.Batch.Id);

                        var itemResult = new BulkAchTransactionItemResult
                        {
                            Index = record.Index,
                            TransactionExternalId = normalizedOperationalId,
                            Reference = normalizedReference,
                            Succeeded = true
                        };

                        response.ItemResults.Add(itemResult);
                        pendingSuccesses.Add((itemResult, persisted.Transaction));
                    }
                    catch (Exception ex) when (ex is ArgumentException or InvalidOperationException)
                    {
                        await dbTransaction.RollbackToSavepointAsync(itemSavepoint, CancellationToken.None);
                        RecordRollback("item_savepoint", "rejected");
                        RecordItemOutcome(record.Index, "rejected", itemFailureStage, ex.GetType());
                        _context.ChangeTracker.Clear();
                        customerCache = await BuildCustomerCacheAsync(normalizedItems, ct);

                        response.ItemResults.Add(new BulkAchTransactionItemResult
                        {
                            Index = record.Index,
                            TransactionExternalId = ResolveOperationalId(record.Data),
                            Reference = record.Data.Reference,
                            Succeeded = false,
                            ErrorCode = "ITEM_VALIDATION_FAILED",
                            ErrorMessage = ex.Message
                        });
                    }
                    catch (OperationCanceledException ex)
                    {
                        RecordItemOutcome(record.Index, "canceled", "cancellation", ex.GetType());
                        throw;
                    }
                    catch (Exception ex)
                    {
                        RecordItemOutcome(record.Index, "failed", "unexpected", ex.GetType());
                        throw;
                    }
                }

                if (touchedBatchIds.Count > 0)
                {
                    foreach (var batchId in touchedBatchIds)
                    {
                        var batch = await _context.AchBatches.SingleAsync(x => x.Id == batchId, ct);
                        await _transactionPersister.UpdateBatchTotalsAsync(batch, ct);
                        await _transactionPersister.UpdateBatchServiceClassCodeAsync(batch, ct);
                    }

                    ct.ThrowIfCancellationRequested();
                    await _unitOfWork.CommitAsync(ct);
                    ct.ThrowIfCancellationRequested();
                    await dbTransaction.CommitAsync(ct);

                    foreach (var pending in pendingSuccesses)
                    {
                        pending.Result.TransactionId = pending.Transaction.Id;
                        if (pending.Transaction.Id > 0)
                        {
                            response.CreatedTransactionIds.Add(pending.Transaction.Id);
                        }

                        RecordItemOutcome(pending.Result.Index, "accepted", "committed");
                    }

                    _context.ChangeTracker.Clear();
                }
                else
                {
                    await dbTransaction.RollbackAsync(ct);
                    RecordRollback("chunk", "no_successful_items");
                    _context.ChangeTracker.Clear();
                }
            }
            catch (OperationCanceledException)
            {
                try
                {
                    await dbTransaction.RollbackAsync(CancellationToken.None);
                    RecordRollback("chunk", "canceled");
                }
                finally
                {
                    _context.ChangeTracker.Clear();
                }

                throw;
            }
            catch
            {
                try
                {
                    await dbTransaction.RollbackAsync(CancellationToken.None);
                    RecordRollback("chunk", "failed");
                }
                finally
                {
                    _context.ChangeTracker.Clear();
                }

                throw;
            }
        }

            response.TotalProcessed = response.ItemResults.Count;
            response.TotalSucceeded = response.ItemResults.Count(x => x.Succeeded);
            response.TotalFailed = response.ItemResults.Count(x => !x.Succeeded);

            var terminalOutcome = response.TotalFailed == 0
                ? "accepted"
                : response.TotalSucceeded == 0
                    ? "rejected"
                    : "partial";
            RecordTerminalOutcome(terminalOutcome, "completed");
            terminalOutcomeRecorded = true;
            return response;
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
                    "ACH BULK registration outcome {Outcome} at stage {Stage}.",
                    outcome,
                    stage);
                return;
            }

            _logger?.LogWarning(
                "ACH BULK registration outcome {Outcome} at stage {Stage}; failure type {ExceptionType}.",
                outcome,
                stage,
                exceptionType.Name);
        });
    }

    private void RecordItemOutcome(int index, string outcome, string stage, Type? exceptionType = null)
    {
        TryObserve(() => ItemCounter.Add(1,
            new KeyValuePair<string, object?>("outcome", outcome),
            new KeyValuePair<string, object?>("stage", stage)));

        TryObserve(() =>
        {
            if (exceptionType is null)
            {
                _logger?.LogInformation(
                    "ACH BULK item {ItemIndex} outcome {Outcome} at stage {Stage}.",
                    index,
                    outcome,
                    stage);
                return;
            }

            _logger?.LogWarning(
                "ACH BULK item {ItemIndex} outcome {Outcome} at stage {Stage}; failure type {ExceptionType}.",
                index,
                outcome,
                stage,
                exceptionType.Name);
        });
    }

    private void RecordRetry(string reason)
    {
        TryObserve(() => RetryCounter.Add(1, new KeyValuePair<string, object?>("reason", reason)));
        TryObserve(() => _logger?.LogInformation(
            "ACH BULK item retry scheduled for reason {RetryReason}.",
            reason));
    }

    private void RecordRollback(string scope, string reason)
    {
        TryObserve(() => RollbackCounter.Add(1,
            new KeyValuePair<string, object?>("scope", scope),
            new KeyValuePair<string, object?>("reason", reason)));
        TryObserve(() => _logger?.LogWarning(
            "ACH BULK rollback observed for scope {RollbackScope} and reason {RollbackReason}.",
            scope,
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


    private async Task<ThirdPartyAccountEligibilityResult?> ValidateCanonicalTransactionPrerequisitesAsync(
        AchTransactionRequestData request,
        ThirdPartyAccountRef? canonicalAccount,
        TransactionBatchContext batchContext,
        CancellationToken ct)
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
            throw new InvalidOperationException($"{prerequisite.Code}: {prerequisite.Message}");
        }

        return eligibility;
    }


    private static AchTransactionRequestData MapToRequestData(BulkAchTransactionItemRequest item)
    {
        return new AchTransactionRequestData
        {
            Amount = item.Amount,
            TransactionExternalId = item.TransactionExternalId,
            Reference = item.Reference,
            Type = item.Type,
            AccountType = item.AccountType,
            IsPrenotification = item.IsPrenotification,
            DestinationInstitutionId = item.DestinationInstitutionId,
            SourceAccountNumber = item.SourceAccountNumber,
            DestinationAccountNumber = item.DestinationAccountNumber,
            CompanyName = item.CompanyName,
            CompanyIdentification = item.CompanyIdentification,
            CompanyEntryDescriptionId = item.CompanyEntryDescriptionId,
            SourcePersonType = item.SourcePersonType,
            RecipientPersonType = item.RecipientPersonType,
            RecipientIdNumber = item.RecipientIdNumber,
            RecipientName = item.RecipientName,
            RequiresIdentityValidation = item.RequiresIdentityValidation,
            Addendas = item.Addendas
        };
    }

    private static void ValidateBatchRequest(BulkAchTransactionRequest request, int maxItems)
    {
        if (string.IsNullOrWhiteSpace(request.BatchReference))
        {
            throw new ArgumentException("batchReference es obligatorio.", nameof(request.BatchReference));
        }

        if (request.Transactions is null || request.Transactions.Count == 0)
        {
            throw new ArgumentException("El lote no contiene transacciones.", nameof(request.Transactions));
        }

        if (request.Transactions.Count > maxItems)
        {
            throw new ArgumentException($"El lote supera el máximo permitido de {maxItems} transacciones.", nameof(request.Transactions));
        }
    }

    private async Task<HashSet<int>> PreloadActiveCompanyEntryDescriptionIdsAsync(CancellationToken ct)
    {
        return await _context.CompanyEntryDescriptionCatalogs
            .AsNoTracking()
            .Where(item => item.IsActive)
            .Select(item => item.Id)
            .ToHashSetAsync(ct);
    }

    private static HashSet<string> GetDuplicateOperationalIds(IEnumerable<NormalizedBulkItem> normalizedItems)
    {
        return normalizedItems
            .Select(ResolveOperationalId)
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .GroupBy(x => x, StringComparer.OrdinalIgnoreCase)
            .Where(g => g.Count() > 1)
            .Select(g => g.Key)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
    }

    private async Task<HashSet<string>> LoadExistingOperationalIdsAsync(IEnumerable<NormalizedBulkItem> normalizedItems, CancellationToken ct)
    {
        var operationalIds = normalizedItems
            .Select(ResolveOperationalId)
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();

        if (operationalIds.Length == 0)
        {
            return [];
        }

        return (await _context.AchTransactions
            .AsNoTracking()
            .Where(x => operationalIds.Contains(x.TransactionExternalId)
                || (string.IsNullOrWhiteSpace(x.TransactionExternalId) && operationalIds.Contains(x.Reference)))
            .Select(x => string.IsNullOrWhiteSpace(x.TransactionExternalId) ? x.Reference : x.TransactionExternalId)
            .Distinct()
            .ToListAsync(ct))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
    }

    private static string ResolveOperationalId(NormalizedBulkItem item) => ResolveOperationalId(item.Data);

    private static string ResolveOperationalId(AchTransactionRequestData data)
    {
        return !string.IsNullOrWhiteSpace(data.TransactionExternalId)
            ? data.TransactionExternalId.Trim()
            : data.Reference.Trim();
    }

    private async Task<CustomerCache> BuildCustomerCacheAsync(IEnumerable<NormalizedBulkItem> normalizedItems, CancellationToken ct)
    {
        var documentNumbers = normalizedItems
            .SelectMany(x => new[] { x.Data.CompanyIdentification, x.Data.RecipientIdNumber })
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Select(x => x!.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();

        var customers = documentNumbers.Length == 0
            ? []
            : await _context.Customers
                .Include(c => c.Accounts)
                .Where(c => documentNumbers.Contains(c.DocumentNumber))
                .ToListAsync(ct);

        var byDocAndType = customers
            .GroupBy(c => $"{c.DocumentType}|{c.DocumentNumber}", StringComparer.OrdinalIgnoreCase)
            .ToDictionary(g => g.Key, g => g.First(), StringComparer.OrdinalIgnoreCase);

        var byDocNumber = customers
            .GroupBy(c => c.DocumentNumber, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(g => g.Key, g => g.ToList(), StringComparer.OrdinalIgnoreCase);

        return new CustomerCache(byDocAndType, byDocNumber);
    }

    private async Task<CustomerOnboardingContext> EnsureCustomerAndAccountsAsync(
        AchTransactionRequestData request,
        CustomerCache cache,
        IReadOnlyDictionary<string, string> documentTypeByDefault,
        IReadOnlyDictionary<string, string> personTypeByDefault,
        CancellationToken ct)
    {
        var sourceCustomer = await EnsureCustomerWithAccountAsync(
            documentNumber: request.CompanyIdentification,
            preferredName: request.CompanyName,
            accountNumber: request.SourceAccountNumber,
            defaultPersonType: ResolvePersonTypeCode(request.SourcePersonType, "PJ"),
            defaultDocumentType: "NIT",
            cache,
            documentTypeByDefault,
            personTypeByDefault,
            ct);

        Customer? recipientCustomer = null;
        if (!string.IsNullOrWhiteSpace(request.RecipientIdNumber))
        {
            recipientCustomer = await EnsureCustomerWithAccountAsync(
                documentNumber: request.RecipientIdNumber,
                preferredName: request.RecipientName,
                accountNumber: request.DestinationAccountNumber,
                defaultPersonType: ResolvePersonTypeCode(request.RecipientPersonType, "PN"),
                defaultDocumentType: "CC",
                cache,
                documentTypeByDefault,
                personTypeByDefault,
                ct);
        }

        return new CustomerOnboardingContext(sourceCustomer, recipientCustomer);
    }

    private async Task<Customer?> EnsureCustomerWithAccountAsync(
        string? documentNumber,
        string? preferredName,
        string? accountNumber,
        string defaultPersonType,
        string defaultDocumentType,
        CustomerCache cache,
        IReadOnlyDictionary<string, string> documentTypeByDefault,
        IReadOnlyDictionary<string, string> personTypeByDefault,
        CancellationToken ct)
    {
        var normalizedDocument = (documentNumber ?? string.Empty).Trim();
        var normalizedAccount = (accountNumber ?? string.Empty).Trim();

        if (string.IsNullOrWhiteSpace(normalizedDocument) || string.IsNullOrWhiteSpace(normalizedAccount))
        {
            return null;
        }

        var resolvedDocumentType = documentTypeByDefault[defaultDocumentType];
        var resolvedPersonType = personTypeByDefault[defaultPersonType];

        var compositeKey = $"{resolvedDocumentType}|{normalizedDocument}";
        cache.ByDocumentAndType.TryGetValue(compositeKey, out var customer);

        if (customer is null && cache.ByDocumentNumber.TryGetValue(normalizedDocument, out var legacyCustomers) && legacyCustomers.Count == 1)
        {
            customer = legacyCustomers[0];
            if (customer.DocumentType == "OTRO" && customer.DocumentType != resolvedDocumentType)
            {
                customer.DocumentType = resolvedDocumentType;
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
            cache.ByDocumentAndType[compositeKey] = customer;
            if (!cache.ByDocumentNumber.TryGetValue(normalizedDocument, out var documentList))
            {
                documentList = [];
                cache.ByDocumentNumber[normalizedDocument] = documentList;
            }
            documentList.Add(customer);
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

    private static (string FirstName, string LastName, string? CompanyName) BuildAutoProfile(string personType, string? preferredName)
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

    private sealed record NormalizedBulkItem(int Index, BulkAchTransactionItemRequest Item, AchTransactionRequestData Data);
    private sealed record CustomerOnboardingContext(Customer? SourceCustomer, Customer? RecipientCustomer);
    private sealed record CustomerCache(
        Dictionary<string, Customer> ByDocumentAndType,
        Dictionary<string, List<Customer>> ByDocumentNumber);
}
