using Cfa.ACHInterbank.Application.ACH.Interfaces;
using Cfa.ACHInterbank.Application.ACH.Interfaces.ExternalFileNames;
using Cfa.ACHInterbank.Application.ACH.Interfaces.PaymentRails;
using Cfa.ACHInterbank.Application.ACH.Models;
using Cfa.ACHInterbank.Application.ACH.Models.ExternalFileNames;
using Cfa.ACHInterbank.Application.ACH.Models.PaymentRails;
using DigitoChequeoHelper = Cfa.ACHInterbank.Application.Helpers.DigitoChequeo.DigitoChequeo;
using Cfa.ACHInterbank.Domain.Models.ACH;
using Cfa.ACHInterbank.Domain.Models.Configurations;
using Cfa.ACHInterbank.Domain.Entities.Transactions.Enums;
using Cfa.ACHInterbank.Persistence.ACH.Services.Implementation.ExternalFileNames;
using Cfa.ACHInterbank.Persistence.DataBase;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using System.Text;

namespace Cfa.ACHInterbank.Persistence.ACH.Services.Implementation;

[Scoped]
public class AchReturnsService(
    AchDbContext context,
    TimeProvider? timeProvider = null,
    IAchRegulatoryCatalogService? regulatoryCatalogService = null,
    IAchReturnEligibilityService? returnEligibilityService = null,
    IAchReturnGenerationLockService? returnGenerationLockService = null,
    IPaymentRailContextService? paymentRailContextService = null,
    IPaymentRailOperationalStrategyResolver? strategyResolver = null,
    IPaymentRailShadowCompareService? shadowCompareService = null,
    IExternalFileNamePolicy? externalFileNamePolicy = null,
    IAchCauseCodePolicy? causeCodePolicy = null,
    ILogger<AchReturnsService>? logger = null,
    IAchStateTransitionService? stateTransitionService = null,
    INachaFileBuilder? nachaFileBuilder = null,
    IAchReturnTraceSequenceService? returnTraceSequenceService = null,
    ICenitIncomingReturnPolicy? cenitReturnPolicy = null) : IAchReturnsService
{
    private readonly IAchRegulatoryCatalogService _regulatoryCatalogService = regulatoryCatalogService
                                                                           ?? throw new InvalidOperationException("IAchRegulatoryCatalogService es requerido para gobernanza regulatoria de devoluciones.");
    private readonly IAchReturnEligibilityService _returnEligibilityService = returnEligibilityService
                                                                        ?? throw new InvalidOperationException("IAchReturnEligibilityService es requerido para evaluar elegibilidad de devoluciones.");
    private readonly IAchReturnGenerationLockService _returnGenerationLockService = returnGenerationLockService
        ?? throw new InvalidOperationException("IAchReturnGenerationLockService es requerido para control de concurrencia en devoluciones.");
    private readonly TimeProvider _timeProvider = timeProvider ?? TimeProvider.System;
    private readonly IPaymentRailContextService? _paymentRailContextService = paymentRailContextService;
    private readonly IPaymentRailOperationalStrategyResolver? _strategyResolver = strategyResolver;
    private readonly IPaymentRailShadowCompareService? _shadowCompareService = shadowCompareService;
    private readonly IExternalFileNamePolicy? _externalFileNamePolicy = externalFileNamePolicy;
    private readonly IAchCauseCodePolicy? _causeCodePolicy = causeCodePolicy;
    private readonly ILogger<AchReturnsService> _logger = logger ?? NullLogger<AchReturnsService>.Instance;
    // The canonical service is deliberately used even by legacy composition roots that
    // have not yet supplied it through DI.  This keeps a single audited transition path.
    private readonly IAchStateTransitionService _stateTransitionService = stateTransitionService ?? new AchStateTransitionService(context);
    private readonly INachaFileBuilder? _nachaFileBuilder = nachaFileBuilder;
    private readonly IAchReturnTraceSequenceService _returnTraceSequenceService = returnTraceSequenceService
        ?? new AchReturnTraceSequenceService(context);
    private readonly ICenitIncomingReturnPolicy? _cenitReturnPolicy = cenitReturnPolicy;
    private const string ImmediateDestinationAchColombia = "000101006";
    public async Task<IReadOnlyList<ReturnEligibleTransactionDto>> GetTransactionsByCycleAsync(string cycleId, CancellationToken ct = default)
    {
        var cycle = await context.AchCycles.AsNoTracking().FirstOrDefaultAsync(c => c.Id == cycleId, ct)
            ?? throw new InvalidOperationException("No se encontró el ciclo seleccionado.");

        var transactions = await context.AchTransactions
            .AsNoTracking()
            .Where(t => t.AchCycleId == cycleId)
            .OrderBy(t => t.Id)
            .ToListAsync(ct);

        var cycleOrder = await GetCycleOrderAsync(cycle.ClearingHouseId, ct);
        cycleOrder.TryGetValue(cycle.Id, out var selectedCycleOrder);

        var alreadyReturned = await context.Set<AchReturnGenerated>()
            .AsNoTracking()
            .Select(r => r.OriginalTransactionId)
            .ToHashSetAsync(ct);

        return transactions.Select(tx =>
        {
            var isEligible = true;
            string? message = null;

            if (alreadyReturned.Contains(tx.Id))
            {
                isEligible = false;
                message = "La transacción ya tiene devolución generada.";
            }

            if (!cycleOrder.TryGetValue(tx.AchCycleId, out var txCycleOrder))
            {
                isEligible = false;
                message = "No fue posible validar la antigüedad por ciclo.";
            }
            else if (IsAchColombia(cycle.ClearingHouse?.Code) && (selectedCycleOrder - txCycleOrder) > 4)
            {
                isEligible = false;
                message = "La transacción supera el máximo de 4 ciclos para devolución.";
            }

            return new ReturnEligibleTransactionDto(
                tx.Id,
                tx.TraceNumber,
                tx.Amount,
                tx.TransactionCode,
                tx.Reference,
                tx.SourceAccountNumber,
                tx.DestinationAccountNumber,
                tx.OriginatingDFI,
                tx.ReceivingDFI,
                tx.AchCycleId,
                tx.EffectiveEntryDate,
                tx.IsPrenotification,
                isEligible,
                message);
        }).ToList();
    }

    public async Task<GenerateReturnsFileResponse> GenerateReturnsFileAsync(GenerateReturnsFileRequest request, CancellationToken ct = default)
    {
        if (request.Items is null || request.Items.Count == 0)
        {
            throw new InvalidOperationException("Debe seleccionar al menos una transacción para devolver.");
        }

        var duplicateSelections = request.Items
            .GroupBy(item => item.TransactionId)
            .FirstOrDefault(group => group.Count() > 1);
        if (duplicateSelections is not null)
        {
            throw new InvalidOperationException($"La transacción {duplicateSelections.Key} está repetida en la solicitud de devolución.");
        }

        var originalCycle = await context.AchCycles
            .Include(c => c.ClearingHouse)
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == request.CycleId, ct)
            ?? throw new InvalidOperationException("No se encontró el ciclo de operación.");

        var returnCycleId = string.IsNullOrWhiteSpace(request.ReturnCycleId) ? request.CycleId : request.ReturnCycleId.Trim();
        var cycle = string.Equals(returnCycleId, originalCycle.Id, StringComparison.OrdinalIgnoreCase)
            ? originalCycle
            : await context.AchCycles
                .Include(c => c.ClearingHouse)
                .AsNoTracking()
                .FirstOrDefaultAsync(c => c.Id == returnCycleId, ct)
                ?? throw new InvalidOperationException("No se encontró el ciclo de devolución.");

        if (cycle.ClearingHouseId != originalCycle.ClearingHouseId)
        {
            throw new InvalidOperationException("No se permite generar una devolución en una cámara distinta de la operación original.");
        }

        var paymentRailCode = cycle.ClearingHouseId is int clearingHouseId
            ? await context.ClearingHouseConfigs
                .AsNoTracking()
                .Where(config => config.ClearingHouseId == clearingHouseId)
                .Select(config => config.PaymentRailCode)
                .FirstOrDefaultAsync(ct)
            : null;

        var isAchColombia = IsAchColombia(cycle.ClearingHouse?.Code);
        var isCenit = IsCenit(cycle.ClearingHouse?.Code);
        if (!isAchColombia && !isCenit)
        {
            throw new InvalidOperationException("RETURN_OUT_CLEARING_HOUSE_NOT_SUPPORTED: la cámara no dispone de un perfil Return Out implementado.");
        }

        if (isCenit && _cenitReturnPolicy is null)
        {
            throw new InvalidOperationException("CENIT_RETURN_POLICY_REQUIRED: la policy normativa CENIT no está registrada.");
        }

        var selectedIds = request.Items.Select(i => i.TransactionId).Distinct().ToList();
        await using var generationLock = await _returnGenerationLockService.AcquireAsync(selectedIds, ct);

        var transactions = await context.AchTransactions
            .Include(t => t.AchCycle)
            .Where(t => selectedIds.Contains(t.Id))
            .ToListAsync(ct);

        if (transactions.Count != selectedIds.Count)
        {
            throw new InvalidOperationException("Algunas transacciones seleccionadas no existen.");
        }

        if (transactions.Any(t => t.AchCycleId != request.CycleId))
        {
            throw new InvalidOperationException("No se permite mezclar transacciones de ciclos distintos en el mismo archivo de devolución.");
        }

        var cycleOrder = await GetCycleOrderAsync(cycle.ClearingHouseId, ct);
        cycleOrder.TryGetValue(cycle.Id, out var selectedCycleOrder);
        var cenitCycleEvidence = isCenit
            ? await ResolveCenitCycleEvidenceAsync(originalCycle, cycle, ct)
            : null;
        var cenitSources = isCenit
            ? await LoadCenitOriginalSourcesAsync(selectedIds, ct)
            : new Dictionary<int, CenitOriginalSource>();

        var alreadyReturnedTransactions = await context.Set<AchReturnGenerated>()
            .AsNoTracking()
            .Where(r => selectedIds.Contains(r.OriginalTransactionId))
            .Select(r => r.OriginalTransactionId)
            .ToHashSetAsync(ct);

        if (alreadyReturnedTransactions.Count > 0)
        {
            throw new AchReturnAlreadyGeneratedException(alreadyReturnedTransactions);
        }

        var now = _timeProvider.GetUtcNow().UtcDateTime;
        var transactionsById = transactions.ToDictionary(tx => tx.Id);
        var prepared = new List<PreparedReturn>(request.Items.Count);

        foreach (var item in request.Items)
        {
            var tx = transactionsById[item.TransactionId];

            if (tx.Type is Domain.Entities.Transactions.Enums.TransactionTypeEnum.Return or Domain.Entities.Transactions.Enums.TransactionTypeEnum.Reversal)
            {
                throw new InvalidOperationException($"La transacción {tx.Id} no es elegible para devolución porque ya corresponde a un retorno o reverso.");
            }

            if (isCenit && CenitReturnIn2026Layout.IsReturnOfReturnCause(item.ReturnReasonCode))
            {
                throw new InvalidOperationException("CENIT_ROR_NOT_ORDINARY_RETURN: las causales R60-R74 requieren el flujo independiente de devolución de una devolución.");
            }

            if (!cycleOrder.TryGetValue(tx.AchCycleId, out var txCycleOrder)
                || (isAchColombia && (selectedCycleOrder - txCycleOrder) > 4))
            {
                throw new InvalidOperationException($"La transacción {tx.Id} excede la ventana máxima de 4 ciclos para devolución.");
            }

            var eligibility = await _returnEligibilityService.EvaluateOutgoingReturnAsync(
                new AchReturnEligibilityRequest(
                    tx.Id,
                    item.ReturnReasonCode,
                    isCenit ? cycle.ProcessingDate : now,
                    HasAddenda: true),
                ct);
            if (!eligibility.IsEligible)
            {
                throw new InvalidOperationException(eligibility.Failures.First().Message);
            }

            var reasonCode = eligibility.NormalizedReasonCode!;
            if (isCenit && CenitReturnIn2026Layout.IsReturnOfReturnCause(reasonCode))
            {
                throw new InvalidOperationException("CENIT_ROR_NOT_ORDINARY_RETURN: las causales R60-R74 requieren el flujo independiente de devolución de una devolución.");
            }
            if (_causeCodePolicy is not null)
            {
                var policyResult = await _causeCodePolicy.EvaluateAsync(
                    new AchCauseCodePolicyRequest(
                        reasonCode,
                        AchCauseCodeFlow.OutboundReturn,
                        cycle.ClearingHouseId,
                        cycle.ClearingHouse?.Code,
                        tx.Type.ToString(),
                        tx.EffectiveEntryDate,
                        Source: nameof(GenerateReturnsFileAsync)),
                    ct);

                foreach (var issue in policyResult.Issues.Where(i => i.Severity != AchCauseCodePolicySeverity.Error))
                {
                    _logger.LogWarning("CAUSE_POLICY_WARNING flow={Flow} code={Code} severity={Severity} detail={Detail}", AchCauseCodeFlow.OutboundReturn, reasonCode, issue.Severity, issue.Message);
                }

                var blocking = policyResult.Issues.Where(i => i.Severity == AchCauseCodePolicySeverity.Error).ToList();
                if (!policyResult.IsAllowed || blocking.Count > 0)
                {
                    var detail = blocking.Count > 0
                        ? string.Join(" | ", blocking.Select(x => $"{x.Code}: {x.Message}"))
                        : "Causal no permitida por política rail-flow.";
                    throw new InvalidOperationException($"La causal {reasonCode} no está permitida para la cámara/flujo de devolución saliente. {detail}");
                }
            }

            var amount = tx.IsPrenotification ? 0m : tx.Amount;
            CenitOriginalSource? cenitSource = null;
            if (isCenit)
            {
                cenitSource = cenitSources[tx.Id];
                var evidence = item.CenitOperationalEvidence ?? new CenitIncomingReturnOperationalEvidence();
                var prenoteDirection = evidence.PrenotificationDirection != CenitPrenotificationDirection.Unknown
                    ? evidence.PrenotificationDirection
                    : ResolveCenitPrenotificationDirection(tx.TransactionCode);
                var policyResult = _cenitReturnPolicy!.Evaluate(new CenitIncomingReturnPolicyRequest(
                    tx.Type,
                    reasonCode,
                    tx.EffectiveEntryDate,
                    cycle.ProcessingDate,
                    cenitCycleEvidence!.OriginalCycleNumber,
                    cenitCycleEvidence.ReturnCycleNumber,
                    cenitCycleEvidence.LastReturnCycleNumber,
                    tx.Amount,
                    amount,
                    prenoteDirection,
                    evidence.ReturnRequestDate,
                    evidence.ImmediateReturnCycleConfirmed,
                    evidence.FundsAvailabilityRequired,
                    evidence.FundsAvailabilityConfirmed,
                    evidence.ConfirmationToOriginatorRecorded,
                    evidence.ReceiverRejectionDeadlineDate));
                if (!policyResult.IsAllowed)
                {
                    throw new InvalidOperationException($"{policyResult.Code}: {policyResult.Message}");
                }
            }
            var originalSequence = NormalizeDigits(tx.TraceNumber, 15);
            var receiverEntity = NormalizeDigits(tx.OriginatingDFI, 8);
            var originEntity = NormalizeDigits(tx.ReceivingDFI, 8);
            var returnTransactionCode = ResolveReturnTransactionCode(tx.TransactionCode, isCenit);
            prepared.Add(new PreparedReturn(
                tx,
                reasonCode,
                amount,
                originalSequence,
                receiverEntity,
                originEntity,
                returnTransactionCode,
                cenitSource?.StandardEntryClassCode ?? "PPD"));

            CompareReturnShadow(
                cycle.ClearingHouseId,
                cycle.ClearingHouse?.Code,
                paymentRailCode,
                cycle.Id,
                tx.EffectiveEntryDate.Date,
                $"RETURN_GENERATED:{reasonCode}",
                legacyOperationSucceeded: true);
        }

        if (_nachaFileBuilder is null)
        {
            throw new InvalidOperationException("RETURN_OUT_ACH_OPTION_C_REQUIRED: INachaFileBuilder es requerido; no existe fallback hardcoded.");
        }
        if (_externalFileNamePolicy is null)
        {
            throw new InvalidOperationException("RETURN_FILENAME_POLICY_REQUIRED: No existe política oficial de naming para ReturnOut.");
        }

        prepared = prepared
            .OrderBy(item => item.Transaction.AchBatchId)
            .ThenBy(item => item.Transaction.Id)
            .ToList();
        var participantCodes = prepared
            .Select(item => item.OriginEntity)
            .Distinct(StringComparer.Ordinal)
            .ToArray();
        if (participantCodes.Length != 1)
        {
            throw new InvalidOperationException("RETURN_TRACE_PARTICIPANT_SCOPE_INVALID: un archivo ReturnOut debe pertenecer a un único participante generador.");
        }

        var sequenceDate = DateOnly.FromDateTime(cycle.ProcessingDate.Date);
        var generatedRows = new List<AchReturnGenerated>(prepared.Count);
        var semanticEntries = new Dictionary<int, NachaReturnOutEntry>(prepared.Count);
        await using var databaseTransaction = context.Database.IsRelational()
            && context.Database.CurrentTransaction is null
            ? await context.Database.BeginTransactionAsync(ct)
            : null;
        try
        {
            var sequenceRange = await _returnTraceSequenceService.ReserveRangeAsync(
                participantCodes[0],
                sequenceDate,
                prepared.Count,
                now,
                ct);

            for (var index = 0; index < prepared.Count; index++)
            {
                var item = prepared[index];
                var newSequence = $"{item.OriginEntity}{sequenceRange.StartValue + index:0000000}";
                semanticEntries[item.Transaction.Id] = new NachaReturnOutEntry(
                    item.Transaction.Id,
                    item.ReturnTransactionCode,
                    item.ReceiverEntity,
                    DigitoChequeoHelper.CalcularDigitoChequeo(item.ReceiverEntity).ToString(),
                    item.Transaction.DestinationAccountNumber,
                    item.Amount,
                    item.Transaction.RecipientIdNumber,
                    item.Transaction.CompanyName,
                    item.Transaction.DiscretionaryData,
                    newSequence,
                    item.ReasonCode,
                    item.OriginalSequence,
                    string.Empty,
                    item.OriginEntity,
                    string.Empty,
                    newSequence);

                generatedRows.Add(new AchReturnGenerated
                {
                    OriginalTransactionId = item.Transaction.Id,
                    ReturnCycleId = cycle.Id,
                    ReturnReasonCode = item.ReasonCode,
                    Amount = item.Amount,
                    NewSequenceNumber = newSequence,
                    OriginalSequenceNumber = item.OriginalSequence,
                    ReceiverEntityCode = item.ReceiverEntity,
                    OriginatorEntityCode = item.OriginEntity,
                    FileName = string.Empty,
                    SequenceDate = sequenceDate,
                    GeneratedAtUtc = now
                });
            }

            context.Set<AchReturnGenerated>().AddRange(generatedRows);
            if (context.Database.IsRelational())
            {
                await context.SaveChangesAsync(ct);
            }

            var provisionalFileName = $"RET_{cycle.Id}_{now:yyyyMMddHHmmss}.RET";
            var semanticBatches = prepared
                .GroupBy(item => new { item.Transaction.AchBatchId, item.StandardEntryClassCode })
                .OrderBy(group => group.Key.AchBatchId)
                .ThenBy(group => group.Key.StandardEntryClassCode)
                .Select((group, index) => BuildReturnOutBatch(group, semanticEntries, index + 1, now))
                .ToList();
            var returnParticipant = participantCodes[0];
            var immediateOrigin = returnParticipant + DigitoChequeoHelper.CalcularDigitoChequeo(returnParticipant).ToString();
            var header = ResolveReturnHeader(prepared, cycle, immediateOrigin, cenitSources, isCenit);
            var optionCRequest = new NachaReturnOutBuildRequest(
                now,
                "A",
                header.ImmediateDestination,
                header.ImmediateOrigin,
                header.ImmediateDestinationName,
                header.ImmediateOriginName,
                "RETURN",
                semanticBatches,
                PersistAudit: false,
                ClearingHouseCode: isCenit ? "CENIT" : "ACH",
                ClearingHouseName: cycle.ClearingHouse?.Name ?? (isCenit ? "CENIT" : "ACH Colombia"),
                NormativeVersion: isCenit ? CenitReturnOut2026Layout.NormativeVersion : "V35");
            var provisionalArtifact = await _nachaFileBuilder.BuildReturnOutAsync(optionCRequest, ct);
            var namingResult = await ResolveReturnExternalFileNameAsync(cycle, request, provisionalFileName, provisionalArtifact.Content, ct);
            var fileIdModifier = namingResult.Components.FileIdModifier?.ToString()
                ?? throw new InvalidOperationException("RETURN_FILENAME_FILE_ID_REQUIRED: la política no asignó identificador ZZZ.");
            var finalArtifact = await _nachaFileBuilder.BuildReturnOutAsync(optionCRequest with
            {
                FileIdModifier = fileIdModifier,
                PersistAudit = true
            }, ct);
            var fileContent = finalArtifact.Content;
            var fileName = namingResult.ExternalFileName;

            foreach (var row in generatedRows)
            {
                row.FileName = fileName;
            }

            await context.SaveChangesAsync(ct);

            foreach (var row in generatedRows)
            {
                var originalTx = transactionsById[row.OriginalTransactionId];
                var payload = BuildReturnFileGeneratedPayload(
                    originalTx,
                    row,
                    cycle,
                    fileName,
                    finalArtifact.RecordCount,
                    generatedRows.Count,
                    now,
                    fileContent,
                    AchTransferStateEnum.ReturnedByEpr);

                var transition = await _stateTransitionService.TransitionAsync(new AchStateTransitionRequest(
                    row.OriginalTransactionId,
                    AchTransferStateEnum.ReturnedByEpr,
                    AchStateEventSourceEnum.Epr,
                    row.ReturnReasonCode,
                    payload,
                    row.OriginalSequenceNumber,
                    now,
                    $"outbound-return-v1:{row.OriginalTransactionId}",
                    cycle.ClearingHouseId), ct);
                if (transition.WasDuplicate)
                {
                    throw new AchReturnAlreadyGeneratedException([row.OriginalTransactionId]);
                }
            }

            if (databaseTransaction is not null)
            {
                await databaseTransaction.CommitAsync(ct);
            }

            return new GenerateReturnsFileResponse(fileName, "text/plain", Encoding.UTF8.GetBytes(fileContent), finalArtifact.RecordCount, generatedRows.Count);
        }
        catch (Exception ex)
        {
            if (databaseTransaction is not null)
            {
                await databaseTransaction.RollbackAsync(CancellationToken.None);
            }

            if (ex is DbUpdateException && RelationalDatabaseExceptionClassifier.IsUniqueViolation(ex))
            {
                context.ChangeTracker.Clear();
                var conflictingIds = await context.Set<AchReturnGenerated>()
                    .AsNoTracking()
                    .Where(row => selectedIds.Contains(row.OriginalTransactionId))
                    .Select(row => row.OriginalTransactionId)
                    .ToListAsync(CancellationToken.None);
                if (conflictingIds.Count > 0)
                {
                    throw new AchReturnAlreadyGeneratedException(conflictingIds);
                }
            }

            throw;
        }
    }

    private sealed record PreparedReturn(
        AchTransaction Transaction,
        string ReasonCode,
        decimal Amount,
        string OriginalSequence,
        string ReceiverEntity,
        string OriginEntity,
        string ReturnTransactionCode,
        string StandardEntryClassCode);

    private sealed record CenitOriginalSource(
        string StandardEntryClassCode,
        string ImmediateDestination,
        string ImmediateOrigin,
        string ImmediateDestinationName,
        string ImmediateOriginName);

    private sealed record CenitCycleEvidence(
        int OriginalCycleNumber,
        int ReturnCycleNumber,
        int LastReturnCycleNumber);

    private sealed record ReturnHeader(
        string ImmediateDestination,
        string ImmediateOrigin,
        string ImmediateDestinationName,
        string ImmediateOriginName);


    private static string BuildReturnFileGeneratedPayload(
        AchTransaction originalTx,
        AchReturnGenerated generatedRow,
        AchCycle cycle,
        string fileName,
        int recordCount,
        int returnCount,
        DateTime createdAtUtc,
        string fileContent,
        AchTransferStateEnum newState)
    {
        var payload = new
        {
            schemaVersion = 1,
            eventType = "ReturnFileGenerated",
            source = $"{nameof(AchReturnsService)}.{nameof(GenerateReturnsFileAsync)}",
            generationMode = "outbound-return",
            stateChanged = true,
            originalTransactionId = generatedRow.OriginalTransactionId,
            transactionExternalId = originalTx.TransactionExternalId,
            reference = originalTx.Reference,
            transactionType = originalTx.Type.ToString(),
            previousState = originalTx.State.ToString(),
            newState = newState.ToString(),
            returnReasonCode = generatedRow.ReturnReasonCode,
            returnCycleId = generatedRow.ReturnCycleId,
            clearingHouseId = cycle.ClearingHouseId,
            clearingHouseCode = cycle.ClearingHouse?.Code,
            clearingHouseName = cycle.ClearingHouse?.Name,
            fileName,
            externalFileName = fileName,
            contentSha256 = ComputeSha256Hex(fileContent),
            recordCount,
            returnCount,
            originalTraceNumber = generatedRow.OriginalSequenceNumber,
            newTraceNumber = generatedRow.NewSequenceNumber,
            originalSequenceNumber = generatedRow.OriginalSequenceNumber,
            newSequenceNumber = generatedRow.NewSequenceNumber,
            amount = generatedRow.Amount,
            currency = "COP",
            receiverEntityCode = generatedRow.ReceiverEntityCode,
            originatorEntityCode = generatedRow.OriginatorEntityCode,
            generatedAtUtc = generatedRow.GeneratedAtUtc,
            createdAtUtc,
            warnings = Array.Empty<string>(),
            transmissionStatus = "GeneratedNotTransmitted",
            productiveStatus = "TechnicalGeneratedOnly"
        };

        return System.Text.Json.JsonSerializer.Serialize(payload);
    }


    private static string ComputeSha256Hex(string content)
    {
        var bytes = Encoding.UTF8.GetBytes(content);
        var hash = System.Security.Cryptography.SHA256.HashData(bytes);
        return Convert.ToHexString(hash).ToLowerInvariant();
    }

    private static bool IsAchColombia(string? clearingHouseCode)
        => string.Equals(clearingHouseCode, "ACH", StringComparison.OrdinalIgnoreCase)
           || string.Equals(clearingHouseCode, "ACHCOL", StringComparison.OrdinalIgnoreCase)
           || string.Equals(clearingHouseCode, "ACHCOLOMBIA", StringComparison.OrdinalIgnoreCase);

    private static bool IsCenit(string? clearingHouseCode)
        => string.Equals(clearingHouseCode, "CENIT", StringComparison.OrdinalIgnoreCase);



    private async Task<ExternalFileNamePolicyResult> ResolveReturnExternalFileNameAsync(
        AchCycle cycle,
        GenerateReturnsFileRequest request,
        string provisionalFileName,
        string nachaContent,
        CancellationToken ct)
    {
        if (cycle.ClearingHouse is null)
        {
            throw new InvalidOperationException("No se pudo resolver la cÃ¡mara para el archivo de devoluciÃ³n.");
        }

        var context = new ExternalFileNameContext
        {
            ClearingHouseId = cycle.ClearingHouseId,
            ClearingHouseCode = cycle.ClearingHouse.Code,
            ClearingHouseOriginCode = cycle.ClearingHouse.OriginCode,
            CycleId = cycle.Id,
            CycleName = cycle.CycleName,
            ProcessingDate = cycle.ProcessingDate,
            ExternalFileType = ExternalFileType.ReturnOut,
            Flow = ExternalFileFlow.Originacion,
            Direction = ExternalFileDirection.Outbound,
            InternalFileName = provisionalFileName,
            NachaContent = nachaContent,
            RequestedBy = "system"
        };

        if (_externalFileNamePolicy is null)
        {
            throw new InvalidOperationException("RETURN_FILENAME_POLICY_REQUIRED: No existe política oficial de naming para ReturnOut.");
        }

        var policyResult = await _externalFileNamePolicy.GenerateExternalNameAsync(context, ct);
        if (policyResult.Validation.IsHardBlocked)
        {
            var details = string.Join(" | ", policyResult.Validation.Issues.Select(x => $"{x.RuleCode}:{x.Message}"));
            throw new InvalidOperationException($"Error Fatal ID: External filename validation failed. {details}");
        }

        if (policyResult.Validation.Issues.Count > 0)
        {
            _logger.LogWarning("RETURN_FILENAME_POLICY_WARNING|CycleId={CycleId}|FileName={FileName}|Issues={Issues}",
                cycle.Id,
                policyResult.ExternalFileName,
                string.Join(" | ", policyResult.Validation.Issues.Select(x => $"{x.RuleCode}:{x.Message}")));
        }

        if (!string.IsNullOrWhiteSpace(policyResult.ExternalFileName))
        {
            return policyResult;
        }

        throw new InvalidOperationException("RETURN_FILENAME_POLICY_REQUIRED: No existe política oficial de naming para ReturnOut.");
    }

    private async Task<Dictionary<int, CenitOriginalSource>> LoadCenitOriginalSourcesAsync(
        IReadOnlyCollection<int> transactionIds,
        CancellationToken ct)
    {
        var rows = await context.IncomingNachaTransactionLinks
            .AsNoTracking()
            .Where(link => link.IsFinal
                && link.AchTransactionId.HasValue
                && transactionIds.Contains(link.AchTransactionId.Value)
                && link.EntryDetail != null
                && link.EntryDetail.NachaHeader != null
                && link.EntryDetail.BatchHeader != null)
            .Select(link => new
            {
                TransactionId = link.AchTransactionId!.Value,
                StandardEntryClassCode = link.EntryDetail!.BatchHeader!.StandardEntryClassCode,
                ImmediateDestination = link.EntryDetail.NachaHeader!.ImmediateDestination,
                ImmediateOrigin = link.EntryDetail.NachaHeader.ImmediateOrigin,
                ImmediateDestinationName = link.EntryDetail.NachaHeader.ImmediateDestinationName,
                ImmediateOriginName = link.EntryDetail.NachaHeader.ImmediateOriginName
            })
            .ToListAsync(ct);

        var result = new Dictionary<int, CenitOriginalSource>();
        foreach (var transactionId in transactionIds)
        {
            var candidates = rows
                .Where(row => row.TransactionId == transactionId)
                .Select(row => new CenitOriginalSource(
                    NormalizeCenitSec(row.StandardEntryClassCode),
                    NormalizeDigits(row.ImmediateDestination, 10),
                    NormalizeDigits(row.ImmediateOrigin, 10),
                    (row.ImmediateDestinationName ?? string.Empty).Trim(),
                    (row.ImmediateOriginName ?? string.Empty).Trim()))
                .Distinct()
                .ToList();
            if (candidates.Count != 1)
            {
                throw new InvalidOperationException($"CENIT_RETURN_ORIGINAL_RAW_EVIDENCE_REQUIRED: la transacción {transactionId} no tiene un único registro 1/5/6 original trazable.");
            }

            result[transactionId] = candidates[0];
        }

        return result;
    }

    private async Task<CenitCycleEvidence> ResolveCenitCycleEvidenceAsync(
        AchCycle originalCycle,
        AchCycle returnCycle,
        CancellationToken ct)
    {
        if (!ExternalFileNameSupport.TryExtractPositiveCycleNumber(originalCycle.CycleName, out var originalCycleNumber)
            || !ExternalFileNameSupport.TryExtractPositiveCycleNumber(returnCycle.CycleName, out var returnCycleNumber))
        {
            throw new InvalidOperationException("CENIT_RETURN_CYCLE_EVIDENCE_REQUIRED: los ciclos CENIT no tienen numeración normalizada.");
        }

        var cycleNames = await context.AchCycles
            .AsNoTracking()
            .Where(item => item.ClearingHouseId == returnCycle.ClearingHouseId
                && item.ProcessingDate.Date == returnCycle.ProcessingDate.Date)
            .Select(item => item.CycleName)
            .ToListAsync(ct);
        var lastReturnCycleNumber = cycleNames
            .Select(name => ExternalFileNameSupport.TryExtractPositiveCycleNumber(name, out var number) ? number : 0)
            .DefaultIfEmpty(0)
            .Max();
        if (lastReturnCycleNumber <= 0)
        {
            throw new InvalidOperationException("CENIT_RETURN_CYCLE_EVIDENCE_REQUIRED: no existe último ciclo CENIT normalizado para la fecha operacional.");
        }

        return new CenitCycleEvidence(originalCycleNumber, returnCycleNumber, lastReturnCycleNumber);
    }

    private static ReturnHeader ResolveReturnHeader(
        IReadOnlyCollection<PreparedReturn> prepared,
        AchCycle cycle,
        string immediateOrigin,
        IReadOnlyDictionary<int, CenitOriginalSource> cenitSources,
        bool isCenit)
    {
        if (!isCenit)
        {
            return new ReturnHeader(
                ImmediateDestinationAchColombia,
                immediateOrigin,
                "ACH COLOMBIA",
                cycle.ClearingHouse?.Name ?? "CFA");
        }

        var headers = prepared
            .Select(item => cenitSources[item.Transaction.Id])
            .Select(source => new ReturnHeader(
                source.ImmediateOrigin,
                source.ImmediateDestination,
                source.ImmediateOriginName,
                source.ImmediateDestinationName))
            .Distinct()
            .ToList();
        if (headers.Count != 1)
        {
            throw new InvalidOperationException("CENIT_RETURN_HEADER_SCOPE_INVALID: el archivo mezcla participantes inmediatos de archivos originales distintos.");
        }

        return headers[0];
    }

    private static string NormalizeCenitSec(string? value)
    {
        var sec = (value ?? string.Empty).Trim().ToUpperInvariant();
        return sec switch
        {
            "PPD" or "CCD" => sec,
            "CTX" => throw new InvalidOperationException(CenitReturnIn2026Layout.CtxScopeStatus),
            _ => throw new InvalidOperationException($"CENIT_RETURN_SEC_NOT_SUPPORTED: SEC {sec} no está soportado por Return Out CENIT.")
        };
    }

    private static CenitPrenotificationDirection ResolveCenitPrenotificationDirection(string transactionCode)
        => transactionCode switch
        {
            "21" or "22" or "23" or "31" or "32" or "33" or "51" or "52" or "53" => CenitPrenotificationDirection.Credit,
            "26" or "27" or "28" or "36" or "37" or "38" or "55" or "56" or "57" => CenitPrenotificationDirection.Debit,
            _ => CenitPrenotificationDirection.Unknown
        };

    private static NachaReturnOutBatch BuildReturnOutBatch(
        IEnumerable<PreparedReturn> group,
        IReadOnlyDictionary<int, NachaReturnOutEntry> entriesByTransaction,
        int batchNumber,
        DateTime now)
    {
        var prepared = group.OrderBy(item => item.Transaction.Id).ToList();
        var original = prepared[0].Transaction;
        var entries = prepared.Select(item => entriesByTransaction[item.Transaction.Id]).ToList();
        var serviceClassCode = ResolveReturnServiceClassCode(entries);
        var originatingDfi = entries.Select(entry => entry.NewTraceNumber[..8]).Distinct(StringComparer.Ordinal).Single();

        return new NachaReturnOutBatch(
            serviceClassCode,
            original.CompanyName,
            string.Empty,
            original.CompanyIdentification,
            prepared[0].StandardEntryClassCode,
            "RETURN",
            original.EffectiveEntryDate,
            now,
            string.Empty,
            originatingDfi,
            batchNumber,
            entries);
    }

    private static string ResolveReturnServiceClassCode(IEnumerable<NachaReturnOutEntry> entries)
    {
        var materialized = entries.ToList();
        var hasCredits = materialized.Any(entry => entry.TransactionCode is "21" or "31" or "51");
        var hasDebits = materialized.Any(entry => entry.TransactionCode is "26" or "36" or "56");
        return hasCredits && hasDebits ? "200" : hasCredits ? "220" : "225";
    }

    private static string ResolveReturnTransactionCode(string originalTransactionCode, bool isCenit)
        => originalTransactionCode switch
        {
            "21" or "22" or "23" => "21",
            "31" or "32" or "33" => "31",
            "51" or "52" or "53" => "51",
            "26" or "27" or "28" => "26",
            "36" or "37" or "38" => "36",
            "55" or "56" or "57" => "56",
            _ => throw new InvalidOperationException($"{(isCenit ? "CENIT_RETURN_TRANSACTION_CODE_UNSUPPORTED" : "RETURN_OUT_ACH_V35_TRANSACTION_CODE_UNSUPPORTED")}: {originalTransactionCode} no identifica una cuenta admitida.")
        };

    private async Task<string> ResolveReturnExternalFileNameWithoutPolicyAsync(AchCycle cycle, CancellationToken ct)
    {
        var sameDayFiles = await context.Set<AchReturnGenerated>()
            .AsNoTracking()
            .Include(x => x.ReturnCycle)
            .Where(x => x.ReturnCycle.ClearingHouseId == cycle.ClearingHouseId && x.GeneratedAtUtc.Date == cycle.ProcessingDate.Date)
            .Select(x => x.FileName)
            .Where(x => x != null && x != string.Empty)
            .Distinct()
            .ToListAsync(ct);

        var parsedSequences = sameDayFiles
            .Select(fileName =>
            {
                var parsed = ExternalFileNameSupport.Parse(new ExternalFileNameContext
                {
                    ClearingHouseId = cycle.ClearingHouseId,
                    ClearingHouseCode = cycle.ClearingHouse?.Code ?? string.Empty,
                    ClearingHouseOriginCode = cycle.ClearingHouse?.OriginCode,
                    ProcessingDate = cycle.ProcessingDate,
                    ExternalFileType = ExternalFileType.ReturnOut,
                    Flow = ExternalFileFlow.Originacion,
                    Direction = ExternalFileDirection.Outbound
                }, fileName);

                return parsed.ExternalSequence;
            })
            .Where(x => x.HasValue)
            .Select(x => x!.Value)
            .ToList();

        var nextSequence = parsedSequences.Count == 0 ? 1 : parsedSequences.Max() + 1;
        if (nextSequence > 36)
        {
            throw new InvalidOperationException("Regla RET HARD BLOCK: mÃ¡ximo 36 archivos diarios por participante.");
        }

        _logger.LogWarning(
            "RETURN_FILENAME_POLICY_FALLBACK|CycleId={CycleId}|ClearingHouseId={ClearingHouseId}|Sequence={Sequence}",
            cycle.Id,
            cycle.ClearingHouseId,
            nextSequence);

        var originCode = NormalizeDigits(cycle.ClearingHouse?.OriginCode, 7);
        return ExternalFileNameSupport.BuildReturnName(originCode, nextSequence);
    }

    private void CompareReturnShadow(
        int? clearingHouseId,
        string? clearingHouseCode,
        string? paymentRailCode,
        string? achCycleId,
        DateTime? operationalDate,
        string legacyDecisionCode,
        bool legacyOperationSucceeded)
    {
        if (_paymentRailContextService is null || _strategyResolver is null || _shadowCompareService is null)
        {
            return;
        }

        try
        {
            var context = _paymentRailContextService.ResolveContext(clearingHouseId, clearingHouseCode, achCycleId, operationalDate, paymentRailCode);
            var strategy = _strategyResolver.ResolveStrategy(new PaymentRailResolveRequest(clearingHouseId, clearingHouseCode, paymentRailCode));
            var wrapperResult = strategy.EvaluateCapabilityWrapper(new PaymentRailWrapperCallRequest(
                context.OperationalContext,
                PaymentRailCapabilityKind.Return,
                legacyDecisionCode));
            var shadowResult = _shadowCompareService.CompareReturnOperation(
                context,
                wrapperResult,
                legacyDecisionCode,
                legacyOperationSucceeded);

            _logger.LogInformation(
                "PAYMENT_RAIL_SHADOW_COMPARE_RETURN|RailCode={RailCode}|LegacyDecision={LegacyDecision}|WrapperDecision={WrapperDecision}|Equivalent={Equivalent}|Code={Code}",
                shadowResult.RailCode,
                shadowResult.LegacyDecisionCode,
                shadowResult.WrapperDecisionCode,
                shadowResult.IsEquivalent,
                shadowResult.ComparisonCode);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "PAYMENT_RAIL_SHADOW_COMPARE_RETURN_FAILED");
        }
    }


    private async Task<Dictionary<string, int>> GetCycleOrderAsync(int clearingHouseId, CancellationToken ct)
    {
        var buffered = await context.AchCycles
            .AsNoTracking()
            .Where(c => c.ClearingHouseId == clearingHouseId)
            .OrderBy(c => c.ProcessingDate)
            .ToListAsync(ct);

        var cycles = buffered
            .OrderBy(c => c.ProcessingDate)
            .ThenBy(c => c.CutoffTime)
            .Select(c => c.Id)
            .ToList();

        return cycles.Select((id, index) => new { id, index }).ToDictionary(x => x.id, x => x.index, StringComparer.OrdinalIgnoreCase);
    }

    private static string NormalizeDigits(string? value, int length)
    {
        var digits = new string((value ?? string.Empty).Where(char.IsDigit).ToArray());
        if (digits.Length > length)
        {
            digits = digits[^length..];
        }

        return digits.PadLeft(length, '0');
    }
}
