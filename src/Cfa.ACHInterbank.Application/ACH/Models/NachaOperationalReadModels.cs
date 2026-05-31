namespace Cfa.ACHInterbank.Application.ACH.Models;

public sealed record NachaOperationalSummaryReadModel
{
    public required string ProductiveStatus { get; init; }
    public required string BackendPhase { get; init; }
    public required string SoapMode { get; init; }
    public bool ProductiveExecution { get; init; }
    public bool WouldInvokeRealSoap { get; init; }
    public int TotalFiles { get; init; }
    public int TotalIncomingFiles { get; init; }
    public int TotalOutgoingFiles { get; init; }
    public int TotalReturnFiles { get; init; }
    public int TotalDecisions { get; init; }
    public int TotalSoapCandidates { get; init; }
    public int TotalNoGoBlocks { get; init; }
    public int TotalManualReview { get; init; }
    public int TotalReadinessChecks { get; init; }
    public DateTimeOffset LastUpdatedAt { get; init; }
    public bool IsDemoData { get; init; }
    public bool IsPartialData { get; init; }
    public string DataSource { get; init; } = "demo-safe";
    public IReadOnlyList<string> Warnings { get; init; } = [];
}

public sealed record NachaOperationalFileReadModel
{
    public required string FileId { get; init; }
    public required string FileName { get; init; }
    public string DataSource { get; init; } = "demo-safe";
    public string? HeaderId { get; init; }
    public int PersistedRecordCount { get; init; }
    public DateTimeOffset? LastParsedAt { get; init; }
    public bool NoSensitiveData { get; init; } = true;
    public required string ClearingHouseCode { get; init; }
    public required string ProfileCode { get; init; }
    public required string FlowType { get; init; }
    public bool IsReturnFile { get; init; }
    public bool ValidationPassed { get; init; }
    public int BatchCount { get; init; }
    public int EntryCount { get; init; }
    public int AddendaCount { get; init; }
    public int BatchControlCount { get; init; }
    public int FileControlCount { get; init; }
    public required string ProcessingStatus { get; init; }
    public DateTimeOffset? ReceivedAt { get; init; }
    public DateTimeOffset CreatedAt { get; init; }
    public required string CorrelationId { get; init; }
    public bool HasErrors { get; init; }
    public int WarningCount { get; init; }
    public int ErrorCount { get; init; }
}

public sealed record NachaOperationalDecisionReadModel
{
    public required string CorrelationId { get; init; }
    public required string FileName { get; init; }
    public required string EntryTraceNumber { get; init; }
    public string? OriginalTraceNumber { get; init; }
    public required string DecisionType { get; init; }
    public required string SoapOperationCandidate { get; init; }
    public bool RequiresMonetaryMovement { get; init; }
    public required string ReasonCode { get; init; }
    public required string ReasonDescription { get; init; }
    public required string NewInternalStatus { get; init; }
    public bool ManualReviewRequired { get; init; }
    public bool IsBlocked { get; init; }
    public string? BlockReason { get; init; }
    public string DataSource { get; init; } = "demo-safe";
    public bool IsDerived { get; init; }
    public bool IsPersisted { get; init; }
    public string? Warning { get; init; }
    public DateTimeOffset CreatedAt { get; init; }
}

public sealed record NachaSoapReadinessReadModel
{
    public required string CorrelationId { get; init; }
    public required string OperationCandidate { get; init; }
    public bool IsReadyForUat { get; init; }
    public bool IsBlocked { get; init; }
    public IReadOnlyList<string> BlockReasons { get; init; } = [];
    public bool PayloadMappingPassed { get; init; }
    public bool RequestMappingPassed { get; init; }
    public bool OperationalGatePassed { get; init; }
    public bool ReadinessCheckPassed { get; init; }
    public bool SimulationPassed { get; init; }
    public bool ResiliencePassed { get; init; }
    public bool WouldInvokeRealSoap { get; init; }
    public bool ProductiveExecution { get; init; }
    public bool RequiresMonetaryMovement { get; init; }
    public required string Phase { get; init; }
    public string DataSource { get; init; } = "demo-safe";
    public bool IsDerived { get; init; }
    public bool IsPersisted { get; init; }
    public string? Warning { get; init; }
    public DateTimeOffset LastCheckedAt { get; init; }
}

public sealed record NachaOperationalAuditReadModel
{
    public required string CorrelationId { get; init; }
    public required string Phase { get; init; }
    public required string EventType { get; init; }
    public required string Severity { get; init; }
    public required string Message { get; init; }
    public bool IsBlocked { get; init; }
    public string DataSource { get; init; } = "demo-safe";
    public bool IsDerived { get; init; }
    public bool IsPersisted { get; init; }
    public string? Warning { get; init; }
    public DateTimeOffset Timestamp { get; init; }
    public IReadOnlyDictionary<string, string> SanitizedDetails { get; init; } = new Dictionary<string, string>();
}

public sealed record NachaOperationalFileDetailReadModel
{
    public required string FileId { get; init; }
    public string? HeaderId { get; init; }
    public required string FileName { get; init; }
    public required string ClearingHouseCode { get; init; }
    public required string ProfileCode { get; init; }
    public required string FlowType { get; init; }
    public bool IsReturnFile { get; init; }
    public required string ProcessingStatus { get; init; }
    public bool ValidationPassed { get; init; }
    public DateTimeOffset? ReceivedAt { get; init; }
    public DateTimeOffset CreatedAt { get; init; }
    public required string CorrelationId { get; init; }
    public required string DataSource { get; init; }
    public bool IsPartialData { get; init; }
    public IReadOnlyList<string> Warnings { get; init; } = [];
    public NachaOperationalHeaderReadModel? Header { get; init; }
    public IReadOnlyList<NachaOperationalBatchHeaderReadModel> Batches { get; init; } = [];
    public IReadOnlyList<NachaOperationalEntryDetailReadModel> Entries { get; init; } = [];
    public IReadOnlyList<NachaOperationalAddendaRecordReadModel> Addendas { get; init; } = [];
    public IReadOnlyList<NachaOperationalBatchControlReadModel> BatchControls { get; init; } = [];
    public IReadOnlyList<NachaOperationalFileControlReadModel> FileControls { get; init; } = [];
    public required NachaOperationalTotalsSummaryReadModel TotalsSummary { get; init; }
    public bool NoSensitiveData { get; init; } = true;
}

public sealed record NachaOperationalHeaderReadModel
{
    public string? HeaderId { get; init; }
    public string? PriorityCode { get; init; }
    public string? ImmediateDestination { get; init; }
    public string? ImmediateOrigin { get; init; }
    public string? FileCreationDate { get; init; }
    public string? FileCreationTime { get; init; }
    public string? FileIdModifier { get; init; }
    public string? RecordSize { get; init; }
    public string? BlockingFactor { get; init; }
    public string? FormatCode { get; init; }
    public string? ReferenceCode { get; init; }
    public int CycleNumber { get; init; }
}

public sealed record NachaOperationalBatchHeaderReadModel
{
    public int BatchId { get; init; }
    public string? ServiceClassCode { get; init; }
    public string? CompanyName { get; init; }
    public string? StandardEntryClassCode { get; init; }
    public string? CompanyEntryDescription { get; init; }
    public string? EffectiveEntryDate { get; init; }
    public int BatchNumber { get; init; }
}

public sealed record NachaOperationalEntryDetailReadModel
{
    public int EntryDetailId { get; init; }
    public string? TransactionCode { get; init; }
    public string? ReceivingParticipantEntityCode { get; init; }
    public string? CheckDigit { get; init; }
    public string? AccountNumberMasked { get; init; }
    public decimal? Amount { get; init; }
    public string? RecipIdNumberMasked { get; init; }
    public string? RecipUserNameMasked { get; init; }
    public string? AddendumIndicator { get; init; }
    public string? SequenceNumberMasked { get; init; }
}

public sealed record NachaOperationalAddendaRecordReadModel
{
    public int AddendaId { get; init; }
    public string? CodeTypeAddendumRecord { get; init; }
    public string? BusinessType { get; init; }
    public string? PurposeOfTransaction { get; init; }
    public string? InvoiceOrAccountNumberMasked { get; init; }
    public string? InfoFromOriginator { get; init; }
    public string? ReturnReasonCode { get; init; }
    public string? OriginalTraceNumberMasked { get; init; }
    public string? NewTraceNumberMasked { get; init; }
    public string? AddendumSequence { get; init; }
    public string? EntryDetailSequenceNumberMasked { get; init; }
}

public sealed record NachaOperationalBatchControlReadModel
{
    public int BatchControlId { get; init; }
    public string? BatchTranClassCode { get; init; }
    public int? EntryAddendaCount { get; init; }
    public long? EntryHash { get; init; }
    public decimal TotalDebitAmount { get; init; }
    public decimal TotalCreditAmount { get; init; }
    public string? BatchNumber { get; init; }
}

public sealed record NachaOperationalFileControlReadModel
{
    public int FileControlId { get; init; }
    public int BatchCount { get; init; }
    public int BlockCount { get; init; }
    public int EntryAddendaCount { get; init; }
    public long EntryHash { get; init; }
    public decimal TotalDebitAmount { get; init; }
    public decimal TotalCreditAmount { get; init; }
}

public sealed record NachaOperationalTotalsSummaryReadModel
{
    public int BatchCount { get; init; }
    public int EntryCount { get; init; }
    public int AddendaCount { get; init; }
    public int BatchControlCount { get; init; }
    public int FileControlCount { get; init; }
    public int PersistedRecordCount { get; init; }
    public decimal TotalDebitAmount { get; init; }
    public decimal TotalCreditAmount { get; init; }
    public bool ValidationPassed { get; init; }
}

public sealed record NachaOperationalDashboardReadModel
{
    public required NachaOperationalSummaryReadModel Summary { get; init; }
    public IReadOnlyList<NachaOperationalFileReadModel> Files { get; init; } = [];
    public IReadOnlyList<NachaOperationalDecisionReadModel> Decisions { get; init; } = [];
    public IReadOnlyList<NachaSoapReadinessReadModel> Readiness { get; init; } = [];
    public IReadOnlyList<NachaOperationalAuditReadModel> Audit { get; init; } = [];
    public DateTimeOffset GeneratedAt { get; init; }
    public bool IsDemoData { get; init; }
    public bool IsPartialData { get; init; }
    public string DataSource { get; init; } = "demo-safe";
    public IReadOnlyList<string> Warnings { get; init; } = [];
    public required string ProductiveStatus { get; init; }
}
