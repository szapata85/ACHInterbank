namespace Cfa.ACHInterbank.Application.OutgoingTransactionMonitoring;

public sealed class OutgoingTransactionMonitoringStatusPolicy : IOutgoingTransactionMonitoringStatusPolicy
{
    public OutgoingTransactionMonitoringStatus Consolidate(OutgoingTransactionMonitoringFacts facts)
    {
        var process = ResolveProcess(facts);
        var initial = ResolveInitialResult(facts);
        var subsequent = facts.HasReturn && (facts.HasAccepted || facts.HasCertified)
            ? ("ReturnedLater", "Devuelta posteriormente")
            : facts.HasReturn
                ? ("Returned", "Devuelta")
                : ("None", "Sin novedad posterior");

        var attentionReason = facts.HasManualReview
            ? "Existe evidencia persistida que requiere revisión operativa."
            : facts.HasAmbiguousCorrelation
                ? "La correlación de una respuesta no es concluyente."
                : facts.HasTechnicalFailure && !facts.HasSuccessfulIntegration
                    ? "La integración monetaria registra un error técnico sin éxito posterior."
                    : null;

        return new OutgoingTransactionMonitoringStatus(
            process.Code,
            process.Label,
            initial.Code,
            initial.Label,
            subsequent.Item1,
            subsequent.Item2,
            attentionReason is not null,
            attentionReason);
    }

    private static (string Code, string Label) ResolveProcess(OutgoingTransactionMonitoringFacts facts)
    {
        if (facts.HasTechnicalFailure && !facts.HasSuccessfulIntegration)
            return ("TechnicalError", "Error técnico");
        if (facts.IsFutureCycle && !facts.HasDispatchItem && !facts.HasFileMembership
            && !facts.HasAccepted && !facts.HasCertified && !facts.HasReturn)
            return ("Scheduled", "Asignada a un ciclo futuro");
        if (facts.HasSuccessfulIntegration || facts.HasAccepted || facts.HasCertified || facts.HasReturn || facts.HasFileMembership)
            return ("Processed", "Procesada");
        if (facts.HasDispatchItem)
            return ("Processing", "En procesamiento");
        return ("Created", "Creada");
    }

    private static (string Code, string Label) ResolveInitialResult(OutgoingTransactionMonitoringFacts facts)
    {
        if (facts.HasCertified)
            return ("Certified", "Certificada");
        if (facts.HasAccepted)
            return ("Accepted", "Aceptada");
        if (facts.HasFunctionalRejection)
            return ("Rejected", "Rechazada");
        if (facts.HasSuccessfulIntegration && !facts.HasResponse)
            return ("PendingResponse", "Pendiente de respuesta de la cámara compensadora");
        if (facts.HasSuccessfulIntegration)
            return ("IntegrationSuccessful", "Integración exitosa");
        return ("NotDetermined", "No determinado");
    }
}
