using Cfa.ACHInterbank.Application.ACH.Interfaces;
using Cfa.ACHInterbank.Application.ACH.Models;

namespace Cfa.ACHInterbank.Persistence.ACH.Services.Implementation;

public sealed class NachaSoapUatConsoleReadModelService : INachaSoapUatConsoleReadModelService
{
    private const string PersistedSource = "backend read-only";
    private const string PartialSource = "parcial";

    private readonly INachaOperationalReadStore _readStore;

    public NachaSoapUatConsoleReadModelService(INachaOperationalReadStore readStore)
    {
        _readStore = readStore;
    }

    public async Task<NachaSoapUatConsoleDashboardReadModel> GetDashboardAsync(CancellationToken cancellationToken = default)
    {
        var candidates = await GetCandidatesAsync(cancellationToken);
        var audit = await GetAuditAsync(cancellationToken);
        var warnings = BuildWarnings(candidates, audit).ToArray();

        return new NachaSoapUatConsoleDashboardReadModel
        {
            ProductiveStatus = "NO-GO",
            ProductiveExecution = false,
            WouldInvokeRealSoap = false,
            TotalCandidates = candidates.Count,
            TotalReadyForUat = candidates.Count(x => x.IsReadyForUat),
            TotalBlocked = candidates.Count(x => x.IsBlocked),
            TotalManualReview = candidates.Count(x => x.ManualReviewRequired),
            TotalRegistrarRespuesta = candidates.Count(x => x.OperationCandidate == "RegistrarRespuestaTransaccion"),
            TotalProcTransacciones = candidates.Count(x => x.OperationCandidate == "ProcTransacciones"),
            TotalProcContrapartidas = candidates.Count(x => x.OperationCandidate == "ProcContrapartidas"),
            TotalNone = candidates.Count(x => x.OperationCandidate == "None"),
            TotalSimulationPassed = candidates.Count(x => x.SimulationStatus == "Passed"),
            TotalSimulationFailed = candidates.Count(x => x.SimulationStatus == "Failed"),
            TotalResilienceWarnings = candidates.Count(x => x.ResilienceStatus == "Warning"),
            TotalDuplicateOrIdempotent = candidates.Count(x => x.IdempotencyStatus is "Duplicate" or "Idempotent"),
            LastUpdatedAt = candidates.Select(x => x.LastAttemptAt ?? DateTimeOffset.MinValue).DefaultIfEmpty(DateTimeOffset.UtcNow).Max(),
            DataSource = warnings.Any(x => x.Contains("parcial", StringComparison.OrdinalIgnoreCase)) ? PartialSource : PersistedSource,
            IsPartialData = warnings.Any(x => x.Contains("parcial", StringComparison.OrdinalIgnoreCase)),
            Warnings = warnings
        };
    }

    public async Task<IReadOnlyList<NachaSoapUatCandidateReadModel>> GetCandidatesAsync(CancellationToken cancellationToken = default)
    {
        var files = await _readStore.GetOperationalFilesAsync(cancellationToken);
        var decisions = await _readStore.GetOperationalDecisionsAsync(cancellationToken);
        var readiness = await _readStore.GetSoapReadinessAsync(cancellationToken);
        var candidates = decisions.Select(decision =>
        {
            var ready = readiness.FirstOrDefault(x => string.Equals(x.CorrelationId, decision.CorrelationId, StringComparison.OrdinalIgnoreCase));
            var fileId = files.FirstOrDefault(x => string.Equals(x.FileName, decision.FileName, StringComparison.OrdinalIgnoreCase))?.FileId;
            return ProjectCandidate(decision, ready, fileId);
        }).ToList();

        foreach (var ready in readiness.Where(x => candidates.All(c => !string.Equals(c.CorrelationId, x.CorrelationId, StringComparison.OrdinalIgnoreCase))))
        {
            candidates.Add(ProjectReadinessOnlyCandidate(ready));
        }

        return candidates
            .OrderByDescending(x => x.LastAttemptAt ?? DateTimeOffset.MinValue)
            .ThenBy(x => x.OperationCandidate, StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    public async Task<NachaSoapUatCandidateReadModel?> GetCandidateAsync(string correlationId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(correlationId))
        {
            return null;
        }

        var candidates = await GetCandidatesAsync(cancellationToken);
        return candidates.FirstOrDefault(x => string.Equals(x.CorrelationId, correlationId, StringComparison.OrdinalIgnoreCase));
    }

    public async Task<IReadOnlyList<NachaSoapUatAuditReadModel>> GetAuditAsync(CancellationToken cancellationToken = default)
    {
        var audit = await _readStore.GetOperationalAuditAsync(cancellationToken);
        return audit
            .Where(x => x.Phase == "6B.5" || x.EventType.Contains("Integration", StringComparison.OrdinalIgnoreCase) || x.EventType.Contains("Soap", StringComparison.OrdinalIgnoreCase))
            .Select(x => new NachaSoapUatAuditReadModel
            {
                CorrelationId = x.CorrelationId,
                Phase = x.Phase,
                EventType = x.EventType,
                Severity = x.Severity,
                Message = SanitizeMessage(x.Message),
                IsBlocked = x.IsBlocked,
                Timestamp = x.Timestamp,
                SanitizedDetails = SanitizeDetails(x.SanitizedDetails),
                DataSource = x.DataSource,
                IsPersisted = x.IsPersisted
            })
            .ToArray();
    }

    private static NachaSoapUatCandidateReadModel ProjectCandidate(
        NachaOperationalDecisionReadModel decision,
        NachaSoapReadinessReadModel? readiness,
        string? fileId)
    {
        var operation = NormalizeOperation(decision.SoapOperationCandidate);
        var manualReview = decision.ManualReviewRequired || decision.DecisionType.Contains("Manual", StringComparison.OrdinalIgnoreCase);
        var blocked = decision.IsBlocked || readiness?.IsBlocked == true || manualReview || operation is "ProcTransacciones" or "ProcContrapartidas";
        var blockReasons = BuildBlockReasons(decision, readiness, operation, manualReview).ToArray();

        return new NachaSoapUatCandidateReadModel
        {
            CorrelationId = decision.CorrelationId,
            FileId = fileId,
            FileName = decision.FileName,
            EntryTraceNumber = decision.EntryTraceNumber,
            DecisionType = decision.DecisionType,
            OperationCandidate = operation,
            RequiresMonetaryMovement = operation is "ProcTransacciones" or "ProcContrapartidas",
            ProductiveExecution = false,
            WouldInvokeRealSoap = false,
            IsReadyForUat = readiness?.IsReadyForUat == true && !blocked,
            IsBlocked = blocked,
            BlockReasons = blockReasons,
            ManualReviewRequired = manualReview,
            ReadinessStatus = ResolveReadinessStatus(readiness, blocked),
            SimulationStatus = readiness is null ? "NotPersisted" : readiness.SimulationPassed ? "Passed" : "Failed",
            ResilienceStatus = readiness is null ? "NotPersisted" : readiness.ResiliencePassed ? "Passed" : "Warning",
            IdempotencyStatus = decision.NewInternalStatus.Contains("duplic", StringComparison.OrdinalIgnoreCase) ? "Duplicate" : "Idempotent",
            LastAttemptAt = readiness?.LastCheckedAt ?? decision.CreatedAt,
            AttemptCount = readiness is null ? 0 : 1,
            DataSource = decision.DataSource,
            IsPersisted = decision.IsPersisted || readiness?.IsPersisted == true,
            IsDerived = true,
            Warning = readiness is null ? "Readiness SOAP/UAT no persistido para esta decision; candidato derivado read-only." : "Candidato SOAP/UAT read-only; no ejecuta SOAP."
        };
    }

    private static NachaSoapUatCandidateReadModel ProjectReadinessOnlyCandidate(NachaSoapReadinessReadModel readiness)
    {
        var operation = NormalizeOperation(readiness.OperationCandidate);
        var blocked = readiness.IsBlocked || operation is "ProcTransacciones" or "ProcContrapartidas";

        return new NachaSoapUatCandidateReadModel
        {
            CorrelationId = readiness.CorrelationId,
            FileId = null,
            FileName = "N/A",
            EntryTraceNumber = "N/A",
            DecisionType = "ReadinessOnly",
            OperationCandidate = operation,
            RequiresMonetaryMovement = operation is "ProcTransacciones" or "ProcContrapartidas",
            ProductiveExecution = false,
            WouldInvokeRealSoap = false,
            IsReadyForUat = readiness.IsReadyForUat && !blocked,
            IsBlocked = blocked,
            BlockReasons = readiness.BlockReasons.Count > 0 ? readiness.BlockReasons : ["Productivo NO-GO; candidato SOAP solo lectura."],
            ManualReviewRequired = false,
            ReadinessStatus = ResolveReadinessStatus(readiness, blocked),
            SimulationStatus = readiness.SimulationPassed ? "Passed" : "Failed",
            ResilienceStatus = readiness.ResiliencePassed ? "Passed" : "Warning",
            IdempotencyStatus = "Idempotent",
            LastAttemptAt = readiness.LastCheckedAt,
            AttemptCount = 1,
            DataSource = readiness.DataSource,
            IsPersisted = readiness.IsPersisted,
            IsDerived = true,
            Warning = "Readiness persistido sin decision asociada en consola; dato parcial read-only."
        };
    }

    private static IEnumerable<string> BuildBlockReasons(
        NachaOperationalDecisionReadModel decision,
        NachaSoapReadinessReadModel? readiness,
        string operation,
        bool manualReview)
    {
        if (manualReview)
        {
            yield return "ManualReviewRequired; no se ejecuta SOAP.";
        }

        if (operation is "ProcTransacciones" or "ProcContrapartidas")
        {
            yield return "Candidato monetario bloqueado por Productivo NO-GO.";
        }

        if (!string.IsNullOrWhiteSpace(decision.BlockReason))
        {
            yield return decision.BlockReason;
        }

        foreach (var reason in readiness?.BlockReasons ?? [])
        {
            yield return reason;
        }

        yield return "WouldInvokeRealSoap=false; ProductiveExecution=false.";
    }

    private static string ResolveReadinessStatus(NachaSoapReadinessReadModel? readiness, bool blocked)
    {
        if (blocked)
        {
            return "BlockedByNoGo";
        }

        if (readiness is null)
        {
            return "Partial";
        }

        return readiness.IsReadyForUat ? "ReadyUat" : "NotReady";
    }

    private static IEnumerable<string> BuildWarnings(
        IReadOnlyList<NachaSoapUatCandidateReadModel> candidates,
        IReadOnlyList<NachaSoapUatAuditReadModel> audit)
    {
        if (candidates.Count == 0)
        {
            yield return "No persisted SOAP/UAT candidates found; consola parcial read-only.";
        }

        if (audit.Count == 0)
        {
            yield return "No persisted SOAP/UAT audit found; auditoria parcial read-only.";
        }

        if (candidates.Any(x => x.Warning?.Contains("parcial", StringComparison.OrdinalIgnoreCase) == true || x.Warning?.Contains("no persistido", StringComparison.OrdinalIgnoreCase) == true))
        {
            yield return "Existen candidatos derivados sin persistencia SOAP/UAT completa; revisar warnings parciales.";
        }

        yield return "Productivo permanece NO-GO; SOAP real deshabilitado.";
    }

    private static IReadOnlyDictionary<string, string> SanitizeDetails(IReadOnlyDictionary<string, string> details)
    {
        var sanitized = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var (key, value) in details)
        {
            sanitized[SanitizeText(key, "Detail", 80)] = IsSensitiveKey(key) ? "Sanitized" : SanitizeText(value, "Sanitized", 120);
        }

        sanitized["ProductiveExecution"] = "false";
        sanitized["WouldInvokeRealSoap"] = "false";
        return sanitized;
    }

    private static bool IsSensitiveKey(string key)
        => key.Contains("payload", StringComparison.OrdinalIgnoreCase)
            || key.Contains("xml", StringComparison.OrdinalIgnoreCase)
            || key.Contains("endpoint", StringComparison.OrdinalIgnoreCase)
            || key.Contains("certificate", StringComparison.OrdinalIgnoreCase)
            || key.Contains("secret", StringComparison.OrdinalIgnoreCase)
            || key.Contains("password", StringComparison.OrdinalIgnoreCase)
            || key.Contains("token", StringComparison.OrdinalIgnoreCase);

    private static string NormalizeOperation(string? operation)
        => operation switch
        {
            "Proc_Transacciones" => "ProcTransacciones",
            "Proc_Contrapartidas" => "ProcContrapartidas",
            null or "" => "None",
            _ => operation
        };

    private static string SanitizeMessage(string? value) => SanitizeText(value, "Evento SOAP/UAT sanitizado.", 180);

    private static string SanitizeText(string? value, string fallback, int maxLength)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return fallback;
        }

        var sanitized = value.Replace("\r", " ", StringComparison.Ordinal).Replace("\n", " ", StringComparison.Ordinal).Trim();
        return sanitized.Length > maxLength ? sanitized[..maxLength] : sanitized;
    }
}
