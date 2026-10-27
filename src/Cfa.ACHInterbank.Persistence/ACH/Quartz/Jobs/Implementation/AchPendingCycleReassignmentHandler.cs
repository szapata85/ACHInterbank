using Cfa.ACHInterbank.Application.ACH.Interfaces;
using Cfa.ACHInterbank.Application.ACH.Models;
using Cfa.ACHInterbank.Application.JobsQuartz.Interfaces;
using Cfa.ACHInterbank.Domain.Entities.SchedulerTask;
using Cfa.ACHInterbank.Domain.Entities.SchedulerTask.enums;
using Cfa.ACHInterbank.Domain.Entities.Transactions.Enums;
using Cfa.ACHInterbank.Domain.Models.ACH.Enums;
using Cfa.ACHInterbank.Domain.Models.Configurations;
using Cfa.ACHInterbank.Persistence.ACH.Services.Implementation;
using Cfa.ACHInterbank.Persistence.DataBase;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Cfa.ACHInterbank.Persistence.ACH.Quartz.Jobs.Implementation;

/// <summary>
/// I03: disparador opt-in para I02, sin cambios de fecha valor ni lógica de transferencia.
/// I05 incorporó la admisión NACHA; la concurrencia relacional aún no está homologada. No habilitar en producción.
/// </summary>
[Scoped]
public sealed class AchPendingCycleReassignmentHandler : ITaskHandler
{
    public const string HandlerCode = "AchPendingCycleReassignment";
    public const string ExecutionFlag = "AchCycleReassignment:EnableExecution";

    private readonly AchDbContext _db;
    private readonly IAchPendingCycleReassignmentService _command;
    private readonly IOperationalCycleWindowResolver _window;
    private readonly IConfiguration _configuration;
    private readonly TimeProvider _clock;
    private readonly ILogger<AchPendingCycleReassignmentHandler> _logger;

    public AchPendingCycleReassignmentHandler(
        AchDbContext db,
        IAchPendingCycleReassignmentService command,
        IOperationalCycleWindowResolver window,
        IConfiguration configuration,
        TimeProvider clock,
        ILogger<AchPendingCycleReassignmentHandler> logger)
    {
        _db = db;
        _command = command;
        _window = window;
        _configuration = configuration;
        _clock = clock;
        _logger = logger;
    }

    public string Code => HandlerCode;

    public async Task<string> ExecuteAsync(TaskDefinition task, CancellationToken cancellationToken)
    {
        // Doble barrera: la definición persistida inicia Disabled y el flag ausente es false.
        // Aplica también ante una invocación manual directa del handler.
        if (task.Status != TaskStatusEnum.Enabled || task.Paused
            || !bool.TryParse(_configuration[ExecutionFlag], out var allowed) || !allowed)
            return "ACH-CR I03 deshabilitado: no se evaluaron ni modificaron transacciones.";

        var batchSize = ReadBounded(task, "BatchSize", 25, 1, 100);
        var maxScanned = ReadBounded(task, "MaxScanned", 250, batchSize, 1000);
        var now = _clock.GetUtcNow();
        // Margen UTC ±1 día: la determinación efectiva la hace la zona horaria de cada cámara.
        var dateFloor = now.UtcDateTime.Date.AddDays(-1);
        var dateCeiling = now.UtcDateTime.Date.AddDays(2);
        var cycles = await _db.AchCycles.AsNoTracking()
            .Include(c => c.ClearingHouse)
                .ThenInclude(ch => ch!.ClearingHouseConfig)
            .Where(c => c.ProcessingDate >= dateFloor && c.ProcessingDate < dateCeiling)
            .ToListAsync(cancellationToken);

        var expiredIds = new List<string>();
        foreach (var cycle in cycles)
        {
            try
            {
                if (_window.Resolve(cycle.ProcessingDate, cycle.StartTime, cycle.CutoffTime,
                    ClearingHouseOperationalTimeZone.Resolve(cycle), now).Status == OperationalCycleWindowStatus.After)
                    expiredIds.Add(cycle.Id);
            }
            catch (Exception ex) when (ex is InvalidOperationException or TimeZoneNotFoundException or InvalidTimeZoneException)
            {
                _logger.LogWarning(ex, "ACH-CR I03 omitió ciclo {CycleId}: ventana operativa no resuelta.", cycle.Id);
            }
        }

        if (expiredIds.Count == 0)
            return "ACH-CR I03: no existen ciclos vencidos en la jornada; sin cambios.";

        // Lecturas keyset por páginas acotadas: no materializar todo el backlog.
        // I01 e I02 toman la decisión real dentro de la unidad de trabajo.
        var moved = 0;
        var skipped = 0;
        var failed = 0;
        var scanned = 0;
        var lastTransactionId = 0;
        while (scanned < maxScanned && moved < batchSize)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var pageSize = Math.Min(50, maxScanned - scanned);
            var ids = await _db.AchTransactions.AsNoTracking()
                .Where(t => t.Id > lastTransactionId && expiredIds.Contains(t.AchCycleId)
                    && t.State == AchTransferStateEnum.Pending
                    && t.Direction == AchTransactionDirection.Outgoing
                    && t.Origin == AchTransactionOrigin.Cfa
                    && t.ClassificationStatus == AchTransactionClassificationStatus.Determined
                    && t.Type != TransactionTypeEnum.Return && t.Type != TransactionTypeEnum.Reversal
                    && !_db.AchFileExportTransactions.Any(e => e.AchTransactionId == t.Id)
                    && !_db.ContrapartidaDispatchItems.Any(e => e.AchTransactionId == t.Id))
                .OrderBy(t => t.Id)
                .Select(t => t.Id)
                .Take(pageSize)
                .ToListAsync(cancellationToken);
            if (ids.Count == 0)
                break;

            foreach (var id in ids)
            {
                cancellationToken.ThrowIfCancellationRequested();
                if (moved >= batchSize)
                    break;
                lastTransactionId = id;
                scanned++;
                try
                {
                    var result = await _command.ReassignAsync(id, cancellationToken);
                    if (result.Moved)
                        moved++;
                    else
                    {
                        skipped++;
                        _logger.LogDebug("ACH-CR I03 omitió transacción {TransactionId}: {ReasonCode}.", id, result.ReasonCode);
                    }
                }
                catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
                {
                    throw;
                }
                catch (Exception ex)
                {
                    failed++;
                    _logger.LogError(ex, "ACH-CR I03 falló reasignación de transacción {TransactionId}.", id);
                }
            }
        }

        var summary = $"ACH-CR I03: evaluadas={scanned}, trasladadas={moved}, omitidas={skipped}, fallidas={failed}, limite={batchSize}, maxEscaneadas={maxScanned}.";
        _logger.LogInformation("{Summary}", summary);
        if (failed > 0)
            throw new InvalidOperationException($"{summary} Revisar logs; las ya trasladadas no deben repetirse por idempotencia I02.");
        return summary;
    }

    private static int ReadBounded(TaskDefinition task, string key, int fallback, int min, int max)
    {
        var raw = task.Parameters.FirstOrDefault(p => p.Key == key)?.Value;
        return int.TryParse(raw, out var value) && value >= min && value <= max
            ? value
            : fallback;
    }
}
