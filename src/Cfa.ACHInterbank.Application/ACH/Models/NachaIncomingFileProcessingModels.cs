namespace Cfa.ACHInterbank.Application.ACH.Models;

public sealed class NachaIncomingFileRequest
{
    public required string FileName { get; init; }
    public string? Content { get; init; }
    public byte[]? ContentBytes { get; init; }
    public string ClearingHouseCode { get; init; } = string.Empty;
    public DateTime? ReceivedAt { get; init; }
    public string Source { get; init; } = "FunctionalValidation";
    public string CorrelationId { get; init; } = string.Empty;
    public bool IsSimulation { get; init; } = true;
    public string UploadedBy { get; init; } = "system";
    public string? ExpectedProfileCode { get; init; }
}

public sealed class NachaIncomingFileProcessingResult
{
    public string CorrelationId { get; init; } = string.Empty;
    public string FileName { get; init; } = string.Empty;
    public string ClearingHouseCode { get; init; } = string.Empty;
    public string ProfileCode { get; init; } = string.Empty;
    public NachaIncomingFlowType FlowType { get; init; } = NachaIncomingFlowType.Unknown;
    public bool IsReturnFile { get; init; }
    public bool IsDuplicate { get; init; }
    public string? ParsedHeaderId { get; init; }
    public int BatchCount { get; init; }
    public int EntryCount { get; init; }
    public int AddendaCount { get; init; }
    public int BatchControlCount { get; init; }
    public int FileControlCount { get; init; }
    public bool ValidationPassed { get; init; }
    public bool PersistencePassed { get; init; }
    public Guid? IngestionId { get; init; }
    public IReadOnlyList<NachaIncomingDecision> Decisions { get; init; } = [];
    public IReadOnlyList<string> Errors { get; init; } = [];
    public IReadOnlyList<string> Warnings { get; init; } = [];
    public IReadOnlyDictionary<string, string> Trace { get; init; } = new Dictionary<string, string>();
}

public sealed class NachaIncomingDecision
{
    public string EntryTraceNumber { get; init; } = string.Empty;
    public string? OriginalTraceNumber { get; init; }
    public int? TransactionId { get; init; }
    public int? PrenotificationId { get; init; }
    public NachaIncomingDecisionType DecisionType { get; init; } = NachaIncomingDecisionType.ManualReviewRequired;
    public bool RequiresMonetaryMovement { get; init; }
    public NachaSoapOperationCandidate SoapOperation { get; init; } = NachaSoapOperationCandidate.None;
    public string? ReasonCode { get; init; }
    public string ReasonDescription { get; init; } = string.Empty;
    public string NewInternalStatus { get; init; } = string.Empty;
    public string AuditMessage { get; init; } = string.Empty;
}

public enum NachaIncomingFlowType
{
    IncomingCreditFromExternalOriginator = 1,
    IncomingDebitFromExternalOriginator = 2,
    ReturnFile = 3,
    PrenotificationResponse = 4,
    DifferentialResponse = 5,
    Unknown = 6
}

public enum NachaIncomingDecisionType
{
    ApplyCreditMovement = 1,
    ApplyDebitMovement = 2,
    RegisterDifferentialResponse = 3,
    ApprovePrenotification = 4,
    RejectPrenotification = 5,
    MarkTransactionRejected = 6,
    MarkTransactionAccepted = 7,
    IgnoreDuplicate = 8,
    ManualReviewRequired = 9
}

public enum NachaSoapOperationCandidate
{
    None = 0,
    ProcContrapartidas = 1,
    ProcTransacciones = 2,
    RegistrarRespuestaTransaccion = 3
}
