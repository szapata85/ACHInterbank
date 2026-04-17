using Cfa.ACHInterbank.Domain.Entities.SchedulerTask.Base;

namespace Cfa.ACHInterbank.Domain.Models.ACH;

public class IncomingNachaFileIngestion : AuditableEntity
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string FileName { get; set; } = string.Empty;
    public string FileHashSha256 { get; set; } = string.Empty;
    public long FileSize { get; set; }
    public string ContentType { get; set; } = string.Empty;
    public DateTime UploadedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? ReceivedAtUtc { get; set; }
    public string UploadedBy { get; set; } = string.Empty;
    public string? ReceivedBy { get; set; }
    public IncomingNachaIngestionStatus IngestionStatus { get; set; } = IncomingNachaIngestionStatus.Recibido;
    public IncomingNachaCycleResolutionStatus CycleResolutionStatus { get; set; } = IncomingNachaCycleResolutionStatus.NoIntentado;
    public IncomingNachaParsingStatus ParsingStatus { get; set; } = IncomingNachaParsingStatus.NoEjecutado;
    public int? DetectedClearingHouseId { get; set; }
    public int? ResolvedClearingHouseId { get; set; }
    public DateTime? OperationalDate { get; set; }
    public string? ResolvedAchCycleId { get; set; }
    public string? ResolutionMode { get; set; }
    public decimal? ResolutionConfidence { get; set; }
    public string ResolutionEvidenceJson { get; set; } = "{}";
    public string? RawStorageReference { get; set; }
    public string CorrelationId { get; set; } = string.Empty;
    public Guid? ParentIngestionId { get; set; }
    public bool IsReprocess { get; set; }
    public string Notes { get; set; } = string.Empty;
    public string WarningsJson { get; set; } = "[]";

    public IncomingNachaFileIngestion? ParentIngestion { get; set; }
    public ICollection<IncomingNachaFileIngestion> ReprocessChildren { get; set; } = new List<IncomingNachaFileIngestion>();
    public ICollection<IncomingNachaFileProcessingResult> ProcessingResults { get; set; } = new List<IncomingNachaFileProcessingResult>();
    public ICollection<IncomingNachaTransactionLink> TransactionLinks { get; set; } = new List<IncomingNachaTransactionLink>();
    public ICollection<NachaHeader> ParsedHeaders { get; set; } = new List<NachaHeader>();
}

public class IncomingNachaFileProcessingResult : AuditableEntity
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid IncomingNachaFileIngestionId { get; set; }
    public int AttemptNumber { get; set; }
    public DateTime StartedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? FinishedAtUtc { get; set; }
    public int TotalBatches { get; set; }
    public int TotalEntries { get; set; }
    public int TotalAddendas { get; set; }
    public int ValidCount { get; set; }
    public int InvalidCount { get; set; }
    public int WarningCount { get; set; }
    public int ErrorCount { get; set; }
    public IncomingNachaProcessingOutcomeStatus OutcomeStatus { get; set; } = IncomingNachaProcessingOutcomeStatus.EnProceso;
    public string FailureStage { get; set; } = string.Empty;
    public string ParserWarningsJson { get; set; } = "[]";
    public string ParserErrorsJson { get; set; } = "[]";
    public bool IsReprocessable { get; set; } = true;

    public IncomingNachaFileIngestion Ingestion { get; set; } = null!;
}

public class IncomingNachaTransactionLink : AuditableEntity
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid IncomingNachaFileIngestionId { get; set; }
    public int? EntryDetailId { get; set; }
    public int? AddendaRecordId { get; set; }
    public int? AchTransactionId { get; set; }
    public IncomingNachaLinkType LinkType { get; set; } = IncomingNachaLinkType.NoResuelto;
    public decimal ConfidenceScore { get; set; }
    public string EvidenceJson { get; set; } = "{}";
    public DateTime LinkedAtUtc { get; set; } = DateTime.UtcNow;
    public string LinkedBy { get; set; } = string.Empty;
    public bool IsFinal { get; set; }

    public IncomingNachaFileIngestion Ingestion { get; set; } = null!;
    public EntryDetail? EntryDetail { get; set; }
    public AddendaRecord? AddendaRecord { get; set; }
    public AchTransaction? AchTransaction { get; set; }
}

public enum IncomingNachaIngestionStatus
{
    Recibido = 1,
    Duplicado = 2,
    EnValidacion = 3,
    PendienteResolucion = 4,
    ListoParaParseo = 5,
    Parseado = 6,
    Bloqueado = 7,
    Fallido = 8,
    Completado = 9
}

public enum IncomingNachaCycleResolutionStatus
{
    NoIntentado = 1,
    ResueltoInferido = 2,
    ResueltoConfirmado = 3,
    ResueltoManual = 4,
    Ambiguo = 5,
    NoResuelto = 6
}

public enum IncomingNachaParsingStatus
{
    NoEjecutado = 1,
    EnProceso = 2,
    Exitoso = 3,
    ExitosoConAdvertencias = 4,
    FallidoReprocesable = 5,
    FallidoNoReprocesable = 6
}

public enum IncomingNachaLinkType
{
    NoResuelto = 1,
    ExactTrace = 2,
    ExactExternalId = 3,
    Manual = 4,
    Ambiguo = 5,
    NoEncontrado = 6
}

public enum IncomingNachaProcessingOutcomeStatus
{
    EnProceso = 1,
    Exitoso = 2,
    ExitosoConAdvertencias = 3,
    Fallido = 4,
    BloqueadoAmbiguo = 5,
    Duplicado = 6
}
