using Cfa.ACHInterbank.Application.ACH.Interfaces;
using Cfa.ACHInterbank.Application.ACH.Models;
using Cfa.ACHInterbank.Domain.Models.ACH;
using Cfa.ACHInterbank.Domain.Models.Configurations;
using Cfa.ACHInterbank.Persistence.DataBase;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Cfa.ACHInterbank.Persistence.ACH.Services.Implementation;

[Scoped]
public sealed class CycleCalendarGuard : ICycleCalendarGuard
{
    private readonly AchDbContext _context;
    private readonly IOperationalCalendarService _calendar;
    private readonly TimeProvider _timeProvider;
    private readonly ILogger<CycleCalendarGuard> _logger;

    public CycleCalendarGuard(
        AchDbContext context,
        IOperationalCalendarService? calendar = null,
        TimeProvider? timeProvider = null,
        ILogger<CycleCalendarGuard>? logger = null)
    {
        _context = context;
        _calendar = calendar ?? new OperationalCalendarService(context);
        _timeProvider = timeProvider ?? TimeProvider.System;
        _logger = logger ?? NullLogger<CycleCalendarGuard>.Instance;
    }

    public async Task<CycleCalendarGuardResult> EnsureExecutableAsync(
        AchCycle cycle,
        CancellationToken ct = default)
    {
        var processingDate = DateOnly.FromDateTime(cycle.ProcessingDate);
        if (!cycle.RescheduleOnHoliday)
        {
            return new CycleCalendarGuardResult(true, false, processingDate, null, "La política del ciclo permite operar sin restricción de días hábiles.");
        }

        var explanation = await _calendar.ExplainDayAsync(processingDate, cycle.ClearingHouseId, ct);
        if (explanation.IsBusinessDay)
        {
            return new CycleCalendarGuardResult(true, false, processingDate, null, "Día hábil para la cámara.");
        }

        var nextDate = await _calendar.GetNextBusinessDayAsync(processingDate.AddDays(1), cycle.ClearingHouseId, ct);
        var reason = string.Join("; ", explanation.Reasons.Select(x => x.Description));

        var persistedCycle = _context.ChangeTracker.Entries<AchCycle>()
            .FirstOrDefault(entry => entry.Entity.Id == cycle.Id)?.Entity
            ?? await _context.AchCycles.FirstAsync(x => x.Id == cycle.Id, ct);

        if (DateOnly.FromDateTime(persistedCycle.ProcessingDate) != processingDate)
        {
            return new CycleCalendarGuardResult(
                false,
                false,
                processingDate,
                DateOnly.FromDateTime(persistedCycle.ProcessingDate),
                "El ciclo ya había sido diferido por otra ejecución concurrente.");
        }

        if (!persistedCycle.OriginalProcessingDate.HasValue)
        {
            persistedCycle.OriginalProcessingDate = cycle.ProcessingDate.Date;
        }
        persistedCycle.ProcessingDate = nextDate.ToDateTime(TimeOnly.MinValue);
        persistedCycle.CalendarDeferredAtUtc = _timeProvider.GetUtcNow();
        persistedCycle.CalendarDeferralReason = reason.Length <= 1000 ? reason : reason[..1000];
        persistedCycle.CalendarDeferralCount += 1;
        persistedCycle.OperationalStatus = AchCycleOperationalStatus.Scheduled;

        try
        {
            await _context.SaveChangesAsync(ct);
        }
        catch (DbUpdateConcurrencyException)
        {
            await _context.Entry(persistedCycle).ReloadAsync(ct);
            return new CycleCalendarGuardResult(
                false,
                false,
                processingDate,
                DateOnly.FromDateTime(persistedCycle.ProcessingDate),
                "El ciclo ya había sido diferido por otra ejecución concurrente.");
        }
        cycle.OriginalProcessingDate = persistedCycle.OriginalProcessingDate;
        cycle.ProcessingDate = persistedCycle.ProcessingDate;
        cycle.CalendarDeferredAtUtc = persistedCycle.CalendarDeferredAtUtc;
        cycle.CalendarDeferralReason = persistedCycle.CalendarDeferralReason;
        cycle.CalendarDeferralCount = persistedCycle.CalendarDeferralCount;
        cycle.OperationalStatus = persistedCycle.OperationalStatus;
        _logger.LogInformation(
            "Ciclo {CycleId} de cámara {ClearingHouseId} diferido por calendario desde {OriginalDate} hasta {NextDate}. Reason={Reason}",
            cycle.Id,
            cycle.ClearingHouseId,
            processingDate,
            nextDate,
            reason);

        return new CycleCalendarGuardResult(false, true, processingDate, nextDate, reason);
    }
}
