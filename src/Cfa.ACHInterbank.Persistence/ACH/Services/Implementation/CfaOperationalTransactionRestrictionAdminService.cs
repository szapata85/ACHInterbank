using Cfa.ACHInterbank.Application.ACH.Interfaces;
using Cfa.ACHInterbank.Application.ACH.Models;
using Cfa.ACHInterbank.Domain.Entities.Transactions.Enums;
using Cfa.ACHInterbank.Domain.Models.ACH;
using Cfa.ACHInterbank.Domain.Models.Configurations;
using Cfa.ACHInterbank.Persistence.DataBase;
using Microsoft.EntityFrameworkCore;

namespace Cfa.ACHInterbank.Persistence.ACH.Services.Implementation;

[Scoped]
public sealed class CfaOperationalTransactionRestrictionAdminService(AchDbContext context)
    : ICfaOperationalTransactionRestrictionAdminService
{
    public async Task<IReadOnlyList<CfaOperationalTransactionRestrictionDto>> GetAsync(
        CfaOperationalTransactionRestrictionFilter filter,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(filter);

        var query = BaseQuery();
        var clearingHouseCode = NormalizeCode(filter.ClearingHouseCode);
        var policyVersion = NormalizeOptional(filter.PolicyVersion);

        if (clearingHouseCode is not null)
        {
            query = query.Where(x =>
                (x.ClearingHouse != null && x.ClearingHouse.Code == clearingHouseCode)
                || (x.ClearingHouseCycleConfig != null
                    && x.ClearingHouseCycleConfig.ClearingHouse.Code == clearingHouseCode));
        }

        if (policyVersion is not null)
        {
            query = query.Where(x =>
                x.ClearingHouseCycleConfig != null
                && x.ClearingHouseCycleConfig.PolicyVersion == policyVersion);
        }

        if (filter.ClearingHouseCycleConfigId.HasValue)
        {
            query = query.Where(x => x.ClearingHouseCycleConfigId == filter.ClearingHouseCycleConfigId.Value);
        }

        if (filter.TransactionType.HasValue)
        {
            EnsureDefined(filter.TransactionType.Value, nameof(filter.TransactionType));
            query = query.Where(x => x.TransactionType == filter.TransactionType.Value);
        }

        if (filter.IsPrenotification.HasValue)
        {
            query = query.Where(x => x.IsPrenotification == filter.IsPrenotification.Value);
        }

        if (filter.IsActive.HasValue)
        {
            query = query.Where(x => x.IsActive == filter.IsActive.Value);
        }

        if (filter.EffectiveAt.HasValue)
        {
            var effectiveAt = filter.EffectiveAt.Value;
            query = query.Where(x =>
                x.EffectiveFrom <= effectiveAt
                && (!x.EffectiveTo.HasValue || x.EffectiveTo.Value >= effectiveAt));
        }

        var rows = await query
            .OrderBy(x => x.ClearingHouseCycleConfigId.HasValue ? 2 : x.ClearingHouseId.HasValue ? 1 : 0)
            .ThenBy(x => x.ClearingHouse != null
                ? x.ClearingHouse.Code
                : x.ClearingHouseCycleConfig != null
                    ? x.ClearingHouseCycleConfig.ClearingHouse.Code
                    : string.Empty)
            .ThenBy(x => x.ClearingHouseCycleConfig != null ? x.ClearingHouseCycleConfig.PolicyVersion : string.Empty)
            .ThenBy(x => x.ClearingHouseCycleConfig != null ? x.ClearingHouseCycleConfig.CycleName : string.Empty)
            .ThenBy(x => x.TransactionType)
            .ThenBy(x => x.IsPrenotification)
            .ThenByDescending(x => x.EffectiveFrom)
            .ThenBy(x => x.Id)
            .ToListAsync(ct);

        return rows.Select(Map).ToArray();
    }

    public async Task<CfaOperationalTransactionRestrictionDto?> GetByIdAsync(
        int id,
        CancellationToken ct = default)
    {
        var entity = await BaseQuery().FirstOrDefaultAsync(x => x.Id == id, ct);
        return entity is null ? null : Map(entity);
    }

    public async Task<CfaOperationalTransactionRestrictionDto> CreateAsync(
        CreateCfaOperationalTransactionRestrictionRequest request,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        ValidateValues(
            request.TransactionType,
            request.MaxAmountPerTransaction,
            request.MaxAmountPerCycle,
            request.MaxTransactionsPerCycle,
            request.AllowedAccountTypes,
            request.EffectiveFrom,
            request.EffectiveTo);

        var scope = await ResolveScopeAsync(request.ClearingHouseCode, request.ClearingHouseCycleConfigId, ct);
        var entity = new CfaOperationalTransactionRestriction
        {
            ClearingHouseId = scope.DirectClearingHouseId,
            ClearingHouseCycleConfigId = scope.CycleConfigId,
            TransactionType = request.TransactionType,
            IsPrenotification = request.IsPrenotification,
            MaxAmountPerTransaction = request.MaxAmountPerTransaction,
            MaxAmountPerCycle = request.MaxAmountPerCycle,
            MaxTransactionsPerCycle = request.MaxTransactionsPerCycle,
            AllowedAccountTypes = NormalizeAllowedAccountTypes(request.AllowedAccountTypes),
            EffectiveFrom = request.EffectiveFrom,
            EffectiveTo = request.EffectiveTo,
            IsActive = request.IsActive
        };

        context.CfaOperationalTransactionRestrictions.Add(entity);
        await context.SaveChangesAsync(ct);
        return await GetRequiredAsync(entity.Id, ct);
    }

    public async Task<CfaOperationalTransactionRestrictionDto?> UpdateAsync(
        int id,
        UpdateCfaOperationalTransactionRestrictionRequest request,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        ValidateValues(
            request.TransactionType,
            request.MaxAmountPerTransaction,
            request.MaxAmountPerCycle,
            request.MaxTransactionsPerCycle,
            request.AllowedAccountTypes,
            request.EffectiveFrom,
            request.EffectiveTo);

        var entity = await context.CfaOperationalTransactionRestrictions
            .FirstOrDefaultAsync(x => x.Id == id, ct);
        if (entity is null)
        {
            return null;
        }

        EnsureExpectedRowVersion(entity, request.ExpectedRowVersion);
        var scope = await ResolveScopeAsync(request.ClearingHouseCode, request.ClearingHouseCycleConfigId, ct);

        entity.ClearingHouseId = scope.DirectClearingHouseId;
        entity.ClearingHouseCycleConfigId = scope.CycleConfigId;
        entity.TransactionType = request.TransactionType;
        entity.IsPrenotification = request.IsPrenotification;
        entity.MaxAmountPerTransaction = request.MaxAmountPerTransaction;
        entity.MaxAmountPerCycle = request.MaxAmountPerCycle;
        entity.MaxTransactionsPerCycle = request.MaxTransactionsPerCycle;
        entity.AllowedAccountTypes = NormalizeAllowedAccountTypes(request.AllowedAccountTypes);
        entity.EffectiveFrom = request.EffectiveFrom;
        entity.EffectiveTo = request.EffectiveTo;
        entity.IsActive = request.IsActive;

        await SaveWithConcurrencyHandlingAsync(entity, ct);
        return await GetRequiredAsync(entity.Id, ct);
    }

    public async Task<CfaOperationalTransactionRestrictionDto?> SetActiveAsync(
        int id,
        bool isActive,
        ChangeCfaOperationalTransactionRestrictionStatusRequest request,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        var entity = await context.CfaOperationalTransactionRestrictions
            .FirstOrDefaultAsync(x => x.Id == id, ct);
        if (entity is null)
        {
            return null;
        }

        EnsureExpectedRowVersion(entity, request.ExpectedRowVersion);
        var effectiveTo = request.EffectiveTo ?? entity.EffectiveTo;
        ValidateEffectiveRange(entity.EffectiveFrom, effectiveTo);

        entity.IsActive = isActive;
        if (request.EffectiveTo.HasValue)
        {
            entity.EffectiveTo = request.EffectiveTo.Value;
        }

        await SaveWithConcurrencyHandlingAsync(entity, ct);
        return await GetRequiredAsync(entity.Id, ct);
    }

    private IQueryable<CfaOperationalTransactionRestriction> BaseQuery()
        => context.CfaOperationalTransactionRestrictions
            .AsNoTracking()
            .Include(x => x.ClearingHouse)
            .Include(x => x.ClearingHouseCycleConfig)
                .ThenInclude(x => x.ClearingHouse);

    private async Task<ScopeResolution> ResolveScopeAsync(
        string? clearingHouseCode,
        int? clearingHouseCycleConfigId,
        CancellationToken ct)
    {
        var normalizedCode = NormalizeCode(clearingHouseCode);

        if (clearingHouseCycleConfigId.HasValue)
        {
            var cycle = await context.ClearingHouseCycleConfigs
                .AsNoTracking()
                .Include(x => x.ClearingHouse)
                .FirstOrDefaultAsync(x => x.Id == clearingHouseCycleConfigId.Value, ct)
                ?? throw new CfaOperationalTransactionRestrictionValidationException(
                    "La configuración versionada del ciclo no existe.");

            if (normalizedCode is not null
                && !string.Equals(cycle.ClearingHouse.Code, normalizedCode, StringComparison.OrdinalIgnoreCase))
            {
                throw new CfaOperationalTransactionRestrictionValidationException(
                    "La configuración versionada del ciclo no pertenece a la cámara indicada.");
            }

            return new ScopeResolution(null, cycle.Id);
        }

        if (normalizedCode is null)
        {
            return new ScopeResolution(null, null);
        }

        var house = await context.ClearingHouses
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Code == normalizedCode, ct)
            ?? throw new CfaOperationalTransactionRestrictionValidationException(
                $"No existe una cámara con código {normalizedCode}.");

        return new ScopeResolution(house.Id, null);
    }

    private async Task SaveWithConcurrencyHandlingAsync(
        CfaOperationalTransactionRestriction entity,
        CancellationToken ct)
    {
        try
        {
            await context.SaveChangesAsync(ct);
        }
        catch (DbUpdateConcurrencyException)
        {
            var databaseValues = await context.Entry(entity).GetDatabaseValuesAsync(ct);
            var currentRowVersion = databaseValues is null
                ? null
                : Convert.ToBase64String((byte[])databaseValues[nameof(CfaOperationalTransactionRestriction.RowVersion)]!);

            throw new CfaOperationalTransactionRestrictionConcurrencyException(
                "La restricción fue modificada por otro proceso. Recargue los datos e intente nuevamente.",
                currentRowVersion);
        }
    }

    private static void EnsureExpectedRowVersion(
        CfaOperationalTransactionRestriction entity,
        string expectedRowVersion)
    {
        if (string.IsNullOrWhiteSpace(expectedRowVersion))
        {
            throw new CfaOperationalTransactionRestrictionValidationException(
                "ExpectedRowVersion es obligatorio para modificar la restricción.");
        }

        byte[] expected;
        try
        {
            expected = Convert.FromBase64String(expectedRowVersion);
        }
        catch (FormatException)
        {
            throw new CfaOperationalTransactionRestrictionValidationException(
                "ExpectedRowVersion no tiene un formato Base64 válido.");
        }

        if (!entity.RowVersion.SequenceEqual(expected))
        {
            throw new CfaOperationalTransactionRestrictionConcurrencyException(
                "La restricción fue modificada por otro proceso. Recargue los datos e intente nuevamente.",
                Convert.ToBase64String(entity.RowVersion));
        }
    }

    private static void ValidateValues(
        TransactionTypeEnum? transactionType,
        decimal? maxAmountPerTransaction,
        decimal? maxAmountPerCycle,
        int? maxTransactionsPerCycle,
        IReadOnlyList<AccountTypeEnum>? allowedAccountTypes,
        DateTime effectiveFrom,
        DateTime? effectiveTo)
    {
        if (transactionType.HasValue)
        {
            EnsureDefined(transactionType.Value, nameof(transactionType));
        }

        if (maxAmountPerTransaction < 0)
        {
            throw new CfaOperationalTransactionRestrictionValidationException(
                "MaxAmountPerTransaction no puede ser negativo.");
        }

        if (maxAmountPerCycle < 0)
        {
            throw new CfaOperationalTransactionRestrictionValidationException(
                "MaxAmountPerCycle no puede ser negativo.");
        }

        if (maxTransactionsPerCycle < 0)
        {
            throw new CfaOperationalTransactionRestrictionValidationException(
                "MaxTransactionsPerCycle no puede ser negativo.");
        }

        if (allowedAccountTypes is not null)
        {
            foreach (var accountType in allowedAccountTypes)
            {
                EnsureDefined(accountType, nameof(allowedAccountTypes));
            }
        }

        ValidateEffectiveRange(effectiveFrom, effectiveTo);
    }

    private static void ValidateEffectiveRange(DateTime effectiveFrom, DateTime? effectiveTo)
    {
        if (effectiveFrom == default)
        {
            throw new CfaOperationalTransactionRestrictionValidationException(
                "EffectiveFrom es obligatorio.");
        }

        if (effectiveTo.HasValue && effectiveTo.Value < effectiveFrom)
        {
            throw new CfaOperationalTransactionRestrictionValidationException(
                "EffectiveTo no puede ser anterior a EffectiveFrom.");
        }
    }

    private static void EnsureDefined<TEnum>(TEnum value, string fieldName)
        where TEnum : struct, Enum
    {
        if (!Enum.IsDefined(value))
        {
            throw new CfaOperationalTransactionRestrictionValidationException(
                $"{fieldName} contiene un valor no soportado.");
        }
    }

    private static List<AccountTypeEnum>? NormalizeAllowedAccountTypes(
        IReadOnlyList<AccountTypeEnum>? allowedAccountTypes)
        => allowedAccountTypes is null
            ? null
            : allowedAccountTypes.Distinct().OrderBy(x => (int)x).ToList();

    private static string? NormalizeCode(string? value)
        => string.IsNullOrWhiteSpace(value) ? null : value.Trim().ToUpperInvariant();

    private static string? NormalizeOptional(string? value)
        => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private async Task<CfaOperationalTransactionRestrictionDto> GetRequiredAsync(int id, CancellationToken ct)
        => await GetByIdAsync(id, ct)
           ?? throw new InvalidOperationException("No se pudo consultar la restricción operacional guardada.");

    private static CfaOperationalTransactionRestrictionDto Map(CfaOperationalTransactionRestriction entity)
    {
        var cycle = entity.ClearingHouseCycleConfig;
        var house = entity.ClearingHouse ?? cycle?.ClearingHouse;

        return new CfaOperationalTransactionRestrictionDto(
            entity.Id,
            house?.Id,
            house?.Code,
            house?.Name,
            entity.ClearingHouseCycleConfigId,
            cycle?.PolicyVersion,
            cycle?.CycleName,
            entity.TransactionType,
            entity.IsPrenotification,
            entity.MaxAmountPerTransaction,
            entity.MaxAmountPerCycle,
            entity.MaxTransactionsPerCycle,
            entity.AllowedAccountTypes is null
                ? null
                : entity.AllowedAccountTypes.Distinct().OrderBy(x => (int)x).ToArray(),
            entity.EffectiveFrom,
            entity.EffectiveTo,
            entity.IsActive,
            entity.CreatedAt,
            entity.UpdatedAt,
            Convert.ToBase64String(entity.RowVersion));
    }

    private sealed record ScopeResolution(int? DirectClearingHouseId, int? CycleConfigId);
}
