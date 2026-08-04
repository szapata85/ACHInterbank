using Cfa.ACHInterbank.Application.ACH.Interfaces;
using Cfa.ACHInterbank.Application.ACH.Models;
using Cfa.ACHInterbank.Domain.Models.ACH;
using Cfa.ACHInterbank.Domain.Models.Configurations;
using Cfa.ACHInterbank.Persistence.ACH.Services.StrategyImplementation;
using Cfa.ACHInterbank.Persistence.DataBase;
using Microsoft.EntityFrameworkCore;

namespace Cfa.ACHInterbank.Persistence.ACH.Services.Implementation;

[Scoped]
public sealed class BankHolidayProvisioningService : IBankHolidayProvisioningService
{
    private static readonly SemaphoreSlim ProvisioningGate = new(1, 1);
    private static readonly IReadOnlyDictionary<string, string> LegacyDescriptions =
        new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["CO_SAINT_JOSEPH"] = "San José",
            ["CO_ASSUMPTION"] = "La Asunción",
            ["CO_ETHNIC_CULTURAL_DIVERSITY"] = "Día de la Raza",
            ["CO_ALL_SAINTS"] = "Todos los Santos",
            ["CO_SACRED_HEART"] = "Sagrado Corazón"
        };
    private readonly AchDbContext _context;
    private readonly ColombianHolidayStrategy _generator = new();

    public BankHolidayProvisioningService(AchDbContext context)
    {
        _context = context;
    }

    public async Task<BankHolidayProvisioningResult> EnsureYearsAsync(
        IEnumerable<int> years,
        CancellationToken ct = default)
    {
        var requestedYears = years.Distinct().OrderBy(x => x).ToArray();
        if (requestedYears.Length == 0)
        {
            return new BankHolidayProvisioningResult([]);
        }

        if (requestedYears.Any(x => x is < 1900 or > 9999))
        {
            throw new ArgumentOutOfRangeException(nameof(years), "Los años deben estar entre 1900 y 9999.");
        }

        await ProvisioningGate.WaitAsync(ct);
        try
        {
            var results = new List<BankHolidayProvisioningYearResult>(requestedYears.Length);
            foreach (var year in requestedYears)
            {
                results.Add(await EnsureYearAsync(year, ct));
            }

            await _context.SaveChangesAsync(ct);
            return new BankHolidayProvisioningResult(results);
        }
        finally
        {
            ProvisioningGate.Release();
        }
    }

    private async Task<BankHolidayProvisioningYearResult> EnsureYearAsync(int year, CancellationToken ct)
    {
        var expected = _generator.GenerateHolidays(year);
        var existing = await _context.BankHolidays
            .Where(x => x.CountryCode == "CO"
                        && (x.Date.Year == year
                            || (x.CommemorativeDate.HasValue && x.CommemorativeDate.Value.Year == year)))
            .ToListAsync(ct);

        var inserted = 0;
        var updated = 0;
        var unchanged = 0;
        var skippedManual = 0;

        foreach (var legalHoliday in expected)
        {
            var generated = existing.FirstOrDefault(x =>
                x.IsSystemGenerated
                && x.RuleCode == legalHoliday.RuleCode
                && x.CommemorativeDate == legalHoliday.CommemorativeDate);

            if (generated is not null)
            {
                if (HasChanges(generated, legalHoliday))
                {
                    var manualCollision = existing.Any(x =>
                        !x.IsSystemGenerated
                        && x.Date == legalHoliday.Date
                        && x.CountryCode == legalHoliday.CountryCode);
                    if (manualCollision)
                    {
                        skippedManual++;
                        continue;
                    }

                    CopyLegalFields(generated, legalHoliday);
                    updated++;
                }
                else
                {
                    unchanged++;
                }

                continue;
            }

            var legacyCandidates = existing.Where(x =>
                !x.IsSystemGenerated
                && string.IsNullOrWhiteSpace(x.RuleCode)
                && !x.CommemorativeDate.HasValue
                && x.CountryCode == legalHoliday.CountryCode
                && x.Date == legalHoliday.Date
                && IsKnownLegacyDescription(x.Description, legalHoliday))
                .ToList();
            if (legacyCandidates.Count == 1)
            {
                CopyLegalFields(legacyCandidates[0], legalHoliday);
                updated++;
                continue;
            }

            var manualOnEffectiveDate = existing.Any(x =>
                !x.IsSystemGenerated
                && x.Date == legalHoliday.Date
                && x.CountryCode == legalHoliday.CountryCode);
            if (manualOnEffectiveDate)
            {
                skippedManual++;
                continue;
            }

            _context.BankHolidays.Add(legalHoliday);
            existing.Add(legalHoliday);
            inserted++;
        }

        return new BankHolidayProvisioningYearResult(
            year,
            expected.Count,
            inserted,
            updated,
            unchanged,
            skippedManual);
    }

    private static bool IsKnownLegacyDescription(string currentDescription, BankHolidayModel expected)
        => string.Equals(currentDescription, expected.Description, StringComparison.Ordinal)
           || (expected.RuleCode is not null
               && LegacyDescriptions.TryGetValue(expected.RuleCode, out var legacyDescription)
               && string.Equals(currentDescription, legacyDescription, StringComparison.Ordinal));

    private static bool HasChanges(BankHolidayModel current, BankHolidayModel expected)
        => current.Date != expected.Date
           || current.Description != expected.Description
           || current.RuleKind != expected.RuleKind
           || current.LegalOrigin != expected.LegalOrigin
           || current.EffectiveFromYear != expected.EffectiveFromYear
           || current.CountryCode != expected.CountryCode;

    private static void CopyLegalFields(BankHolidayModel target, BankHolidayModel source)
    {
        target.Date = source.Date;
        target.CommemorativeDate = source.CommemorativeDate;
        target.Description = source.Description;
        target.CountryCode = source.CountryCode;
        target.RuleCode = source.RuleCode;
        target.RuleKind = source.RuleKind;
        target.IsSystemGenerated = true;
        target.LegalOrigin = source.LegalOrigin;
        target.EffectiveFromYear = source.EffectiveFromYear;
    }
}
