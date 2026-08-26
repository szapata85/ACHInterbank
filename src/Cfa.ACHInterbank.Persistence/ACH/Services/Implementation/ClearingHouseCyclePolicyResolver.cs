using Cfa.ACHInterbank.Application.ACH.Interfaces;
using Cfa.ACHInterbank.Application.ACH.Models;
using Cfa.ACHInterbank.Domain.Models.ACH;
using Cfa.ACHInterbank.Domain.Models.Configurations;
using Cfa.ACHInterbank.Persistence.DataBase;
using Microsoft.EntityFrameworkCore;

namespace Cfa.ACHInterbank.Persistence.ACH.Services.Implementation;

[Scoped]
public sealed class ClearingHouseCyclePolicyResolver(
    AchDbContext context,
    ICycleNumberResolver cycleNumberResolver) : IClearingHouseCyclePolicyResolver
{
    public async Task<ResolvedClearingHouseCyclePolicy> ResolveAsync(
        int clearingHouseId,
        DateTime operationalDate,
        CancellationToken ct = default)
    {
        if (clearingHouseId <= 0 || operationalDate == default)
        {
            throw new InvalidOperationException("La cámara y la fecha operativa son requeridas para resolver la política de ciclos.");
        }

        var clearingHouse = await context.ClearingHouses
            .AsNoTracking()
            .Include(house => house.ClearingHouseConfig)
            .SingleOrDefaultAsync(house => house.Id == clearingHouseId, ct)
            ?? throw new InvalidOperationException("La cámara compensadora no existe.");
        if (!clearingHouse.IsActive)
        {
            throw new InvalidOperationException("La cámara compensadora está inactiva.");
        }

        var timeZoneId = clearingHouse.ClearingHouseConfig?.TimeZoneId?.Trim();
        _ = ResolveTimeZone(timeZoneId);
        var date = DateTime.SpecifyKind(operationalDate.Date, DateTimeKind.Utc);
        var candidates = await context.ClearingHouseCycleConfigs
            .AsNoTracking()
            .Where(config => config.ClearingHouseId == clearingHouseId
                && config.IsActive
                && config.EffectiveFrom.Date <= date.Date
                && (!config.EffectiveTo.HasValue || config.EffectiveTo.Value.Date >= date.Date))
            .ToListAsync(ct);
        if (candidates.Count == 0)
        {
            throw new InvalidOperationException("No existe una política de ciclos activa y vigente para la cámara y fecha operativa.");
        }

        var versions = candidates
            .GroupBy(config => NormalizeVersion(config.PolicyVersion), StringComparer.OrdinalIgnoreCase)
            .ToList();
        if (versions.Count != 1)
        {
            throw new InvalidOperationException("Existen múltiples versiones de política de ciclos vigentes para la misma cámara y fecha.");
        }

        var cycles = versions[0].OrderBy(config => config.CutoffTime).ToList();
        ValidateCycles(cycles);
        return new ResolvedClearingHouseCyclePolicy(
            clearingHouseId,
            clearingHouse.Code,
            versions[0].Key,
            date,
            timeZoneId!,
            cycles);
    }

    public async Task<ResolvedClearingHouseCyclePolicy> ResolveAtInstantAsync(
        int clearingHouseId,
        DateTimeOffset instant,
        CancellationToken ct = default)
    {
        var timeZoneId = await context.ClearingHouses
            .AsNoTracking()
            .Where(house => house.Id == clearingHouseId)
            .Select(house => house.ClearingHouseConfig.TimeZoneId)
            .SingleOrDefaultAsync(ct);
        var zone = ResolveTimeZone(timeZoneId);
        var localDate = TimeZoneInfo.ConvertTime(instant, zone).Date;
        return await ResolveAsync(clearingHouseId, localDate, ct);
    }

    private void ValidateCycles(IReadOnlyList<ClearingHouseCycleConfig> cycles)
    {
        var duplicateName = cycles
            .GroupBy(config => config.CycleName.Trim(), StringComparer.OrdinalIgnoreCase)
            .FirstOrDefault(group => group.Count() > 1);
        if (duplicateName is not null)
        {
            throw new InvalidOperationException($"La política contiene el ciclo duplicado '{duplicateName.Key}'.");
        }

        var duplicateNumber = cycles
            .Select(config => new { Config = config, Number = cycleNumberResolver.Resolve(config.CycleName) })
            .Where(item => item.Number.HasValue)
            .GroupBy(item => item.Number!.Value)
            .FirstOrDefault(group => group.Count() > 1);
        if (duplicateNumber is not null)
        {
            throw new InvalidOperationException($"La política contiene duplicado el número de ciclo {duplicateNumber.Key}.");
        }

        foreach (var cycle in cycles)
        {
            ValidateStageOrder(cycle);
        }
    }

    internal static void ValidateStageOrder(ClearingHouseCycleConfig cycle)
    {
        if (cycle.StartTime == cycle.EndTime)
        {
            throw new InvalidOperationException($"El ciclo '{cycle.CycleName}' tiene una ventana operativa imposible.");
        }

        var close = OffsetFromOpen(cycle.StartTime, cycle.EndTime);
        var cutoff = OffsetFromOpen(cycle.StartTime, cycle.CutoffTime);
        var release = OffsetFromOpen(cycle.StartTime, cycle.OutputReleaseTime);
        if (cutoff > close)
        {
            throw new InvalidOperationException($"El cutoff de entrada del ciclo '{cycle.CycleName}' ocurre después del cierre.");
        }
        if (release < close)
        {
            throw new InvalidOperationException($"La liberación de salida del ciclo '{cycle.CycleName}' ocurre antes del cierre.");
        }
    }

    private static TimeSpan OffsetFromOpen(TimeSpan open, TimeSpan value)
        => value >= open ? value - open : TimeSpan.FromDays(1) - open + value;

    private static string NormalizeVersion(string? value)
        => string.IsNullOrWhiteSpace(value) ? "LEGACY" : value.Trim();

    private static TimeZoneInfo ResolveTimeZone(string? timeZoneId)
    {
        if (string.IsNullOrWhiteSpace(timeZoneId))
        {
            throw new InvalidOperationException("La cámara no tiene configurada una zona horaria operativa.");
        }

        try
        {
            return TimeZoneInfo.FindSystemTimeZoneById(timeZoneId.Trim());
        }
        catch (TimeZoneNotFoundException) when (TimeZoneInfo.TryConvertIanaIdToWindowsId(timeZoneId.Trim(), out var windowsId))
        {
            return TimeZoneInfo.FindSystemTimeZoneById(windowsId);
        }
        catch (TimeZoneNotFoundException exception)
        {
            throw new InvalidOperationException("La zona horaria operativa configurada no es válida.", exception);
        }
        catch (InvalidTimeZoneException exception)
        {
            throw new InvalidOperationException("La zona horaria operativa configurada no es válida.", exception);
        }
    }
}
