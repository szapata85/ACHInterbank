using Cfa.ACHInterbank.Application.ACH.Interfaces;
using Cfa.ACHInterbank.Application.ACH.Models;
using Cfa.ACHInterbank.Domain.Entities.Transactions.Enums;
using Cfa.ACHInterbank.Domain.Models.ACH;
using Cfa.ACHInterbank.Domain.Models.Configurations;
using Cfa.ACHInterbank.Persistence.DataBase;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Cfa.ACHInterbank.Persistence.ACH.Services.Implementation;

/// <summary>
/// Onboarding canónico de master data. No resuelve cámara, elegibilidad ni validación interbancaria.
/// Los SaveChanges internos materializan IDs y permiten resolver unique-conflict + re-read; los
/// orquestadores transaccionales deben mantenerlos dentro de su transacción/unidad de trabajo.
/// </summary>
[Scoped]
public sealed class ThirdPartyOnboardingService : IThirdPartyOnboardingService
{
    private readonly AchDbContext _context;
    private readonly ILogger<ThirdPartyOnboardingService> _logger;

    public ThirdPartyOnboardingService(
        AchDbContext context,
        ILogger<ThirdPartyOnboardingService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<ThirdPartyRef> EnsureThirdPartyAsync(
        ThirdPartyMasterRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        cancellationToken.ThrowIfCancellationRequested();
        ValidateThirdPartyRequest(request);

        var documentType = ThirdParty.NormalizeCatalogCode(request.DocumentType, nameof(request.DocumentType));
        var personType = ThirdParty.NormalizeCatalogCode(request.PersonType, nameof(request.PersonType));
        var documentNumber = ThirdParty.NormalizeDocumentNumber(request.DocumentNumber);
        var displayName = ThirdParty.NormalizeDisplayName(request.DisplayName);

        var thirdParty = await FindThirdPartyAsync(
            request.CustomerId,
            documentType,
            documentNumber,
            cancellationToken);
        var created = false;
        var recoveredFromConflict = false;

        if (thirdParty is null)
        {
            var candidate = new ThirdParty
            {
                CustomerId = request.CustomerId,
                DocumentType = documentType,
                DocumentNumberNormalized = documentNumber,
                DisplayName = displayName,
                PersonType = personType
            };

            _context.ThirdParties.Add(candidate);
            var addedBeforeSave = SnapshotAddedEntities();
            try
            {
                await _context.SaveChangesAsync(cancellationToken);
                thirdParty = candidate;
                created = true;
            }
            catch (DbUpdateException ex) when (RelationalDatabaseExceptionClassifier.IsUniqueViolation(ex))
            {
                DetachEntriesAddedDuringFailedSave(addedBeforeSave);
                _context.Entry(candidate).State = EntityState.Detached;
                thirdParty = await FindThirdPartyAsync(
                    request.CustomerId,
                    documentType,
                    documentNumber,
                    cancellationToken);

                if (thirdParty is null)
                {
                    throw;
                }

                recoveredFromConflict = true;
                _logger.LogInformation(
                    "Canonical third-party onboarding recovered a concurrent unique conflict. CustomerId={CustomerId}, ThirdPartyId={ThirdPartyId}.",
                    request.CustomerId,
                    thirdParty.Id);
            }
        }

        if (!string.Equals(thirdParty.PersonType, personType, StringComparison.OrdinalIgnoreCase))
        {
            _logger.LogWarning(
                "Canonical third-party identity matched with a different person type; existing master data was preserved. CustomerId={CustomerId}, ThirdPartyId={ThirdPartyId}.",
                request.CustomerId,
                thirdParty.Id);
        }

        return new ThirdPartyRef(thirdParty.Id, created, recoveredFromConflict);
    }

    public async Task<ThirdPartyAccountRef> EnsureAccountAsync(
        ThirdPartyOnboardingRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        cancellationToken.ThrowIfCancellationRequested();
        ValidateRequest(request);
        // Preserve TPA2 fail-fast semantics: validate/normalize the full account identity before
        // any ThirdParty insert can be materialized by the shared third-party ensure operation.
        var accountNumber = ThirdPartyAccount.NormalizeAccountNumber(request.AccountNumber);

        var thirdPartyRef = await EnsureThirdPartyAsync(
            new ThirdPartyMasterRequest(
                request.CustomerId,
                request.DocumentType,
                request.DocumentNumber,
                request.DisplayName,
                request.PersonType),
            cancellationToken);

        var accountCreated = false;
        var recoveredFromConflict = thirdPartyRef.RecoveredFromUniqueConflict;
        var account = await FindAccountAsync(
            thirdPartyRef.ThirdPartyId,
            request.FinancialInstitutionId,
            request.AccountType,
            accountNumber,
            cancellationToken);

        if (account is null)
        {
            var candidate = new ThirdPartyAccount
            {
                ThirdPartyId = thirdPartyRef.ThirdPartyId,
                FinancialInstitutionId = request.FinancialInstitutionId,
                AccountType = request.AccountType,
                AccountNumberNormalized = accountNumber,
                OriginSource = request.SourceMetadata
            };

            _context.ThirdPartyAccounts.Add(candidate);
            var addedBeforeSave = SnapshotAddedEntities();
            try
            {
                await _context.SaveChangesAsync(cancellationToken);
                account = candidate;
                accountCreated = true;
            }
            catch (DbUpdateException ex) when (RelationalDatabaseExceptionClassifier.IsUniqueViolation(ex))
            {
                DetachEntriesAddedDuringFailedSave(addedBeforeSave);
                _context.Entry(candidate).State = EntityState.Detached;
                account = await FindAccountAsync(
                    thirdPartyRef.ThirdPartyId,
                    request.FinancialInstitutionId,
                    request.AccountType,
                    accountNumber,
                    cancellationToken);

                if (account is null)
                {
                    throw;
                }

                recoveredFromConflict = true;
                _logger.LogInformation(
                    "Canonical third-party account onboarding recovered a concurrent unique conflict. ThirdPartyId={ThirdPartyId}, ThirdPartyAccountId={ThirdPartyAccountId}, FinancialInstitutionId={FinancialInstitutionId}.",
                    thirdPartyRef.ThirdPartyId,
                    account.Id,
                    request.FinancialInstitutionId);
            }
        }
        else if (account.OriginSource is null && request.SourceMetadata is not null)
        {
            account.OriginSource = request.SourceMetadata;
            await _context.SaveChangesAsync(cancellationToken);
        }

        _logger.LogInformation(
            "Canonical third-party account ensured. CustomerId={CustomerId}, ThirdPartyId={ThirdPartyId}, ThirdPartyAccountId={ThirdPartyAccountId}, ThirdPartyCreated={ThirdPartyCreated}, AccountCreated={AccountCreated}, RecoveredFromConflict={RecoveredFromConflict}, Source={Source}.",
            request.CustomerId,
            thirdPartyRef.ThirdPartyId,
            account.Id,
            thirdPartyRef.ThirdPartyCreated,
            accountCreated,
            recoveredFromConflict,
            request.SourceMetadata);

        return new ThirdPartyAccountRef(
            thirdPartyRef.ThirdPartyId,
            account.Id,
            thirdPartyRef.ThirdPartyCreated,
            accountCreated,
            recoveredFromConflict);
    }


    private HashSet<object> SnapshotAddedEntities()
        => _context.ChangeTracker.Entries()
            .Where(entry => entry.State == EntityState.Added)
            .Select(entry => entry.Entity)
            .ToHashSet(ReferenceEqualityComparer.Instance);

    private void DetachEntriesAddedDuringFailedSave(IReadOnlySet<object> entitiesTrackedBeforeSave)
    {
        // AchDbContext agrega AuditLog durante SaveChanges. Si el INSERT canónico pierde una carrera
        // de unique-key, esos logs también quedan Added después del rollback del savepoint. Sólo se
        // desacoplan las entidades agregadas por ese intento fallido, sin limpiar el ChangeTracker ni
        // perder cambios legacy/transaccionales que pertenecen a la unidad de trabajo del caller.
        foreach (var entry in _context.ChangeTracker.Entries().Where(entry => entry.State == EntityState.Added).ToList())
        {
            if (!entitiesTrackedBeforeSave.Contains(entry.Entity))
            {
                entry.State = EntityState.Detached;
            }
        }
    }

    private Task<ThirdParty?> FindThirdPartyAsync(
        int customerId,
        string documentType,
        string documentNumber,
        CancellationToken cancellationToken)
        => _context.ThirdParties.FirstOrDefaultAsync(
            x => x.CustomerId == customerId
                && x.DocumentType == documentType
                && x.DocumentNumberNormalized == documentNumber,
            cancellationToken);

    private Task<ThirdPartyAccount?> FindAccountAsync(
        long thirdPartyId,
        int financialInstitutionId,
        AccountTypeEnum accountType,
        string accountNumber,
        CancellationToken cancellationToken)
        => _context.ThirdPartyAccounts.FirstOrDefaultAsync(
            x => x.ThirdPartyId == thirdPartyId
                && x.FinancialInstitutionId == financialInstitutionId
                && x.AccountType == accountType
                && x.AccountNumberNormalized == accountNumber,
            cancellationToken);

    private static void ValidateThirdPartyRequest(ThirdPartyMasterRequest request)
    {
        if (request.CustomerId <= 0)
        {
            throw new ArgumentException("CustomerId debe identificar un Customer persistido.", nameof(request.CustomerId));
        }
    }

    private static void ValidateRequest(ThirdPartyOnboardingRequest request)
    {
        if (request.CustomerId <= 0)
        {
            throw new ArgumentException("CustomerId debe identificar un Customer persistido.", nameof(request.CustomerId));
        }

        if (request.FinancialInstitutionId <= 0)
        {
            throw new ArgumentException("FinancialInstitutionId es obligatorio.", nameof(request.FinancialInstitutionId));
        }

        if (!Enum.IsDefined(request.AccountType))
        {
            throw new ArgumentOutOfRangeException(nameof(request.AccountType));
        }
    }
}
