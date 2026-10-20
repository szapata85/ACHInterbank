using Cfa.ACHInterbank.Application.ACH.Interfaces;
using Cfa.ACHInterbank.Application.ACH.Models;
using Cfa.ACHInterbank.Domain.Models.ACH;
using Cfa.ACHInterbank.Domain.Models.Configurations;
using Cfa.ACHInterbank.Persistence.ACH.Services.StrategyImplementation;
using Cfa.ACHInterbank.Persistence.DataBase;
using Microsoft.EntityFrameworkCore;

namespace Cfa.ACHInterbank.Persistence.ACH.Services.Implementation;

[Scoped]
public sealed class OperationalCalendarService : IOperationalCalendarService
{
    private readonly AchDbContext _context;
    private readonly ColombianHolidayStrategy _generator = new();

    public OperationalCalendarService(AchDbContext context)
    {
        _context = context;
    }

    public async Task<IReadOnlyList<BankHolidayModel>> GetNationalHolidaysAsync(
        int year,
        CancellationToken ct = default)
    {
        var persisted = await _context.BankHolidays
            .AsNoTracking()
            .Where(x => x.CountryCode == "CO" && x.Date.Year == year)
            .ToListAsync(ct);
        var legal = _generator.GenerateHolidays(year);

        return persisted
            .Concat(legal.Where(expected => persisted.All(current => current.Date != expected.Date)))
            .GroupBy(x => x.Date)
            .Select(group => group.OrderByDescending(x => x.IsSystemGenerated).First())
            .OrderBy(x => x.Date)
            .ToArray();
    }

    public async Task<IReadOnlyList<ClearingHouseSpecialDate>> GetSpecialDatesAsync(
        int clearingHouseId,
        DateOnly from,
        DateOnly to,
        CancellationToken ct = default)
    {
        ValidateRange(from, to);
        return await _context.ClearingHouseSpecialDates
            .AsNoTracking()
            .Where(x => x.ClearingHouseId == clearingHouseId
                        && x.IsActive
                        && x.Date >= from
                        && x.Date <= to)
            .OrderBy(x => x.Date)
            .ToListAsync(ct);
    }

    public async Task<bool> IsNationalHolidayAsync(DateOnly date, CancellationToken ct = default)
    {
        if (_generator.GenerateHolidays(date.Year).Any(x => x.Date == date))
        {
            return true;
        }

        return await _context.BankHolidays
            .AsNoTracking()
            .AnyAsync(x => x.CountryCode == "CO" && x.Date == date, ct);
    }

    public async Task<bool> IsBusinessDayAsync(DateOnly date, int clearingHouseId, CancellationToken ct = default)
        => (await ExplainDayAsync(date, clearingHouseId, ct)).IsBusinessDay;

    public async Task<DateOnly> GetNextBusinessDayAsync(DateOnly date, int clearingHouseId, CancellationToken ct = default)
    {
        var candidate = date;
        while (!await IsBusinessDayAsync(candidate, clearingHouseId, ct))
        {
            candidate = candidate.AddDays(1);
        }

        return candidate;
    }

    public async Task<DateOnly> GetPreviousBusinessDayAsync(DateOnly date, int clearingHouseId, CancellationToken ct = default)
    {
        var candidate = date;
        while (!await IsBusinessDayAsync(candidate, clearingHouseId, ct))
        {
            candidate = candidate.AddDays(-1);
        }

        return candidate;
    }

    public async Task<DateOnly> ShiftBusinessDaysAsync(DateOnly date, int amount, int clearingHouseId, CancellationToken ct = default)
    {
        if (amount == 0)
        {
            return await GetNextBusinessDayAsync(date, clearingHouseId, ct);
        }

        var direction = Math.Sign(amount);
        var remaining = Math.Abs(amount);
        var candidate = date;
        while (remaining > 0)
        {
            candidate = candidate.AddDays(direction);
            if (await IsBusinessDayAsync(candidate, clearingHouseId, ct))
            {
                remaining--;
            }
        }

        return candidate;
    }

    public async Task<OperationalDayExplanation> ExplainDayAsync(
        DateOnly date,
        int clearingHouseId,
        CancellationToken ct = default)
    {
        var reasons = new List<OperationalCalendarReason>();
        if (date.DayOfWeek == DayOfWeek.Saturday)
        {
            reasons.Add(new("SATURDAY", "Sábado", date, clearingHouseId));
        }
        else if (date.DayOfWeek == DayOfWeek.Sunday)
        {
            reasons.Add(new("SUNDAY", "Domingo", date, clearingHouseId));
        }

        var holidays = await GetNationalHolidaysAsync(date.Year, ct);
        var holiday = holidays.FirstOrDefault(x => x.Date == date);
        if (holiday is not null)
        {
            reasons.Add(new(
                holiday.RuleCode ?? "NATIONAL_HOLIDAY",
                ExplainHoliday(holiday),
                date,
                clearingHouseId,
                holiday.RuleKind,
                holiday.CommemorativeDate));
        }

        var specialDate = await _context.ClearingHouseSpecialDates
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.ClearingHouseId == clearingHouseId && x.Date == date && x.IsActive, ct);
        if (specialDate is not null)
        {
            reasons.Add(new(
                "CLEARING_HOUSE_SPECIAL_DATE",
                $"Fecha especial no operativa de la cámara: {specialDate.Description}",
                date,
                clearingHouseId));
        }

        return new OperationalDayExplanation(date, clearingHouseId, reasons.Count == 0, reasons);
    }

    private static string ExplainHoliday(BankHolidayModel holiday)
        => holiday.RuleKind switch
        {
            BankHolidayRuleKind.Fixed => $"Festivo nacional fijo: {holiday.Description}",
            BankHolidayRuleKind.Emiliani => $"{holiday.Description}, trasladado al lunes por la Ley Emiliani",
            BankHolidayRuleKind.Easter => $"{holiday.Description}, festivo calculado a partir de la Pascua",
            BankHolidayRuleKind.EasterEmiliani => $"{holiday.Description}, calculado desde la Pascua y trasladado al lunes por la Ley Emiliani",
            BankHolidayRuleKind.ChiquinquiraEmiliani => "Día de Nuestra Señora del Rosario de Chiquinquirá, trasladado al lunes por la Ley Emiliani",
            _ => $"Festivo nacional: {holiday.Description}"
        };

    private static void ValidateRange(DateOnly from, DateOnly to)
    {
        if (from > to)
        {
            throw new ArgumentException("La fecha inicial no puede ser posterior a la fecha final.");
        }
    }
}
