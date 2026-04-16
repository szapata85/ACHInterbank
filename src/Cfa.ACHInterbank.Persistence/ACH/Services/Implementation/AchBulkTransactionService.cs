using Cfa.ACHInterbank.Application.ACH.Interfaces;
using Cfa.ACHInterbank.Application.ACH.Interfaces.Repositories;
using Cfa.ACHInterbank.Application.ACH.Models;
using Cfa.ACHInterbank.Application.DataBase;
using Cfa.ACHInterbank.Domain.Models.ACH;
using Cfa.ACHInterbank.Domain.Models.Configurations;
using Cfa.ACHInterbank.Persistence.DataBase;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

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
    private readonly ITransactionPolicyService? _transactionPolicyService;
    private readonly IConfiguration _configuration;
    private readonly IContrapartidaDispatchPersistenceService _contrapartidaDispatchPersistenceService;
    private readonly ICenitCycleQueueService? _cenitCycleQueueService;
    private readonly IAchRegulatoryCatalogService? _catalogService;

    public AchBulkTransactionService(
        AchDbContext context,
        IUnitOfWork unitOfWork,
        IAchCustomerRepository customerRepository,
        ITransactionValidator transactionValidator,
        IBatchResolver batchResolver,
        ITransactionPersister transactionPersister,
        IPrenotificationHandler prenotificationHandler,
        IConfiguration configuration,
        IContrapartidaDispatchPersistenceService contrapartidaDispatchPersistenceService,
        ICenitCycleQueueService? cenitCycleQueueService = null,
        IAchRegulatoryCatalogService? catalogService = null,
        ITransactionPolicyService? transactionPolicyService = null)
    {
        _context = context;
        _unitOfWork = unitOfWork;
        _customerRepository = customerRepository;
        _transactionValidator = transactionValidator;
        _batchResolver = batchResolver;
        _transactionPersister = transactionPersister;
        _prenotificationHandler = prenotificationHandler;
        _configuration = configuration;
        _contrapartidaDispatchPersistenceService = contrapartidaDispatchPersistenceService;
        _cenitCycleQueueService = cenitCycleQueueService;
        _catalogService = catalogService;
        _transactionPolicyService = transactionPolicyService;
    }

    public async Task<BulkAchTransactionResponse> RegisterBulkAsync(BulkAchTransactionRequest request, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(request);

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

        var customerCache = await BuildCustomerCacheAsync(normalizedItems, ct);
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

            var touchedBatches = new HashSet<AchBatch>(AchBatchReferenceComparer.Instance);
            var pendingSuccesses = new List<(BulkAchTransactionItemResult Result, AchTransaction Transaction)>();

            foreach (var record in chunk)
            {
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
                        var prenoteRequired = await _catalogService.IsPrenotificationRequiredAsync(record.Data.Type, ct);
                        if (prenoteRequired && !record.Data.IsPrenotification)
                        {
                            throw new ArgumentException($"La política regulatoria exige prenotificación para tipo {record.Data.Type}.", nameof(record.Data.IsPrenotification));
                        }
                    }

                    if (_transactionPolicyService is not null)
                    {
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

                    await EnsureCustomerAndAccountsAsync(record.Data, customerCache, documentTypeByDefault, personTypeByDefault, ct);

                    var batchContext = await _batchResolver.ResolveAsync(record.Data, ct);
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
                        await _prenotificationHandler.HandleAsync(record.Data, persisted.Transaction, ct);
                    }

                    touchedBatches.Add(persisted.Batch);
                    await _contrapartidaDispatchPersistenceService.EnsurePendingDispatchAsync(
                        persisted.Transaction,
                        batchContext.ClearingHouseId,
                        ct);

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
            }

            if (touchedBatches.Count > 0)
            {
                foreach (var batch in touchedBatches)
                {
                    await _transactionPersister.UpdateBatchTotalsAsync(batch, ct);
                    await _transactionPersister.UpdateBatchServiceClassCodeAsync(batch, ct);
                }

                await _unitOfWork.CommitAsync(ct);

                foreach (var pending in pendingSuccesses)
                {
                    pending.Result.TransactionId = pending.Transaction.Id;
                    if (pending.Transaction.Id > 0)
                    {
                        response.CreatedTransactionIds.Add(pending.Transaction.Id);
                    }
                }

                _context.ChangeTracker.Clear();
            }
        }

        response.TotalProcessed = response.ItemResults.Count;
        response.TotalSucceeded = response.ItemResults.Count(x => x.Succeeded);
        response.TotalFailed = response.ItemResults.Count(x => !x.Succeeded);

        return response;
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

    private async Task EnsureCustomerAndAccountsAsync(
        AchTransactionRequestData request,
        CustomerCache cache,
        IReadOnlyDictionary<string, string> documentTypeByDefault,
        IReadOnlyDictionary<string, string> personTypeByDefault,
        CancellationToken ct)
    {
        await EnsureCustomerWithAccountAsync(
            documentNumber: request.CompanyIdentification,
            preferredName: request.CompanyName,
            accountNumber: request.SourceAccountNumber,
            defaultPersonType: ResolvePersonTypeCode(request.SourcePersonType, "PJ"),
            defaultDocumentType: "NIT",
            cache,
            documentTypeByDefault,
            personTypeByDefault,
            ct);

        if (!string.IsNullOrWhiteSpace(request.RecipientIdNumber))
        {
            await EnsureCustomerWithAccountAsync(
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
    }

    private async Task EnsureCustomerWithAccountAsync(
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
            return;
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

    private sealed class AchBatchReferenceComparer : IEqualityComparer<AchBatch>
    {
        public static readonly AchBatchReferenceComparer Instance = new();
        public bool Equals(AchBatch? x, AchBatch? y) => ReferenceEquals(x, y);
        public int GetHashCode(AchBatch obj) => System.Runtime.CompilerServices.RuntimeHelpers.GetHashCode(obj);
    }

    private sealed record NormalizedBulkItem(int Index, BulkAchTransactionItemRequest Item, AchTransactionRequestData Data);
    private sealed record CustomerCache(
        Dictionary<string, Customer> ByDocumentAndType,
        Dictionary<string, List<Customer>> ByDocumentNumber);
}
