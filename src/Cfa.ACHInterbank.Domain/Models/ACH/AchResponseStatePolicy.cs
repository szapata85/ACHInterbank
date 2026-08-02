using Cfa.ACHInterbank.Domain.Models.ACH.Enums;

namespace Cfa.ACHInterbank.Domain.Models.ACH;

public static class AchResponseStatePolicy
{
    private static readonly IReadOnlyDictionary<AchResponseProcessingStatus, HashSet<AchResponseProcessingStatus>> Allowed =
        new Dictionary<AchResponseProcessingStatus, HashSet<AchResponseProcessingStatus>>
        {
            [AchResponseProcessingStatus.Recibida] = [AchResponseProcessingStatus.Homologada, AchResponseProcessingStatus.Notificada, AchResponseProcessingStatus.NoHomologada, AchResponseProcessingStatus.PendienteCorrelacion, AchResponseProcessingStatus.Huerfana, AchResponseProcessingStatus.RequiereRevisionManual, AchResponseProcessingStatus.ErrorFuncional, AchResponseProcessingStatus.ErrorTecnico, AchResponseProcessingStatus.Duplicada],
            [AchResponseProcessingStatus.PendienteCorrelacion] = [AchResponseProcessingStatus.Huerfana, AchResponseProcessingStatus.Homologada, AchResponseProcessingStatus.RequiereRevisionManual],
            [AchResponseProcessingStatus.Huerfana] = [AchResponseProcessingStatus.EnRevision],
            [AchResponseProcessingStatus.NoHomologada] = [AchResponseProcessingStatus.Huerfana, AchResponseProcessingStatus.EnRevision, AchResponseProcessingStatus.PendienteReproceso],
            [AchResponseProcessingStatus.RequiereRevisionManual] = [AchResponseProcessingStatus.EnRevision, AchResponseProcessingStatus.PendienteReproceso],
            [AchResponseProcessingStatus.EnRevision] = [AchResponseProcessingStatus.Resuelta, AchResponseProcessingStatus.Rechazada, AchResponseProcessingStatus.PendienteReproceso],
            [AchResponseProcessingStatus.Homologada] = [AchResponseProcessingStatus.Notificada, AchResponseProcessingStatus.PendienteReintento, AchResponseProcessingStatus.ErrorFuncional, AchResponseProcessingStatus.ErrorTecnico],
            [AchResponseProcessingStatus.PendienteReintento] = [AchResponseProcessingStatus.PendienteReproceso, AchResponseProcessingStatus.Reprocesando],
            [AchResponseProcessingStatus.ErrorFuncional] = [AchResponseProcessingStatus.EnRevision, AchResponseProcessingStatus.PendienteReproceso],
            [AchResponseProcessingStatus.ErrorTecnico] = [AchResponseProcessingStatus.PendienteReproceso],
            [AchResponseProcessingStatus.PendienteReproceso] = [AchResponseProcessingStatus.Reprocesando],
            [AchResponseProcessingStatus.Reprocesando] = [AchResponseProcessingStatus.Reprocesada, AchResponseProcessingStatus.ErrorTecnico, AchResponseProcessingStatus.RequiereRevisionManual],
            [AchResponseProcessingStatus.Reprocesada] = [AchResponseProcessingStatus.Cerrada],
            [AchResponseProcessingStatus.Resuelta] = [AchResponseProcessingStatus.PendienteReproceso, AchResponseProcessingStatus.Cerrada],
            [AchResponseProcessingStatus.Rechazada] = [AchResponseProcessingStatus.Cerrada],
            [AchResponseProcessingStatus.Notificada] = [AchResponseProcessingStatus.Cerrada]
        };

    public static bool CanTransition(AchResponseProcessingStatus source, AchResponseProcessingStatus target)
        => source == target || Allowed.TryGetValue(source, out var targets) && targets.Contains(target);

    public static void EnsureTransition(AchResponseProcessingStatus source, AchResponseProcessingStatus target,
        string actor, string reason, string correlationId)
    {
        if (string.IsNullOrWhiteSpace(actor)) throw new ArgumentException("El actor es obligatorio.", nameof(actor));
        if (string.IsNullOrWhiteSpace(reason)) throw new ArgumentException("El motivo es obligatorio.", nameof(reason));
        if (string.IsNullOrWhiteSpace(correlationId)) throw new ArgumentException("El correlation ID es obligatorio.", nameof(correlationId));
        if (!CanTransition(source, target))
            throw new InvalidOperationException($"Transición no permitida: {source} -> {target}.");
    }
}
