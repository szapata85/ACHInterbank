using System.Diagnostics.Metrics;
using Cfa.ACHInterbank.Application.ACH.Interfaces;
using Cfa.ACHInterbank.Application.ACH.Models;
using Cfa.ACHInterbank.Application.ACH.Services;
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
public sealed class ThirdPartyAccountShadowComparisonService : IThirdPartyAccountShadowComparisonService
{
    private const int MaxCanonicalOnlyScope = 200;
    private static readonly Meter Meter = new("Cfa.ACHInterbank.ThirdPartyAccountGovernance", "1.0");
    private static readonly Counter<long> ComparisonCounter = Meter.CreateCounter<long>(
        "achinterbank.third_party_account.shadow_comparison",
        unit: "comparisons",
        description: "TPA4 legacy/canonical read shadow comparison outcomes without customer or account identifiers.");

    private readonly AchDbContext _context;
    private readonly IThirdPartyAccountEligibilityService _eligibilityService;
    private readonly ILogger<ThirdPartyAccountShadowComparisonService> _logger;
    private readonly TimeProvider _timeProvider;

    public ThirdPartyAccountShadowComparisonService(
        AchDbContext context,
        IThirdPartyAccountEligibilityService eligibilityService,
        ILogger<ThirdPartyAccountShadowComparisonService> logger,
        TimeProvider? timeProvider = null)
    {
        _context = context;
        _eligibilityService = eligibilityService;
        _logger = logger;
        _timeProvider = timeProvider ?? TimeProvider.System;
    }

    public async Task<ThirdPartyAccountShadowComparisonSummary> CompareLegacyReadAsync(
        CustomerThirdPartyQuery query,
        IReadOnlyCollection<int> legacyThirdPartyIds,
        int legacyTotal,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(query);
        ArgumentNullException.ThrowIfNull(legacyThirdPartyIds);

        if (legacyThirdPartyIds.Count == 0 && !CanDiscoverCanonicalOnly(query, legacyTotal))
        {
            return EmptySummary();
        }

        var legacyRows = await _context.CustomerThirdParties
            .AsNoTracking()
            .Where(x => legacyThirdPartyIds.Contains(x.Id))
            .Select(x => new LegacyShadowRow(
                x.Id,
                x.CustomerId,
                x.DestinationInstitutionId,
                x.DestinationAccountNumber,
                x.RecipientIdNumber,
                x.Status,
                x.PrenotificationTransactionId,
                x.PrenotificationTransaction == null ? null : x.PrenotificationTransaction.TransactionCode,
                x.PrenotificationTransaction == null ? null : (int?)x.PrenotificationTransaction.CompanyEntryDescriptionId,
                x.PrenotificationTransaction == null ? null : (int?)x.PrenotificationTransaction.AchCycle.ClearingHouseId))
            .ToListAsync(ct);

        var transactionIds = legacyRows
            .Where(x => x.PrenotificationTransactionId.HasValue)
            .Select(x => x.PrenotificationTransactionId!.Value)
            .Distinct()
            .ToArray();

        var companyEntryDescriptionIds = legacyRows
            .Where(x => x.CompanyEntryDescriptionId.HasValue && x.CompanyEntryDescriptionId.Value > 0)
            .Select(x => x.CompanyEntryDescriptionId!.Value)
            .Distinct()
            .ToArray();
        var serviceTypeByCompanyEntryDescriptionId = companyEntryDescriptionIds.Length == 0
            ? new Dictionary<int, string>()
            : await _context.CompanyEntryDescriptionCatalogs
                .AsNoTracking()
                .Where(x => companyEntryDescriptionIds.Contains(x.Id))
                .ToDictionaryAsync(x => x.Id, x => x.StandardEntryClassCode, ct);

        List<PrenotificationValidationAttempt> linkedAttempts = transactionIds.Length == 0
            ? []
            : await _context.PrenotificationValidationAttempts
                .AsNoTracking()
                .Include(x => x.ThirdPartyAccountValidation)
                .Where(x => x.AchTransactionId.HasValue && transactionIds.Contains(x.AchTransactionId.Value))
                .ToListAsync(ct);
        var linkedAttemptByTransaction = linkedAttempts
            .Where(x => x.AchTransactionId.HasValue)
            .GroupBy(x => x.AchTransactionId!.Value)
            .ToDictionary(x => x.Key, x => x.OrderByDescending(a => a.AttemptNumber).ThenByDescending(a => a.Id).First());

        var customerIds = legacyRows.Select(x => x.CustomerId).Distinct().ToArray();
        List<ThirdPartyAccount> canonicalAccounts = customerIds.Length == 0
            ? []
            : await _context.ThirdPartyAccounts
                .AsNoTracking()
                .Include(x => x.ThirdParty)
                .Where(x => customerIds.Contains(x.ThirdParty.CustomerId))
                .ToListAsync(ct);

        var canonicalById = canonicalAccounts.ToDictionary(x => x.Id);
        var interim = new List<InterimMatch>(legacyRows.Count);
        var consumedCanonicalIds = new HashSet<long>();

        foreach (var legacy in legacyRows)
        {
            if (!HasCompleteLegacyIdentity(legacy))
            {
                interim.Add(new InterimMatch(legacy, null, false, null, null, null, null));
                continue;
            }

            PrenotificationValidationAttempt? linkedAttempt = null;
            if (legacy.PrenotificationTransactionId.HasValue)
            {
                linkedAttemptByTransaction.TryGetValue(legacy.PrenotificationTransactionId.Value, out linkedAttempt);
            }

            if (linkedAttempt is not null
                && canonicalById.TryGetValue(linkedAttempt.ThirdPartyAccountValidation.ThirdPartyAccountId, out var linkedAccount))
            {
                if (!IdentityMatches(legacy, linkedAccount))
                {
                    interim.Add(new InterimMatch(legacy, null, false, linkedAttempt.ThirdPartyAccountValidation, null, null, null));
                    continue;
                }

                consumedCanonicalIds.Add(linkedAccount.Id);
                interim.Add(new InterimMatch(
                    legacy,
                    linkedAccount,
                    true,
                    linkedAttempt.ThirdPartyAccountValidation,
                    linkedAttempt.ThirdPartyAccountValidation.ClearingHouseId,
                    linkedAttempt.ThirdPartyAccountValidation.TransactionNature,
                    linkedAttempt.ThirdPartyAccountValidation.ServiceType));
                continue;
            }

            var candidates = canonicalAccounts
                .Where(x => IdentityMatches(legacy, x))
                .ToArray();
            if (candidates.Length == 0)
            {
                interim.Add(new InterimMatch(legacy, null, true, null, null, null, null));
                continue;
            }

            if (candidates.Length > 1)
            {
                foreach (var candidate in candidates)
                {
                    consumedCanonicalIds.Add(candidate.Id);
                }
                interim.Add(new InterimMatch(legacy, null, false, null, null, null, null));
                continue;
            }

            var account = candidates[0];
            consumedCanonicalIds.Add(account.Id);
            var nature = ResolveNature(legacy.TransactionCode);
            var serviceType = legacy.CompanyEntryDescriptionId.HasValue
                && serviceTypeByCompanyEntryDescriptionId.TryGetValue(legacy.CompanyEntryDescriptionId.Value, out var resolvedServiceType)
                    ? resolvedServiceType
                    : null;
            interim.Add(new InterimMatch(
                legacy,
                account,
                true,
                null,
                legacy.ClearingHouseId,
                nature,
                serviceType));
        }

        var matchedAccountIds = interim
            .Where(x => x.Account is not null)
            .Select(x => x.Account!.Id)
            .Distinct()
            .ToArray();
        List<ThirdPartyAccountValidation> validations = matchedAccountIds.Length == 0
            ? []
            : await _context.ThirdPartyAccountValidations
                .AsNoTracking()
                .Where(x => matchedAccountIds.Contains(x.ThirdPartyAccountId))
                .ToListAsync(ct);

        var results = new List<ThirdPartyAccountShadowComparisonResult>(interim.Count + 4);
        var todayUtc = _timeProvider.GetUtcNow().UtcDateTime.Date;
        foreach (var match in interim)
        {
            if (!match.IdentityComplete)
            {
                results.Add(Result(ThirdPartyAccountShadowClassifier.Classify(new(
                    LegacyExists: true,
                    CanonicalExists: match.Account is not null,
                    IdentityComplete: false,
                    PolicyContextComplete: false,
                    LegacyStatus: match.Legacy.Status,
                    CanonicalValidationStatus: null,
                    CanonicalEligibilityDecision: null)), match));
                continue;
            }

            if (match.Account is null)
            {
                results.Add(Result(ThirdPartyAccountShadowDiscrepancyType.LegacyOnly, match));
                continue;
            }

            if (!match.ClearingHouseId.HasValue || match.ClearingHouseId.Value <= 0 || !match.TransactionNature.HasValue)
            {
                results.Add(Result(ThirdPartyAccountShadowDiscrepancyType.PolicyContextMissing, match));
                continue;
            }

            var serviceType = ThirdPartyAccountValidation.NormalizeServiceType(match.ServiceType);
            var validation = match.LinkedValidation
                ?? validations.FirstOrDefault(x => x.ThirdPartyAccountId == match.Account.Id
                    && x.ClearingHouseId == match.ClearingHouseId.Value
                    && x.TransactionNature == match.TransactionNature.Value
                    && x.ServiceType == serviceType
                    && x.ValidationType == ThirdPartyAccountValidationType.Prenotification);

            var eligibility = await _eligibilityService.EvaluateAsync(
                new ThirdPartyAccountEligibilityRequest(
                    match.Account.Id,
                    match.ClearingHouseId.Value,
                    match.TransactionNature.Value,
                    todayUtc,
                    serviceType),
                ct);

            var discrepancy = ThirdPartyAccountShadowClassifier.Classify(new(
                LegacyExists: true,
                CanonicalExists: true,
                IdentityComplete: true,
                PolicyContextComplete: true,
                LegacyStatus: match.Legacy.Status,
                CanonicalValidationStatus: validation?.Status,
                CanonicalEligibilityDecision: eligibility.Decision));
            results.Add(Result(discrepancy, match));
        }

        if (CanDiscoverCanonicalOnly(query, legacyTotal))
        {
            var sourceCustomerId = await ResolveSourceCustomerIdAsync(query.SourceAccountNumber!, ct);
            if (sourceCustomerId.HasValue)
            {
                var canonicalScope = ApplyLegacyComparableScope(
                    _context.ThirdPartyAccounts.AsNoTracking().Include(x => x.ThirdParty),
                    query,
                    sourceCustomerId.Value);
                var canonicalCount = await canonicalScope.CountAsync(ct);
                if (canonicalCount <= MaxCanonicalOnlyScope)
                {
                    var scopedCanonical = await canonicalScope.ToListAsync(ct);
                    foreach (var account in scopedCanonical.Where(x => !consumedCanonicalIds.Contains(x.Id)))
                    {
                        var hasLegacyIdentity = legacyRows.Any(legacy => HasCompleteLegacyIdentity(legacy) && IdentityMatches(legacy, account));
                        if (!hasLegacyIdentity)
                        {
                            results.Add(new ThirdPartyAccountShadowComparisonResult(
                                ThirdPartyAccountShadowDiscrepancyType.CanonicalOnly,
                                null,
                                account.Id));
                        }
                    }
                }
            }
        }

        return RecordAndLog(results);
    }

    private ThirdPartyAccountShadowComparisonSummary RecordAndLog(
        IReadOnlyCollection<ThirdPartyAccountShadowComparisonResult> results)
    {
        var counts = Enum.GetValues<ThirdPartyAccountShadowDiscrepancyType>()
            .ToDictionary(type => type, type => results.Count(x => x.Discrepancy == type));

        foreach (var (type, count) in counts.Where(x => x.Value > 0))
        {
            ComparisonCounter.Add(count, new KeyValuePair<string, object?>("discrepancy", type.ToString().ToUpperInvariant()));
        }

        _logger.LogInformation(
            "TPA4 shadow comparison completed without PII. Compared={Compared}, Match={Match}, LegacyOnly={LegacyOnly}, CanonicalOnly={CanonicalOnly}, StatusMismatch={StatusMismatch}, EligibilityMismatch={EligibilityMismatch}, IdentityIncomplete={IdentityIncomplete}, PolicyContextMissing={PolicyContextMissing}.",
            results.Count,
            counts[ThirdPartyAccountShadowDiscrepancyType.Match],
            counts[ThirdPartyAccountShadowDiscrepancyType.LegacyOnly],
            counts[ThirdPartyAccountShadowDiscrepancyType.CanonicalOnly],
            counts[ThirdPartyAccountShadowDiscrepancyType.StatusMismatch],
            counts[ThirdPartyAccountShadowDiscrepancyType.EligibilityMismatch],
            counts[ThirdPartyAccountShadowDiscrepancyType.IdentityIncomplete],
            counts[ThirdPartyAccountShadowDiscrepancyType.PolicyContextMissing]);

        return new ThirdPartyAccountShadowComparisonSummary(results.Count, counts);
    }

    private static ThirdPartyAccountShadowComparisonSummary EmptySummary()
        => new(0, Enum.GetValues<ThirdPartyAccountShadowDiscrepancyType>().ToDictionary(x => x, _ => 0));

    private static ThirdPartyAccountShadowComparisonResult Result(
        ThirdPartyAccountShadowDiscrepancyType discrepancy,
        InterimMatch match)
        => new(discrepancy, match.Legacy.Id, match.Account?.Id);

    private static bool HasCompleteLegacyIdentity(LegacyShadowRow row)
        => row.CustomerId > 0
            && row.DestinationInstitutionId > 0
            && !string.IsNullOrWhiteSpace(row.DestinationAccountNumber)
            && !string.IsNullOrWhiteSpace(row.RecipientIdNumber);

    private static bool IdentityMatches(LegacyShadowRow legacy, ThirdPartyAccount canonical)
        => canonical.ThirdParty.CustomerId == legacy.CustomerId
            && canonical.FinancialInstitutionId == legacy.DestinationInstitutionId
            && string.Equals(
                canonical.AccountNumberNormalized,
                ThirdPartyAccount.NormalizeAccountNumber(legacy.DestinationAccountNumber),
                StringComparison.Ordinal)
            && string.Equals(
                canonical.ThirdParty.DocumentNumberNormalized,
                ThirdParty.NormalizeDocumentNumber(legacy.RecipientIdNumber),
                StringComparison.Ordinal);

    private static TransactionNature? ResolveNature(string? transactionCode)
    {
        var direction = NachaTransactionCodeTaxonomy.ResolvePrenotificationDirection(transactionCode);
        return direction.HasValue ? TransactionPrerequisitePolicyService.ResolveNature(direction.Value) : null;
    }

    private static bool CanDiscoverCanonicalOnly(CustomerThirdPartyQuery query, int legacyTotal)
    {
        var page = query.Page <= 0 ? 1 : query.Page;
        var pageSize = query.PageSize <= 0 ? 20 : query.PageSize;
        return page == 1
            && legacyTotal <= pageSize
            && query.Status is null
            && !string.IsNullOrWhiteSpace(query.SourceAccountNumber);
    }

    private async Task<int?> ResolveSourceCustomerIdAsync(string sourceAccountNumber, CancellationToken ct)
    {
        var normalized = sourceAccountNumber.Trim();
        return await _context.Customers
            .AsNoTracking()
            .Where(x => x.Accounts.Any(a => a.AccountNumber == normalized))
            .Select(x => (int?)x.Id)
            .FirstOrDefaultAsync(ct);
    }

    private static IQueryable<ThirdPartyAccount> ApplyLegacyComparableScope(
        IQueryable<ThirdPartyAccount> source,
        CustomerThirdPartyQuery query,
        int customerId)
    {
        source = source.Where(x => x.ThirdParty.CustomerId == customerId);

        if (query.DestinationInstitutionId.HasValue)
        {
            source = source.Where(x => x.FinancialInstitutionId == query.DestinationInstitutionId.Value);
        }

        if (!string.IsNullOrWhiteSpace(query.DestinationAccountNumber))
        {
            var account = query.DestinationAccountNumber.Trim();
            source = source.Where(x => x.AccountNumberNormalized.Contains(account));
        }

        if (!string.IsNullOrWhiteSpace(query.RecipientIdNumber))
        {
            var document = query.RecipientIdNumber.Trim().ToUpperInvariant();
            source = source.Where(x => x.ThirdParty.DocumentNumberNormalized.Contains(document));
        }

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var term = query.Search.Trim();
            var normalizedDocument = term.ToUpperInvariant();
            source = source.Where(x =>
                x.AccountNumberNormalized.Contains(term)
                || x.ThirdParty.DocumentNumberNormalized.Contains(normalizedDocument));
        }

        return source;
    }

    private sealed record LegacyShadowRow(
        int Id,
        int CustomerId,
        int DestinationInstitutionId,
        string DestinationAccountNumber,
        string RecipientIdNumber,
        CustomerThirdPartyStatusEnum Status,
        int? PrenotificationTransactionId,
        string? TransactionCode,
        int? CompanyEntryDescriptionId,
        int? ClearingHouseId);

    private sealed record InterimMatch(
        LegacyShadowRow Legacy,
        ThirdPartyAccount? Account,
        bool IdentityComplete,
        ThirdPartyAccountValidation? LinkedValidation,
        int? ClearingHouseId,
        TransactionNature? TransactionNature,
        string? ServiceType);
}
