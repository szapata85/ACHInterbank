using Cfa.ACHInterbank.Application.ACH.Interfaces;
using Cfa.ACHInterbank.Application.ACH.Models;
using Cfa.ACHInterbank.Application.ACH.Services;
using Cfa.ACHInterbank.Domain.Entities.Integrations;
using Cfa.ACHInterbank.Domain.Entities.Transactions.Enums;
using Cfa.ACHInterbank.Domain.Models.ACH;
using Cfa.ACHInterbank.Domain.Models.ACH.Enums;
using Cfa.ACHInterbank.Persistence.DataBase;
using Microsoft.EntityFrameworkCore;

namespace Cfa.ACHInterbank.Persistence.ACH.Services.Implementation;

/// <summary>
/// I02-CENIT: lee resultados REALES de aplicación del receptor; no se suscribe al procesamiento,
/// no modifica entidades, no crea eventos/candidatos persistidos y nunca construye ni envía NACHA-M.
/// Una causal verificada es solo un indicio: sesión, fecha valor y autorización son I03/I04.
/// </summary>
public sealed class CenitPassiveReturnDetector(
    AchDbContext context, ICenitAutomaticReturnSettingsService settings) : ICenitPassiveReturnDetector
{
    private const int ScanWindow = 500;
    private const string Proven = "INDICIO_CAUSAL_CENIT";
    private const string ReviewCause = "REVISION_CAUSAL";
    private const string ReviewRequest = "REVISION_SOLICITUD";
    private const string ReviewPrenote = "REVISION_PRENOTIFICACION";

    public async Task<CenitPassiveReturnDetection> DetectAsync(
        int maxTransactions = 100, CancellationToken cancellationToken = default)
    {
        if (maxTransactions is < 1 or > 100)
            throw new ArgumentOutOfRangeException(nameof(maxTransactions), "El límite permitido es de 1 a 100.");

        // Reutiliza la comprobación de fila exclusiva CENIT de I01, también si el modo es DESACTIVADO.
        // DESACTIVADO no impide la consulta humana de evidencia ya persistida.
        var configuration = await settings.GetAsync(cancellationToken);
        var cenitId = await context.ClearingHouses.AsNoTracking()
            .Where(x => x.Code == RegulatoryCycleScheduleCatalog.CenitCode)
            .Select(x => x.Id).SingleAsync(cancellationToken);

        // Evaluar la ÚLTIMA ejecución por transacción, incluso si fue exitosa o técnica.
        // Filtrar primero por rechazos permitiría reportar fallos históricos ya resueltos.
        var attempts = await context.IncomingNachaIntegrationExecution.AsNoTracking()
            .Where(x => x.ClearingHouseId == cenitId
                && x.DispatchQueue.ClearingHouseId == cenitId
                && x.DispatchQueue.AchTransactionId.HasValue)
            .Include(x => x.DispatchQueue).ThenInclude(q => q.TransactionLink)
            .Include(x => x.DispatchQueue).ThenInclude(q => q.Classification)
            .Include(x => x.DispatchQueue).ThenInclude(q => q.AchTransaction)
                .ThenInclude(tx => tx!.AchCycle).ThenInclude(c => c!.ClearingHouse)
            .OrderByDescending(x => x.StartedAtUtc).ThenByDescending(x => x.Id)
            .Take(ScanWindow).ToListAsync(cancellationToken);

        var returnCodes = await context.AchReturnCodes.AsNoTracking()
            .Where(x => x.ClearingHouseId == cenitId && x.IsActive)
            .ToListAsync(cancellationToken);
        var inspectedIds = attempts.Select(x => x.DispatchQueue.AchTransactionId!.Value).Distinct().ToArray();
        var alreadyReturned = await context.AchReturnsGenerated.AsNoTracking()
            .Where(x => inspectedIds.Contains(x.OriginalTransactionId))
            .Select(x => x.OriginalTransactionId).ToListAsync(cancellationToken);
        var returnedIds = alreadyReturned.ToHashSet();
        var visited = new HashSet<int>();
        var findings = new List<CenitPassiveReturnFinding>();
        foreach (var attempt in attempts)
        {
            var id = attempt.DispatchQueue?.AchTransactionId;
            if (!id.HasValue || !visited.Add(id.Value)) continue;
            if (visited.Count > maxTransactions) break;
            if (returnedIds.Contains(id.Value)) continue;
            var finding = Evaluate(attempt, cenitId, returnCodes);
            if (finding is not null) findings.Add(finding);
        }

        return new CenitPassiveReturnDetection(configuration.Mode, ProcessingConnected: false,
            ExaminedTransactions: Math.Min(visited.Count, maxTransactions),
            LimitedWindow: attempts.Count == ScanWindow || visited.Count > maxTransactions,
            Findings: findings);
    }

    /// <summary>Reglas puras testeables con evidencia de DB; excluyen fallos de transporte y respuestas históricas.</summary>
    internal static CenitPassiveReturnFinding? Evaluate(IncomingNachaIntegrationExecution attempt,
        int cenitId, IReadOnlyList<AchReturnCode> codes)
    {
        var queue = attempt.DispatchQueue;
        var tx = queue?.AchTransaction;
        var link = queue?.TransactionLink;
        var classification = queue?.Classification;
        var cycle = tx?.AchCycle;
        if (queue is null || tx is null || link is null || cycle is null || classification is null
            || queue.ClearingHouseId != cenitId || attempt.ClearingHouseId != cenitId
            || cycle.ClearingHouseId != cenitId
            || cycle.ClearingHouse?.Code != RegulatoryCycleScheduleCatalog.CenitCode
            || queue.SourceKind != IncomingNachaDispatchSourceKinds.CycleBound
            || queue.QueueStatus != IncomingNachaDispatchQueueStatus.FailedFinal
            || queue.LastErrorCode != "IFUNC"
            || classification.Id != queue.IncomingNachaEntryClassificationId
            || classification.IncomingNachaFileIngestionId != queue.IncomingNachaFileIngestionId
            || classification.RequiresManualResolution
            || classification.EligibilityStatus != IncomingNachaEligibilityStatus.Elegible
            || (tx.Type == TransactionTypeEnum.Credit && classification.FunctionalClass != IncomingNachaFunctionalClass.CreditoEntrante
                || tx.Type == TransactionTypeEnum.Debit && classification.FunctionalClass != IncomingNachaFunctionalClass.DebitoEntrante
                || tx.Type == TransactionTypeEnum.Prenotification && classification.FunctionalClass != IncomingNachaFunctionalClass.Prenotificacion)
            || queue.AchTransactionId != tx.Id || tx.Id <= 0
            || queue.AchCycleId != cycle.Id || tx.AchCycleId != cycle.Id
            || queue.IncomingNachaTransactionLinkId != link.Id
            || !link.IsFinal || link.AchTransactionId != tx.Id
            || link.IncomingNachaFileIngestionId != queue.IncomingNachaFileIngestionId
            || link.LinkType is IncomingNachaLinkType.Ambiguous or IncomingNachaLinkType.NotFound
                or IncomingNachaLinkType.NoResuelto
            || attempt.DispatchQueueId != queue.Id
            || !attempt.EntryDetailId.HasValue || attempt.EntryDetailId != link.EntryDetailId
            || attempt.SoapMethodName != "Proc_Transacciones"
            || !IncomingNachaPostProcessingMode.IsLive(attempt.ExecutionMode)
            || !attempt.ProcessedAtUtc.HasValue
            || attempt.TransportStatus != IntegrationTransportStatus.Succeeded
            || attempt.BusinessStatus != IntegrationResponseBusinessStatus.Rejected
            || attempt.BusinessOutcome is not (IncomingNachaBusinessOutcome.Rejected or IncomingNachaBusinessOutcome.Returned)
            || !attempt.IsFunctionalRejection || attempt.IsTechnicalFailure || attempt.IsSuccessful || attempt.IsSuccess
            || attempt.RetryAllowed || attempt.RequiresManualReview
            || attempt.ProcessingStatus != IncomingNachaIndividualProcessingStatus.Completed
            || !attempt.ResponseCatalogId.HasValue
            || tx.State is AchTransferStateEnum.ReturnedByEpr or AchTransferStateEnum.ReturnedByOperator
            || tx.Direction != AchTransactionDirection.Incoming
            || tx.Origin != AchTransactionOrigin.ExternalInstitution
            || tx.ClassificationStatus != AchTransactionClassificationStatus.Determined
            || tx.MonetaryIntegrationRoute != AchMonetaryIntegrationRoute.ProcTransacciones
            || tx.Type is not (TransactionTypeEnum.Credit or TransactionTypeEnum.Debit or TransactionTypeEnum.Prenotification))
            return null;

        var code = (attempt.ResultCode ?? string.Empty).Trim().ToUpperInvariant();
        var isRxx = code.Length == 3 && code[0] == 'R'
            && char.IsAsciiDigit(code[1]) && char.IsAsciiDigit(code[2]);
        var cause = isRxx ? CenitIncomingReturnPolicy.CauseDefinitions.SingleOrDefault(x => x.Code == code) : null;
        var mapped = cause is not null && attempt.AchReturnCodeId.HasValue
            && codes.Any(x => x.Id == attempt.AchReturnCodeId && x.ClearingHouseId == cenitId
                && x.IsActive && x.Code == code && x.FlowType == AchReturnFlowType.Any
                && x.EffectiveFrom.Date <= attempt.ProcessedAtUtc.Value.Date
                && (!x.EffectiveTo.HasValue || x.EffectiveTo.Value.Date >= attempt.ProcessedAtUtc.Value.Date)
                && (tx.Type == TransactionTypeEnum.Credit && x.AppliesToCredit
                    || tx.Type == TransactionTypeEnum.Debit && x.AppliesToDebit
                    || tx.Type == TransactionTypeEnum.Prenotification && x.AppliesToPrenotification));
        var applicable = cause is not null && (
            tx.Type == TransactionTypeEnum.Credit && cause.AppliesToCreditMonetary
            || tx.Type == TransactionTypeEnum.Debit && cause.AppliesToDebitMonetary);

        // R06/R23 requieren solicitud humana acreditada; no se deducen del resultado SOAP.
        // Prenotificaciones requieren sentido, adenda y comprobación específica posterior.
        string status;
        string explanation;
        if (!mapped || (tx.Type != TransactionTypeEnum.Prenotification && !applicable))
        {
            status = ReviewCause;
            explanation = "Rechazo funcional confirmado, sin causal CENIT verificable y aplicable; revisar manualmente.";
        }
        else if (code is "R06" or "R23" or "R34")
        {
            status = ReviewRequest;
            explanation = "Causal especial CENIT: falta acreditar solicitud, decisión humana o cancelación manual.";
        }
        else if (tx.Type == TransactionTypeEnum.Prenotification)
        {
            status = ReviewPrenote;
            explanation = "Prenotificación: falta confirmar sentido, adenda y condiciones CENIT.";
        }
        else
        {
            status = Proven;
            explanation = "Indicio de falla funcional con causal CENIT verificable; NO valida plazo, ciclo ni autorización de devolución.";
        }
        return new CenitPassiveReturnFinding(tx.Id, queue.Id, attempt.Id, status,
            mapped && (applicable || tx.Type == TransactionTypeEnum.Prenotification) ? code : null,
            explanation, attempt.ProcessedAtUtc.Value);
    }
}
