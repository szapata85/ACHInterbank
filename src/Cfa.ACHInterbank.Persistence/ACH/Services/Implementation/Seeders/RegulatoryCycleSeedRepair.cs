using Cfa.ACHInterbank.Application.ACH.Interfaces;
using Cfa.ACHInterbank.Application.ACH.Models;
using Cfa.ACHInterbank.Application.ACH.Services;
using Cfa.ACHInterbank.Domain.Models.ACH;
using Cfa.ACHInterbank.Persistence.DataBase;
using Microsoft.EntityFrameworkCore;

namespace Cfa.ACHInterbank.Persistence.ACH.Services.Implementation.Seeders;

internal static class RegulatoryCycleSeedRepair
{
    // These fingerprints are compatibility snapshots. They must not depend on whichever
    // policy becomes current later, otherwise a future catalog change could rewrite history.
    private static readonly IReadOnlyList<IReadOnlyList<RegulatoryCycleSchedule>> CenitDefectiveFingerprints = FreezeFingerprints(
        RegulatoryCycleScheduleCatalog.AchColombiaV35HistoricalFingerprint,
        Freeze(
            Legacy(1, 19, 0, 8, 0),
            Legacy(2, 8, 1, 10, 30),
            Legacy(3, 10, 31, 13, 0),
            Legacy(4, 13, 1, 15, 30),
            Legacy(5, 15, 31, 18, 0)));

    private static readonly IReadOnlyList<IReadOnlyList<RegulatoryCycleSchedule>> AchColombiaDefectiveFingerprints = FreezeFingerprints(
        Freeze(
            Legacy(1, 19, 1, 8, 15),
            Legacy(2, 8, 16, 10, 45),
            Legacy(3, 10, 46, 13, 15),
            Legacy(4, 13, 16, 15, 30),
            Legacy(5, 15, 31, 18, 0)));

    public static async Task ApplyAsync(
        AchDbContext context,
        int clearingHouseId,
        string clearingHouseCode,
        int requestedYear,
        ICycleNumberResolver? cycleNumberResolver = null,
        CancellationToken ct = default)
    {
        if (string.Equals(clearingHouseCode, RegulatoryCycleScheduleCatalog.AchColombiaCode, StringComparison.OrdinalIgnoreCase))
        {
            await ApplyAsync(
                context,
                clearingHouseId,
                RegulatoryCycleScheduleCatalog.AchColombiaV35HistoricalPolicy,
                cycleNumberResolver,
                ct);
            return;
        }

        if (!string.Equals(clearingHouseCode, RegulatoryCycleScheduleCatalog.CenitCode, StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        var periodStart = DateTime.SpecifyKind(new DateTime(requestedYear, 1, 1), DateTimeKind.Utc);
        var periodEnd = DateTime.SpecifyKind(new DateTime(requestedYear, 12, 31), DateTimeKind.Utc);
        var existingPeriod = await context.ClearingHouseCycleConfigs
            .Where(config => config.ClearingHouseId == clearingHouseId
                && config.IsActive
                && config.EffectiveFrom.Date <= periodEnd.Date
                && (!config.EffectiveTo.HasValue || config.EffectiveTo.Value.Date >= periodStart.Date))
            .OrderBy(config => config.EffectiveFrom)
            .ToListAsync(ct);

        var basePolicy = RegulatoryCycleScheduleCatalog.GetHistoricalSeedPolicy(clearingHouseCode);
        RegulatoryCyclePolicySnapshot policy;
        if (existingPeriod.Count == 0)
        {
            policy = basePolicy with
            {
                PolicyVersion = $"REGULATORY-{requestedYear}",
                EffectiveFrom = periodStart
            };
        }
        else
        {
            var starts = existingPeriod.Select(config => UtcDate(config.EffectiveFrom)).Distinct().ToArray();
            var versions = existingPeriod.Select(config => NormalizeVersion(config.PolicyVersion)).Distinct(StringComparer.OrdinalIgnoreCase).ToArray();
            if (starts.Length != 1 || versions.Length != 1)
            {
                throw new InvalidOperationException(
                    $"Existen múltiples versiones de política de ciclos CENIT vigentes para el año {requestedYear}.");
            }

            policy = basePolicy with
            {
                PolicyVersion = versions[0],
                EffectiveFrom = starts[0]
            };
        }

        await ApplyAsync(context, clearingHouseId, policy, cycleNumberResolver, ct);
    }

    public static async Task ApplyAsync(
        AchDbContext context,
        int clearingHouseId,
        RegulatoryCyclePolicySnapshot policy,
        ICycleNumberResolver? cycleNumberResolver = null,
        CancellationToken ct = default)
    {
        var resolver = cycleNumberResolver ?? new CycleNumberResolver();
        ValidatePolicy(clearingHouseId, policy, resolver);

        var effectiveFrom = UtcDate(policy.EffectiveFrom);
        var allActive = await context.ClearingHouseCycleConfigs
            .Where(config => config.ClearingHouseId == clearingHouseId && config.IsActive)
            .ToListAsync(ct);

        var target = allActive
            .Where(config => config.EffectiveFrom.Date == effectiveFrom.Date)
            .ToList();

        if (target.Count > 0)
        {
            var targetVersions = target
                .Select(config => NormalizeVersion(config.PolicyVersion))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToArray();
            if (targetVersions.Length != 1)
            {
                throw new InvalidOperationException(
                    $"Existen múltiples versiones de política de ciclos con EffectiveFrom {effectiveFrom:yyyy-MM-dd} para la cámara '{policy.ClearingHouseCode}'.");
            }
            if (!string.Equals(targetVersions[0], policy.PolicyVersion.Trim(), StringComparison.OrdinalIgnoreCase)
                && !string.Equals(targetVersions[0], "LEGACY", StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException(
                    $"No se puede activar la política '{policy.PolicyVersion}' en {effectiveFrom:yyyy-MM-dd}: ya existe la versión '{targetVersions[0]}' con el mismo EffectiveFrom.");
            }

            RepairKnownDefectiveFingerprint(target, policy, resolver);
            AddMissingCycles(context, clearingHouseId, policy, target, resolver);
            allActive = await ActiveWithPendingAddsAsync(context, clearingHouseId, ct);
        }
        else
        {
            CloseSinglePreviousOverlap(allActive, effectiveFrom, policy.ClearingHouseCode);
            var effectiveTo = ResolveNextPolicyBoundary(allActive, effectiveFrom);
            AddPolicyVersion(context, clearingHouseId, policy, effectiveFrom, effectiveTo);
            allActive = await ActiveWithPendingAddsAsync(context, clearingHouseId, ct);
        }

        CloseSinglePreviousOverlap(allActive, effectiveFrom, policy.ClearingHouseCode);
        AlignTargetEndWithNextVersion(allActive, effectiveFrom, policy.ClearingHouseCode);
        ValidateNoVersionOverlaps(allActive, policy.ClearingHouseCode);

        if (context.ChangeTracker.HasChanges())
        {
            await context.SaveChangesAsync(ct);
        }
    }

    private static void ValidatePolicy(
        int clearingHouseId,
        RegulatoryCyclePolicySnapshot policy,
        ICycleNumberResolver resolver)
    {
        if (clearingHouseId <= 0)
        {
            throw new InvalidOperationException("La cámara es requerida para aplicar una política regulatoria de ciclos.");
        }
        if (string.IsNullOrWhiteSpace(policy.ClearingHouseCode))
        {
            throw new InvalidOperationException("La política regulatoria debe identificar la cámara.");
        }
        if (string.IsNullOrWhiteSpace(policy.PolicyVersion))
        {
            throw new InvalidOperationException("La política regulatoria debe tener una versión.");
        }
        if (policy.EffectiveFrom == default)
        {
            throw new InvalidOperationException(
                $"La política '{policy.PolicyVersion}' no puede activarse sin un EffectiveFrom válido.");
        }
        if (policy.Cycles.Count == 0)
        {
            throw new InvalidOperationException($"La política '{policy.PolicyVersion}' no contiene ciclos.");
        }

        var duplicate = policy.Cycles
            .GroupBy(cycle => cycle.CycleNumber)
            .FirstOrDefault(group => group.Count() > 1);
        if (duplicate is not null)
        {
            throw new InvalidOperationException(
                $"La política '{policy.PolicyVersion}' contiene duplicado el número de ciclo {duplicate.Key}.");
        }

        foreach (var cycle in policy.Cycles)
        {
            if (cycle.CycleNumber <= 0 || resolver.Resolve($"Ciclo {cycle.CycleNumber}") != cycle.CycleNumber)
            {
                throw new InvalidOperationException($"La política '{policy.PolicyVersion}' contiene un número de ciclo inválido.");
            }
        }
    }

    private static void RepairKnownDefectiveFingerprint(
        IReadOnlyList<ClearingHouseCycleConfig> target,
        RegulatoryCyclePolicySnapshot policy,
        ICycleNumberResolver resolver)
    {
        IReadOnlyList<IReadOnlyList<RegulatoryCycleSchedule>> defectiveFingerprints = string.Equals(
                policy.ClearingHouseCode,
                RegulatoryCycleScheduleCatalog.CenitCode,
                StringComparison.OrdinalIgnoreCase)
            ? CenitDefectiveFingerprints
            : string.Equals(
                policy.ClearingHouseCode,
                RegulatoryCycleScheduleCatalog.AchColombiaCode,
                StringComparison.OrdinalIgnoreCase)
                ? AchColombiaDefectiveFingerprints
                : Array.Empty<IReadOnlyList<RegulatoryCycleSchedule>>();

        if (!defectiveFingerprints.Any(fingerprint => MatchesCompleteFingerprint(target, fingerprint, resolver)))
        {
            return;
        }

        foreach (var config in target)
        {
            var number = resolver.Resolve(config.CycleName);
            if (!number.HasValue)
            {
                continue;
            }

            var expected = policy.Cycles.SingleOrDefault(item => item.CycleNumber == number.Value);
            if (expected is not null)
            {
                ApplySchedulePayload(config, expected);
            }
        }
    }

    private static void AddMissingCycles(
        AchDbContext context,
        int clearingHouseId,
        RegulatoryCyclePolicySnapshot policy,
        IReadOnlyList<ClearingHouseCycleConfig> target,
        ICycleNumberResolver resolver)
    {
        var existingNumbers = target
            .Select(config => resolver.Resolve(config.CycleName))
            .Where(number => number.HasValue)
            .Select(number => number!.Value)
            .ToHashSet();

        var version = target
            .Select(config => NormalizeVersion(config.PolicyVersion))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Single();
        var effectiveFrom = target
            .Select(config => UtcDate(config.EffectiveFrom))
            .Distinct()
            .Single();
        var effectiveTo = ResolveConsistentEffectiveTo(target, policy.ClearingHouseCode);

        foreach (var expected in policy.Cycles.Where(item => !existingNumbers.Contains(item.CycleNumber)))
        {
            context.ClearingHouseCycleConfigs.Add(CreateConfig(
                clearingHouseId,
                version,
                effectiveFrom,
                effectiveTo,
                expected));
        }
    }

    private static void AddPolicyVersion(
        AchDbContext context,
        int clearingHouseId,
        RegulatoryCyclePolicySnapshot policy,
        DateTime effectiveFrom,
        DateTime? effectiveTo)
    {
        foreach (var expected in policy.Cycles)
        {
            context.ClearingHouseCycleConfigs.Add(CreateConfig(
                clearingHouseId,
                policy.PolicyVersion.Trim(),
                effectiveFrom,
                effectiveTo,
                expected));
        }
    }

    private static ClearingHouseCycleConfig CreateConfig(
        int clearingHouseId,
        string policyVersion,
        DateTime effectiveFrom,
        DateTime? effectiveTo,
        RegulatoryCycleSchedule expected)
    {
        var config = new ClearingHouseCycleConfig
        {
            ClearingHouseId = clearingHouseId,
            PolicyVersion = policyVersion,
            CycleName = $"Ciclo {expected.CycleNumber}",
            EffectiveFrom = effectiveFrom,
            EffectiveTo = effectiveTo,
            IsActive = true
        };
        ApplySchedulePayload(config, expected);
        return config;
    }

    private static void CloseSinglePreviousOverlap(
        IReadOnlyList<ClearingHouseCycleConfig> allActive,
        DateTime targetFrom,
        string clearingHouseCode)
    {
        var overlapping = allActive
            .Where(config => config.EffectiveFrom.Date < targetFrom.Date
                && (!config.EffectiveTo.HasValue || config.EffectiveTo.Value.Date >= targetFrom.Date))
            .GroupBy(config => new PolicyPeriodKey(
                CanonicalVersion(config.PolicyVersion),
                UtcDate(config.EffectiveFrom)))
            .ToList();

        if (overlapping.Count == 0)
        {
            return;
        }
        if (overlapping.Count != 1)
        {
            throw new InvalidOperationException(
                $"No se puede activar la política de '{clearingHouseCode}': existen múltiples versiones previas solapadas.");
        }

        var closeAt = targetFrom.AddDays(-1);
        foreach (var config in overlapping[0])
        {
            config.EffectiveTo = closeAt;
        }
    }

    private static void AlignTargetEndWithNextVersion(
        IReadOnlyList<ClearingHouseCycleConfig> allActive,
        DateTime targetFrom,
        string clearingHouseCode)
    {
        var target = allActive
            .Where(config => config.EffectiveFrom.Date == targetFrom.Date)
            .ToList();
        if (target.Count == 0)
        {
            return;
        }

        var nextFrom = allActive
            .Where(config => config.EffectiveFrom.Date > targetFrom.Date)
            .Select(config => UtcDate(config.EffectiveFrom))
            .Distinct()
            .OrderBy(value => value)
            .FirstOrDefault();
        if (nextFrom == default)
        {
            return;
        }

        var expectedEnd = nextFrom.AddDays(-1);
        var currentEnd = ResolveConsistentEffectiveTo(target, clearingHouseCode);
        if (!currentEnd.HasValue || currentEnd.Value.Date >= nextFrom.Date)
        {
            foreach (var config in target)
            {
                config.EffectiveTo = expectedEnd;
            }
        }
    }

    private static DateTime? ResolveNextPolicyBoundary(
        IReadOnlyList<ClearingHouseCycleConfig> allActive,
        DateTime targetFrom)
    {
        var nextFrom = allActive
            .Where(config => config.EffectiveFrom.Date > targetFrom.Date)
            .Select(config => UtcDate(config.EffectiveFrom))
            .Distinct()
            .OrderBy(value => value)
            .FirstOrDefault();

        return nextFrom == default ? null : nextFrom.AddDays(-1);
    }

    private static DateTime? ResolveConsistentEffectiveTo(
        IReadOnlyList<ClearingHouseCycleConfig> configs,
        string clearingHouseCode)
    {
        var values = configs
            .Select(config => config.EffectiveTo.HasValue ? UtcDate(config.EffectiveTo.Value) : (DateTime?)null)
            .Distinct()
            .ToArray();
        if (values.Length != 1)
        {
            throw new InvalidOperationException(
                $"La política de ciclos de '{clearingHouseCode}' tiene rangos EffectiveTo inconsistentes dentro de la misma versión.");
        }

        return values[0];
    }

    private static void ValidateNoVersionOverlaps(
        IReadOnlyList<ClearingHouseCycleConfig> allActive,
        string clearingHouseCode)
    {
        var periods = allActive
            .GroupBy(config => new PolicyPeriodKey(
                CanonicalVersion(config.PolicyVersion),
                UtcDate(config.EffectiveFrom)))
            .Select(group => new
            {
                group.Key,
                EffectiveTo = ResolveConsistentEffectiveTo(group.ToList(), clearingHouseCode)
            })
            .OrderBy(period => period.Key.EffectiveFrom)
            .ToList();

        for (var index = 0; index < periods.Count - 1; index++)
        {
            var current = periods[index];
            var next = periods[index + 1];
            if (!current.EffectiveTo.HasValue || current.EffectiveTo.Value.Date >= next.Key.EffectiveFrom.Date)
            {
                throw new InvalidOperationException(
                    $"Las versiones de política de ciclos de '{clearingHouseCode}' se solapan entre {current.Key.PolicyVersion} y {next.Key.PolicyVersion}.");
            }
        }
    }

    private static async Task<List<ClearingHouseCycleConfig>> ActiveWithPendingAddsAsync(
        AchDbContext context,
        int clearingHouseId,
        CancellationToken ct)
    {
        var persisted = await context.ClearingHouseCycleConfigs
            .Where(config => config.ClearingHouseId == clearingHouseId && config.IsActive)
            .ToListAsync(ct);
        var pending = context.ChangeTracker
            .Entries<ClearingHouseCycleConfig>()
            .Where(entry => entry.State == EntityState.Added
                && entry.Entity.ClearingHouseId == clearingHouseId
                && entry.Entity.IsActive)
            .Select(entry => entry.Entity)
            .ToList();

        return persisted
            .Concat(pending.Where(candidate => !persisted.Contains(candidate)))
            .ToList();
    }

    private static bool MatchesCompleteFingerprint(
        IReadOnlyList<ClearingHouseCycleConfig> effective,
        IReadOnlyList<RegulatoryCycleSchedule> fingerprint,
        ICycleNumberResolver resolver)
    {
        if (effective.Count != fingerprint.Count)
        {
            return false;
        }

        var byNumber = effective
            .Select(config => new { Config = config, Number = resolver.Resolve(config.CycleName) })
            .Where(item => item.Number.HasValue)
            .GroupBy(item => item.Number!.Value)
            .ToDictionary(group => group.Key, group => group.Select(item => item.Config).ToList());

        return byNumber.Count == fingerprint.Count
            && fingerprint.All(expected => byNumber.TryGetValue(expected.CycleNumber, out var configs)
                && configs.Count == 1
                && HasFingerprintTimes(configs[0], expected));
    }

    // Matching semantics intentionally preserve the pre-versioning repair contract: the
    // known defective fingerprints are identified by their start/end/cutoff triplets.
    private static bool HasFingerprintTimes(ClearingHouseCycleConfig config, RegulatoryCycleSchedule expected)
        => config.StartTime == expected.StartTime
            && config.EndTime == expected.EndTime
            && config.CutoffTime == expected.CutoffTime;

    private static void ApplySchedulePayload(ClearingHouseCycleConfig config, RegulatoryCycleSchedule expected)
    {
        config.StartTime = expected.StartTime;
        config.EndTime = expected.EndTime;
        config.CutoffTime = expected.CutoffTime;
        config.OutputReleaseTime = expected.OutputReleaseTime;
        config.CompensationStatementStartTime = expected.CompensationStatementStartTime;
        config.CompensationStatementEndTime = expected.CompensationStatementEndTime;
        config.CompensationPaymentStartTime = expected.CompensationPaymentStartTime;
        config.CompensationPaymentEndTime = expected.CompensationPaymentEndTime;
        config.OutputReleaseStartTime = expected.OutputReleaseStartTime;
        config.OutputReleaseEndTime = expected.OutputReleaseEndTime;
        config.AllowsMonetaryCredit = expected.AllowsMonetaryCredit;
        config.AllowsMonetaryDebit = expected.AllowsMonetaryDebit;
        config.AllowsCreditPrenotification = expected.AllowsCreditPrenotification;
        config.AllowsDebitPrenotification = expected.AllowsDebitPrenotification;
        config.AllowsReturn = expected.AllowsReturn;
        config.AllowsReturnOfReturn = expected.AllowsReturnOfReturn;
    }

    private static RegulatoryCycleSchedule Legacy(int number, int startHour, int startMinute, int endHour, int endMinute)
    {
        var start = new TimeSpan(startHour, startMinute, 0);
        var end = new TimeSpan(endHour, endMinute, 0);
        return new RegulatoryCycleSchedule(number, start, end, end, end);
    }

    private static IReadOnlyList<T> Freeze<T>(params T[] values)
        => Array.AsReadOnly(values);

    private static IReadOnlyList<IReadOnlyList<RegulatoryCycleSchedule>> FreezeFingerprints(
        params IReadOnlyList<RegulatoryCycleSchedule>[] values)
        => Array.AsReadOnly(values);

    private static string NormalizeVersion(string? value)
        => string.IsNullOrWhiteSpace(value) ? "LEGACY" : value.Trim();

    private static string CanonicalVersion(string? value)
        => NormalizeVersion(value).ToUpperInvariant();

    private static DateTime UtcDate(DateTime value)
        => DateTime.SpecifyKind(value.Date, DateTimeKind.Utc);

    private sealed record PolicyPeriodKey(string PolicyVersion, DateTime EffectiveFrom);
}
