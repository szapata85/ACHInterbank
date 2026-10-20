using Cfa.ACHInterbank.Application.ACH.Interfaces;
using Cfa.ACHInterbank.Application.ACH.Interfaces.Repositories;
using Cfa.ACHInterbank.Application.ACH.Models;
using Cfa.ACHInterbank.Domain.Entities.Transactions.Enums;
using Cfa.ACHInterbank.Domain.Models.ACH.Enums;
using Cfa.ACHInterbank.Domain.Models.Configurations;
using Cfa.ACHInterbank.Persistence.DataBase;
using Microsoft.EntityFrameworkCore;

namespace Cfa.ACHInterbank.Persistence.ACH.Services.Implementation;

[Scoped]
public sealed class TransactionThirdPartyAccountSelectorService : ITransactionThirdPartyAccountSelectorService
{
    private readonly AchDbContext _context;
    private readonly IAchCustomerRepository _customerRepository;
    private readonly IRoutingStrategyService _routingStrategyService;
    private readonly IThirdPartyAccountEligibilityService _eligibilityService;
    private readonly TimeProvider _timeProvider;

    public TransactionThirdPartyAccountSelectorService(
        AchDbContext context,
        IAchCustomerRepository customerRepository,
        IRoutingStrategyService routingStrategyService,
        IThirdPartyAccountEligibilityService eligibilityService,
        TimeProvider? timeProvider = null)
    {
        _context = context;
        _customerRepository = customerRepository;
        _routingStrategyService = routingStrategyService;
        _eligibilityService = eligibilityService;
        _timeProvider = timeProvider ?? TimeProvider.System;
    }

    public async Task<TransactionThirdPartyAccountSelectorResponse> GetAsync(
        TransactionThirdPartyAccountSelectorRequest request,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var sourceAccountNumber = (request.SourceAccountNumber ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(sourceAccountNumber))
        {
            throw new ArgumentException("La cuenta de origen es obligatoria para seleccionar cuentas de terceros.", nameof(request));
        }
        if (request.DestinationInstitutionId <= 0)
        {
            throw new ArgumentException("La institución destino es obligatoria para seleccionar cuentas de terceros.", nameof(request));
        }
        if (request.CompanyEntryDescriptionId <= 0)
        {
            throw new ArgumentException("La descripción de entrada es obligatoria para seleccionar cuentas de terceros.", nameof(request));
        }

        var nature = TransactionPrerequisitePolicyService.ResolveNature(request.TransactionType)
            ?? throw new ArgumentException("El selector contextual sólo aplica a transacciones crédito o débito.", nameof(request));

        var customer = await _customerRepository.FindBySourceAccountNumberAsync(sourceAccountNumber, ct)
            ?? throw new InvalidOperationException("La cuenta de origen no está asociada a un Customer conocido.");

        var destinationInstitutionExists = await _context.FinancialInstitutions
            .AsNoTracking()
            .AnyAsync(x => x.Id == request.DestinationInstitutionId && x.Status == FinancialInstitutionStatus.Active, ct);
        if (!destinationInstitutionExists)
        {
            throw new InvalidOperationException("Institución destino no encontrada o inactiva.");
        }

        var serviceType = await _context.CompanyEntryDescriptionCatalogs
            .AsNoTracking()
            .Where(x => x.Id == request.CompanyEntryDescriptionId && x.IsActive)
            .Select(x => x.StandardEntryClassCode)
            .FirstOrDefaultAsync(ct);
        if (string.IsNullOrWhiteSpace(serviceType))
        {
            throw new InvalidOperationException("La descripción de entrada seleccionada no existe o está inactiva.");
        }
        serviceType = serviceType.Trim().ToUpperInvariant();

        var cycleId = await _routingStrategyService.ResolveClearingHouseForTransactionAsync(
            request.DestinationInstitutionId,
            _timeProvider.GetUtcNow().UtcDateTime,
            ct);

        var route = await _context.AchCycles
            .AsNoTracking()
            .Where(x => x.Id == cycleId)
            .Select(x => new
            {
                x.ClearingHouseId,
                ClearingHouseCode = x.ClearingHouse == null ? string.Empty : x.ClearingHouse.Code,
                ClearingHouseName = x.ClearingHouse == null ? string.Empty : x.ClearingHouse.Name,
                EffectiveDate = x.ProcessingDate
            })
            .FirstOrDefaultAsync(ct)
            ?? throw new InvalidOperationException("No se encontró el ciclo ACH resuelto para el selector transaccional.");

        var accounts = await _context.ThirdPartyAccounts
            .AsNoTracking()
            .Where(x => x.ThirdParty.CustomerId == customer.Id
                        && x.FinancialInstitutionId == request.DestinationInstitutionId)
            .OrderBy(x => x.ThirdParty.DisplayName)
            .ThenBy(x => x.AccountNumberNormalized)
            .Select(x => new SelectorAccountProjection(
                x.Id,
                x.ThirdPartyId,
                x.FinancialInstitutionId,
                x.FinancialInstitution.Name,
                x.AccountNumberNormalized,
                x.AccountType,
                x.ThirdParty.DocumentNumberNormalized,
                x.ThirdParty.DisplayName,
                x.ThirdParty.PersonType))
            .ToListAsync(ct);

        var items = new List<TransactionThirdPartyAccountSelectorItem>(accounts.Count);
        foreach (var account in accounts)
        {
            var eligibility = await _eligibilityService.EvaluateAsync(
                new ThirdPartyAccountEligibilityRequest(
                    account.Id,
                    route.ClearingHouseId,
                    nature,
                    route.EffectiveDate.Date,
                    serviceType),
                ct);

            items.Add(new TransactionThirdPartyAccountSelectorItem(
                account.Id,
                account.ThirdPartyId,
                account.FinancialInstitutionId,
                account.FinancialInstitutionName,
                MaskSensitive(account.AccountNumberNormalized),
                account.AccountType,
                MaskSensitive(account.DocumentNumberNormalized),
                account.DisplayName,
                account.PersonType,
                eligibility.Decision,
                RedactSensitive(eligibility.Code, account.AccountNumberNormalized, account.DocumentNumberNormalized),
                RedactSensitive(eligibility.Message, account.AccountNumberNormalized, account.DocumentNumberNormalized),
                eligibility.Decision is ThirdPartyAccountEligibilityDecision.Allowed
                    or ThirdPartyAccountEligibilityDecision.AllowedWithoutPrenotification));
        }

        return new TransactionThirdPartyAccountSelectorResponse(
            customer.Id,
            request.DestinationInstitutionId,
            route.ClearingHouseId,
            route.ClearingHouseCode ?? string.Empty,
            route.ClearingHouseName ?? string.Empty,
            nature,
            route.EffectiveDate.Date,
            serviceType,
            items);
    }

    private static string MaskSensitive(string? value)
    {
        var normalized = new string((value ?? string.Empty).Where(char.IsLetterOrDigit).ToArray());
        return normalized.Length <= 4 ? "N/A" : $"****{normalized[^4..]}";
    }

    private static string RedactSensitive(string? message, string accountNumber, string documentNumber)
    {
        var safe = message ?? string.Empty;
        if (!string.IsNullOrWhiteSpace(accountNumber))
        {
            safe = safe.Replace(accountNumber, MaskSensitive(accountNumber), StringComparison.OrdinalIgnoreCase);
        }
        if (!string.IsNullOrWhiteSpace(documentNumber))
        {
            safe = safe.Replace(documentNumber, MaskSensitive(documentNumber), StringComparison.OrdinalIgnoreCase);
        }
        return safe;
    }

    private sealed record SelectorAccountProjection(
        long Id,
        long ThirdPartyId,
        int FinancialInstitutionId,
        string FinancialInstitutionName,
        string AccountNumberNormalized,
        AccountTypeEnum AccountType,
        string DocumentNumberNormalized,
        string DisplayName,
        string PersonType);
}
