using Cfa.ACHInterbank.Application.ACH.Interfaces;
using Cfa.ACHInterbank.Application.ACH.Models;

namespace Cfa.ACHInterbank.Persistence.ACH.Services.Implementation;

public sealed class NachaOperationalReadModelService : INachaOperationalReadModelService
{
    private static readonly DateTimeOffset SnapshotTime = new(2026, 5, 24, 23, 0, 0, TimeSpan.Zero);
    private readonly INachaOperationalReadStore? _readStore;

    public NachaOperationalReadModelService(INachaOperationalReadStore? readStore = null)
    {
        _readStore = readStore;
    }

    public async Task<NachaOperationalDashboardReadModel> GetDashboardAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (_readStore is not null)
        {
            try
            {
                var persisted = await _readStore.GetDashboardAsync(cancellationToken);
                if (persisted.Files.Count > 0)
                {
                    return persisted;
                }
            }
            catch
            {
                return BuildDemoDashboard(["Read-store persisted query failed; using safe read-only demo fallback."]);
            }

            return BuildDemoDashboard(["No persisted NACHA read-store data found; using safe read-only demo fallback."]);
        }

        return BuildDemoDashboard();
    }

    public async Task<NachaOperationalSummaryReadModel> GetSummaryAsync(CancellationToken cancellationToken = default)
        => (await GetDashboardAsync(cancellationToken)).Summary;

    public async Task<IReadOnlyList<NachaOperationalFileReadModel>> GetFilesAsync(CancellationToken cancellationToken = default)
        => (await GetDashboardAsync(cancellationToken)).Files;

    public async Task<NachaOperationalFileDetailReadModel?> GetFileDetailAsync(string fileId, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (string.IsNullOrWhiteSpace(fileId) || fileId.StartsWith("demo-", StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        if (_readStore is null)
        {
            return null;
        }

        try
        {
            return await _readStore.GetOperationalFileDetailAsync(fileId, cancellationToken);
        }
        catch
        {
            return null;
        }
    }

    public async Task<IReadOnlyList<NachaOperationalDecisionReadModel>> GetDecisionsAsync(CancellationToken cancellationToken = default)
        => (await GetDashboardAsync(cancellationToken)).Decisions;

    public async Task<IReadOnlyList<NachaSoapReadinessReadModel>> GetSoapReadinessAsync(CancellationToken cancellationToken = default)
        => (await GetDashboardAsync(cancellationToken)).Readiness;

    public async Task<IReadOnlyList<NachaOperationalAuditReadModel>> GetAuditAsync(CancellationToken cancellationToken = default)
        => (await GetDashboardAsync(cancellationToken)).Audit;

    private static NachaOperationalDashboardReadModel BuildDemoDashboard(IReadOnlyList<string>? extraWarnings = null)
    {
        var files = BuildFiles();
        var decisions = BuildDecisions();
        var readiness = BuildReadiness();
        var audit = BuildAudit();
        var summary = BuildSummary(files, decisions, readiness, extraWarnings ?? []);

        return new NachaOperationalDashboardReadModel
        {
            Summary = summary,
            Files = files,
            Decisions = decisions,
            Readiness = readiness,
            Audit = audit,
            GeneratedAt = SnapshotTime,
            IsDemoData = true,
            IsPartialData = false,
            DataSource = "demo seguro",
            Warnings = summary.Warnings,
            ProductiveStatus = "NO-GO"
        };
    }

    private static NachaOperationalSummaryReadModel BuildSummary(
        IReadOnlyList<NachaOperationalFileReadModel> files,
        IReadOnlyList<NachaOperationalDecisionReadModel> decisions,
        IReadOnlyList<NachaSoapReadinessReadModel> readiness,
        IReadOnlyList<string> extraWarnings)
    {
        var warnings = new List<string>
        {
            "Datos backend demo read-only sanitizados hasta conectar read-store operativo persistido.",
            "Productivo permanece NO-GO y SOAP real esta deshabilitado."
        };
        warnings.AddRange(extraWarnings);

        return new NachaOperationalSummaryReadModel
        {
            ProductiveStatus = "NO-GO",
            BackendPhase = "6B.5.6",
            SoapMode = "Simulated",
            ProductiveExecution = false,
            WouldInvokeRealSoap = false,
            TotalFiles = files.Count,
            TotalIncomingFiles = files.Count(x => !x.IsReturnFile && x.FlowType.Contains("Incoming", StringComparison.OrdinalIgnoreCase)),
            TotalOutgoingFiles = files.Count(x => x.FlowType.Contains("Outgoing", StringComparison.OrdinalIgnoreCase)),
            TotalReturnFiles = files.Count(x => x.IsReturnFile),
            TotalDecisions = decisions.Count,
            TotalSoapCandidates = decisions.Count(x => x.SoapOperationCandidate != "None"),
            TotalNoGoBlocks = readiness.Count(x => x.IsBlocked) + decisions.Count(x => x.IsBlocked),
            TotalManualReview = decisions.Count(x => x.ManualReviewRequired),
            TotalReadinessChecks = readiness.Count,
            LastUpdatedAt = SnapshotTime,
            IsDemoData = true,
            IsPartialData = extraWarnings.Count > 0,
            DataSource = "demo seguro",
            Warnings = warnings
        };
    }

    private static IReadOnlyList<NachaOperationalFileReadModel> BuildFiles() =>
    [
        new()
        {
            FileId = "demo-ach-in-001",
            FileName = "ACH_COL_IN_001.ach",
            ClearingHouseCode = "ACH",
            ProfileCode = AchColOfficialNachaLayout.InboundOriginalProfileCode,
            FlowType = "IncomingCreditFromExternalOriginator",
            IsReturnFile = false,
            ValidationPassed = true,
            BatchCount = 1,
            EntryCount = 2,
            AddendaCount = 1,
            BatchControlCount = 1,
            FileControlCount = 1,
            ProcessingStatus = "Processed",
            ReceivedAt = SnapshotTime.AddHours(-8),
            CreatedAt = SnapshotTime.AddHours(-8),
            CorrelationId = "phase-6c2-ach-in",
            HasErrors = false,
            WarningCount = 0,
            ErrorCount = 0
        },
        new()
        {
            FileId = "demo-cenit-in-001",
            FileName = "CENIT_IN_001.ach",
            ClearingHouseCode = "CENIT",
            ProfileCode = "OFFICIAL_CENIT_ENTRADA_ORIGINAL_V1_0",
            FlowType = "DifferentialResponse",
            IsReturnFile = false,
            ValidationPassed = true,
            BatchCount = 1,
            EntryCount = 1,
            AddendaCount = 1,
            BatchControlCount = 1,
            FileControlCount = 1,
            ProcessingStatus = "Processed",
            ReceivedAt = SnapshotTime.AddHours(-7),
            CreatedAt = SnapshotTime.AddHours(-7),
            CorrelationId = "phase-6c2-cenit-in",
            HasErrors = false,
            WarningCount = 1,
            ErrorCount = 0
        },
        new()
        {
            FileId = "demo-ach-ret-001",
            FileName = "ACH_COL_RET_001.RET",
            ClearingHouseCode = "ACH",
            ProfileCode = "OFFICIAL_ACH_DEVOLUCION_V1_0",
            FlowType = "ReturnFile",
            IsReturnFile = true,
            ValidationPassed = true,
            BatchCount = 1,
            EntryCount = 1,
            AddendaCount = 1,
            BatchControlCount = 1,
            FileControlCount = 1,
            ProcessingStatus = "ManualReviewRequired",
            ReceivedAt = SnapshotTime.AddHours(-6),
            CreatedAt = SnapshotTime.AddHours(-6),
            CorrelationId = "phase-6c2-ach-ret",
            HasErrors = false,
            WarningCount = 1,
            ErrorCount = 0
        }
    ];

    private static IReadOnlyList<NachaOperationalDecisionReadModel> BuildDecisions() =>
    [
        new()
        {
            CorrelationId = "phase-6c2-ach-in",
            FileName = "ACH_COL_IN_001.ach",
            EntryTraceNumber = "900000010000001",
            OriginalTraceNumber = null,
            DecisionType = "ApplyCreditMovement",
            SoapOperationCandidate = "ProcTransacciones",
            RequiresMonetaryMovement = true,
            ReasonCode = "00",
            ReasonDescription = "Decision UAT simulada para credito externo hacia CFA.",
            NewInternalStatus = "Accepted",
            ManualReviewRequired = false,
            IsBlocked = false,
            BlockReason = null,
            CreatedAt = SnapshotTime.AddMinutes(-45)
        },
        new()
        {
            CorrelationId = "phase-6c2-cenit-in",
            FileName = "CENIT_IN_001.ach",
            EntryTraceNumber = "900000020000001",
            OriginalTraceNumber = "800000020000001",
            DecisionType = "RegisterDifferentialResponse",
            SoapOperationCandidate = "RegistrarRespuestaTransaccion",
            RequiresMonetaryMovement = false,
            ReasonCode = "R01",
            ReasonDescription = "Respuesta diferencial sin movimiento monetario.",
            NewInternalStatus = "Rejected",
            ManualReviewRequired = false,
            IsBlocked = false,
            BlockReason = null,
            CreatedAt = SnapshotTime.AddMinutes(-40)
        },
        new()
        {
            CorrelationId = "phase-6c2-ach-ret",
            FileName = "ACH_COL_RET_001.RET",
            EntryTraceNumber = "900000030000001",
            OriginalTraceNumber = "800000030000001",
            DecisionType = "ManualReviewRequired",
            SoapOperationCandidate = "None",
            RequiresMonetaryMovement = false,
            ReasonCode = "MR",
            ReasonDescription = "Referencia original requiere revision manual.",
            NewInternalStatus = "ManualReviewRequired",
            ManualReviewRequired = true,
            IsBlocked = true,
            BlockReason = "Manual review requerido; no se ejecuta SOAP.",
            CreatedAt = SnapshotTime.AddMinutes(-35)
        }
    ];

    private static IReadOnlyList<NachaSoapReadinessReadModel> BuildReadiness() =>
    [
        new()
        {
            CorrelationId = "phase-6c2-ach-in",
            OperationCandidate = "ProcTransacciones",
            IsReadyForUat = true,
            IsBlocked = false,
            BlockReasons = [],
            PayloadMappingPassed = true,
            RequestMappingPassed = true,
            OperationalGatePassed = true,
            ReadinessCheckPassed = true,
            SimulationPassed = true,
            ResiliencePassed = true,
            WouldInvokeRealSoap = false,
            ProductiveExecution = false,
            RequiresMonetaryMovement = true,
            Phase = "6B.5",
            LastCheckedAt = SnapshotTime.AddMinutes(-20)
        },
        new()
        {
            CorrelationId = "phase-6c2-nogo",
            OperationCandidate = "ProcContrapartidas",
            IsReadyForUat = false,
            IsBlocked = true,
            BlockReasons = ["Invocacion SOAP real bloqueada por NO-GO."],
            PayloadMappingPassed = true,
            RequestMappingPassed = true,
            OperationalGatePassed = false,
            ReadinessCheckPassed = false,
            SimulationPassed = false,
            ResiliencePassed = false,
            WouldInvokeRealSoap = false,
            ProductiveExecution = false,
            RequiresMonetaryMovement = true,
            Phase = "6B.5",
            LastCheckedAt = SnapshotTime.AddMinutes(-15)
        }
    ];

    private static IReadOnlyList<NachaOperationalAuditReadModel> BuildAudit() =>
    [
        new()
        {
            CorrelationId = "phase-6c2-ach-in",
            Phase = "6B.5",
            EventType = "ReadinessDashboardProjected",
            Severity = "Information",
            Message = "Read-model operativo NACHA-M generado para consulta read-only.",
            IsBlocked = false,
            Timestamp = SnapshotTime.AddMinutes(-10),
            SanitizedDetails = new Dictionary<string, string>
            {
                ["OperationCandidate"] = "ProcTransacciones",
                ["Productivo"] = "NO-GO",
                ["WouldInvokeRealSoap"] = "false",
                ["Payload"] = "Sanitized"
            }
        },
        new()
        {
            CorrelationId = "phase-6c2-nogo",
            Phase = "6B.5",
            EventType = "BlockedByNoGo",
            Severity = "Warning",
            Message = "La compuerta operacional mantiene bloqueada la invocacion SOAP real.",
            IsBlocked = true,
            Timestamp = SnapshotTime.AddMinutes(-9),
            SanitizedDetails = new Dictionary<string, string>
            {
                ["Productivo"] = "NO-GO",
                ["AllowRealSoapInvocation"] = "false",
                ["SensitiveMaterial"] = "NotPresent"
            }
        }
    ];
}
