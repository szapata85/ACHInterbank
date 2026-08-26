using Cfa.ACHInterbank.Application.ACH.Interfaces;
using Cfa.ACHInterbank.Application.ACH.Models;
using Cfa.ACHInterbank.Application.ACH.Services;
using Cfa.ACHInterbank.Domain.Models.ACH;
using Cfa.ACHInterbank.Persistence.DataBase;
using Microsoft.EntityFrameworkCore;

namespace Cfa.ACHInterbank.Persistence.ACH.Services.Implementation.Seeders;

internal static class RegulatoryCycleSeedRepair
{
    private static readonly IReadOnlyList<IReadOnlyList<RegulatoryCycleSchedule>> CenitDefectiveFingerprints =
    [
        RegulatoryCycleScheduleCatalog.GetRequired(RegulatoryCycleScheduleCatalog.AchColombiaCode),
        [
            Legacy(1, 19, 0, 8, 0),
            Legacy(2, 8, 1, 10, 30),
            Legacy(3, 10, 31, 13, 0),
            Legacy(4, 13, 1, 15, 30),
            Legacy(5, 15, 31, 18, 0)
        ]
    ];

    private static readonly IReadOnlyList<IReadOnlyList<RegulatoryCycleSchedule>> AchColombiaDefectiveFingerprints =
    [
        [
            Legacy(1, 19, 1, 8, 15),
            Legacy(2, 8, 16, 10, 45),
            Legacy(3, 10, 46, 13, 15),
            Legacy(4, 13, 16, 15, 30),
            Legacy(5, 15, 31, 18, 0)
        ]
    ];

    public static async Task ApplyAsync(
        AchDbContext context,
        int clearingHouseId,
        string clearingHouseCode,
        int effectiveYear,
        ICycleNumberResolver? cycleNumberResolver = null,
        CancellationToken ct = default)
    {
        var resolver = cycleNumberResolver ?? new CycleNumberResolver();
        var normative = RegulatoryCycleScheduleCatalog.GetRequired(clearingHouseCode);
        var periodStart = UtcDate(effectiveYear, 1, 1);
        var periodEnd = UtcDate(effectiveYear, 12, 31);
        var effective = await context.ClearingHouseCycleConfigs
            .Where(config => config.ClearingHouseId == clearingHouseId
                && config.IsActive
                && config.EffectiveFrom.Date <= periodEnd.Date
                && (!config.EffectiveTo.HasValue || config.EffectiveTo.Value.Date >= periodStart.Date))
            .ToListAsync(ct);

        var defectiveFingerprints = string.Equals(clearingHouseCode, RegulatoryCycleScheduleCatalog.CenitCode, StringComparison.OrdinalIgnoreCase)
            ? CenitDefectiveFingerprints
            : AchColombiaDefectiveFingerprints;

        if (defectiveFingerprints.Any(fingerprint => MatchesCompleteFingerprint(effective, fingerprint, resolver)))
        {
            foreach (var config in effective)
            {
                var number = resolver.Resolve(config.CycleName);
                var expected = normative.Single(item => item.CycleNumber == number);
                ApplyTimes(config, expected);
            }
        }

        var existingNumbers = effective
            .Select(config => resolver.Resolve(config.CycleName))
            .Where(number => number.HasValue)
            .Select(number => number!.Value)
            .ToHashSet();

        foreach (var expected in normative.Where(item => !existingNumbers.Contains(item.CycleNumber)))
        {
            context.ClearingHouseCycleConfigs.Add(new ClearingHouseCycleConfig
            {
                ClearingHouseId = clearingHouseId,
                CycleName = $"Ciclo {expected.CycleNumber}",
                StartTime = expected.StartTime,
                EndTime = expected.EndTime,
                CutoffTime = expected.CutoffTime,
                OutputReleaseTime = expected.OutputReleaseTime,
                AllowsMonetaryCredit = expected.AllowsMonetaryCredit,
                AllowsMonetaryDebit = expected.AllowsMonetaryDebit,
                AllowsCreditPrenotification = expected.AllowsCreditPrenotification,
                AllowsDebitPrenotification = expected.AllowsDebitPrenotification,
                AllowsReturn = expected.AllowsReturn,
                AllowsReturnOfReturn = expected.AllowsReturnOfReturn,
                PolicyVersion = $"REGULATORY-{effectiveYear}",
                IsActive = true,
                EffectiveFrom = periodStart
            });
        }

        if (context.ChangeTracker.HasChanges())
        {
            await context.SaveChangesAsync(ct);
        }
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
                && HasTimes(configs[0], expected));
    }

    private static bool HasTimes(ClearingHouseCycleConfig config, RegulatoryCycleSchedule expected)
        => config.StartTime == expected.StartTime
            && config.EndTime == expected.EndTime
            && config.CutoffTime == expected.CutoffTime;

    private static void ApplyTimes(ClearingHouseCycleConfig config, RegulatoryCycleSchedule expected)
    {
        config.StartTime = expected.StartTime;
        config.EndTime = expected.EndTime;
        config.CutoffTime = expected.CutoffTime;
        config.OutputReleaseTime = expected.OutputReleaseTime;
        config.AllowsMonetaryCredit = expected.AllowsMonetaryCredit;
        config.AllowsMonetaryDebit = expected.AllowsMonetaryDebit;
        config.AllowsCreditPrenotification = expected.AllowsCreditPrenotification;
        config.AllowsDebitPrenotification = expected.AllowsDebitPrenotification;
        config.AllowsReturn = expected.AllowsReturn;
        config.AllowsReturnOfReturn = expected.AllowsReturnOfReturn;
        config.PolicyVersion = $"REGULATORY-{config.EffectiveFrom.Year}";
    }

    private static RegulatoryCycleSchedule Legacy(int number, int startHour, int startMinute, int endHour, int endMinute)
    {
        var start = new TimeSpan(startHour, startMinute, 0);
        var end = new TimeSpan(endHour, endMinute, 0);
        return new RegulatoryCycleSchedule(number, start, end, end, end);
    }

    private static DateTime UtcDate(int year, int month, int day)
        => DateTime.SpecifyKind(new DateTime(year, month, day), DateTimeKind.Utc);
}

