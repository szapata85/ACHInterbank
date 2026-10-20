using Cfa.ACHInterbank.Application.ACH.Interfaces;
using Cfa.ACHInterbank.Application.ACH.Models;
using Cfa.ACHInterbank.Domain.Entities.Transactions.Enums;
using Cfa.ACHInterbank.Domain.Models.ACH;
using Cfa.ACHInterbank.Domain.Models.ACH.Enums;
using Cfa.ACHInterbank.Domain.Models.Configurations;
using Cfa.ACHInterbank.Persistence.DataBase;
using Microsoft.EntityFrameworkCore;

namespace Cfa.ACHInterbank.Persistence.ACH.Services.Implementation;

[Scoped]
public sealed class ClearingHouseValidationPolicyService : IClearingHouseValidationPolicyService
{
    private readonly AchDbContext _context;
    private readonly IBankHoliday _holidayService;
    private readonly IOperationalCycleWindowResolver _cycleWindowResolver;

    public ClearingHouseValidationPolicyService(
        AchDbContext context,
        IBankHoliday holidayService,
        IOperationalCycleWindowResolver cycleWindowResolver)
    {
        _context = context;
        _holidayService = holidayService;
        _cycleWindowResolver = cycleWindowResolver;
    }

    public async Task<ClearingHouseValidationPolicyResolution> ResolveAsync(
        int clearingHouseId,
        TransactionNature transactionNature,
        DateTime effectiveDate,
        CancellationToken ct = default)
    {
        var transactionType = transactionNature == TransactionNature.Debit
            ? TransactionTypeEnum.Debit
            : TransactionTypeEnum.Credit;

        var candidates = await _context.ClearingHouseTransactionRules
            .AsNoTracking()
            .Where(x => x.ClearingHouseId == clearingHouseId
                        && x.TransactionNature == transactionNature
                        && x.TransactionType == transactionType
                        && x.IsActive
                        && x.AppliesToMonetaryTransactions
                        && x.EffectiveFrom.Date <= effectiveDate.Date
                        && (!x.EffectiveTo.HasValue || x.EffectiveTo.Value.Date >= effectiveDate.Date))
            .OrderByDescending(x => x.EffectiveFrom)
            .Take(2)
            .ToListAsync(ct);

        if (candidates.Count > 1)
        {
            throw new InvalidOperationException(
                $"Existe más de una política vigente para cámara {clearingHouseId}, naturaleza {transactionNature} y fecha {effectiveDate:yyyy-MM-dd}.");
        }

        return new ClearingHouseValidationPolicyResolution(
            candidates.SingleOrDefault(),
            effectiveDate.Date,
            transactionNature,
            clearingHouseId);
    }

    public async Task<DateTimeOffset?> ResolvePrenotificationResponseDeadlineAsync(
        ClearingHouseTransactionRule rule,
        string sourceCycleId,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(rule);
        if (!rule.TacitAcceptanceEnabled
            || rule.PrenotificationResponseDeadlineMode == PrenotificationResponseDeadlineMode.NotApplicable)
        {
            return null;
        }

        var sourceCycle = await _context.AchCycles
            .AsNoTracking()
            .Include(x => x.ClearingHouse)!
                .ThenInclude(x => x.ClearingHouseConfig)
            .FirstOrDefaultAsync(x => x.Id == sourceCycleId && x.ClearingHouseId == rule.ClearingHouseId, ct);

        if (sourceCycle is null)
        {
            return null;
        }

        AchCycle? deadlineCycle;
        switch (rule.PrenotificationResponseDeadlineMode)
        {
            case PrenotificationResponseDeadlineMode.CycleCount:
            {
                var window = rule.PrenotificationResponseWindowCycles.GetValueOrDefault();
                if (window <= 0)
                {
                    return null;
                }

                deadlineCycle = (await _context.AchCycles
                        .AsNoTracking()
                        .Where(x => x.ClearingHouseId == rule.ClearingHouseId
                                    && (x.ProcessingDate.Date > sourceCycle.ProcessingDate.Date
                                        || (x.ProcessingDate.Date == sourceCycle.ProcessingDate.Date
                                            && (x.CutoffTime > sourceCycle.CutoffTime
                                                || (x.CutoffTime == sourceCycle.CutoffTime && x.Id != sourceCycle.Id)))))
                        .OrderBy(x => x.ProcessingDate)
                        .ThenBy(x => x.CutoffTime)
                        .ThenBy(x => x.Id)
                        .Take(window)
                        .ToListAsync(ct))
                    .LastOrDefault();
                break;
            }
            case PrenotificationResponseDeadlineMode.OperationalDayEnd:
                deadlineCycle = await _context.AchCycles
                    .AsNoTracking()
                    .Where(x => x.ClearingHouseId == rule.ClearingHouseId
                                && x.ProcessingDate.Date == sourceCycle.ProcessingDate.Date)
                    .OrderByDescending(x => x.CutoffTime)
                    .ThenByDescending(x => x.Id)
                    .FirstOrDefaultAsync(ct);
                break;
            default:
                return null;
        }

        if (deadlineCycle is null)
        {
            return null;
        }

        var localDeadline = DateTime.SpecifyKind(
            deadlineCycle.ProcessingDate.Date.Add(deadlineCycle.CutoffTime),
            DateTimeKind.Unspecified);
        return _cycleWindowResolver.ConvertLocalToInstant(
            localDeadline,
            ClearingHouseOperationalTimeZone.Resolve(sourceCycle));
    }

    public DateTime AddBusinessDays(DateTime start, int days)
    {
        if (days < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(days));
        }

        var date = start.Date;
        var remaining = days;
        var currentYear = date.Year;
        var holidays = _holidayService.GetHolidays(currentYear).Select(x => x.Date).ToHashSet();

        while (remaining > 0)
        {
            date = date.AddDays(1);
            if (date.Year != currentYear)
            {
                currentYear = date.Year;
                holidays = _holidayService.GetHolidays(currentYear).Select(x => x.Date).ToHashSet();
            }

            if (date.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday
                || holidays.Contains(DateOnly.FromDateTime(date)))
            {
                continue;
            }

            remaining--;
        }

        return date;
    }
}
