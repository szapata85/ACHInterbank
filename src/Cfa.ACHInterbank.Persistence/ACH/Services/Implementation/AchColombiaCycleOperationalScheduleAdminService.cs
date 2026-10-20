using Cfa.ACHInterbank.Application.ACH.Interfaces;
using Cfa.ACHInterbank.Application.ACH.Models;
using Cfa.ACHInterbank.Domain.Models.ACH;
using Cfa.ACHInterbank.Domain.Models.Configurations;
using Cfa.ACHInterbank.Persistence.DataBase;
using Microsoft.EntityFrameworkCore;

namespace Cfa.ACHInterbank.Persistence.ACH.Services.Implementation;

[Scoped]
public sealed class AchColombiaCycleOperationalScheduleAdminService(
    AchDbContext context,
    IAchColombiaOperationalScheduleSynchronizer scheduleSynchronizer)
    : IAchColombiaCycleOperationalScheduleAdminService
{
    public async Task<IReadOnlyList<AchColombiaCycleOperationalScheduleAdministrationRowDto>> GetAdministrationAsync(
        CancellationToken ct = default)
    {
        var house = await context.ClearingHouses
            .AsNoTracking()
            .SingleOrDefaultAsync(x => x.Code == RegulatoryCycleScheduleCatalog.AchColombiaCode, ct);
        if (house is null)
        {
            return [];
        }

        var cycleConfigs = await context.ClearingHouseCycleConfigs
            .AsNoTracking()
            .Where(x => x.ClearingHouseId == house.Id)
            .OrderByDescending(x => x.EffectiveFrom)
            .ThenBy(x => x.CycleName)
            .ToListAsync(ct);
        if (cycleConfigs.Count == 0)
        {
            return [];
        }

        var selectedPolicy = cycleConfigs
            .GroupBy(x => new { x.PolicyVersion, x.EffectiveFrom })
            .OrderByDescending(x => x.Key.EffectiveFrom)
            .First();

        var selected = selectedPolicy
            .OrderBy(x => x.CycleName)
            .ToArray();
        var selectedIds = selected.Select(x => x.Id).ToArray();
        var schedules = await context.AchColombiaCycleOperationalSchedules
            .AsNoTracking()
            .Where(x => selectedIds.Contains(x.ClearingHouseCycleConfigId))
            .ToDictionaryAsync(x => x.ClearingHouseCycleConfigId, ct);

        return selected.Select(cycle =>
        {
            schedules.TryGetValue(cycle.Id, out var schedule);
            return new AchColombiaCycleOperationalScheduleAdministrationRowDto(
                house.Id,
                cycle.Id,
                cycle.PolicyVersion,
                cycle.CycleName,
                cycle.StartTime,
                cycle.CutoffTime,
                cycle.IsActive,
                schedule?.Id,
                schedule?.AutomaticOutboundEnabled ?? false,
                schedule?.AutomaticOutboundStartTime,
                schedule?.AutomaticOutboundLatestStartTime,
                schedule?.OutboundExecutionStrategy ?? AchColombiaOutboundExecutionStrategy.Window,
                schedule?.EffectiveTimeZoneId ?? RegulatoryCycleScheduleCatalog.BogotaTimeZoneId,
                schedule?.IsActive ?? false,
                schedule is null ? null : Convert.ToBase64String(schedule.RowVersion));
        }).ToArray();
    }

    public async Task<IReadOnlyList<AchColombiaCycleOperationalScheduleDto>> GetAsync(
        AchColombiaCycleOperationalScheduleFilter filter,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(filter);

        var query = BaseQuery();
        if (filter.ClearingHouseCycleConfigId.HasValue)
        {
            query = query.Where(x => x.ClearingHouseCycleConfigId == filter.ClearingHouseCycleConfigId.Value);
        }

        if (filter.IsActive.HasValue)
        {
            query = query.Where(x => x.IsActive == filter.IsActive.Value);
        }

        var rows = await query
            .OrderBy(x => x.ClearingHouseCycleConfig.PolicyVersion)
            .ThenBy(x => x.ClearingHouseCycleConfig.CycleName)
            .ThenBy(x => x.Id)
            .ToListAsync(ct);

        return rows.Select(Map).ToArray();
    }

    public async Task<AchColombiaCycleOperationalScheduleDto?> GetByCycleAsync(
        int clearingHouseCycleConfigId,
        CancellationToken ct = default)
    {
        var entity = await BaseQuery()
            .FirstOrDefaultAsync(x => x.ClearingHouseCycleConfigId == clearingHouseCycleConfigId, ct);
        return entity is null ? null : Map(entity);
    }

    public async Task<AchColombiaCycleOperationalScheduleDto> CreateAsync(
        CreateAchColombiaCycleOperationalScheduleRequest request,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        ValidateScheduleValues(
            request.AutomaticOutboundEnabled,
            request.AutomaticOutboundStartTime,
            request.AutomaticOutboundLatestStartTime,
            request.OutboundExecutionStrategy,
            request.EffectiveTimeZoneId);

        var scope = await ResolveScopeAsync(request.ClearingHouseId, request.ClearingHouseCycleConfigId, ct);
        EnsureActivationAllowed(scope.CycleConfig, request.IsActive);
        await EnsureNoDuplicateAsync(request.ClearingHouseId, request.ClearingHouseCycleConfigId, null, ct);

        var entity = new AchColombiaCycleOperationalSchedule
        {
            ClearingHouseId = request.ClearingHouseId,
            ClearingHouseCycleConfigId = request.ClearingHouseCycleConfigId,
            AutomaticOutboundEnabled = request.AutomaticOutboundEnabled,
            AutomaticOutboundStartTime = request.AutomaticOutboundStartTime,
            AutomaticOutboundLatestStartTime = request.AutomaticOutboundLatestStartTime,
            OutboundExecutionStrategy = request.OutboundExecutionStrategy,
            EffectiveTimeZoneId = request.EffectiveTimeZoneId.Trim(),
            IsActive = request.IsActive
        };

        context.AchColombiaCycleOperationalSchedules.Add(entity);
        await context.SaveChangesAsync(ct);
        await scheduleSynchronizer.SynchronizeOutboundAsync(ct);
        return await GetRequiredAsync(entity.ClearingHouseCycleConfigId, ct);
    }

    public async Task<AchColombiaCycleOperationalScheduleDto?> UpdateAsync(
        int id,
        UpdateAchColombiaCycleOperationalScheduleRequest request,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        ValidateScheduleValues(
            request.AutomaticOutboundEnabled,
            request.AutomaticOutboundStartTime,
            request.AutomaticOutboundLatestStartTime,
            request.OutboundExecutionStrategy,
            request.EffectiveTimeZoneId);

        var entity = await context.AchColombiaCycleOperationalSchedules
            .FirstOrDefaultAsync(x => x.Id == id, ct);
        if (entity is null)
        {
            return null;
        }

        EnsureExpectedRowVersion(entity, request.ExpectedRowVersion);
        var scope = await ResolveScopeAsync(request.ClearingHouseId, request.ClearingHouseCycleConfigId, ct);
        EnsureActivationAllowed(scope.CycleConfig, request.IsActive);
        await EnsureNoDuplicateAsync(request.ClearingHouseId, request.ClearingHouseCycleConfigId, id, ct);

        entity.ClearingHouseId = request.ClearingHouseId;
        entity.ClearingHouseCycleConfigId = request.ClearingHouseCycleConfigId;
        entity.AutomaticOutboundEnabled = request.AutomaticOutboundEnabled;
        entity.AutomaticOutboundStartTime = request.AutomaticOutboundStartTime;
        entity.AutomaticOutboundLatestStartTime = request.AutomaticOutboundLatestStartTime;
        entity.OutboundExecutionStrategy = request.OutboundExecutionStrategy;
        entity.EffectiveTimeZoneId = request.EffectiveTimeZoneId.Trim();
        entity.IsActive = request.IsActive;

        await SaveWithConcurrencyHandlingAsync(entity, ct);
        await scheduleSynchronizer.SynchronizeOutboundAsync(ct);
        return await GetRequiredAsync(entity.ClearingHouseCycleConfigId, ct);
    }

    public async Task<AchColombiaCycleOperationalScheduleDto?> DeactivateAsync(
        int id,
        DeactivateAchColombiaCycleOperationalScheduleRequest request,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        var entity = await context.AchColombiaCycleOperationalSchedules
            .FirstOrDefaultAsync(x => x.Id == id, ct);
        if (entity is null)
        {
            return null;
        }

        EnsureExpectedRowVersion(entity, request.ExpectedRowVersion);
        entity.IsActive = false;
        await SaveWithConcurrencyHandlingAsync(entity, ct);
        await scheduleSynchronizer.SynchronizeOutboundAsync(ct);
        return await GetRequiredAsync(entity.ClearingHouseCycleConfigId, ct);
    }

    private IQueryable<AchColombiaCycleOperationalSchedule> BaseQuery()
        => context.AchColombiaCycleOperationalSchedules
            .AsNoTracking()
            .Include(x => x.ClearingHouse)
            .Include(x => x.ClearingHouseCycleConfig);

    private async Task<ScopeResolution> ResolveScopeAsync(
        int clearingHouseId,
        int clearingHouseCycleConfigId,
        CancellationToken ct)
    {
        var house = await context.ClearingHouses
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == clearingHouseId, ct)
            ?? throw new AchColombiaCycleOperationalScheduleValidationException(
                "La cámara de compensación indicada no existe.");

        if (!string.Equals(house.Code, RegulatoryCycleScheduleCatalog.AchColombiaCode, StringComparison.OrdinalIgnoreCase))
        {
            throw new AchColombiaCycleOperationalScheduleValidationException(
                "La programación operacional de intercambio solo puede configurarse para ACH Colombia.");
        }

        var cycleConfig = await context.ClearingHouseCycleConfigs
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == clearingHouseCycleConfigId, ct)
            ?? throw new AchColombiaCycleOperationalScheduleValidationException(
                "La configuración versionada del ciclo no existe.");

        if (cycleConfig.ClearingHouseId != house.Id)
        {
            throw new AchColombiaCycleOperationalScheduleValidationException(
                "La configuración versionada del ciclo no pertenece a la cámara ACH Colombia indicada.");
        }

        return new ScopeResolution(house, cycleConfig);
    }

    private async Task EnsureNoDuplicateAsync(
        int clearingHouseId,
        int clearingHouseCycleConfigId,
        int? excludingId,
        CancellationToken ct)
    {
        var duplicate = await context.AchColombiaCycleOperationalSchedules
            .AsNoTracking()
            .AnyAsync(x => x.ClearingHouseId == clearingHouseId
                && x.ClearingHouseCycleConfigId == clearingHouseCycleConfigId
                && (!excludingId.HasValue || x.Id != excludingId.Value), ct);

        if (duplicate)
        {
            throw new AchColombiaCycleOperationalScheduleValidationException(
                "Ya existe una programación operacional para la cámara y configuración de ciclo indicadas.");
        }
    }

    private static void EnsureActivationAllowed(ClearingHouseCycleConfig cycleConfig, bool requestedActive)
    {
        if (!requestedActive)
        {
            return;
        }

        if (string.Equals(
                cycleConfig.PolicyVersion,
                RegulatoryCycleScheduleCatalog.AchColombiaSevenCycleCandidateVersion,
                StringComparison.OrdinalIgnoreCase)
            && !RegulatoryCycleScheduleCatalog.AchColombiaSevenCycleCandidate.IsActivatable)
        {
            throw new AchColombiaCycleOperationalScheduleValidationException(
                "ACHCOL-7C-CANDIDATE permanece inactiva y no activable; su programación operacional solo puede prepararse como inactiva.");
        }

        if (!cycleConfig.IsActive)
        {
            throw new AchColombiaCycleOperationalScheduleValidationException(
                "Una programación operacional no puede activarse sobre una configuración regulatoria de ciclo inactiva.");
        }
    }

    private static void ValidateScheduleValues(
        bool automaticOutboundEnabled,
        TimeSpan? automaticOutboundStartTime,
        TimeSpan? automaticOutboundLatestStartTime,
        AchColombiaOutboundExecutionStrategy outboundExecutionStrategy,
        string effectiveTimeZoneId)
    {
        if (!Enum.IsDefined(outboundExecutionStrategy))
        {
            throw new AchColombiaCycleOperationalScheduleValidationException(
                "OutboundExecutionStrategy contiene un valor no soportado.");
        }

        if (automaticOutboundStartTime.HasValue
            && automaticOutboundLatestStartTime.HasValue
            && automaticOutboundStartTime.Value > automaticOutboundLatestStartTime.Value)
        {
            throw new AchColombiaCycleOperationalScheduleValidationException(
                "AutomaticOutboundStartTime no puede ser posterior a AutomaticOutboundLatestStartTime.");
        }

        if (outboundExecutionStrategy == AchColombiaOutboundExecutionStrategy.ExactTime
            && !automaticOutboundStartTime.HasValue)
        {
            throw new AchColombiaCycleOperationalScheduleValidationException(
                "AutomaticOutboundStartTime es obligatorio para la estrategia ExactTime.");
        }

        if (automaticOutboundEnabled
            && outboundExecutionStrategy == AchColombiaOutboundExecutionStrategy.Window
            && !automaticOutboundStartTime.HasValue
            && !automaticOutboundLatestStartTime.HasValue)
        {
            throw new AchColombiaCycleOperationalScheduleValidationException(
                "Una programación outbound automática con estrategia Window requiere al menos un límite horario operacional.");
        }

        ValidateTimeZone(effectiveTimeZoneId);
    }

    private static void ValidateTimeZone(string effectiveTimeZoneId)
    {
        if (string.IsNullOrWhiteSpace(effectiveTimeZoneId))
        {
            throw new AchColombiaCycleOperationalScheduleValidationException(
                "EffectiveTimeZoneId es obligatorio.");
        }

        var candidate = effectiveTimeZoneId.Trim();
        if (CanResolveTimeZone(candidate))
        {
            return;
        }

        if (TimeZoneInfo.TryConvertIanaIdToWindowsId(candidate, out var windowsId)
            && CanResolveTimeZone(windowsId))
        {
            return;
        }

        throw new AchColombiaCycleOperationalScheduleValidationException(
            "EffectiveTimeZoneId no corresponde a una zona horaria válida.");
    }

    private static bool CanResolveTimeZone(string timeZoneId)
    {
        try
        {
            _ = TimeZoneInfo.FindSystemTimeZoneById(timeZoneId);
            return true;
        }
        catch (TimeZoneNotFoundException)
        {
            return false;
        }
        catch (InvalidTimeZoneException)
        {
            return false;
        }
    }

    private async Task SaveWithConcurrencyHandlingAsync(
        AchColombiaCycleOperationalSchedule entity,
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
                : Convert.ToBase64String((byte[])databaseValues[nameof(AchColombiaCycleOperationalSchedule.RowVersion)]!);

            throw new AchColombiaCycleOperationalScheduleConcurrencyException(
                "La programación operacional fue modificada por otro proceso. Recargue los datos e intente nuevamente.",
                currentRowVersion);
        }
    }

    private static void EnsureExpectedRowVersion(
        AchColombiaCycleOperationalSchedule entity,
        string expectedRowVersion)
    {
        if (string.IsNullOrWhiteSpace(expectedRowVersion))
        {
            throw new AchColombiaCycleOperationalScheduleValidationException(
                "ExpectedRowVersion es obligatorio para modificar la programación operacional.");
        }

        byte[] expected;
        try
        {
            expected = Convert.FromBase64String(expectedRowVersion);
        }
        catch (FormatException)
        {
            throw new AchColombiaCycleOperationalScheduleValidationException(
                "ExpectedRowVersion no tiene un formato Base64 válido.");
        }

        if (!entity.RowVersion.SequenceEqual(expected))
        {
            throw new AchColombiaCycleOperationalScheduleConcurrencyException(
                "La programación operacional fue modificada por otro proceso. Recargue los datos e intente nuevamente.",
                Convert.ToBase64String(entity.RowVersion));
        }
    }

    private async Task<AchColombiaCycleOperationalScheduleDto> GetRequiredAsync(
        int clearingHouseCycleConfigId,
        CancellationToken ct)
        => await GetByCycleAsync(clearingHouseCycleConfigId, ct)
           ?? throw new InvalidOperationException("No se pudo consultar la programación operacional guardada.");

    private static AchColombiaCycleOperationalScheduleDto Map(AchColombiaCycleOperationalSchedule entity)
        => new(
            entity.Id,
            entity.ClearingHouseId,
            entity.ClearingHouse.Code,
            entity.ClearingHouseCycleConfigId,
            entity.ClearingHouseCycleConfig.PolicyVersion,
            entity.ClearingHouseCycleConfig.CycleName,
            entity.AutomaticOutboundEnabled,
            entity.AutomaticOutboundStartTime,
            entity.AutomaticOutboundLatestStartTime,
            entity.OutboundExecutionStrategy,
            entity.EffectiveTimeZoneId,
            entity.IsActive,
            entity.CreatedAt,
            entity.UpdatedAt,
            Convert.ToBase64String(entity.RowVersion));

    private sealed record ScopeResolution(ClearingHouse House, ClearingHouseCycleConfig CycleConfig);
}
