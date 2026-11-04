using Cfa.ACHInterbank.Application.ACH.Interfaces;
using Cfa.ACHInterbank.Application.ACH.Models;
using Cfa.ACHInterbank.Domain.Models.Configurations;
using Cfa.ACHInterbank.Domain.Models.ACH;
using Cfa.ACHInterbank.Persistence.DataBase;
using Microsoft.EntityFrameworkCore;

namespace Cfa.ACHInterbank.Persistence.ACH.Services.Implementation;

[Scoped]
public class CenitCycleExecutionService : ICenitCycleExecutionService
{
    private const string CenitCode = "CENIT";
    private readonly AchDbContext _context;
    private readonly ICycleCalendarGuard _calendarGuard;
    private readonly TimeProvider _timeProvider;

    public CenitCycleExecutionService(
        AchDbContext context,
        ICycleCalendarGuard? calendarGuard = null,
        TimeProvider? timeProvider = null)
    {
        _context = context;
        _timeProvider = timeProvider ?? TimeProvider.System;
        _calendarGuard = calendarGuard ?? new CycleCalendarGuard(context, timeProvider: _timeProvider);
    }

    public async Task<CenitCycleExecution> StartExecutionAsync(AchCycle cycle, CancellationToken ct)
    {
        var clearingHouse = await _context.ClearingHouses
            .AsNoTracking()
            .FirstAsync(x => x.Id == cycle.ClearingHouseId, ct);

        if (!string.Equals(clearingHouse.Code, CenitCode, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("La ejecución dedicada CENIT solo aplica para la cámara CENIT.");
        }

        var calendarDecision = await _calendarGuard.EnsureExecutableAsync(cycle, ct);
        if (!calendarDecision.CanExecute)
        {
            throw new CycleDeferredByCalendarException(cycle.Id, calendarDecision);
        }

        var execution = await _context.CenitCycleExecutions
            .FirstOrDefaultAsync(x => x.AchCycleId == cycle.Id, ct);

        if (execution is null)
        {
            execution = new CenitCycleExecution
            {
                AchCycleId = cycle.Id,
                StartedAtUtc = _timeProvider.GetUtcNow().UtcDateTime,
                Status = "Running",
                Summary = "Execution started"
            };
            _context.CenitCycleExecutions.Add(execution);
            await _context.SaveChangesAsync(ct);
        }
        else if (string.Equals(execution.Status, "AwaitingOfficialResult", StringComparison.OrdinalIgnoreCase)
                 || string.Equals(execution.Status, "Completed", StringComparison.OrdinalIgnoreCase))
        {
            return execution;
        }

        // F01: el fin de la ventana NO es evidencia de liquidación, neteo ni rechazo.
        // No ejecutar simuladores en Quartz ni consumir colas: F03 debe incorporar
        // y confirmar cada operación en su ciclo destino antes de consumirla.
        // Las transiciones definitivas pertenecen a respuestas oficiales del operador.
        execution.Status = "AwaitingOfficialResult";
        execution.CompletedAtUtc = null;
        execution.Summary = "CENIT window ended; awaiting official chamber results. No simulated settlement.";
        await _context.SaveChangesAsync(ct);

        return execution;
    }
}
