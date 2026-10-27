using System.Text.Json;
using Cfa.ACHInterbank.Application.ACH.Interfaces;
using Cfa.ACHInterbank.Application.ACH.Models;
using Cfa.ACHInterbank.Domain.Models.ACH;
using Cfa.ACHInterbank.Domain.Models.Configurations;
using Cfa.ACHInterbank.Persistence.ACH.Services.Implementation.ExternalFileNames;

namespace Cfa.ACHInterbank.Persistence.ACH.Services.Implementation;

[Scoped]
public class IncomingNachaDispatchEligibilityPolicy(
    ICycleTransactionPolicy? cycleTransactionPolicy = null) : IIncomingNachaDispatchEligibilityPolicy
{
    public Task<IncomingNachaDispatchEligibilityResult> EvaluateAsync(
        IncomingNachaFileIngestion ingestion,
        IncomingNachaEntryClassification classification,
        IncomingNachaTransactionLink link,
        AchTransaction transaction,
        DateTime nowLocal,
        CancellationToken ct = default)
    {
        if (ingestion.IngestionStatus is IncomingNachaIngestionStatus.Bloqueado or IncomingNachaIngestionStatus.Fallido)
        {
            return Task.FromResult(Blocked("Ingesta bloqueada/fallida."));
        }

        if (ingestion.CycleResolutionStatus is IncomingNachaCycleResolutionStatus.Ambiguo or IncomingNachaCycleResolutionStatus.NoResuelto
            || string.IsNullOrWhiteSpace(ingestion.ResolvedAchCycleId)
            || !ingestion.ResolvedClearingHouseId.HasValue)
        {
            return Task.FromResult(Blocked("Ciclo/cámara no resuelto de forma determinística."));
        }

        if (!link.IsFinal || link.AchTransactionId is null || link.LinkType is IncomingNachaLinkType.Ambiguous or IncomingNachaLinkType.NotFound)
        {
            return Task.FromResult(Blocked("Linking no seguro o no final."));
        }

        if (classification.RequiresManualResolution || classification.EligibilityStatus is IncomingNachaEligibilityStatus.Bloqueada or IncomingNachaEligibilityStatus.RevisionManual)
        {
            return Task.FromResult(Blocked("Clasificación bloqueada o en revisión manual."));
        }

        if (classification.FunctionalClass is IncomingNachaFunctionalClass.Prenotificacion
            or IncomingNachaFunctionalClass.Devolucion
            or IncomingNachaFunctionalClass.RechazadaOperador
            or IncomingNachaFunctionalClass.RetornoEpr
            or IncomingNachaFunctionalClass.NoProcesable
            or IncomingNachaFunctionalClass.Ambigua
            or IncomingNachaFunctionalClass.Inconsistente)
        {
            return Task.FromResult(Blocked($"Clase funcional {classification.FunctionalClass} no despachable a Proc_Transacciones."));
        }

        return EvaluateResolvedPolicyAsync();

        async Task<IncomingNachaDispatchEligibilityResult> EvaluateResolvedPolicyAsync()
        {
            if (cycleTransactionPolicy is not null)
            {
                var policy = await cycleTransactionPolicy.EvaluateAsync(new CycleTransactionPolicyRequest(
                    transaction.AchCycle.ClearingHouseId,
                    transaction.AchCycle.ClearingHouseCycleConfigId,
                    transaction.AchCycle.ProcessingDate,
                    transaction.AchCycle.ClearingHouse?.Code,
                    transaction.AchCycle.ClearingHouse?.ClearingHouseConfig?.PaymentRailCode,
                    transaction.AchCycle.CycleName,
                    transaction.Type,
                    transaction.IsPrenotification,
                    transaction.ReturnReasonCode,
                    transaction.OriginalTraceRef,
                    TransactionCode: transaction.TransactionCode), ct);
                if (!policy.IsAllowed)
                {
                    return Blocked($"{policy.ReasonCode}: {policy.Message}");
                }
            }

            var cycleStart = transaction.AchCycle.StartTime;
            var cycleEnd = transaction.AchCycle.EndTime;
            var isInWindow = IsWithinWindow(nowLocal, transaction.AchCycle.ProcessingDate, cycleStart, cycleEnd);
            if (!isInWindow)
            {
                return new IncomingNachaDispatchEligibilityResult(
                IsEligible: false,
                IsWaitingWindow: true,
                IsBlocked: false,
                Priority: 200,
                Reason: "Fuera de ventana operativa de ciclo.",
                EvidenceJson: JsonSerializer.Serialize(new { nowLocal, cycleStart, cycleEnd, transaction.AchCycleId }));
            }

        // Prioridad normativa exclusiva de CENIT; se resuelve por identidad persistida, nunca por ID semilla.
            var priority = string.Equals(transaction.AchCycle.ClearingHouse?.Code, "CENIT", StringComparison.OrdinalIgnoreCase)
                ? 80
                : 100;
            return new IncomingNachaDispatchEligibilityResult(
            IsEligible: true,
            IsWaitingWindow: false,
            IsBlocked: false,
            Priority: priority,
            Reason: "Elegible para despacho Proc_Transacciones.",
            EvidenceJson: JsonSerializer.Serialize(new
            {
                classification.FunctionalClass,
                classification.EligibilityStatus,
                link.LinkType,
                transaction.AchCycleId,
                transaction.AchCycle.ClearingHouseId,
                priority
            }));
        }
    }

    /// <summary>
    /// F4.1: fail-closed monetary eligibility for an ACHCOL NACHA entry.
    /// This deliberately does not use the internal cycle policy or cycle windows:
    /// those require AchCycle and cannot be applied to the file-based route.
    /// A positive answer is only a classification, NOT authorization to send SOAP.
    /// </summary>
    public Task<IncomingNachaDispatchEligibilityResult> EvaluateFileBasedAsync(
        IncomingNachaFileIngestion ingestion,
        IncomingNachaEntryClassification classification,
        NachaHeader header,
        EntryDetail entry,
        ClearingHouse clearingHouse,
        bool featureEnabled,
        CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();
        if (!featureEnabled)
            return Task.FromResult(Blocked("ACHCOL_FLAG_DISABLED"));

        if (ingestion.IngestionStatus != IncomingNachaIngestionStatus.Completado
            || !string.Equals(ingestion.ResolutionMode, IncomingNachaDispatchSourceKinds.AchColFileBased, StringComparison.Ordinal)
            || ingestion.CycleResolutionStatus != IncomingNachaCycleResolutionStatus.ResueltoSinCiclo
            || ingestion.ResolvedAchCycleId is not null
            || ingestion.ResolvedClearingHouseId is null
            || ingestion.OperationalDate is null)
            return Task.FromResult(Blocked("ACHCOL_FILE_CONTEXT_INVALID"));

        if (!clearingHouse.IsActive
            || !string.Equals(clearingHouse.Code, "ACHCOL", StringComparison.OrdinalIgnoreCase)
            || clearingHouse.ClearingHouseId <= 0
            || clearingHouse.ClearingHouseId != ingestion.ResolvedClearingHouseId.Value)
            return Task.FromResult(Blocked("ACHCOL_CLEARING_HOUSE_INVALID"));

        if (!AchColInboundFileNameParser.TryParse(ingestion.FileName, out var parsed, out _)
            || parsed is null
            || !string.Equals(parsed.CanonicalPlainFileName, ingestion.CanonicalPlainFileName, StringComparison.OrdinalIgnoreCase)
            || parsed.FileSequence != ingestion.FileSequence
            || parsed.ExternalCycleNumber != ingestion.ExternalCycleNumber
            || !string.Equals(parsed.FileIdModifier.ToString(), ingestion.FileIdModifier, StringComparison.Ordinal)
            || !string.Equals(parsed.ProtectionKind.ToString(), ingestion.FileProtectionKind, StringComparison.Ordinal)
            || parsed.FileDate != DateOnly.FromDateTime(ingestion.OperationalDate.Value)
            || (ingestion.FileNameDate.HasValue && parsed.FileDate != DateOnly.FromDateTime(ingestion.FileNameDate.Value)))
            return Task.FromResult(Blocked("ACHCOL_PHYSICAL_FILE_CONTEXT_MISMATCH"));

        if (classification.IncomingNachaFileIngestionId != ingestion.Id
            || classification.EntryDetailId != entry.EntryDetailID
            || string.IsNullOrWhiteSpace(entry.NachaID)
            || !string.Equals(entry.NachaID, header.NachaID, StringComparison.Ordinal)
            || header.IncomingNachaFileIngestionId != ingestion.Id
            || !string.Equals(header.FileIdModifier, ingestion.FileIdModifier, StringComparison.Ordinal))
            return Task.FromResult(Blocked("ACHCOL_ENTRY_OR_HEADER_MISMATCH"));

        if (classification.EligibilityStatus != IncomingNachaEligibilityStatus.Elegible
            || classification.RequiresManualResolution
            || classification.PrenoteStatus != IncomingNachaPrenoteStatus.NoAplica
            || !string.IsNullOrWhiteSpace(classification.ReturnReasonCode)
            || !string.IsNullOrWhiteSpace(classification.OriginalTraceRef))
            return Task.FromResult(Blocked("ACHCOL_CLASSIFICATION_NOT_ELIGIBLE"));

        // Only monetary codes in ACHCOL's own type-6 layout. The surrounding
        // NACHA parser already documents 23/28/33/38/53/57 as prenotifications.
        // Direction must agree with the persisted functional classification.
        var code = entry.TransactionCode?.Trim();
        var isMonetaryCode = classification.FunctionalClass switch
        {
            IncomingNachaFunctionalClass.CreditoEntrante => code is "22" or "32" or "52",
            IncomingNachaFunctionalClass.DebitoEntrante => code is "27" or "37" or "55",
            _ => false
        };
        if (!isMonetaryCode)
            return Task.FromResult(Blocked("ACHCOL_NON_MONETARY_OR_DIRECTION_MISMATCH"));

        if (!IncomingNachaFileBasedDispatchIdentity.TryBuild(
                clearingHouse.ClearingHouseId,
                ingestion.OperationalDate.Value,
                header, entry, out _))
            return Task.FromResult(Blocked("ACHCOL_MONETARY_IDENTITY_INCOMPLETE"));

        return Task.FromResult(new IncomingNachaDispatchEligibilityResult(
            IsEligible: true,
            IsWaitingWindow: false,
            IsBlocked: false,
            Priority: 100,
            Reason: "ACHCOL_FILE_BASED_MONETARY_ELIGIBLE_NOT_AUTHORIZED_FOR_SOAP",
            EvidenceJson: JsonSerializer.Serialize(new
            {
                sourceKind = IncomingNachaDispatchSourceKinds.AchColFileBased,
                classification.FunctionalClass,
                classification.EligibilityStatus,
                transactionCode = code,
                ingestion.FileSequence,
                ingestion.ExternalCycleNumber,
                ingestion.ResolvedClearingHouseId,
                cycleBound = false,
                transportAuthorized = false
            })));
    }

    private static IncomingNachaDispatchEligibilityResult Blocked(string reason)
    {
        return new IncomingNachaDispatchEligibilityResult(
            IsEligible: false,
            IsWaitingWindow: false,
            IsBlocked: true,
            Priority: 999,
            Reason: reason,
            EvidenceJson: JsonSerializer.Serialize(new { reason }));
    }

    private static bool IsWithinWindow(DateTime now, DateTime processingDate, TimeSpan startTime, TimeSpan endTime)
    {
        if (startTime <= endTime)
        {
            var start = processingDate.Date + startTime;
            var end = processingDate.Date + endTime;
            return now >= start && now <= end;
        }

        var overnightStart = processingDate.Date.AddDays(-1) + startTime;
        var overnightEnd = processingDate.Date + endTime;
        return now >= overnightStart && now <= overnightEnd;
    }
}
