using Cfa.ACHInterbank.Application.ACH.Interfaces;
using Cfa.ACHInterbank.Application.ACH.Models;
using Cfa.ACHInterbank.Application.Common;
using Cfa.ACHInterbank.Domain.Models.ACH;
using Cfa.ACHInterbank.Domain.Models.Configurations;
using Cfa.ACHInterbank.Persistence.DataBase;
using Microsoft.EntityFrameworkCore;

namespace Cfa.ACHInterbank.Persistence.ACH.Services.Implementation;

[Scoped]
public sealed class ThirdPartyAccountReadService : IThirdPartyAccountReadService
{
    private const int MaxPageSize = 100;
    private readonly AchDbContext _context;
    private readonly IThirdPartyAccountEligibilityService _eligibilityService;

    public ThirdPartyAccountReadService(
        AchDbContext context,
        IThirdPartyAccountEligibilityService eligibilityService)
    {
        _context = context;
        _eligibilityService = eligibilityService;
    }

    public async Task<PagedResponse<ThirdPartyAccountReadDto>> GetAsync(
        ThirdPartyAccountReadQuery query,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(query);
        if (query.CustomerId is null or <= 0)
        {
            throw new ArgumentException("La consulta canónica requiere contexto de Customer.", nameof(query));
        }

        var page = query.Page <= 0 ? 1 : query.Page;
        var pageSize = Math.Clamp(query.PageSize <= 0 ? 20 : query.PageSize, 1, MaxPageSize);

        var filtered = ApplyFilters(_context.ThirdPartyAccounts.AsNoTracking(), query);
        var total = await filtered.CountAsync(ct);
        var accounts = await filtered
            .OrderBy(x => x.ThirdParty.CustomerId)
            .ThenBy(x => x.ThirdPartyId)
            .ThenBy(x => x.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Include(x => x.ThirdParty)
                .ThenInclude(x => x.Customer)
            .Include(x => x.FinancialInstitution)
            .Include(x => x.Validations)
                .ThenInclude(x => x.ClearingHouse)
            .Include(x => x.Validations)
                .ThenInclude(x => x.Attempts
                    .OrderByDescending(attempt => attempt.AttemptNumber)
                    .ThenByDescending(attempt => attempt.Id)
                    .Take(1))
            .ToListAsync(ct);

        var items = new List<ThirdPartyAccountReadDto>(accounts.Count);
        foreach (var account in accounts)
        {
            items.Add(await MapAsync(account, query.EligibilityContext, ct));
        }

        return new PagedResponse<ThirdPartyAccountReadDto>
        {
            Items = items,
            Total = total,
            Page = page,
            PageSize = pageSize
        };
    }

    public async Task<ThirdPartyAccountReadDto?> GetByAccountIdAsync(
        long thirdPartyAccountId,
        ThirdPartyAccountEligibilityReadContext? eligibilityContext = null,
        CancellationToken ct = default)
    {
        if (thirdPartyAccountId <= 0)
        {
            return null;
        }

        var account = await _context.ThirdPartyAccounts
            .AsNoTracking()
            .Include(x => x.ThirdParty)
                .ThenInclude(x => x.Customer)
            .Include(x => x.FinancialInstitution)
            .Include(x => x.Validations)
                .ThenInclude(x => x.ClearingHouse)
            .Include(x => x.Validations)
                .ThenInclude(x => x.Attempts
                    .OrderByDescending(attempt => attempt.AttemptNumber)
                    .ThenByDescending(attempt => attempt.Id)
                    .Take(1))
            .FirstOrDefaultAsync(x => x.Id == thirdPartyAccountId, ct);

        return account is null ? null : await MapAsync(account, eligibilityContext, ct);
    }

    public async Task<IReadOnlyList<ThirdPartyAccountValidationHistoryDto>?> GetValidationHistoryAsync(
        long thirdPartyAccountId,
        int? customerId = null,
        long? thirdPartyId = null,
        CancellationToken ct = default)
    {
        if (thirdPartyAccountId <= 0)
        {
            return null;
        }

        var accountQuery = _context.ThirdPartyAccounts.AsNoTracking().Where(x => x.Id == thirdPartyAccountId);
        if (customerId.HasValue)
        {
            accountQuery = accountQuery.Where(x => x.ThirdParty.CustomerId == customerId.Value);
        }
        if (thirdPartyId.HasValue)
        {
            accountQuery = accountQuery.Where(x => x.ThirdPartyId == thirdPartyId.Value);
        }

        if (!await accountQuery.AnyAsync(ct))
        {
            return null;
        }

        var validations = await _context.ThirdPartyAccountValidations
            .AsNoTracking()
            .Where(x => x.ThirdPartyAccountId == thirdPartyAccountId)
            .Include(x => x.ClearingHouse)
            .Include(x => x.Attempts)
            .OrderBy(x => x.ClearingHouseId)
            .ThenBy(x => x.TransactionNature)
            .ThenBy(x => x.ServiceType)
            .ToListAsync(ct);

        return validations.Select(validation => new ThirdPartyAccountValidationHistoryDto
        {
            ValidationId = validation.Id,
            ThirdPartyAccountId = validation.ThirdPartyAccountId,
            ClearingHouseId = validation.ClearingHouseId,
            ClearingHouseCode = validation.ClearingHouse?.Code ?? string.Empty,
            ClearingHouseName = validation.ClearingHouse?.Name ?? string.Empty,
            TransactionNature = validation.TransactionNature,
            ServiceType = validation.ServiceType,
            ValidationType = validation.ValidationType,
            Status = validation.Status,
            PolicyRuleId = validation.PolicyRuleId,
            PolicyEffectiveFrom = validation.PolicyEffectiveFrom,
            EffectiveDate = validation.EffectiveDate,
            ExpiresAtUtc = validation.ExpiresAtUtc,
            LastMonetaryUseAtUtc = validation.LastMonetaryUseAtUtc,
            Attempts = validation.Attempts
                .OrderByDescending(x => x.AttemptNumber)
                .ThenByDescending(x => x.Id)
                .Select(attempt => new PrenotificationValidationAttemptHistoryDto
                {
                    AttemptId = attempt.Id,
                    AttemptNumber = attempt.AttemptNumber,
                    AchTransactionId = attempt.AchTransactionId,
                    Status = attempt.Status,
                    Resolution = attempt.Resolution,
                    SentAtUtc = attempt.SentAtUtc,
                    ResponseDeadlineAtUtc = attempt.ResponseDeadlineAtUtc,
                    ResolvedAtUtc = attempt.ResolvedAtUtc,
                    ReturnCode = SafeCode(attempt.ReturnCode),
                    SourceCycleId = SafeCode(attempt.SourceCycleId, 100)
                })
                .ToArray()
        }).ToArray();
    }

    private static IQueryable<ThirdPartyAccount> ApplyFilters(
        IQueryable<ThirdPartyAccount> source,
        ThirdPartyAccountReadQuery query)
    {
        if (query.CustomerId.HasValue)
        {
            source = source.Where(x => x.ThirdParty.CustomerId == query.CustomerId.Value);
        }

        if (query.ThirdPartyId.HasValue)
        {
            source = source.Where(x => x.ThirdPartyId == query.ThirdPartyId.Value);
        }

        if (query.ThirdPartyAccountId.HasValue)
        {
            source = source.Where(x => x.Id == query.ThirdPartyAccountId.Value);
        }

        if (query.FinancialInstitutionId.HasValue)
        {
            source = source.Where(x => x.FinancialInstitutionId == query.FinancialInstitutionId.Value);
        }

        if (query.ThirdPartyStatus.HasValue)
        {
            source = source.Where(x => x.ThirdParty.Status == query.ThirdPartyStatus.Value);
        }

        if (query.AccountStatus.HasValue)
        {
            source = source.Where(x => x.Status == query.AccountStatus.Value);
        }

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var term = query.Search.Trim();
            source = source.Where(x =>
                x.ThirdParty.DisplayName.Contains(term)
                || x.ThirdParty.DocumentNumberNormalized.Contains(term)
                || x.AccountNumberNormalized.Contains(term)
                || x.FinancialInstitution.Name.Contains(term)
                || (x.ThirdParty.Customer.CompanyName ?? string.Empty).Contains(term)
                || (x.ThirdParty.Customer.FirstName + " " + x.ThirdParty.Customer.LastName).Contains(term));
        }

        return source;
    }

    private async Task<ThirdPartyAccountReadDto> MapAsync(
        ThirdPartyAccount account,
        ThirdPartyAccountEligibilityReadContext? eligibilityContext,
        CancellationToken ct)
    {
        var validations = account.Validations
            .OrderBy(x => x.ClearingHouseId)
            .ThenBy(x => x.TransactionNature)
            .ThenBy(x => x.ServiceType, StringComparer.Ordinal)
            .Select(MapValidation)
            .ToArray();

        ThirdPartyAccountEligibilityReadDto? eligibility = null;
        if (eligibilityContext is not null)
        {
            var evaluated = await _eligibilityService.EvaluateAsync(
                new ThirdPartyAccountEligibilityRequest(
                    account.Id,
                    eligibilityContext.ClearingHouseId,
                    eligibilityContext.TransactionNature,
                    eligibilityContext.EffectiveDate,
                    eligibilityContext.ServiceType),
                ct);

            eligibility = new ThirdPartyAccountEligibilityReadDto
            {
                Decision = evaluated.Decision,
                Code = evaluated.Code,
                PolicyRuleId = evaluated.PolicyRule?.Id,
                ValidationId = evaluated.ValidationId,
                ValidationStatus = evaluated.ValidationStatus,
                AttemptId = evaluated.AttemptId
            };
        }

        return new ThirdPartyAccountReadDto
        {
            ThirdPartyAccountId = account.Id,
            ThirdPartyId = account.ThirdPartyId,
            CustomerId = account.ThirdParty.CustomerId,
            CustomerDisplayName = CustomerDisplayName(account.ThirdParty.Customer),
            ThirdPartyDisplayName = account.ThirdParty.DisplayName,
            PersonType = account.ThirdParty.PersonType,
            DocumentType = account.ThirdParty.DocumentType,
            DocumentNumberMasked = MaskSensitive(account.ThirdParty.DocumentNumberNormalized),
            ThirdPartyStatus = account.ThirdParty.Status,
            FinancialInstitutionId = account.FinancialInstitutionId,
            FinancialInstitutionName = account.FinancialInstitution.Name,
            AccountType = account.AccountType,
            AccountNumberMasked = MaskSensitive(account.AccountNumberNormalized),
            AccountStatus = account.Status,
            OriginSource = account.OriginSource,
            Validations = validations,
            Eligibility = eligibility
        };
    }

    private static ThirdPartyAccountValidationReadDto MapValidation(ThirdPartyAccountValidation validation)
    {
        var latest = validation.Attempts
            .OrderByDescending(x => x.AttemptNumber)
            .ThenByDescending(x => x.Id)
            .FirstOrDefault();

        return new ThirdPartyAccountValidationReadDto
        {
            ValidationId = validation.Id,
            ClearingHouseId = validation.ClearingHouseId,
            ClearingHouseCode = validation.ClearingHouse?.Code ?? string.Empty,
            ClearingHouseName = validation.ClearingHouse?.Name ?? string.Empty,
            TransactionNature = validation.TransactionNature,
            ServiceType = validation.ServiceType,
            ValidationType = validation.ValidationType,
            Status = validation.Status,
            PolicyRuleId = validation.PolicyRuleId,
            PolicyEffectiveFrom = validation.PolicyEffectiveFrom,
            EffectiveDate = validation.EffectiveDate,
            ExpiresAtUtc = validation.ExpiresAtUtc,
            LastMonetaryUseAtUtc = validation.LastMonetaryUseAtUtc,
            LatestAttempt = latest is null
                ? null
                : new PrenotificationValidationAttemptReadDto
                {
                    AttemptId = latest.Id,
                    AttemptNumber = latest.AttemptNumber,
                    AchTransactionId = latest.AchTransactionId,
                    Status = latest.Status,
                    Resolution = latest.Resolution,
                    SentAtUtc = latest.SentAtUtc,
                    ResponseDeadlineAtUtc = latest.ResponseDeadlineAtUtc,
                    ResolvedAtUtc = latest.ResolvedAtUtc,
                    ReturnCode = SafeCode(latest.ReturnCode),
                    SourceCycleId = SafeCode(latest.SourceCycleId, 100)
                }
        };
    }

    private static string CustomerDisplayName(Customer customer)
        => string.IsNullOrWhiteSpace(customer.CompanyName)
            ? string.Join(" ", new[] { customer.FirstName, customer.LastName, customer.SecondLastName }
                .Where(x => !string.IsNullOrWhiteSpace(x)))
            : customer.CompanyName!.Trim();

    private static string MaskSensitive(string? value)
    {
        var normalized = new string((value ?? string.Empty).Where(char.IsLetterOrDigit).ToArray());
        return normalized.Length <= 4 ? "N/A" : $"****{normalized[^4..]}";
    }

    private static string? SafeCode(string? value, int maxLength = 20)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var normalized = value.Replace("\r", string.Empty, StringComparison.Ordinal)
            .Replace("\n", string.Empty, StringComparison.Ordinal)
            .Trim();
        return normalized.Length <= maxLength ? normalized : normalized[..maxLength];
    }
}
