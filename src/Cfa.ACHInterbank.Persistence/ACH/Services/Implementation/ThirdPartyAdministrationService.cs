using Cfa.ACHInterbank.Application.ACH.Interfaces;
using Cfa.ACHInterbank.Application.ACH.Models;
using Cfa.ACHInterbank.Application.Common;
using Cfa.ACHInterbank.Domain.Models.ACH;
using Cfa.ACHInterbank.Domain.Models.ACH.Enums;
using Cfa.ACHInterbank.Domain.Models.Configurations;
using Cfa.ACHInterbank.Persistence.DataBase;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Cfa.ACHInterbank.Persistence.ACH.Services.Implementation;

/// <summary>
/// Commands and administrative reads for canonical third-party master data.
/// Clearing-house/transaction context is intentionally absent from this service.
/// </summary>
[Scoped]
public sealed class ThirdPartyAdministrationService : IThirdPartyAdministrationService
{
    private const int MaxPageSize = 100;
    private readonly AchDbContext _context;
    private readonly IThirdPartyOnboardingService _onboardingService;
    private readonly IThirdPartyAccountReadService _accountReadService;
    private readonly ILogger<ThirdPartyAdministrationService> _logger;

    public ThirdPartyAdministrationService(
        AchDbContext context,
        IThirdPartyOnboardingService onboardingService,
        IThirdPartyAccountReadService accountReadService,
        ILogger<ThirdPartyAdministrationService> logger)
    {
        _context = context;
        _onboardingService = onboardingService;
        _accountReadService = accountReadService;
        _logger = logger;
    }

    public async Task<PagedResponse<ThirdPartyAdministrationDto>> GetThirdPartiesAsync(
        int customerId,
        ThirdPartyAdministrationQuery query,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(query);
        if (customerId <= 0)
        {
            return EmptyPage(query);
        }

        var page = query.Page <= 0 ? 1 : query.Page;
        var pageSize = Math.Clamp(query.PageSize <= 0 ? 20 : query.PageSize, 1, MaxPageSize);
        var source = _context.ThirdParties.AsNoTracking().Where(x => x.CustomerId == customerId);
        if (query.Status.HasValue)
        {
            source = source.Where(x => x.Status == query.Status.Value);
        }

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var search = query.Search.Trim();
            source = source.Where(x => x.DisplayName.Contains(search) || x.DocumentNumberNormalized.Contains(search));
        }

        var total = await source.CountAsync(ct);
        var rows = await source
            .OrderBy(x => x.DisplayName)
            .ThenBy(x => x.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(x => new ThirdPartyProjection(
                x.Id,
                x.CustomerId,
                x.PersonType,
                x.DocumentType,
                x.DocumentNumberNormalized,
                x.DisplayName,
                x.Status,
                x.Accounts.Count,
                x.CreatedAt,
                x.UpdatedAt))
            .ToListAsync(ct);

        return new PagedResponse<ThirdPartyAdministrationDto>
        {
            Items = rows.Select(Map).ToArray(),
            Total = total,
            Page = page,
            PageSize = pageSize
        };
    }

    public async Task<ThirdPartyAdministrationDto?> GetThirdPartyAsync(
        int customerId,
        long thirdPartyId,
        CancellationToken ct = default)
    {
        var row = await _context.ThirdParties
            .AsNoTracking()
            .Where(x => x.CustomerId == customerId && x.Id == thirdPartyId)
            .Select(x => new ThirdPartyProjection(
                x.Id,
                x.CustomerId,
                x.PersonType,
                x.DocumentType,
                x.DocumentNumberNormalized,
                x.DisplayName,
                x.Status,
                x.Accounts.Count,
                x.CreatedAt,
                x.UpdatedAt))
            .FirstOrDefaultAsync(ct);
        return row is null ? null : Map(row);
    }

    public async Task<ThirdPartyCommandResult<ThirdPartyAdministrationDto>> CreateThirdPartyAsync(
        int customerId,
        CreateThirdPartyCommand command,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(command);
        await ValidateMasterReferencesAsync(customerId, command.DocumentType, command.PersonType, ct);

        var ensured = await _onboardingService.EnsureThirdPartyAsync(new ThirdPartyMasterRequest(
            customerId,
            command.DocumentType,
            command.DocumentNumber,
            command.DisplayName,
            command.PersonType), ct);

        var persisted = await _context.ThirdParties
            .AsNoTracking()
            .Where(x => x.CustomerId == customerId && x.Id == ensured.ThirdPartyId)
            .Select(x => new ThirdPartyProjection(
                x.Id,
                x.CustomerId,
                x.PersonType,
                x.DocumentType,
                x.DocumentNumberNormalized,
                x.DisplayName,
                x.Status,
                x.Accounts.Count,
                x.CreatedAt,
                x.UpdatedAt))
            .FirstAsync(ct);
        if (!ensured.ThirdPartyCreated)
        {
            var requestedPersonType = ThirdParty.NormalizeCatalogCode(command.PersonType, nameof(command.PersonType));
            if (!string.Equals(persisted.PersonType, requestedPersonType, StringComparison.Ordinal))
            {
                throw new ThirdPartyAdministrationConflictException(
                    "IDENTITY_METADATA_CONFLICT",
                    "Ya existe un tercero con la misma clave natural y un tipo de persona distinto; el update genérico no reescribe identidad ni reutiliza validaciones históricas.");
            }

            if (!string.Equals(persisted.DisplayName, ThirdParty.NormalizeDisplayName(command.DisplayName), StringComparison.Ordinal))
            {
                throw new ThirdPartyAdministrationConflictException(
                    "DUPLICATE_IDENTITY_CONFLICT",
                    "Ya existe un tercero con la misma identidad y un nombre administrativo distinto; use el comando explícito de actualización.");
            }
        }

        var dto = Map(persisted);

        _logger.LogInformation(
            "Canonical third-party administrative create completed. CustomerId={CustomerId}, ThirdPartyId={ThirdPartyId}, Created={Created}, RecoveredFromConflict={RecoveredFromConflict}.",
            customerId,
            ensured.ThirdPartyId,
            ensured.ThirdPartyCreated,
            ensured.RecoveredFromUniqueConflict);

        return new ThirdPartyCommandResult<ThirdPartyAdministrationDto>(
            dto,
            ensured.ThirdPartyCreated,
            !ensured.ThirdPartyCreated,
            ensured.RecoveredFromUniqueConflict);
    }

    public async Task<ThirdPartyAdministrationDto?> UpdateThirdPartyAsync(
        int customerId,
        long thirdPartyId,
        UpdateThirdPartyCommand command,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(command);
        var thirdParty = await FindOwnedThirdPartyAsync(customerId, thirdPartyId, ct);
        if (thirdParty is null)
        {
            return null;
        }

        // Identity-bearing fields (DocumentType, DocumentNumber and PersonType) are deliberately
        // immutable through the generic administrative update. A correction that changes identity
        // must use an explicit replacement strategy rather than silently reusing prior validations.
        var normalizedDisplayName = ThirdParty.NormalizeDisplayName(command.DisplayName);
        if (!string.Equals(thirdParty.DisplayName, normalizedDisplayName, StringComparison.Ordinal))
        {
            thirdParty.DisplayName = normalizedDisplayName;
            await _context.SaveChangesAsync(ct);
        }

        _logger.LogInformation(
            "Canonical third-party administrative data updated. CustomerId={CustomerId}, ThirdPartyId={ThirdPartyId}.",
            customerId,
            thirdPartyId);
        return await GetThirdPartyAsync(customerId, thirdPartyId, ct);
    }

    public async Task<ThirdPartyAdministrationDto?> SetThirdPartyStatusAsync(
        int customerId,
        long thirdPartyId,
        SetThirdPartyAdministrativeStatusCommand command,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(command);
        ValidateOperationalAdministrativeStatus(command.Status);
        var thirdParty = await FindOwnedThirdPartyAsync(customerId, thirdPartyId, ct);
        if (thirdParty is null)
        {
            return null;
        }

        if (thirdParty.Status != command.Status)
        {
            thirdParty.Status = command.Status;
            await _context.SaveChangesAsync(ct);
        }

        _logger.LogInformation(
            "Canonical third-party administrative status set. CustomerId={CustomerId}, ThirdPartyId={ThirdPartyId}, Status={Status}.",
            customerId,
            thirdPartyId,
            command.Status);
        return await GetThirdPartyAsync(customerId, thirdPartyId, ct);
    }

    public async Task<ThirdPartyCommandResult<ThirdPartyAccountReadDto>?> CreateAccountAsync(
        int customerId,
        long thirdPartyId,
        CreateThirdPartyAccountCommand command,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(command);
        var thirdParty = await GetOwnedThirdPartyIdentityAsync(customerId, thirdPartyId, ct);
        if (thirdParty is null)
        {
            return null;
        }

        if (command.FinancialInstitutionId <= 0
            || !await _context.FinancialInstitutions.AsNoTracking().AnyAsync(x => x.Id == command.FinancialInstitutionId, ct))
        {
            throw new ThirdPartyAdministrationConflictException("FINANCIAL_INSTITUTION_NOT_FOUND", "La institución financiera no existe.");
        }
        if (!Enum.IsDefined(command.AccountType))
        {
            throw new ArgumentOutOfRangeException(nameof(command.AccountType));
        }

        var ensured = await _onboardingService.EnsureAccountAsync(new ThirdPartyOnboardingRequest(
            customerId,
            thirdParty.DocumentType,
            thirdParty.DocumentNumberNormalized,
            thirdParty.DisplayName,
            thirdParty.PersonType,
            command.FinancialInstitutionId,
            command.AccountType,
            command.AccountNumber,
            ThirdPartyAccountOriginSource.Manual), ct);

        if (ensured.ThirdPartyId != thirdPartyId)
        {
            throw new ThirdPartyAdministrationConflictException(
                "THIRD_PARTY_IDENTITY_CONFLICT",
                "La identidad solicitada resolvió a un tercero distinto; no se creó una cuenta sobre un master ambiguo.");
        }

        var dto = await _accountReadService.GetByAccountIdAsync(ensured.ThirdPartyAccountId, null, ct)
            ?? throw new InvalidOperationException("La cuenta canónica asegurada no pudo releerse.");

        _logger.LogInformation(
            "Canonical third-party account administrative create completed. CustomerId={CustomerId}, ThirdPartyId={ThirdPartyId}, ThirdPartyAccountId={ThirdPartyAccountId}, Created={Created}, RecoveredFromConflict={RecoveredFromConflict}.",
            customerId,
            thirdPartyId,
            ensured.ThirdPartyAccountId,
            ensured.ThirdPartyAccountCreated,
            ensured.RecoveredFromUniqueConflict);

        return new ThirdPartyCommandResult<ThirdPartyAccountReadDto>(
            dto,
            ensured.ThirdPartyAccountCreated,
            !ensured.ThirdPartyAccountCreated,
            ensured.RecoveredFromUniqueConflict);
    }

    public Task<ThirdPartyAccountReadDto?> UpdateAccountAsync(
        int customerId,
        long thirdPartyId,
        long thirdPartyAccountId,
        UpdateThirdPartyAccountCommand command,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(command);
        return SetOwnedAccountStatusAsync(customerId, thirdPartyId, thirdPartyAccountId, command.Status, ct);
    }

    public Task<ThirdPartyAccountReadDto?> SetAccountStatusAsync(
        int customerId,
        long thirdPartyId,
        long thirdPartyAccountId,
        SetThirdPartyAdministrativeStatusCommand command,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(command);
        return SetOwnedAccountStatusAsync(customerId, thirdPartyId, thirdPartyAccountId, command.Status, ct);
    }

    private async Task<ThirdPartyAccountReadDto?> SetOwnedAccountStatusAsync(
        int customerId,
        long thirdPartyId,
        long thirdPartyAccountId,
        ThirdPartyAdministrativeStatus status,
        CancellationToken ct)
    {
        ValidateOperationalAdministrativeStatus(status);
        var account = await _context.ThirdPartyAccounts
            .FirstOrDefaultAsync(x => x.Id == thirdPartyAccountId
                                      && x.ThirdPartyId == thirdPartyId
                                      && x.ThirdParty.CustomerId == customerId, ct);
        if (account is null)
        {
            return null;
        }

        if (account.Status != status)
        {
            account.Status = status;
            await _context.SaveChangesAsync(ct);
        }

        _logger.LogInformation(
            "Canonical third-party account administrative status set. CustomerId={CustomerId}, ThirdPartyId={ThirdPartyId}, ThirdPartyAccountId={ThirdPartyAccountId}, Status={Status}.",
            customerId,
            thirdPartyId,
            thirdPartyAccountId,
            status);
        return await _accountReadService.GetByAccountIdAsync(thirdPartyAccountId, null, ct);
    }

    private Task<ThirdParty?> FindOwnedThirdPartyAsync(int customerId, long thirdPartyId, CancellationToken ct)
        => _context.ThirdParties.FirstOrDefaultAsync(x => x.CustomerId == customerId && x.Id == thirdPartyId, ct);

    private Task<OwnedThirdPartyIdentity?> GetOwnedThirdPartyIdentityAsync(
        int customerId,
        long thirdPartyId,
        CancellationToken ct)
        => _context.ThirdParties
            .AsNoTracking()
            .Where(x => x.CustomerId == customerId && x.Id == thirdPartyId)
            .Select(x => new OwnedThirdPartyIdentity(
                x.DocumentType,
                x.DocumentNumberNormalized,
                x.DisplayName,
                x.PersonType))
            .FirstOrDefaultAsync(ct);

    private async Task ValidateMasterReferencesAsync(
        int customerId,
        string documentType,
        string personType,
        CancellationToken ct)
    {
        if (customerId <= 0 || !await _context.Customers.AsNoTracking().AnyAsync(x => x.Id == customerId, ct))
        {
            throw new ThirdPartyAdministrationConflictException("CUSTOMER_NOT_FOUND", "El Customer solicitado no existe.");
        }

        var normalizedDocumentType = ThirdParty.NormalizeCatalogCode(documentType, nameof(documentType));
        if (!await _context.DocumentTypes.AsNoTracking().AnyAsync(x => x.Code == normalizedDocumentType, ct))
        {
            throw new ThirdPartyAdministrationConflictException("DOCUMENT_TYPE_NOT_FOUND", "El tipo documental no existe en el catálogo vigente.");
        }

        var normalizedPersonType = ThirdParty.NormalizeCatalogCode(personType, nameof(personType));
        if (!await _context.PersonTypes.AsNoTracking().AnyAsync(x => x.Code == normalizedPersonType, ct))
        {
            throw new ThirdPartyAdministrationConflictException("PERSON_TYPE_NOT_FOUND", "El tipo de persona no existe en el catálogo vigente.");
        }
    }

    private static void ValidateOperationalAdministrativeStatus(ThirdPartyAdministrativeStatus status)
    {
        if (status is not ThirdPartyAdministrativeStatus.Active and not ThirdPartyAdministrativeStatus.Inactive)
        {
            throw new ArgumentException("La operación administrativa sólo permite estados Active o Inactive.", nameof(status));
        }
    }

    private static PagedResponse<ThirdPartyAdministrationDto> EmptyPage(ThirdPartyAdministrationQuery query)
    {
        var page = query.Page <= 0 ? 1 : query.Page;
        var pageSize = Math.Clamp(query.PageSize <= 0 ? 20 : query.PageSize, 1, MaxPageSize);
        return new PagedResponse<ThirdPartyAdministrationDto>
        {
            Items = [],
            Total = 0,
            Page = page,
            PageSize = pageSize
        };
    }

    private static ThirdPartyAdministrationDto Map(ThirdPartyProjection row) => new()
    {
        ThirdPartyId = row.Id,
        CustomerId = row.CustomerId,
        PersonType = row.PersonType,
        DocumentType = row.DocumentType,
        DocumentNumberMasked = MaskSensitive(row.DocumentNumberNormalized),
        DisplayName = row.DisplayName,
        Status = row.Status,
        AccountCount = row.AccountCount,
        CreatedAt = row.CreatedAt,
        UpdatedAt = row.UpdatedAt
    };

    private static string MaskSensitive(string value)
    {
        var normalized = new string((value ?? string.Empty).Where(char.IsLetterOrDigit).ToArray());
        return normalized.Length <= 4 ? "N/A" : $"****{normalized[^4..]}";
    }

    private sealed record ThirdPartyProjection(
        long Id,
        int CustomerId,
        string PersonType,
        string DocumentType,
        string DocumentNumberNormalized,
        string DisplayName,
        ThirdPartyAdministrativeStatus Status,
        int AccountCount,
        DateTimeOffset CreatedAt,
        DateTimeOffset UpdatedAt);

    private sealed record OwnedThirdPartyIdentity(
        string DocumentType,
        string DocumentNumberNormalized,
        string DisplayName,
        string PersonType);
}
