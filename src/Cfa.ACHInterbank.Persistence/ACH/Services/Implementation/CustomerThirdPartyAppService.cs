using Cfa.ACHInterbank.Application.ACH.Interfaces;
using Cfa.ACHInterbank.Application.Common;
using Cfa.ACHInterbank.Domain.Entities.Transactions.Dtos;
using Cfa.ACHInterbank.Domain.Entities.Transactions.Enums;
using Cfa.ACHInterbank.Domain.Models.ACH;
using Cfa.ACHInterbank.Domain.Models.ACH.Enums;
using Cfa.ACHInterbank.Domain.Models.Configurations;
using Cfa.ACHInterbank.Persistence.DataBase;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Cfa.ACHInterbank.Persistence.ACH.Services.Implementation;

[Scoped]
public class CustomerThirdPartyAppService : ICustomerThirdPartyAppService
{
    private readonly AchDbContext _context;
    private readonly IThirdPartyAccountShadowComparisonService? _shadowComparisonService;
    private readonly ILogger<CustomerThirdPartyAppService>? _logger;

    public CustomerThirdPartyAppService(
        AchDbContext context,
        IThirdPartyAccountShadowComparisonService? shadowComparisonService = null,
        ILogger<CustomerThirdPartyAppService>? logger = null)
    {
        _context = context;
        _shadowComparisonService = shadowComparisonService;
        _logger = logger;
    }

    public async Task<PagedResponse<CustomerThirdPartyListDto>> GetAsync(CustomerThirdPartyQuery query, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(query);
        ct.ThrowIfCancellationRequested();

        var page = query.Page <= 0 ? 1 : query.Page;
        var pageSize = query.PageSize <= 0 ? 20 : query.PageSize;
        var sourceResolution = await ResolveSourceCustomerAsync(query.SourceAccountNumber, ct);

        IReadOnlyList<CustomerThirdPartyListDto> rows;
        if (sourceResolution.IsAmbiguous || sourceResolution.IsMissing)
        {
            rows = [];
        }
        else
        {
            var legacyRows = await LoadLegacyCompatibilityRowsAsync(query, sourceResolution.CustomerId, ct);
            try
            {
                var canonicalAccounts = await LoadCanonicalAccountsAsync(query, sourceResolution.CustomerId, ct);
                rows = await BuildCutoverRowsAsync(canonicalAccounts, legacyRows, ct);
            }
            catch (OperationCanceledException) when (ct.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logger?.LogWarning(
                    "TPA8B canonical compatibility read failed closed to conservative legacy fallback without PII. ErrorType={ErrorType}.",
                    ex.GetType().Name);
                rows = legacyRows.Select(MapLegacyFallbackRow).ToArray();
            }
        }

        var filteredRows = query.Status.HasValue
            ? rows.Where(x => x.Status == query.Status.Value).ToArray()
            : rows.ToArray();
        var orderedRows = filteredRows
            .OrderByDescending(x => x.ValidationReceivedAt ?? DateTime.MinValue)
            .ThenByDescending(x => x.Id)
            .ToArray();

        var response = new PagedResponse<CustomerThirdPartyListDto>
        {
            Items = orderedRows.Skip((page - 1) * pageSize).Take(pageSize).ToArray(),
            Total = orderedRows.Length,
            Page = page,
            PageSize = pageSize
        };

        // TPA8B keeps the TPA4 legacy/canonical comparison as observation only. The response above is
        // canonical-first; shadow failures and legacy CustomerThirdParty.Status never change it.
        await RunLegacyShadowComparisonAsync(query, ct);

        return response;
    }

    private async Task<IReadOnlyList<ThirdPartyAccount>> LoadCanonicalAccountsAsync(
        CustomerThirdPartyQuery query,
        int? sourceCustomerId,
        CancellationToken ct)
    {
        var dataQuery = _context.ThirdPartyAccounts
            .AsNoTracking()
            .Include(x => x.ThirdParty)
                .ThenInclude(x => x.Customer)
            .Include(x => x.FinancialInstitution)
            .Include(x => x.Validations)
                .ThenInclude(x => x.ClearingHouse)
            .Include(x => x.Validations)
                .ThenInclude(x => x.Attempts)
            .AsQueryable();

        if (sourceCustomerId.HasValue)
        {
            dataQuery = dataQuery.Where(x => x.ThirdParty.CustomerId == sourceCustomerId.Value);
        }

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var term = query.Search.Trim();
            var normalizedDocument = term.ToUpperInvariant();
            dataQuery = dataQuery.Where(x =>
                (x.ThirdParty.Customer.FirstName + " " + x.ThirdParty.Customer.LastName).Contains(term)
                || (x.ThirdParty.Customer.CompanyName ?? string.Empty).Contains(term)
                || x.AccountNumberNormalized.Contains(term)
                || x.ThirdParty.DocumentNumberNormalized.Contains(normalizedDocument));
        }

        if (!string.IsNullOrWhiteSpace(query.DestinationAccountNumber))
        {
            var destinationAccount = query.DestinationAccountNumber.Trim();
            dataQuery = dataQuery.Where(x => x.AccountNumberNormalized.Contains(destinationAccount));
        }

        if (!string.IsNullOrWhiteSpace(query.RecipientIdNumber))
        {
            var recipientIdNumber = query.RecipientIdNumber.Trim().ToUpperInvariant();
            dataQuery = dataQuery.Where(x => x.ThirdParty.DocumentNumberNormalized.Contains(recipientIdNumber));
        }

        if (query.DestinationInstitutionId.HasValue)
        {
            dataQuery = dataQuery.Where(x => x.FinancialInstitutionId == query.DestinationInstitutionId.Value);
        }

        return await dataQuery.ToListAsync(ct);
    }

    private async Task<IReadOnlyList<LegacyCompatibilityRow>> LoadLegacyCompatibilityRowsAsync(
        CustomerThirdPartyQuery query,
        int? sourceCustomerId,
        CancellationToken ct)
    {
        var dataQuery = _context.CustomerThirdParties
            .AsNoTracking()
            .Include(x => x.Customer)
            .Include(x => x.DestinationInstitution)
            .AsQueryable();

        if (sourceCustomerId.HasValue)
        {
            dataQuery = dataQuery.Where(x => x.CustomerId == sourceCustomerId.Value);
        }

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var term = query.Search.Trim();
            dataQuery = dataQuery.Where(x =>
                (x.Customer.FirstName + " " + x.Customer.LastName).Contains(term)
                || (x.Customer.CompanyName ?? string.Empty).Contains(term)
                || x.DestinationAccountNumber.Contains(term)
                || x.RecipientIdNumber.Contains(term));
        }

        if (!string.IsNullOrWhiteSpace(query.DestinationAccountNumber))
        {
            var destinationAccount = query.DestinationAccountNumber.Trim();
            dataQuery = dataQuery.Where(x => x.DestinationAccountNumber.Contains(destinationAccount));
        }

        if (!string.IsNullOrWhiteSpace(query.RecipientIdNumber))
        {
            var recipientIdNumber = query.RecipientIdNumber.Trim();
            dataQuery = dataQuery.Where(x => x.RecipientIdNumber.Contains(recipientIdNumber));
        }

        if (query.DestinationInstitutionId.HasValue)
        {
            dataQuery = dataQuery.Where(x => x.DestinationInstitutionId == query.DestinationInstitutionId.Value);
        }

        return await dataQuery
            .Select(x => new LegacyCompatibilityRow(
                x.Id,
                x.CustomerId,
                x.Customer.CompanyName,
                x.Customer.FirstName,
                x.Customer.LastName,
                x.Customer.SecondLastName,
                x.DestinationInstitutionId,
                x.DestinationInstitution.Name,
                x.DestinationAccountNumber,
                x.RecipientIdNumber,
                x.PrenotificationTransactionId,
                x.ValidationCycleId,
                x.ValidationReceivedAt,
                x.ValidationMessage,
                x.PrenotificationTransaction == null
                    ? null
                    : x.PrenotificationTransaction.AchCycle.ClearingHouse!.Name,
                x.PrenotificationTransaction == null
                    ? null
                    : x.PrenotificationTransaction.AchCycle.ClearingHouse!.Code,
                x.PrenotificationTransaction == null
                    ? null
                    : x.PrenotificationTransaction.AchCycleId,
                x.PrenotificationTransactionId == null
                    ? null
                    : _context.IncomingNachaTransactionLinks
                        .Where(link => link.AchTransactionId == x.PrenotificationTransactionId && link.IsFinal)
                        .OrderByDescending(link => link.LinkedAtUtc)
                        .Select(link => link.Ingestion.FileName)
                        .FirstOrDefault(),
                x.PrenotificationTransactionId == null
                    ? null
                    : _context.IncomingNachaTransactionLinks
                        .Where(link => link.AchTransactionId == x.PrenotificationTransactionId && link.IsFinal)
                        .OrderByDescending(link => link.LinkedAtUtc)
                        .Select(link => link.Ingestion.CorrelationId)
                        .FirstOrDefault()))
            .ToListAsync(ct);
    }

    private async Task<IReadOnlyList<CustomerThirdPartyListDto>> BuildCutoverRowsAsync(
        IReadOnlyList<ThirdPartyAccount> canonicalAccounts,
        IReadOnlyList<LegacyCompatibilityRow> legacyRows,
        CancellationToken ct)
    {
        var legacyByKey = legacyRows
            .Select(row => new { Row = row, Key = TryCreateLegacyNaturalKey(row) })
            .Where(x => x.Key.HasValue)
            .GroupBy(x => x.Key!.Value)
            .ToDictionary(x => x.Key, x => x.Select(y => y.Row).ToArray());

        var canonicalKeys = canonicalAccounts
            .Select(CreateCanonicalNaturalKey)
            .ToHashSet();

        var canonicalProjections = new List<CanonicalCompatibilityProjection>(canonicalAccounts.Count);
        foreach (var account in canonicalAccounts)
        {
            var key = CreateCanonicalNaturalKey(account);
            legacyByKey.TryGetValue(key, out var matchingLegacyRows);
            canonicalProjections.Add(ResolveCanonicalProjection(account, matchingLegacyRows ?? []));
        }

        var responseEvidenceByTransactionId = await LoadResponseEvidenceAsync(
            canonicalProjections
                .Where(x => x.Attempt?.AchTransactionId.HasValue == true)
                .Select(x => x.Attempt!.AchTransactionId!.Value)
                .Distinct()
                .ToArray(),
            ct);

        var result = canonicalProjections
            .Select(x => MapCanonicalRow(x, responseEvidenceByTransactionId))
            .ToList();

        foreach (var legacy in legacyRows)
        {
            var legacyKey = TryCreateLegacyNaturalKey(legacy);
            if (legacyKey.HasValue && canonicalKeys.Contains(legacyKey.Value))
            {
                continue;
            }

            // Legacy-only rows remain visible for compatibility/rollback, but legacy Pending/Active/Rejected
            // is never projected as functional authority after TPA8B. Without canonical evidence the safe
            // compatibility state is Pending and cannot satisfy an Active/Rejected filter.
            result.Add(MapLegacyFallbackRow(legacy));
        }

        return result;
    }

    private static CanonicalCompatibilityProjection ResolveCanonicalProjection(
        ThirdPartyAccount account,
        IReadOnlyCollection<LegacyCompatibilityRow> matchingLegacyRows)
    {
        var prenotificationValidations = account.Validations
            .Where(x => x.ValidationType == ThirdPartyAccountValidationType.Prenotification)
            .ToArray();

        var linkedTransactionIds = matchingLegacyRows
            .Where(x => x.PrenotificationTransactionId.HasValue)
            .Select(x => x.PrenotificationTransactionId!.Value)
            .Distinct()
            .ToHashSet();

        ThirdPartyAccountValidation? selectedValidation = null;
        var statusSource = "Modelo canónico sin validación contextual";

        if (linkedTransactionIds.Count > 0)
        {
            var linkedValidations = prenotificationValidations
                .Where(validation => validation.Attempts.Any(attempt =>
                    attempt.AchTransactionId.HasValue && linkedTransactionIds.Contains(attempt.AchTransactionId.Value)))
                .DistinctBy(x => x.Id)
                .ToArray();

            if (linkedValidations.Length == 1)
            {
                selectedValidation = linkedValidations[0];
                statusSource = "Validación contextual canónica enlazada a prenotificación";
            }
            else if (linkedValidations.Length > 1)
            {
                statusSource = "Modelo canónico con contexto enlazado ambiguo; requiere contexto explícito";
            }
        }

        if (selectedValidation is null && linkedTransactionIds.Count == 0)
        {
            if (prenotificationValidations.Length == 1)
            {
                selectedValidation = prenotificationValidations[0];
                statusSource = "Validación contextual canónica única";
            }
            else if (prenotificationValidations.Length > 1)
            {
                statusSource = "Modelo canónico con múltiples contextos; requiere contexto explícito";
            }
        }

        var latestAttempt = selectedValidation?.Attempts
            .OrderByDescending(x => x.AttemptNumber)
            .ThenByDescending(x => x.Id)
            .FirstOrDefault();

        return new CanonicalCompatibilityProjection(account, selectedValidation, latestAttempt, statusSource);
    }

    private static CustomerThirdPartyListDto MapCanonicalRow(
        CanonicalCompatibilityProjection projection,
        IReadOnlyDictionary<int, ResponseEvidence> responseEvidenceByTransactionId)
    {
        var account = projection.Account;
        var validation = projection.Validation;
        var attempt = projection.Attempt;
        ResponseEvidence? responseEvidence = null;
        if (attempt?.AchTransactionId.HasValue == true)
        {
            responseEvidenceByTransactionId.TryGetValue(attempt.AchTransactionId.Value, out responseEvidence);
        }

        return new CustomerThirdPartyListDto
        {
            Id = account.Id,
            CustomerId = account.ThirdParty.CustomerId,
            CustomerName = CustomerDisplayName(
                account.ThirdParty.Customer.CompanyName,
                account.ThirdParty.Customer.FirstName,
                account.ThirdParty.Customer.LastName,
                account.ThirdParty.Customer.SecondLastName),
            DestinationInstitutionId = account.FinancialInstitutionId,
            DestinationInstitutionName = account.FinancialInstitution.Name,
            DestinationAccountNumber = account.AccountNumberNormalized,
            RecipientIdNumber = account.ThirdParty.DocumentNumberNormalized,
            Status = ToCompatibilityStatus(validation, attempt),
            PrenotificationTransactionId = attempt?.AchTransactionId,
            ValidationCycleId = attempt?.SourceCycleId,
            ValidationReceivedAt = attempt?.ResolvedAtUtc?.UtcDateTime,
            ValidationMessage = null,
            ClearingHouseName = validation?.ClearingHouse?.Name,
            ClearingHouseCode = validation?.ClearingHouse?.Code,
            SendingCycleId = attempt?.SourceCycleId,
            ResponseFileName = responseEvidence?.FileName,
            ResponseCorrelationId = responseEvidence?.CorrelationId,
            StatusSource = projection.StatusSource
        };
    }

    private static CustomerThirdPartyListDto MapLegacyFallbackRow(LegacyCompatibilityRow legacy)
        => new()
        {
            Id = legacy.Id,
            CustomerId = legacy.CustomerId,
            CustomerName = CustomerDisplayName(legacy.CompanyName, legacy.FirstName, legacy.LastName, legacy.SecondLastName),
            DestinationInstitutionId = legacy.DestinationInstitutionId,
            DestinationInstitutionName = legacy.DestinationInstitutionName,
            DestinationAccountNumber = legacy.DestinationAccountNumber,
            RecipientIdNumber = legacy.RecipientIdNumber,
            Status = CustomerThirdPartyStatusEnum.Pending,
            PrenotificationTransactionId = legacy.PrenotificationTransactionId,
            ValidationCycleId = legacy.ValidationCycleId,
            ValidationReceivedAt = legacy.ValidationReceivedAt,
            ValidationMessage = "Registro legacy visible por compatibilidad; falta autoridad canónica verificable.",
            ClearingHouseName = legacy.ClearingHouseName,
            ClearingHouseCode = legacy.ClearingHouseCode,
            SendingCycleId = legacy.SendingCycleId,
            ResponseFileName = legacy.ResponseFileName,
            ResponseCorrelationId = legacy.ResponseCorrelationId,
            StatusSource = "Compatibilidad legacy sin autoridad funcional"
        };

    private static CustomerThirdPartyStatusEnum ToCompatibilityStatus(
        ThirdPartyAccountValidation? validation,
        PrenotificationValidationAttempt? latestAttempt)
    {
        if (validation is null || latestAttempt is null)
        {
            return CustomerThirdPartyStatusEnum.Pending;
        }
        if (validation.CurrentAttemptId.HasValue && validation.CurrentAttemptId.Value != latestAttempt.Id)
        {
            return CustomerThirdPartyStatusEnum.Pending;
        }

        return validation.Status switch
        {
            ThirdPartyAccountValidationStatus.Accepted
                when latestAttempt.Status == PrenotificationValidationAttemptStatus.Accepted
                     && latestAttempt.Resolution == PrenotificationValidationResolution.Accepted
                => CustomerThirdPartyStatusEnum.Active,
            ThirdPartyAccountValidationStatus.TacitlyAccepted
                when latestAttempt.Status == PrenotificationValidationAttemptStatus.TacitlyAccepted
                     && latestAttempt.Resolution == PrenotificationValidationResolution.TacitlyAccepted
                => CustomerThirdPartyStatusEnum.Active,
            ThirdPartyAccountValidationStatus.Rejected
                when latestAttempt.Status == PrenotificationValidationAttemptStatus.Rejected
                     && latestAttempt.Resolution == PrenotificationValidationResolution.Rejected
                => CustomerThirdPartyStatusEnum.Rejected,
            _ => CustomerThirdPartyStatusEnum.Pending
        };
    }

    private async Task<IReadOnlyDictionary<int, ResponseEvidence>> LoadResponseEvidenceAsync(
        IReadOnlyCollection<int> transactionIds,
        CancellationToken ct)
    {
        if (transactionIds.Count == 0)
        {
            return new Dictionary<int, ResponseEvidence>();
        }

        var rows = await _context.IncomingNachaTransactionLinks
            .AsNoTracking()
            .Where(x => x.IsFinal && x.AchTransactionId.HasValue && transactionIds.Contains(x.AchTransactionId.Value))
            .Select(x => new
            {
                TransactionId = x.AchTransactionId!.Value,
                x.LinkedAtUtc,
                x.Ingestion.FileName,
                x.Ingestion.CorrelationId
            })
            .ToListAsync(ct);

        return rows
            .GroupBy(x => x.TransactionId)
            .ToDictionary(
                x => x.Key,
                x => x.OrderByDescending(y => y.LinkedAtUtc)
                    .Select(y => new ResponseEvidence(y.FileName, y.CorrelationId))
                    .First());
    }

    private async Task<SourceCustomerResolution> ResolveSourceCustomerAsync(string? sourceAccountNumber, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(sourceAccountNumber))
        {
            return new SourceCustomerResolution(null, false, false);
        }

        var normalized = sourceAccountNumber.Trim();
        var customerIds = await _context.Customers
            .AsNoTracking()
            .Where(x => x.Accounts.Any(a => a.AccountNumber == normalized))
            .Select(x => x.Id)
            .Distinct()
            .Take(2)
            .ToListAsync(ct);

        if (customerIds.Count == 1)
        {
            return new SourceCustomerResolution(customerIds[0], false, false);
        }

        if (customerIds.Count == 0)
        {
            return new SourceCustomerResolution(null, false, true);
        }

        _logger?.LogWarning(
            "TPA8B canonical compatibility read skipped an ambiguous source-account customer resolution without logging account data.");
        return new SourceCustomerResolution(null, true, false);
    }

    private async Task RunLegacyShadowComparisonAsync(CustomerThirdPartyQuery query, CancellationToken ct)
    {
        if (_shadowComparisonService is null)
        {
            return;
        }

        try
        {
            var legacyShadow = await LoadLegacyShadowSliceAsync(query, ct);
            await _shadowComparisonService.CompareLegacyReadAsync(
                query,
                legacyShadow.Ids,
                legacyShadow.Total,
                ct);
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            _logger?.LogDebug(
                "TPA8B shadow comparison was cancelled after the canonical read completed; canonical response authority is preserved.");
        }
        catch (Exception ex)
        {
            _logger?.LogWarning(
                "TPA8B shadow comparison failed without changing the canonical customer-third-party compatibility response and without PII. ErrorType={ErrorType}.",
                ex.GetType().Name);
        }
    }

    private async Task<LegacyShadowSlice> LoadLegacyShadowSliceAsync(CustomerThirdPartyQuery query, CancellationToken ct)
    {
        var page = query.Page <= 0 ? 1 : query.Page;
        var pageSize = query.PageSize <= 0 ? 20 : query.PageSize;
        var dataQuery = _context.CustomerThirdParties
            .AsNoTracking()
            .Include(x => x.Customer)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var term = query.Search.Trim();
            dataQuery = dataQuery.Where(x =>
                (x.Customer.FirstName + " " + x.Customer.LastName).Contains(term)
                || (x.Customer.CompanyName ?? string.Empty).Contains(term)
                || x.DestinationAccountNumber.Contains(term)
                || x.RecipientIdNumber.Contains(term));
        }

        if (!string.IsNullOrWhiteSpace(query.DestinationAccountNumber))
        {
            var destinationAccount = query.DestinationAccountNumber.Trim();
            dataQuery = dataQuery.Where(x => x.DestinationAccountNumber.Contains(destinationAccount));
        }

        if (!string.IsNullOrWhiteSpace(query.RecipientIdNumber))
        {
            var recipientIdNumber = query.RecipientIdNumber.Trim();
            dataQuery = dataQuery.Where(x => x.RecipientIdNumber.Contains(recipientIdNumber));
        }

        if (query.DestinationInstitutionId.HasValue)
        {
            dataQuery = dataQuery.Where(x => x.DestinationInstitutionId == query.DestinationInstitutionId.Value);
        }

        if (!string.IsNullOrWhiteSpace(query.SourceAccountNumber))
        {
            var sourceResolution = await ResolveSourceCustomerAsync(query.SourceAccountNumber, ct);
            if (sourceResolution.IsMissing || sourceResolution.IsAmbiguous || !sourceResolution.CustomerId.HasValue)
            {
                return new LegacyShadowSlice([], 0);
            }

            dataQuery = dataQuery.Where(x => x.CustomerId == sourceResolution.CustomerId.Value);
        }

        if (query.Status.HasValue)
        {
            dataQuery = dataQuery.Where(x => x.Status == query.Status.Value);
        }

        var total = await dataQuery.CountAsync(ct);
        var ids = await dataQuery
            .OrderByDescending(x => x.ValidationReceivedAt)
            .ThenByDescending(x => x.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(x => x.Id)
            .ToArrayAsync(ct);

        return new LegacyShadowSlice(ids, total);
    }

    private static LegacyNaturalKey CreateCanonicalNaturalKey(ThirdPartyAccount account)
        => new(
            account.ThirdParty.CustomerId,
            account.FinancialInstitutionId,
            account.AccountNumberNormalized,
            account.ThirdParty.DocumentNumberNormalized);

    private static LegacyNaturalKey? TryCreateLegacyNaturalKey(LegacyCompatibilityRow row)
    {
        if (row.CustomerId <= 0
            || row.DestinationInstitutionId <= 0
            || string.IsNullOrWhiteSpace(row.DestinationAccountNumber)
            || string.IsNullOrWhiteSpace(row.RecipientIdNumber))
        {
            return null;
        }

        try
        {
            return new LegacyNaturalKey(
                row.CustomerId,
                row.DestinationInstitutionId,
                ThirdPartyAccount.NormalizeAccountNumber(row.DestinationAccountNumber),
                ThirdParty.NormalizeDocumentNumber(row.RecipientIdNumber));
        }
        catch (ArgumentException)
        {
            return null;
        }
    }

    private static string CustomerDisplayName(string? companyName, string? firstName, string? lastName, string? secondLastName)
        => string.IsNullOrWhiteSpace(companyName)
            ? string.Join(" ", new[] { firstName, lastName, secondLastName }.Where(x => !string.IsNullOrWhiteSpace(x)))
            : companyName.Trim();

    private sealed record SourceCustomerResolution(int? CustomerId, bool IsAmbiguous, bool IsMissing);

    private sealed record LegacyShadowSlice(IReadOnlyCollection<int> Ids, int Total);

    private readonly record struct LegacyNaturalKey(
        int CustomerId,
        int DestinationInstitutionId,
        string AccountNumberNormalized,
        string DocumentNumberNormalized);

    private sealed record LegacyCompatibilityRow(
        int Id,
        int CustomerId,
        string? CompanyName,
        string? FirstName,
        string? LastName,
        string? SecondLastName,
        int DestinationInstitutionId,
        string DestinationInstitutionName,
        string DestinationAccountNumber,
        string RecipientIdNumber,
        int? PrenotificationTransactionId,
        string? ValidationCycleId,
        DateTime? ValidationReceivedAt,
        string? ValidationMessage,
        string? ClearingHouseName,
        string? ClearingHouseCode,
        string? SendingCycleId,
        string? ResponseFileName,
        string? ResponseCorrelationId);

    private sealed record CanonicalCompatibilityProjection(
        ThirdPartyAccount Account,
        ThirdPartyAccountValidation? Validation,
        PrenotificationValidationAttempt? Attempt,
        string StatusSource);

    private sealed record ResponseEvidence(string FileName, string CorrelationId);
}
