using System.Globalization;
using Cfa.ACHInterbank.Application.ACH.Interfaces;
using Cfa.ACHInterbank.Application.ACH.Interfaces.Repositories;
using Cfa.ACHInterbank.Application.ACH.Models;
using Cfa.ACHInterbank.Application.ACH.Services;
using Cfa.ACHInterbank.Domain.Entities.Transactions.Enums;
using Cfa.ACHInterbank.Domain.Models.ACH;
using Cfa.ACHInterbank.Domain.Models.ACH.Enums;
using Cfa.ACHInterbank.Domain.Models.Configurations;
using Cfa.ACHInterbank.Persistence.DataBase;
using Microsoft.EntityFrameworkCore;

namespace Cfa.ACHInterbank.Persistence.ACH.Services.Implementation;

[Scoped]
public sealed class CenitReturnOfReturnService(
    AchDbContext context,
    IAchRegulatoryCatalogService regulatoryCatalogService,
    IAchTransactionRepository transactionRepository,
    IAchReturnGenerationLockService returnGenerationLockService,
    IOperationalCalendarService operationalCalendarService,
    ICycleNumberResolver? cycleNumberResolver = null) : ICenitReturnOfReturnService
{
    private readonly ICycleNumberResolver _cycleNumberResolver = cycleNumberResolver ?? new CycleNumberResolver();

    public async Task<CenitReturnOfReturnResult> CreateOutgoingAsync(CenitReturnOfReturnOutRequest request, CancellationToken ct = default)
    {
        var parentEvent = await context.AchTransactionStateEvents
            .Include(x => x.AchTransaction).ThenInclude(x => x.AchCycle).ThenInclude(x => x.ClearingHouse)
            .Include(x => x.AchTransaction).ThenInclude(x => x.AchBatch)
            .SingleOrDefaultAsync(x => x.Id == request.ParentIncomingReturnStateEventId, ct);
        if (parentEvent?.AchTransaction is null)
            return Failure("PARENT_INCOMING_RETURN_NOT_FOUND", "No existe la devolución CENIT entrante indicada.");

        var original = parentEvent.AchTransaction;
        if (!IsCenit(original)) return Failure("ORIGINAL_NOT_CENIT", "La devolución padre no pertenece a CENIT.");
        if (parentEvent.ToState is not (AchTransferStateEnum.ReturnedByEpr or AchTransferStateEnum.ReturnedByOperator))
            return Failure("PARENT_INCOMING_RETURN_STATE_INVALID", "El evento padre no representa una devolución aplicada.");
        await using var generationLock = await returnGenerationLockService.AcquireAsync([original.Id], ct);
        if (await context.ReturnOfReturnFlows.AsNoTracking().AnyAsync(x => x.ReturnOfReturnTransactionId == original.Id, ct))
            return Failure("ROR_ON_ROR_NOT_ALLOWED", "CENIT permite una sola devolución de devolución; un ROR no puede ser devuelto nuevamente.");

        var reason = NormalizeReason(request.ReasonCode);
        var validation = await ValidatePolicyAsync(
            original.AchCycle.ClearingHouseId,
            parentEvent.ReasonCode,
            reason,
            parentEvent.ToState,
            parentEvent.OccurredAtUtc,
            request.RequestedAtUtc,
            ct);
        if (validation is not null) return validation;

        var existing = await context.ReturnOfReturnFlows
            .AsNoTracking()
            .SingleOrDefaultAsync(x => x.ParentIncomingReturnStateEventId == parentEvent.Id, ct);
        if (existing is not null)
            return new(true, true, existing.Id, existing.ReturnOfReturnTransactionId, "ROR_ALREADY_REGISTERED", "La devolución de devolución ya estaba registrada.");

        var sourceArtifact = await context.IncomingNachaTransactionLinks
            .Include(x => x.EntryDetail).ThenInclude(x => x!.BatchHeader)
            .Include(x => x.AddendaRecord)
            .Where(x => x.AchTransactionId == original.Id
                        && x.AddendaRecord != null
                        && x.AddendaRecord.ReturnReasonCode == parentEvent.ReasonCode)
            .OrderByDescending(x => x.LinkedAtUtc)
            .FirstOrDefaultAsync(ct);
        if (sourceArtifact?.EntryDetail?.BatchHeader is null || sourceArtifact.AddendaRecord is null)
            return Failure("PARENT_INCOMING_RETURN_RAW_NOT_FOUND", "No existe evidencia NACHA raw correlacionada para la devolución padre.");
        if (!string.Equals(sourceArtifact.EntryDetail.BatchHeader.StandardEntryClassCode?.Trim(), "PPD", StringComparison.OrdinalIgnoreCase))
            return Failure(CenitReturnOfReturn2026Layout.CcdScopeStatus, "El contrato específico vigente de ROR CENIT está definido para PPD.");

        var parentCycleId = await context.NachaHeaders.AsNoTracking()
            .Where(x => x.NachaID == sourceArtifact.EntryDetail.NachaID)
            .Select(x => x.AchCycleId)
            .SingleOrDefaultAsync(ct);
        var operationalFailure = await ValidateOperationalWindowAsync(
            original.AchCycle.ClearingHouseId,
            parentCycleId,
            request.ReturnCycleId,
            request.RequestedAtUtc,
            ct);
        if (operationalFailure is not null) return operationalFailure;
        var targetCycle = await context.AchCycles.SingleAsync(x => x.Id == request.ReturnCycleId, ct);

        var participant = Digits(original.OriginatingDFI, 8);
        var effectiveEntryDate = targetCycle.ProcessingDate.Date;
        var sequenceDate = DateOnly.FromDateTime(effectiveEntryDate);
        var sequence = await transactionRepository.AllocateNextTraceSequenceAsync(
            sequenceDate,
            participant,
            request.RequestedAtUtc,
            ct);
        var trace = $"{participant}{sequence:0000000}";
        var ror = CloneAsRor(original, trace, reason, request.RequestedAtUtc, AchTransactionDirection.Outgoing, AchTransactionOrigin.Cfa);
        ror.AchCycleId = targetCycle.Id;
        ror.EffectiveEntryDate = effectiveEntryDate;
        ror.TransactionCode = sourceArtifact.EntryDetail.TransactionCode?.Trim() ?? string.Empty;
        ror.OriginalTraceRef = sourceArtifact.AddendaRecord.OriginalTraceNumber?.Trim() ?? original.TraceNumber;

        await using var transaction = context.Database.IsRelational() && context.Database.CurrentTransaction is null
            ? await context.Database.BeginTransactionAsync(ct)
            : null;
        context.AchTransactions.Add(ror);
        await context.SaveChangesAsync(ct);
        var flow = new ReturnOfReturnFlow
        {
            ParentIncomingReturnStateEventId = parentEvent.Id,
            OriginalTransactionId = original.Id,
            ReturnOfReturnTransactionId = ror.Id,
            ReasonCode = reason,
            Direction = "Out",
            Status = "Registered",
            OrchestratedAtUtc = request.RequestedAtUtc
        };
        context.ReturnOfReturnFlows.Add(flow);
        await context.SaveChangesAsync(ct);
        if (transaction is not null) await transaction.CommitAsync(ct);
        return new(true, false, flow.Id, ror.Id, "ROR_OUT_REGISTERED", "ROR Out CENIT registrada.");
    }

    public async Task<CenitReturnOfReturnResult> IngestIncomingAsync(CenitReturnOfReturnInRequest request, CancellationToken ct = default)
    {
        var parent = await context.Set<AchReturnGenerated>()
            .Include(x => x.OriginalTransaction).ThenInclude(x => x.AchCycle).ThenInclude(x => x.ClearingHouse)
            .Include(x => x.OriginalTransaction).ThenInclude(x => x.AchBatch)
            .SingleOrDefaultAsync(x => x.Id == request.ParentOutgoingReturnGeneratedId, ct);
        if (parent?.OriginalTransaction is null || parent.OriginalTransactionId != request.OriginalTransactionId)
            return Failure("PARENT_OUTGOING_RETURN_NOT_FOUND", "No existe el Return Out CENIT padre indicado.");
        if (!IsCenit(parent.OriginalTransaction)) return Failure("ORIGINAL_NOT_CENIT", "El Return Out padre no pertenece a CENIT.");
        await using var generationLock = await returnGenerationLockService.AcquireAsync([parent.OriginalTransactionId], ct);

        var reason = NormalizeReason(request.ReasonCode);
        var operationalFailure = await ValidateOperationalWindowAsync(
            parent.OriginalTransaction.AchCycle.ClearingHouseId,
            parent.ReturnCycleId,
            request.ReceivedCycleId,
            request.ReceivedAtUtc,
            ct);
        if (operationalFailure is not null) return operationalFailure;
        var targetCycle = await context.AchCycles.SingleAsync(x => x.Id == request.ReceivedCycleId, ct);
        var policyFailure = await ValidatePolicyAsync(
            parent.OriginalTransaction.AchCycle.ClearingHouseId,
            parent.ReturnReasonCode,
            reason,
            parent.OriginalTransaction.State,
            parent.GeneratedAtUtc,
            request.ReceivedAtUtc,
            ct,
            allowReturnedByEpr: true);
        if (policyFailure is not null) return policyFailure;

        if (!string.Equals(parent.NewSequenceNumber, request.SourceReturnTraceNumber, StringComparison.Ordinal)
            || !string.Equals(parent.OriginalSequenceNumber, request.OriginalTraceNumber, StringComparison.Ordinal)
            || !string.Equals(Digits(parent.OriginatorEntityCode, 8), request.OriginalReceivingDfi, StringComparison.Ordinal)
            || parent.Amount != request.Amount
            || !string.Equals(parent.ReturnReasonCode.TrimStart('R'), request.SourceReturnReasonCode, StringComparison.Ordinal)
            || !string.Equals(parent.SequenceDate.DayOfYear.ToString("D3", CultureInfo.InvariantCulture), request.SourceReturnSettlementDate, StringComparison.Ordinal))
            return Failure("ROR_PARENT_FIELDS_MISMATCH", "Los campos preservados no corresponden al Return Out padre.");

        var existing = await context.ReturnOfReturnFlows
            .AsNoTracking()
            .SingleOrDefaultAsync(x => x.ParentOutgoingReturnGeneratedId == parent.Id, ct);
        if (existing is not null)
            return new(true, true, existing.Id, existing.ReturnOfReturnTransactionId, "ROR_ALREADY_REGISTERED", "El ROR In ya estaba registrado.");

        var duplicateTrace = await context.AchTransactions.AsNoTracking().AnyAsync(x => x.TraceNumber == request.TraceNumber, ct);
        if (duplicateTrace) return Failure("ROR_TRACE_DUPLICATE", "El trace del ROR ya existe.");

        var ror = CloneAsRor(parent.OriginalTransaction, request.TraceNumber, reason, request.ReceivedAtUtc, AchTransactionDirection.Incoming, AchTransactionOrigin.ExternalInstitution);
        ror.AchCycleId = targetCycle.Id;
        ror.EffectiveEntryDate = targetCycle.ProcessingDate.Date;
        ror.TransactionCode = request.TransactionCode;
        ror.Amount = request.Amount;
        ror.OriginalTraceRef = request.OriginalTraceNumber;

        await using var transaction = context.Database.IsRelational() && context.Database.CurrentTransaction is null
            ? await context.Database.BeginTransactionAsync(ct)
            : null;
        context.AchTransactions.Add(ror);
        await context.SaveChangesAsync(ct);
        var flow = new ReturnOfReturnFlow
        {
            ParentOutgoingReturnGeneratedId = parent.Id,
            OriginalTransactionId = parent.OriginalTransactionId,
            ReturnOfReturnTransactionId = ror.Id,
            ReasonCode = reason,
            Direction = "In",
            Status = "Applied",
            OrchestratedAtUtc = request.ReceivedAtUtc
        };
        context.ReturnOfReturnFlows.Add(flow);
        await context.SaveChangesAsync(ct);
        if (transaction is not null) await transaction.CommitAsync(ct);
        return new(true, false, flow.Id, ror.Id, "ROR_IN_APPLIED", "ROR In CENIT correlacionado y persistido.");
    }

    private async Task<CenitReturnOfReturnResult?> ValidatePolicyAsync(
        int clearingHouseId,
        string? originalReason,
        string reason,
        AchTransferStateEnum state,
        DateTime parentDate,
        DateTime requestedDate,
        CancellationToken ct,
        bool allowReturnedByEpr = false)
    {
        if (!CenitReturnOfReturn2026Layout.IsCause(reason))
            return Failure("ROR_REASON_INVALID", "La causal ROR debe pertenecer al rango R60-R74.");
        if (!CenitReturnIn2026Layout.IsOrdinaryReturnCause(originalReason))
            return Failure("PARENT_RETURN_REASON_INVALID", "La causal de la devolución padre no es una causal ordinaria CENIT.");
        if (!allowReturnedByEpr && state is not (AchTransferStateEnum.ReturnedByEpr or AchTransferStateEnum.ReturnedByOperator))
            return Failure("PARENT_RETURN_STATE_INVALID", "El estado de la devolución padre no es elegible.");

        var policy = await regulatoryCatalogService.ValidateReturnOfReturnAsync(
            clearingHouseId,
            originalReason!.Trim().ToUpperInvariant(),
            reason,
            state.ToString(),
            parentDate.Date,
            requestedDate.Date,
            ct);
        return policy.IsAllowed ? null : Failure("ROR_POLICY_REJECTED", policy.Reason ?? "La policy ROR rechazó la operación.");
    }

    private async Task<CenitReturnOfReturnResult?> ValidateOperationalWindowAsync(
        int clearingHouseId,
        string? parentCycleId,
        string targetCycleId,
        DateTime processedAtUtc,
        CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(parentCycleId) || string.IsNullOrWhiteSpace(targetCycleId))
            return Failure("CENIT_ROR_CYCLE_EVIDENCE_REQUIRED", "ROR requiere ciclos CENIT padre y destino resueltos.");

        var cycles = await context.AchCycles.AsNoTracking()
            .Where(x => x.ClearingHouseId == clearingHouseId && (x.Id == parentCycleId || x.Id == targetCycleId))
            .ToListAsync(ct);
        var parent = cycles.SingleOrDefault(x => x.Id == parentCycleId);
        var target = cycles.SingleOrDefault(x => x.Id == targetCycleId);
        if (parent is null || target is null)
            return Failure("CENIT_ROR_CYCLE_NOT_FOUND", "No fue posible resolver los ciclos CENIT padre y destino.");
        if (target.ProcessingDate.Date != processedAtUtc.Date)
            return Failure("CENIT_ROR_VALUE_DATE_MISMATCH", "La Fecha Valor ROR debe ser la fecha de transmisión del ciclo destino.");

        var parentNumber = _cycleNumberResolver.Resolve(parent.CycleName);
        var targetNumber = _cycleNumberResolver.Resolve(target.CycleName);
        var lastCycleNumber = await context.AchCycles.AsNoTracking()
            .Where(x => x.ClearingHouseId == clearingHouseId && x.ProcessingDate.Date == target.ProcessingDate.Date)
            .Select(x => x.CycleName)
            .ToListAsync(ct);
        var last = lastCycleNumber.Select(_cycleNumberResolver.Resolve).Where(x => x.HasValue).Select(x => x!.Value).DefaultIfEmpty().Max();
        if (!parentNumber.HasValue || !targetNumber.HasValue || last <= 0)
            return Failure("CENIT_ROR_CYCLE_EVIDENCE_REQUIRED", "Los ciclos CENIT no tienen numeración normalizada suficiente.");

        if (target.ProcessingDate.Date == parent.ProcessingDate.Date)
        {
            return parentNumber.Value >= 2 && parentNumber.Value < last && targetNumber.Value == last
                ? null
                : Failure("CENIT_ROR_SAME_DAY_CYCLE_INVALID", "ROR del mismo día debe generarse en el último ciclo para una devolución recibida entre el segundo y el penúltimo ciclo.");
        }

        var nextBusinessDay = await operationalCalendarService.GetNextBusinessDayAsync(
            DateOnly.FromDateTime(parent.ProcessingDate.Date).AddDays(1),
            clearingHouseId,
            ct);
        return DateOnly.FromDateTime(target.ProcessingDate.Date) == nextBusinessDay && targetNumber.Value <= 2
            ? null
            : Failure("CENIT_ROR_DEADLINE_EXCEEDED", "ROR debe transmitirse a más tardar en el segundo ciclo del Día Hábil Bancario siguiente.");
    }

    private static AchTransaction CloneAsRor(
        AchTransaction original,
        string trace,
        string reason,
        DateTime effectiveAt,
        AchTransactionDirection direction,
        AchTransactionOrigin origin)
        => new()
        {
            Amount = original.Amount,
            TransactionExternalId = $"CENIT-ROR-{direction}-{trace}",
            Reference = $"ROR-{trace}",
            Type = TransactionTypeEnum.Return,
            TransactionCode = original.TransactionCode,
            ServiceClassCode = original.ServiceClassCode,
            CompanyEntryDescriptionId = original.CompanyEntryDescriptionId,
            CompanyName = original.CompanyName,
            CompanyIdentification = original.CompanyIdentification,
            OriginatingDFI = original.OriginatingDFI,
            ReceivingDFI = original.ReceivingDFI,
            TraceNumber = trace,
            TraceSequenceNumber = int.TryParse(trace[^7..], out var sequence) ? sequence : 0,
            EffectiveEntryDate = effectiveAt.Date,
            AddendaRecordIndicator = true,
            IsPrenotification = false,
            Direction = direction,
            Origin = origin,
            MonetaryIntegrationRoute = AchMonetaryIntegrationRoute.None,
            ClassificationStatus = AchTransactionClassificationStatus.Determined,
            ClassifiedAtUtc = effectiveAt,
            ClassificationVersion = 1,
            State = AchTransferStateEnum.Certified,
            StateChangedAtUtc = effectiveAt,
            ReturnReasonCode = reason,
            RecipientIdNumber = original.RecipientIdNumber,
            DiscretionaryData = original.DiscretionaryData,
            SourceAccountNumber = original.SourceAccountNumber,
            DestinationAccountNumber = original.DestinationAccountNumber,
            SourceInstitutionId = original.SourceInstitutionId,
            DestinationInstitutionId = original.DestinationInstitutionId,
            AchCycleId = original.AchCycleId,
            AchBatchId = original.AchBatchId
        };

    private static bool IsCenit(AchTransaction transaction)
        => transaction.AchCycle?.ClearingHouse is not null
           && string.Equals(transaction.AchCycle.ClearingHouse.Code, "CENIT", StringComparison.OrdinalIgnoreCase);

    private static string NormalizeReason(string? reason) => reason?.Trim().ToUpperInvariant() ?? string.Empty;
    private static string Digits(string? value, int length)
    {
        var digits = new string((value ?? string.Empty).Where(char.IsDigit).ToArray());
        return digits.Length >= length ? digits[..length] : digits.PadLeft(length, '0');
    }
    private static CenitReturnOfReturnResult Failure(string code, string message) => new(false, false, null, null, code, message);
}
