using System.Text.Json;
using Cfa.ACHInterbank.Application.ACH.Interfaces;
using Cfa.ACHInterbank.Application.ACH.Models;
using Cfa.ACHInterbank.Domain.Models.ACH;
using Cfa.ACHInterbank.Domain.Models.Configurations;

namespace Cfa.ACHInterbank.Persistence.ACH.Services.Implementation;

[Scoped]
public class IncomingNachaDispatchEligibilityPolicy : IIncomingNachaDispatchEligibilityPolicy
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

        var cycleStart = transaction.AchCycle.StartTime;
        var cycleEnd = transaction.AchCycle.EndTime;
        var isInWindow = IsWithinWindow(nowLocal, transaction.AchCycle.ProcessingDate, cycleStart, cycleEnd);
        if (!isInWindow)
        {
            return Task.FromResult(new IncomingNachaDispatchEligibilityResult(
                IsEligible: false,
                IsWaitingWindow: true,
                IsBlocked: false,
                Priority: 200,
                Reason: "Fuera de ventana operativa de ciclo.",
                EvidenceJson: JsonSerializer.Serialize(new { nowLocal, cycleStart, cycleEnd, transaction.AchCycleId })));
        }

        // Prioridad normativa exclusiva de CENIT; se resuelve por identidad persistida, nunca por ID semilla.
        var priority = string.Equals(transaction.AchCycle.ClearingHouse?.Code, "CENIT", StringComparison.OrdinalIgnoreCase)
            ? 80
            : 100;
        return Task.FromResult(new IncomingNachaDispatchEligibilityResult(
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
