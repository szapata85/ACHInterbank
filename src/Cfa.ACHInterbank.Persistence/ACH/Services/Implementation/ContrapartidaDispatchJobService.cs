using Cfa.ACHInterbank.Application.ACH.Interfaces;
using Cfa.ACHInterbank.Application.ACH.Models;
using Cfa.ACHInterbank.Application.External.Connections;
using Cfa.ACHInterbank.Domain.Models.ACH;
using Cfa.ACHInterbank.Domain.Models.Configurations;
using Cfa.ACHInterbank.Persistence.DataBase;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Cfa.ACHInterbank.Persistence.ACH.Services.Implementation;

[Scoped]
public sealed class ContrapartidaDispatchJobService : IContrapartidaDispatchJobService
{
    private static readonly ContrapartidaDispatchItemStateEnum[] EligibleStates =
    [
        ContrapartidaDispatchItemStateEnum.PendingContrapartidaReport,
        ContrapartidaDispatchItemStateEnum.QueuedForContrapartida,
        ContrapartidaDispatchItemStateEnum.RetryPending
    ];

    private readonly AchDbContext _context;
    private readonly IWscfaachSoapClient _soapClient;
    private readonly IProcContrapartidasRequestMapper _procContrapartidasRequestMapper;
    private readonly IProcContrapartidasResponseParser _responseParser;
    private readonly ILogger<ContrapartidaDispatchJobService> _logger;

    public ContrapartidaDispatchJobService(
        AchDbContext context,
        IWscfaachSoapClient soapClient,
        IProcContrapartidasRequestMapper procContrapartidasRequestMapper,
        IProcContrapartidasResponseParser responseParser,
        ILogger<ContrapartidaDispatchJobService> logger)
    {
        _context = context;
        _soapClient = soapClient;
        _procContrapartidasRequestMapper = procContrapartidasRequestMapper;
        _responseParser = responseParser;
        _logger = logger;
    }

    public async Task<ContrapartidaCycleDispatchResult> ProcessCycleAsync(
        string cycleId,
        int clearingHouseId,
        string triggeredBy,
        int chunkSize,
        CancellationToken ct = default)
    {
        chunkSize = Math.Clamp(chunkSize, 10, 2000);
        var safeTriggeredBy = string.IsNullOrWhiteSpace(triggeredBy) ? "quartz:contrapartida" : triggeredBy.Trim();

        var cycle = await _context.AchCycles
            .AsNoTracking()
            .Include(c => c.ClearingHouse)
            .Include(c => c.ClearingHouseCycleConfig)
            .FirstOrDefaultAsync(c => c.Id == cycleId && c.ClearingHouseId == clearingHouseId, ct)
            ?? throw new InvalidOperationException($"No existe ciclo {cycleId} para cámara {clearingHouseId}.");

        ValidateCycleOperationalWindow(cycle, DateTime.Now);

        var processed = 0;
        var succeeded = 0;
        var failed = 0;
        var partial = 0;
        var chunks = 0;

        while (!ct.IsCancellationRequested)
        {
            var pendingItemIds = await _context.ContrapartidaDispatchItems
                .AsNoTracking()
                .Where(i => i.AchCycleId == cycleId && i.ClearingHouseId == clearingHouseId)
                .Where(i => EligibleStates.Contains(i.State))
                .Where(i => !i.NextAttemptAtUtc.HasValue || i.NextAttemptAtUtc <= DateTime.UtcNow)
                .OrderBy(i => i.Id)
                .Select(i => i.Id)
                .Take(chunkSize)
                .ToListAsync(ct);

            if (pendingItemIds.Count == 0)
            {
                break;
            }

            chunks++;
            var chunkPartialCount = 0;

            var batch = new ContrapartidaDispatchBatch
            {
                AchCycleId = cycle.Id,
                ClearingHouseId = cycle.ClearingHouseId,
                TriggerType = ContrapartidaDispatchBatchTriggerTypeEnum.Scheduled,
                RequestedBy = safeTriggeredBy,
                TriggeredAtUtc = DateTime.UtcNow,
                StartedAtUtc = DateTime.UtcNow,
                Status = ContrapartidaDispatchBatchStatusEnum.Processing,
                TotalItems = pendingItemIds.Count
            };
            await _context.ContrapartidaDispatchBatches.AddAsync(batch, ct);

            await _context.ContrapartidaDispatchItems
                .Where(i => pendingItemIds.Contains(i.Id))
                .ExecuteUpdateAsync(setters => setters
                    .SetProperty(i => i.State, ContrapartidaDispatchItemStateEnum.ReportingContrapartida)
                    .SetProperty(i => i.LastDispatchedBy, safeTriggeredBy)
                    .SetProperty(i => i.UpdatedAt, DateTimeOffset.UtcNow), ct);

            var dispatchItems = await _context.ContrapartidaDispatchItems
                .Where(i => pendingItemIds.Contains(i.Id))
                .ToListAsync(ct);

            var transactionIds = dispatchItems.Select(i => i.AchTransactionId).ToArray();
            var transactions = await _context.AchTransactions
                .AsNoTracking()
                .Include(t => t.Addendas)
                .Where(t => transactionIds.Contains(t.Id))
                .OrderBy(t => t.Id)
                .ToListAsync(ct);

            string requestPayload = string.Empty;
            string responsePayload = string.Empty;
            ProcContrapartidasParsedResponse? parseResult = null;
            var startedAtUtc = DateTime.UtcNow;

            try
            {
                var contract = _procContrapartidasRequestMapper.Map(cycle, transactions, DateTime.Now);
                requestPayload = _procContrapartidasRequestMapper.BuildSoapBody(contract);

                responsePayload = await _soapClient.ProcContrapartidasAsync(requestPayload, ct);
                parseResult = _responseParser.Parse(responsePayload);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Error SOAP en Proc_Contrapartidas para ciclo {CycleId} cámara {ClearingHouseId}",
                    cycle.Id,
                    cycle.ClearingHouseId);

                parseResult = new ProcContrapartidasParsedResponse(
                    IsSuccess: false,
                    IsSoapFault: false,
                    IsRetryable: true,
                    IsFunctionalRejection: false,
                    ErrorCode: "SOAP_EXCEPTION",
                    ErrorMessage: ex.Message,
                    RawResponse: ex.ToString(),
                    ResponseCode: "SOAP_EXCEPTION",
                    ItemResults: new Dictionary<int, ProcContrapartidasParsedItemResponse>());
                responsePayload = ex.ToString();
            }

            var finishedAtUtc = DateTime.UtcNow;
            batch.RequestPayloadXml = requestPayload;
            batch.ResponsePayloadXml = responsePayload;

            foreach (var item in dispatchItems)
            {
                var txId = item.AchTransactionId;
                var hasItemResult = parseResult.ItemResults.TryGetValue(txId, out var txResult);

                var isSuccess = hasItemResult ? txResult!.IsSuccess : parseResult.IsSuccess;
                var itemCode = hasItemResult ? txResult!.ResponseCode : parseResult.ErrorCode;
                var itemMessage = hasItemResult ? txResult!.Message : parseResult.ErrorMessage;

                var attempt = new ContrapartidaDispatchAttempt
                {
                    DispatchItemId = item.Id,
                    DispatchBatchId = batch.Id,
                    AttemptNumber = item.AttemptCount + 1,
                    StartedAtUtc = startedAtUtc,
                    FinishedAtUtc = finishedAtUtc,
                    Result = isSuccess
                        ? ContrapartidaDispatchAttemptResultEnum.Success
                        : (hasItemResult && txResult is not null && !txResult.IsSuccess && parseResult.ItemResults.Count > 0 && parseResult.ItemResults.Values.Any(x => x.IsSuccess))
                            ? ContrapartidaDispatchAttemptResultEnum.Partial
                            : ContrapartidaDispatchAttemptResultEnum.Failed,
                    CorrelationId = $"{batch.Id:N}-{item.Id}",
                    TriggeredBy = safeTriggeredBy,
                    RetryEligible = !isSuccess && (hasItemResult ? txResult!.IsRetryable : parseResult.IsRetryable),
                    ExternalResponseCode = itemCode,
                    ExternalResponseMessage = itemMessage ?? string.Empty,
                    ErrorCode = isSuccess ? string.Empty : itemCode,
                    ErrorMessage = isSuccess ? string.Empty : (itemMessage ?? "Error de negocio en contrapartidas"),
                    RequestPayloadXml = requestPayload,
                    ResponsePayloadXml = responsePayload
                };

                _context.ContrapartidaDispatchAttempts.Add(attempt);

                item.AttemptCount += 1;
                item.LastAttemptAtUtc = finishedAtUtc;
                item.LastCorrelationId = attempt.CorrelationId;
                item.LastDispatchedBy = safeTriggeredBy;
                item.LastResponseCode = itemCode;
                item.LastErrorCode = isSuccess ? string.Empty : itemCode;
                item.LastErrorMessage = isSuccess ? string.Empty : (itemMessage ?? string.Empty);

                if (isSuccess)
                {
                    item.State = ContrapartidaDispatchItemStateEnum.ReportedToContrapartida;
                    item.LastSuccessAtUtc = finishedAtUtc;
                    item.NextAttemptAtUtc = null;
                    succeeded++;
                }
                else
                {
                    var retryable = hasItemResult ? txResult!.IsRetryable : parseResult.IsRetryable;
                    item.State = retryable
                        ? ContrapartidaDispatchItemStateEnum.RetryPending
                        : ContrapartidaDispatchItemStateEnum.ContrapartidaReportFailed;
                    item.NextAttemptAtUtc = retryable ? DateTime.UtcNow.AddMinutes(5) : null;
                    failed++;
                    if (hasItemResult && parseResult.ItemResults.Count > 0 && parseResult.ItemResults.Values.Any(x => x.IsSuccess))
                    {
                        partial++;
                        chunkPartialCount++;
                    }
                }

                processed++;
            }

            batch.TotalSucceeded = dispatchItems.Count(x => x.State == ContrapartidaDispatchItemStateEnum.ReportedToContrapartida);
            batch.TotalFailed = dispatchItems.Count(x => x.State != ContrapartidaDispatchItemStateEnum.ReportedToContrapartida);
            batch.TotalPartial = chunkPartialCount;
            batch.FinishedAtUtc = finishedAtUtc;
            batch.Status = batch.TotalFailed == 0
                ? ContrapartidaDispatchBatchStatusEnum.Completed
                : batch.TotalSucceeded > 0
                    ? ContrapartidaDispatchBatchStatusEnum.CompletedWithErrors
                    : ContrapartidaDispatchBatchStatusEnum.Failed;
            batch.SummaryMessage = $"Chunk={chunks} Tx={dispatchItems.Count} Success={batch.TotalSucceeded} Failed={batch.TotalFailed} Code={parseResult.ResponseCode}";

            await _context.SaveChangesAsync(ct);
        }

        var summary = $"Ciclo {cycleId} cámara {clearingHouseId}: Processed={processed}, Success={succeeded}, Failed={failed}, Partial={partial}, Chunks={chunks}";
        return new ContrapartidaCycleDispatchResult(cycleId, clearingHouseId, processed, succeeded, failed, partial, chunks, summary);
    }

    private static void ValidateCycleOperationalWindow(AchCycle cycle, DateTime nowLocal)
    {
        if (cycle.ClearingHouseCycleConfig is not null)
        {
            if (!cycle.ClearingHouseCycleConfig.IsActive)
            {
                throw new InvalidOperationException($"Configuración de ciclo inactiva para {cycle.Id}.");
            }

            var processingDate = cycle.ProcessingDate.Date;
            var effectiveFrom = cycle.ClearingHouseCycleConfig.EffectiveFrom.Date;
            var effectiveTo = cycle.ClearingHouseCycleConfig.EffectiveTo?.Date;
            if (processingDate < effectiveFrom || (effectiveTo.HasValue && processingDate > effectiveTo.Value))
            {
                throw new InvalidOperationException($"Configuración de ciclo fuera de vigencia para {cycle.Id}.");
            }
        }

        var (windowStart, windowEnd) = BuildCycleWindow(cycle.ProcessingDate, cycle.StartTime, cycle.EndTime);
        if (nowLocal < windowStart || nowLocal > windowEnd)
        {
            throw new InvalidOperationException($"Ciclo {cycle.Id} fuera de ventana operativa: {windowStart:yyyy-MM-dd HH:mm} - {windowEnd:yyyy-MM-dd HH:mm}.");
        }
    }

    private static (DateTime Start, DateTime End) BuildCycleWindow(DateTime processingDate, TimeSpan startTime, TimeSpan endTime)
    {
        if (startTime <= endTime)
        {
            return (processingDate.Date + startTime, processingDate.Date + endTime);
        }

        return (processingDate.Date.AddDays(-1) + startTime, processingDate.Date + endTime);
    }
}
