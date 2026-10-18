using Cfa.ACHInterbank.Application.ACH.Interfaces;
using Cfa.ACHInterbank.Application.ACH.Models;
using Cfa.ACHInterbank.Domain.Models.ACH;
using Cfa.ACHInterbank.Domain.Models.ACH.Enums;
using Cfa.ACHInterbank.Domain.Models.Configurations;
using Cfa.ACHInterbank.Persistence.DataBase;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Cfa.ACHInterbank.Persistence.ACH.Services.Implementation;

/// <summary>
/// Administrative command that requests a new interbank prenotification. It never decides
/// the result; resolution remains exclusively in the automatic TPA3 transaction bridge.
/// </summary>
[Scoped]
public sealed class ThirdPartyAccountRevalidationService : IThirdPartyAccountRevalidationService
{
    private const int MaxIdempotencyKeyLength = 160;
    private readonly AchDbContext _context;
    private readonly IAchTransactionService _transactionService;
    private readonly IClearingHouseValidationPolicyService _policyService;
    private readonly ILogger<ThirdPartyAccountRevalidationService> _logger;

    public ThirdPartyAccountRevalidationService(
        AchDbContext context,
        IAchTransactionService transactionService,
        IClearingHouseValidationPolicyService policyService,
        ILogger<ThirdPartyAccountRevalidationService> logger)
    {
        _context = context;
        _transactionService = transactionService;
        _policyService = policyService;
        _logger = logger;
    }

    public async Task<ThirdPartyAccountRevalidationResult?> RequestAsync(
        int customerId,
        long thirdPartyId,
        long thirdPartyAccountId,
        RequestThirdPartyAccountRevalidationCommand command,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(command);
        ValidateCommand(command);
        var idempotencyKey = command.IdempotencyKey.Trim();

        var account = await _context.ThirdPartyAccounts
            .AsNoTracking()
            .Include(x => x.ThirdParty)
                .ThenInclude(x => x.Customer)
                    .ThenInclude(x => x.Accounts)
            .FirstOrDefaultAsync(x => x.Id == thirdPartyAccountId
                                      && x.ThirdPartyId == thirdPartyId
                                      && x.ThirdParty.CustomerId == customerId, ct);
        if (account is null)
        {
            return null;
        }

        if (account.ThirdParty.Status != ThirdPartyAdministrativeStatus.Active
            || account.Status != ThirdPartyAdministrativeStatus.Active)
        {
            throw new ThirdPartyAdministrationConflictException(
                "MASTER_INACTIVE",
                "El tercero y la cuenta deben estar administrativamente activos para solicitar revalidación.");
        }

        var sourceAccount = ThirdPartyAccount.NormalizeAccountNumber(command.SourceAccountNumber);
        if (!account.ThirdParty.Customer.Accounts.Any(x => string.Equals(x.AccountNumber.Trim(), sourceAccount, StringComparison.Ordinal)))
        {
            throw new ThirdPartyAdministrationConflictException(
                "SOURCE_ACCOUNT_NOT_OWNED",
                "La cuenta de origen no pertenece al Customer indicado.");
        }

        var serviceType = ThirdPartyAccountValidation.NormalizeServiceType(command.ServiceType);
        if (string.IsNullOrWhiteSpace(serviceType))
        {
            throw new ArgumentException("ServiceType es obligatorio para solicitar revalidación.", nameof(command.ServiceType));
        }

        var entryDescription = await _context.CompanyEntryDescriptionCatalogs
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == command.CompanyEntryDescriptionId && x.IsActive, ct)
            ?? throw new ThirdPartyAdministrationConflictException(
                "SERVICE_CATALOG_NOT_FOUND",
                "El concepto de lote solicitado no existe o está inactivo.");
        var catalogServiceType = ThirdPartyAccountValidation.NormalizeServiceType(entryDescription.StandardEntryClassCode);
        if (!string.Equals(catalogServiceType, serviceType, StringComparison.Ordinal))
        {
            throw new ThirdPartyAdministrationConflictException(
                "SERVICE_CONTEXT_MISMATCH",
                "El ServiceType no coincide con el concepto de lote seleccionado.");
        }

        var existing = await FindByIdempotencyKeyAsync(idempotencyKey, ct);
        if (existing is not null)
        {
            EnsureCompatibleReplayContext(
                existing,
                thirdPartyAccountId,
                command.ClearingHouseId,
                command.TransactionNature,
                command.EffectiveDate,
                serviceType);
            return Map(existing, reused: true);
        }

        var policy = await _policyService.ResolveAsync(
            command.ClearingHouseId,
            command.TransactionNature,
            command.EffectiveDate,
            ct);
        if (policy.Rule is null)
        {
            throw new ThirdPartyAdministrationConflictException(
                "POLICY_NOT_CONFIGURED",
                "No existe una política efectiva para la cámara, naturaleza y fecha solicitadas.");
        }
        if (!IsResolvedPolicyContextCompatible(policy.Rule, command.ClearingHouseId, command.TransactionNature, command.EffectiveDate))
        {
            throw new ThirdPartyAdministrationConflictException(
                "POLICY_CONTEXT_MISMATCH",
                "La política resuelta no corresponde al contexto solicitado.");
        }
        if (policy.Rule.PrenotificationMode == PrenotificationRequirementMode.NotApplicable)
        {
            throw new ThirdPartyAdministrationConflictException(
                "PRENOTIFICATION_NOT_APPLICABLE",
                "La política efectiva indica que la prenotificación no aplica para el contexto solicitado.");
        }

        var customer = account.ThirdParty.Customer;
        var originatorName = BuildOriginatorName(customer);
        try
        {
            await _transactionService.RegisterCanonicalPrenotificationAsync(
                new CanonicalPrenotificationTransactionRequest(
                    account.Id,
                    command.ClearingHouseId,
                    command.EffectiveDate.Date,
                    idempotencyKey,
                    command.TransactionNature,
                    account.AccountType,
                    account.FinancialInstitutionId,
                    sourceAccount,
                    account.AccountNumberNormalized,
                    originatorName,
                    customer.DocumentNumber.Trim(),
                    command.CompanyEntryDescriptionId,
                    customer.PersonType,
                    account.ThirdParty.PersonType,
                    account.ThirdParty.DocumentNumberNormalized,
                    account.ThirdParty.DisplayName),
                ct);
        }
        catch (PrenotificationIdempotencyConflictException)
        {
            var winner = await FindByIdempotencyKeyAsync(idempotencyKey, ct);
            if (winner is null)
            {
                throw;
            }

            EnsureCompatibleReplayContext(
                winner,
                thirdPartyAccountId,
                command.ClearingHouseId,
                command.TransactionNature,
                command.EffectiveDate,
                serviceType);
            return Map(winner, reused: true);
        }

        var created = await FindByIdempotencyKeyAsync(idempotencyKey, ct)
            ?? throw new InvalidOperationException("La transacción de prenotificación se registró sin materializar su intento canónico.");
        EnsureCompatibleReplayContext(
            created,
            thirdPartyAccountId,
            command.ClearingHouseId,
            command.TransactionNature,
            command.EffectiveDate,
            serviceType);

        _logger.LogInformation(
            "Canonical third-party account revalidation requested. CustomerId={CustomerId}, ThirdPartyId={ThirdPartyId}, ThirdPartyAccountId={ThirdPartyAccountId}, ClearingHouseId={ClearingHouseId}, Nature={Nature}, ValidationId={ValidationId}, AttemptId={AttemptId}.",
            customerId,
            thirdPartyId,
            thirdPartyAccountId,
            command.ClearingHouseId,
            command.TransactionNature,
            created.ThirdPartyAccountValidationId,
            created.Id);

        return Map(created, reused: false);
    }

    private Task<PrenotificationValidationAttempt?> FindByIdempotencyKeyAsync(string idempotencyKey, CancellationToken ct)
        => _context.PrenotificationValidationAttempts
            .AsNoTracking()
            .Include(x => x.ThirdPartyAccountValidation)
            .FirstOrDefaultAsync(x => x.IdempotencyKey == idempotencyKey, ct);

    private static void EnsureCompatibleReplayContext(
        PrenotificationValidationAttempt attempt,
        long thirdPartyAccountId,
        int clearingHouseId,
        TransactionNature transactionNature,
        DateTime effectiveDate,
        string serviceType)
    {
        var validation = attempt.ThirdPartyAccountValidation;
        var sameContext = validation.ThirdPartyAccountId == thirdPartyAccountId
                          && validation.ClearingHouseId == clearingHouseId
                          && validation.TransactionNature == transactionNature
                          && validation.ValidationType == ThirdPartyAccountValidationType.Prenotification
                          && validation.EffectiveDate.Date == effectiveDate.Date
                          && string.Equals(
                              ThirdPartyAccountValidation.NormalizeServiceType(validation.ServiceType),
                              serviceType,
                              StringComparison.Ordinal);
        if (!sameContext)
        {
            throw new ThirdPartyAdministrationConflictException(
                "IDEMPOTENCY_KEY_CONFLICT",
                "La idempotency key ya pertenece a otro contexto de validación.");
        }
    }

    private static bool IsResolvedPolicyContextCompatible(
        ClearingHouseTransactionRule rule,
        int clearingHouseId,
        TransactionNature transactionNature,
        DateTime effectiveDate)
    {
        var requestedDate = effectiveDate.Date;
        return rule.IsActive
               && rule.ClearingHouseId == clearingHouseId
               && rule.TransactionNature == transactionNature
               && rule.EffectiveFrom.Date <= requestedDate
               && (!rule.EffectiveTo.HasValue || rule.EffectiveTo.Value.Date >= requestedDate);
    }

    private static ThirdPartyAccountRevalidationResult Map(PrenotificationValidationAttempt attempt, bool reused)
    {
        if (!attempt.AchTransactionId.HasValue)
        {
            throw new InvalidOperationException("El intento de prenotificación no tiene una transacción ACH persistida.");
        }

        return new ThirdPartyAccountRevalidationResult(
            attempt.AchTransactionId.Value,
            attempt.ThirdPartyAccountValidationId,
            attempt.Id,
            attempt.AttemptNumber,
            attempt.ThirdPartyAccountValidation.Status,
            attempt.Status,
            attempt.ResponseDeadlineAtUtc,
            reused);
    }

    private static void ValidateCommand(RequestThirdPartyAccountRevalidationCommand command)
    {
        if (command.ClearingHouseId <= 0)
        {
            throw new ArgumentException("ClearingHouseId es obligatorio.", nameof(command.ClearingHouseId));
        }
        if (!Enum.IsDefined(command.TransactionNature))
        {
            throw new ArgumentOutOfRangeException(nameof(command.TransactionNature));
        }
        if (command.EffectiveDate == default)
        {
            throw new ArgumentException("EffectiveDate es obligatorio.", nameof(command.EffectiveDate));
        }
        if (command.CompanyEntryDescriptionId <= 0)
        {
            throw new ArgumentException("CompanyEntryDescriptionId es obligatorio.", nameof(command.CompanyEntryDescriptionId));
        }
        if (string.IsNullOrWhiteSpace(command.IdempotencyKey)
            || command.IdempotencyKey.Trim().Length > MaxIdempotencyKeyLength)
        {
            throw new ArgumentException($"IdempotencyKey es obligatorio y no puede superar {MaxIdempotencyKeyLength} caracteres.", nameof(command.IdempotencyKey));
        }
    }

    private static string BuildOriginatorName(Customer customer)
    {
        var preferred = string.Equals(customer.PersonType, "PJ", StringComparison.OrdinalIgnoreCase)
            ? customer.CompanyName
            : string.Join(' ', new[] { customer.FirstName, customer.LastName }.Where(x => !string.IsNullOrWhiteSpace(x)));
        var normalized = string.IsNullOrWhiteSpace(preferred) ? "CLIENTE" : preferred.Trim();
        return normalized.Length <= 16 ? normalized : normalized[..16];
    }
}
