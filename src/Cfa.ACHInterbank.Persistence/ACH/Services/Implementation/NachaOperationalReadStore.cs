using Cfa.ACHInterbank.Application.ACH.Interfaces;
using Cfa.ACHInterbank.Application.ACH.Models;
using Cfa.ACHInterbank.Domain.Models.ACH;
using Cfa.ACHInterbank.Persistence.DataBase;
using Microsoft.EntityFrameworkCore;

namespace Cfa.ACHInterbank.Persistence.ACH.Services.Implementation;

public sealed class NachaOperationalReadStore : INachaOperationalReadStore
{
    private const int MaxFiles = 50;
    private const int MaxRows = 100;
    private const int MaxDetailRows = 200;
    private const string PersistedSource = "backend read-only";
    private const string PartialSource = "parcial";

    private readonly AchDbContext _context;

    public NachaOperationalReadStore(AchDbContext context)
    {
        _context = context;
    }

    public async Task<NachaOperationalDashboardReadModel> GetDashboardAsync(CancellationToken cancellationToken = default)
    {
        var files = await GetOperationalFilesAsync(cancellationToken);
        var decisions = await GetOperationalDecisionsAsync(cancellationToken);
        var readiness = await GetSoapReadinessAsync(cancellationToken);
        var audit = await GetOperationalAuditAsync(cancellationToken);
        var warnings = BuildWarnings(files, decisions, readiness, audit);
        var summary = BuildSummary(files, decisions, readiness, warnings);

        return new NachaOperationalDashboardReadModel
        {
            Summary = summary,
            Files = files,
            Decisions = decisions,
            Readiness = readiness,
            Audit = audit,
            GeneratedAt = summary.LastUpdatedAt,
            IsDemoData = false,
            IsPartialData = warnings.Count > 0,
            DataSource = warnings.Count > 0 ? PartialSource : PersistedSource,
            Warnings = warnings,
            ProductiveStatus = "NO-GO"
        };
    }

    public async Task<NachaOperationalSummaryReadModel> GetOperationalSummaryAsync(CancellationToken cancellationToken = default)
        => (await GetDashboardAsync(cancellationToken)).Summary;

    public async Task<IReadOnlyList<NachaOperationalFileReadModel>> GetOperationalFilesAsync(CancellationToken cancellationToken = default)
    {
        var headers = await _context.NachaHeaders
            .AsNoTracking()
            .Include(x => x.ClearingHouse)
            .Include(x => x.IncomingNachaFileIngestion)
                .ThenInclude(x => x!.ProcessingResults)
            .Include(x => x.Batches)
            .Include(x => x.EntryDetails)
            .Include(x => x.AddendaRecords)
            .Include(x => x.BatchControls)
            .Include(x => x.FileControls)
            .OrderByDescending(x => x.IncomingNachaFileIngestion != null
                ? x.IncomingNachaFileIngestion.ReceivedAtUtc ?? x.IncomingNachaFileIngestion.UploadedAtUtc
                : DateTime.MinValue)
            .ThenByDescending(x => x.NachaID)
            .Take(MaxFiles)
            .ToListAsync(cancellationToken);

        return headers.Select(ProjectFile).ToArray();
    }

    public async Task<NachaOperationalFileDetailReadModel?> GetOperationalFileDetailAsync(string fileId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(fileId))
        {
            return null;
        }

        var headers = await _context.NachaHeaders
            .AsNoTracking()
            .Include(x => x.ClearingHouse)
            .Include(x => x.IncomingNachaFileIngestion)
                .ThenInclude(x => x!.ProcessingResults)
            .Include(x => x.Batches)
            .Include(x => x.EntryDetails)
            .Include(x => x.AddendaRecords)
            .Include(x => x.BatchControls)
            .Include(x => x.FileControls)
            .OrderByDescending(x => x.IncomingNachaFileIngestion != null
                ? x.IncomingNachaFileIngestion.ReceivedAtUtc ?? x.IncomingNachaFileIngestion.UploadedAtUtc
                : DateTime.MinValue)
            .Take(MaxFiles)
            .ToListAsync(cancellationToken);

        var header = headers.FirstOrDefault(x => string.Equals($"nacha-{SafeToken(x.NachaID)}", fileId, StringComparison.OrdinalIgnoreCase));
        return header is null ? null : ProjectFileDetail(header);
    }

    public async Task<IReadOnlyList<NachaOperationalDecisionReadModel>> GetOperationalDecisionsAsync(CancellationToken cancellationToken = default)
    {
        var rows = await _context.IncomingNachaEntryClassifications
            .AsNoTracking()
            .Include(x => x.Ingestion)
            .Include(x => x.EntryDetail)
            .Include(x => x.AddendaRecord)
            .OrderByDescending(x => x.CreatedAt)
            .Take(MaxRows)
            .ToListAsync(cancellationToken);

        return rows.Select(ProjectDecision).ToArray();
    }

    public async Task<IReadOnlyList<NachaSoapReadinessReadModel>> GetSoapReadinessAsync(CancellationToken cancellationToken = default)
    {
        var queues = await _context.IncomingNachaDispatchQueue
            .AsNoTracking()
            .Include(x => x.Executions)
            .OrderByDescending(x => x.LastAttemptAtUtc ?? x.NextAttemptAtUtc ?? x.OperationalDate)
            .Take(MaxRows)
            .ToListAsync(cancellationToken);

        return queues.Select(ProjectReadiness).ToArray();
    }

    public async Task<IReadOnlyList<NachaOperationalAuditReadModel>> GetOperationalAuditAsync(CancellationToken cancellationToken = default)
    {
        var processingEvents = await _context.IncomingNachaProcessingEvents
            .AsNoTracking()
            .Include(x => x.Ingestion)
            .OrderByDescending(x => x.OccurredAtUtc)
            .Take(MaxRows)
            .ToListAsync(cancellationToken);

        var audit = processingEvents.Select(ProjectAudit).ToList();
        if (audit.Count >= MaxRows)
        {
            return audit;
        }

        var executions = await _context.IncomingNachaIntegrationExecution
            .AsNoTracking()
            .OrderByDescending(x => x.FinishedAtUtc ?? x.StartedAtUtc)
            .Take(MaxRows - audit.Count)
            .ToListAsync(cancellationToken);

        audit.AddRange(executions.Select(ProjectExecutionAudit));
        return audit
            .OrderByDescending(x => x.Timestamp)
            .Take(MaxRows)
            .ToArray();
    }

    private static NachaOperationalFileReadModel ProjectFile(NachaHeader header)
    {
        var ingestion = header.IncomingNachaFileIngestion;
        var latestResult = ingestion?.ProcessingResults
            .OrderByDescending(x => x.FinishedAtUtc ?? x.StartedAtUtc)
            .FirstOrDefault();
        var entryCount = header.EntryDetails?.Count ?? latestResult?.TotalEntries ?? 0;
        var addendaCount = header.AddendaRecords?.Count ?? latestResult?.TotalAddendas ?? 0;
        var batchCount = header.Batches?.Count ?? latestResult?.TotalBatches ?? header.FileControls?.Sum(x => x.BatchCount) ?? 0;
        var batchControlCount = header.BatchControls?.Count ?? 0;
        var fileControlCount = header.FileControls?.Count ?? 0;
        var errorCount = latestResult?.ErrorCount ?? 0;
        var warningCount = latestResult?.WarningCount ?? 0;
        var hasErrors = errorCount > 0
            || ingestion?.IngestionStatus is IncomingNachaIngestionStatus.Fallido or IncomingNachaIngestionStatus.Bloqueado
            || ingestion?.ParsingStatus is IncomingNachaParsingStatus.FallidoNoReprocesable or IncomingNachaParsingStatus.FallidoReprocesable
            || latestResult?.OutcomeStatus is IncomingNachaProcessingOutcomeStatus.Fallido or IncomingNachaProcessingOutcomeStatus.BloqueadoAmbiguo;
        var received = ingestion?.ReceivedAtUtc ?? ingestion?.UploadedAtUtc;
        var created = received ?? ParseNachaCreationDate(header) ?? DateTime.UtcNow;
        var safeHeaderId = SafeToken(header.NachaID);

        return new NachaOperationalFileReadModel
        {
            FileId = $"nacha-{safeHeaderId}",
            FileName = SanitizeFileName(ingestion?.FileName, header.NachaID),
            DataSource = PersistedSource,
            HeaderId = safeHeaderId,
            PersistedRecordCount = batchCount + entryCount + addendaCount + batchControlCount + fileControlCount + 1,
            LastParsedAt = ToOffset(latestResult?.FinishedAtUtc ?? latestResult?.StartedAtUtc ?? received),
            NoSensitiveData = true,
            ClearingHouseCode = header.ClearingHouse?.Code ?? ResolveClearingHouseCode(header),
            ProfileCode = "nacha-config profiles",
            FlowType = ResolveFlowType(ingestion, header),
            IsReturnFile = IsReturnFile(ingestion, header),
            ValidationPassed = !hasErrors && (fileControlCount > 0 || latestResult?.InvalidCount == 0),
            BatchCount = batchCount,
            EntryCount = entryCount,
            AddendaCount = addendaCount,
            BatchControlCount = batchControlCount,
            FileControlCount = fileControlCount,
            ProcessingStatus = latestResult?.OutcomeStatus.ToString() ?? ingestion?.ParsingStatus.ToString() ?? "Persisted",
            ReceivedAt = ToOffset(received),
            CreatedAt = ToOffset(created) ?? DateTimeOffset.UtcNow,
            CorrelationId = SafeCorrelation(ingestion?.CorrelationId, header.NachaID),
            HasErrors = hasErrors,
            WarningCount = warningCount,
            ErrorCount = errorCount
        };
    }

    private static NachaOperationalFileDetailReadModel ProjectFileDetail(NachaHeader header)
    {
        var file = ProjectFile(header);
        var warnings = BuildDetailWarnings(header).ToArray();
        var isPartial = warnings.Any(x => x.Contains("parcial", StringComparison.OrdinalIgnoreCase));
        var entries = (header.EntryDetails ?? [])
            .OrderBy(x => x.SequenceNumber)
            .ThenBy(x => x.EntryDetailID)
            .Take(MaxDetailRows)
            .Select(ProjectEntry)
            .ToArray();
        var addendas = (header.AddendaRecords ?? [])
            .OrderBy(x => x.AddendumSequence)
            .ThenBy(x => x.AddendaID)
            .Take(MaxDetailRows)
            .Select(ProjectAddenda)
            .ToArray();
        var batches = (header.Batches ?? [])
            .OrderBy(x => x.BatchNumber)
            .Take(MaxDetailRows)
            .Select(ProjectBatch)
            .ToArray();
        var batchControls = (header.BatchControls ?? [])
            .OrderBy(x => x.BatchNumber)
            .ThenBy(x => x.BatchControlID)
            .Take(MaxDetailRows)
            .Select(ProjectBatchControl)
            .ToArray();
        var fileControls = (header.FileControls ?? [])
            .OrderBy(x => x.FileControlID)
            .Take(MaxDetailRows)
            .Select(ProjectFileControl)
            .ToArray();

        return new NachaOperationalFileDetailReadModel
        {
            FileId = file.FileId,
            HeaderId = file.HeaderId,
            FileName = file.FileName,
            ClearingHouseCode = file.ClearingHouseCode,
            ProfileCode = file.ProfileCode,
            FlowType = file.FlowType,
            IsReturnFile = file.IsReturnFile,
            ProcessingStatus = file.ProcessingStatus,
            ValidationPassed = file.ValidationPassed,
            ReceivedAt = file.ReceivedAt,
            CreatedAt = file.CreatedAt,
            CorrelationId = file.CorrelationId,
            DataSource = isPartial ? PartialSource : PersistedSource,
            IsPartialData = isPartial,
            Warnings = warnings,
            Header = new NachaOperationalHeaderReadModel
            {
                HeaderId = file.HeaderId,
                PriorityCode = SafeText(header.PriorityCode, "N/A", 16),
                ImmediateDestination = SafeText(header.ImmediateDestination, "N/A", 32),
                ImmediateOrigin = SafeText(header.ImmediateOrigin, "N/A", 32),
                FileCreationDate = SafeText(header.FileCreationDate, "N/A", 16),
                FileCreationTime = SafeText(header.FileCreationTime, "N/A", 16),
                FileIdModifier = SafeText(header.FileIdModifier, "N/A", 16),
                RecordSize = SafeText(header.RecordSize, "N/A", 16),
                BlockingFactor = SafeText(header.BlockingFactor, "N/A", 16),
                FormatCode = SafeText(header.FormatCode, "N/A", 16),
                ReferenceCode = SafeText(header.ReferenceCode, "N/A", 32),
                CycleNumber = header.CycleNumber
            },
            Batches = batches,
            Entries = entries,
            Addendas = addendas,
            BatchControls = batchControls,
            FileControls = fileControls,
            TotalsSummary = BuildTotals(file, batchControls, fileControls),
            NoSensitiveData = true
        };
    }

    private static NachaOperationalBatchHeaderReadModel ProjectBatch(BatchHeader row)
        => new()
        {
            BatchId = row.BatchID,
            ServiceClassCode = SafeText(row.ServiceClassCode, "N/A", 16),
            CompanyName = SafeText(row.CompanyName, "N/A", 64),
            StandardEntryClassCode = SafeText(row.StandardEntryClassCode, "N/A", 16),
            CompanyEntryDescription = SafeText(row.CompanyEntryDescription, "N/A", 64),
            EffectiveEntryDate = SafeText(row.EffectiveEntryDate, "N/A", 16),
            BatchNumber = row.BatchNumber
        };

    private static NachaOperationalEntryDetailReadModel ProjectEntry(EntryDetail row)
        => new()
        {
            EntryDetailId = row.EntryDetailID,
            TransactionCode = SafeText(row.TransactionCode, "N/A", 16),
            ReceivingParticipantEntityCode = SafeText(row.ReceivingParticipantEntityCode, "N/A", 32),
            CheckDigit = SafeText(row.CheckDigit, "N/A", 8),
            AccountNumberMasked = MaskSensitive(row.AccountNumber),
            Amount = row.Amount,
            RecipIdNumberMasked = MaskSensitive(row.RecipIdNumber),
            RecipUserNameMasked = MaskName(row.RecipUserName),
            AddendumIndicator = SafeText(row.AddendumIndicator, "N/A", 8),
            SequenceNumberMasked = MaskTrace(row.SequenceNumber)
        };

    private static NachaOperationalAddendaRecordReadModel ProjectAddenda(AddendaRecord row)
        => new()
        {
            AddendaId = row.AddendaID,
            CodeTypeAddendumRecord = SafeText(row.CodeTypeAddendumRecord, "N/A", 16),
            BusinessType = SafeText(row.BusinessType, "N/A", 32),
            PurposeOfTransaction = SafeText(row.PurposeOfTransaction, "N/A", 64),
            InvoiceOrAccountNumberMasked = MaskSensitive(row.InvoiceOrAccountNumber),
            InfoFromOriginator = SafeText(row.InfofromOriginator, "Sanitized", 80),
            ReturnReasonCode = SafeText(row.ReturnReasonCode, "N/A", 16),
            OriginalTraceNumberMasked = MaskTrace(row.OriginalTraceNumber),
            NewTraceNumberMasked = MaskTrace(row.NewTraceNumber),
            AddendumSequence = SafeText(row.AddendumSequence, "N/A", 16),
            EntryDetailSequenceNumberMasked = MaskTrace(row.EntryDetailSequenceNumber)
        };

    private static NachaOperationalBatchControlReadModel ProjectBatchControl(BatchControl row)
        => new()
        {
            BatchControlId = row.BatchControlID,
            BatchTranClassCode = SafeText(row.BatchTranClassCode, "N/A", 16),
            EntryAddendaCount = row.EntryAddendaCount,
            EntryHash = row.EntryHash,
            TotalDebitAmount = row.TotalDebitAmount,
            TotalCreditAmount = row.TotalCreditAmount,
            BatchNumber = SafeText(row.BatchNumber, "N/A", 16)
        };

    private static NachaOperationalFileControlReadModel ProjectFileControl(FileControl row)
        => new()
        {
            FileControlId = row.FileControlID,
            BatchCount = row.BatchCount,
            BlockCount = row.BlockCount,
            EntryAddendaCount = row.EntryAddendaCount,
            EntryHash = row.EntryHash,
            TotalDebitAmount = row.TotalDebitAmount,
            TotalCreditAmount = row.TotalCreditAmount
        };

    private static NachaOperationalTotalsSummaryReadModel BuildTotals(
        NachaOperationalFileReadModel file,
        IReadOnlyList<NachaOperationalBatchControlReadModel> batchControls,
        IReadOnlyList<NachaOperationalFileControlReadModel> fileControls)
        => new()
        {
            BatchCount = file.BatchCount,
            EntryCount = file.EntryCount,
            AddendaCount = file.AddendaCount,
            BatchControlCount = file.BatchControlCount,
            FileControlCount = file.FileControlCount,
            PersistedRecordCount = file.PersistedRecordCount,
            TotalDebitAmount = fileControls.Sum(x => x.TotalDebitAmount) is var fileDebit && fileDebit > 0
                ? fileDebit
                : batchControls.Sum(x => x.TotalDebitAmount),
            TotalCreditAmount = fileControls.Sum(x => x.TotalCreditAmount) is var fileCredit && fileCredit > 0
                ? fileCredit
                : batchControls.Sum(x => x.TotalCreditAmount),
            ValidationPassed = file.ValidationPassed
        };

    private static IEnumerable<string> BuildDetailWarnings(NachaHeader header)
    {
        if (header.Batches?.Count is null or 0)
        {
            yield return "No persisted batch headers found; detalle parcial read-only.";
        }

        if (header.EntryDetails?.Count is null or 0)
        {
            yield return "No persisted entry details found; detalle parcial read-only.";
        }

        if (header.FileControls?.Count is null or 0)
        {
            yield return "No persisted file controls found; totales pueden ser parciales.";
        }

        yield return "Productivo permanece NO-GO; esta consulta no ejecuta SOAP ni movimientos.";
    }

    private static NachaOperationalDecisionReadModel ProjectDecision(IncomingNachaEntryClassification row)
    {
        var manualReview = row.RequiresManualResolution || row.EligibilityStatus == IncomingNachaEligibilityStatus.RevisionManual;
        var blocked = manualReview || row.EligibilityStatus == IncomingNachaEligibilityStatus.Bloqueada;
        var operation = row.FunctionalClass switch
        {
            IncomingNachaFunctionalClass.CreditoEntrante or IncomingNachaFunctionalClass.DebitoEntrante => "ProcTransacciones",
            IncomingNachaFunctionalClass.Devolucion or IncomingNachaFunctionalClass.RechazadaOperador or IncomingNachaFunctionalClass.RetornoEpr => "RegistrarRespuestaTransaccion",
            _ => "None"
        };

        return new NachaOperationalDecisionReadModel
        {
            CorrelationId = SafeCorrelation(row.Ingestion?.CorrelationId, row.IncomingNachaFileIngestionId.ToString("N")),
            FileName = SanitizeFileName(row.Ingestion?.FileName, row.IncomingNachaFileIngestionId.ToString("N")),
            EntryTraceNumber = MaskTrace(row.EntryDetail?.SequenceNumber),
            OriginalTraceNumber = MaskTrace(row.OriginalTraceRef),
            DecisionType = row.FunctionalClass.ToString(),
            SoapOperationCandidate = blocked ? "None" : operation,
            RequiresMonetaryMovement = operation is "ProcTransacciones" or "ProcContrapartidas",
            ReasonCode = string.IsNullOrWhiteSpace(row.ReturnReasonCode) ? "N/A" : row.ReturnReasonCode,
            ReasonDescription = SafeText(row.BusinessMeaning, "Decision derivada de clasificacion NACHA entrante persistida."),
            NewInternalStatus = row.EligibilityStatus.ToString(),
            ManualReviewRequired = manualReview,
            IsBlocked = blocked,
            BlockReason = blocked ? "Decision persistida requiere revision/bloqueo; no se ejecuta SOAP desde dashboard." : null,
            DataSource = PersistedSource,
            IsDerived = true,
            IsPersisted = true,
            Warning = "Decision derivada de clasificacion persistida; no ejecuta SOAP.",
            CreatedAt = row.CreatedAt
        };
    }

    private static NachaSoapReadinessReadModel ProjectReadiness(IncomingNachaDispatchQueue queue)
    {
        var latest = queue.Executions.OrderByDescending(x => x.FinishedAtUtc ?? x.StartedAtUtc).FirstOrDefault();
        var blocked = queue.QueueStatus is IncomingNachaDispatchQueueStatus.Blocked or IncomingNachaDispatchQueueStatus.FailedFinal;

        return new NachaSoapReadinessReadModel
        {
            CorrelationId = SafeCorrelation(latest?.CorrelationId, queue.Id.ToString("N")),
            OperationCandidate = NormalizeMethod(latest?.MethodName),
            IsReadyForUat = !blocked && (queue.QueueStatus is IncomingNachaDispatchQueueStatus.Queued or IncomingNachaDispatchQueueStatus.RetryPending or IncomingNachaDispatchQueueStatus.Dispatched),
            IsBlocked = blocked,
            BlockReasons = blocked ? [SafeText(queue.LastErrorMessage, "Cola bloqueada/fallida; no se ejecuta SOAP desde dashboard.")] : [],
            PayloadMappingPassed = latest != null && !string.IsNullOrWhiteSpace(latest.RequestHash),
            RequestMappingPassed = latest != null && !string.IsNullOrWhiteSpace(latest.RequestHash),
            OperationalGatePassed = !blocked,
            ReadinessCheckPassed = !blocked,
            SimulationPassed = latest?.IsSuccess == true,
            ResiliencePassed = queue.AttemptCount <= 1 || queue.QueueStatus != IncomingNachaDispatchQueueStatus.FailedFinal,
            WouldInvokeRealSoap = false,
            ProductiveExecution = false,
            RequiresMonetaryMovement = true,
            Phase = "6B.5",
            DataSource = PersistedSource,
            IsDerived = true,
            IsPersisted = true,
            Warning = "Readiness derivado de cola/ejecuciones persistidas; SOAP real permanece deshabilitado.",
            LastCheckedAt = ToOffset(latest?.FinishedAtUtc ?? latest?.StartedAtUtc ?? queue.LastAttemptAtUtc ?? queue.NextAttemptAtUtc ?? queue.OperationalDate) ?? DateTimeOffset.UtcNow
        };
    }

    private static NachaOperationalAuditReadModel ProjectAudit(IncomingNachaProcessingEvent row)
        => new()
        {
            CorrelationId = SafeCorrelation(row.Ingestion?.CorrelationId, row.IncomingNachaFileIngestionId.ToString("N")),
            Phase = "6B.4",
            EventType = SafeText(row.EventType, "IncomingNachaProcessingEvent"),
            Severity = SeverityFromStatus(row.EventStatus),
            Message = SafeText(row.Message, "Evento operacional NACHA entrante persistido."),
            IsBlocked = IsBlockedStatus(row.EventStatus) || row.EventType.Contains("Bloqueado", StringComparison.OrdinalIgnoreCase),
            DataSource = PersistedSource,
            IsDerived = false,
            IsPersisted = true,
            Warning = "Detalles sanitizados; no se exponen payloads ni cuentas.",
            Timestamp = ToOffset(row.OccurredAtUtc) ?? DateTimeOffset.UtcNow,
            SanitizedDetails = new Dictionary<string, string>
            {
                ["EventStatus"] = SafeText(row.EventStatus, "N/A"),
                ["RaisedBy"] = SafeText(row.RaisedBy, "sistema"),
                ["Evidence"] = "Sanitized"
            }
        };

    private static NachaOperationalAuditReadModel ProjectExecutionAudit(IncomingNachaIntegrationExecution row)
        => new()
        {
            CorrelationId = SafeCorrelation(row.CorrelationId, row.Id.ToString("N")),
            Phase = "6B.5",
            EventType = "IncomingNachaIntegrationExecution",
            Severity = row.IsSuccess ? "Information" : "Warning",
            Message = SafeText(row.ResponseMessage, "Ejecucion de integracion persistida con payload sanitizado."),
            IsBlocked = !row.IsSuccess && !row.IsRetryable,
            DataSource = PersistedSource,
            IsDerived = false,
            IsPersisted = true,
            Warning = "Request/response XML omitidos por sanitizacion.",
            Timestamp = ToOffset(row.FinishedAtUtc ?? row.StartedAtUtc) ?? DateTimeOffset.UtcNow,
            SanitizedDetails = new Dictionary<string, string>
            {
                ["MethodName"] = NormalizeMethod(row.MethodName),
                ["ResponseCode"] = SafeText(row.ResponseCode, "N/A"),
                ["IsSuccess"] = row.IsSuccess.ToString(),
                ["RequestPayloadXml"] = "Sanitized",
                ["ResponsePayloadXml"] = "Sanitized"
            }
        };

    private static NachaOperationalSummaryReadModel BuildSummary(
        IReadOnlyList<NachaOperationalFileReadModel> files,
        IReadOnlyList<NachaOperationalDecisionReadModel> decisions,
        IReadOnlyList<NachaSoapReadinessReadModel> readiness,
        IReadOnlyList<string> warnings)
        => new()
        {
            ProductiveStatus = "NO-GO",
            BackendPhase = "6C.3",
            SoapMode = "ReadOnly",
            ProductiveExecution = false,
            WouldInvokeRealSoap = false,
            TotalFiles = files.Count,
            TotalIncomingFiles = files.Count(x => x.FlowType.Contains("Incoming", StringComparison.OrdinalIgnoreCase)),
            TotalOutgoingFiles = files.Count(x => x.FlowType.Contains("Outgoing", StringComparison.OrdinalIgnoreCase)),
            TotalReturnFiles = files.Count(x => x.IsReturnFile),
            TotalDecisions = decisions.Count,
            TotalSoapCandidates = decisions.Count(x => x.SoapOperationCandidate != "None"),
            TotalNoGoBlocks = readiness.Count(x => x.IsBlocked) + decisions.Count(x => x.IsBlocked),
            TotalManualReview = decisions.Count(x => x.ManualReviewRequired),
            TotalReadinessChecks = readiness.Count,
            LastUpdatedAt = DateTimeOffset.UtcNow,
            IsDemoData = false,
            IsPartialData = warnings.Count > 0,
            DataSource = warnings.Count > 0 ? PartialSource : PersistedSource,
            Warnings = warnings
        };

    private static IReadOnlyList<string> BuildWarnings(
        IReadOnlyList<NachaOperationalFileReadModel> files,
        IReadOnlyList<NachaOperationalDecisionReadModel> decisions,
        IReadOnlyList<NachaSoapReadinessReadModel> readiness,
        IReadOnlyList<NachaOperationalAuditReadModel> audit)
    {
        var warnings = new List<string>();
        if (files.Count == 0)
        {
            warnings.Add("No persisted NACHA header data found; service may use safe demo fallback.");
        }

        if (decisions.Count == 0)
        {
            warnings.Add("No persisted decision records found; showing available NACHA read-store data only.");
        }

        if (readiness.Count == 0)
        {
            warnings.Add("No persisted SOAP readiness data found; using safe read-only placeholder.");
        }

        if (audit.Count == 0)
        {
            warnings.Add("No persisted operational audit data found; audit section is partial.");
        }

        warnings.Add("Productivo permanece NO-GO; SOAP real deshabilitado.");
        return warnings;
    }

    private static string ResolveClearingHouseCode(NachaHeader header)
        => header.ClearingHouseId switch
        {
            2 => "CENIT",
            1 => "ACH",
            _ => "NACHA"
        };

    private static string ResolveFlowType(IncomingNachaFileIngestion? ingestion, NachaHeader header)
    {
        if (IsReturnFile(ingestion, header))
        {
            return "ReturnFile";
        }

        return ingestion is not null ? "IncomingPersisted" : "PersistedNacha";
    }

    private static bool IsReturnFile(IncomingNachaFileIngestion? ingestion, NachaHeader header)
        => (ingestion?.FileName?.EndsWith(".RET", StringComparison.OrdinalIgnoreCase) == true)
            || header.AddendaRecords?.Any(x => !string.IsNullOrWhiteSpace(x.ReturnReasonCode)) == true;

    private static DateTime? ParseNachaCreationDate(NachaHeader header)
    {
        if (DateTime.TryParse($"{header.FileCreationDate} {header.FileCreationTime}", out var parsed))
        {
            return DateTime.SpecifyKind(parsed, DateTimeKind.Utc);
        }

        return null;
    }

    private static DateTimeOffset? ToOffset(DateTime? value)
        => value.HasValue ? new DateTimeOffset(DateTime.SpecifyKind(value.Value, DateTimeKind.Utc)) : null;

    private static DateTimeOffset? ToOffset(DateTimeOffset? value) => value;

    private static string SanitizeFileName(string? fileName, string? fallback)
    {
        var value = string.IsNullOrWhiteSpace(fileName) ? $"nacha-{SafeToken(fallback)}.ach" : Path.GetFileName(fileName);
        return value.Length > 120 ? value[..120] : value;
    }

    private static string SafeToken(string? value, int maxLength = 16)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return "unknown";
        }

        var safe = new string(value.Where(char.IsLetterOrDigit).Take(maxLength).ToArray());
        return string.IsNullOrWhiteSpace(safe) ? "unknown" : safe;
    }

    private static string SafeCorrelation(string? value, string? fallback)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return $"corr-{SafeToken(fallback)}";
        }

        var compact = value.Trim();
        var looksLikeHash = HasLongHexRun(compact);
        return looksLikeHash ? $"corr-{SafeToken(compact)}" : SafeText(compact, $"corr-{SafeToken(fallback)}", 64);
    }

    private static bool HasLongHexRun(string value)
    {
        var run = 0;
        foreach (var character in value)
        {
            run = Uri.IsHexDigit(character) ? run + 1 : 0;
            if (run >= 32)
            {
                return true;
            }
        }

        return false;
    }

    private static string SafeText(string? value, string fallback, int maxLength = 180)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return fallback;
        }

        var sanitized = value
            .Replace("\r", " ", StringComparison.Ordinal)
            .Replace("\n", " ", StringComparison.Ordinal)
            .Trim();

        return sanitized.Length > maxLength ? sanitized[..maxLength] : sanitized;
    }

    private static string MaskTrace(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return "N/A";
        }

        var digits = new string(value.Where(char.IsDigit).ToArray());
        if (digits.Length <= 4)
        {
            return "****";
        }

        return $"***{digits[^4..]}";
    }

    private static string MaskSensitive(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return "N/A";
        }

        var normalized = new string(value.Where(char.IsLetterOrDigit).ToArray());
        if (normalized.Length <= 4)
        {
            return "****";
        }

        return $"****{normalized[^4..]}";
    }

    private static string MaskName(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return "N/A";
        }

        var sanitized = SafeText(value, "N/A", 64);
        return sanitized.Length <= 2 ? "**" : $"{sanitized[0]}***";
    }

    private static string SeverityFromStatus(string? status)
    {
        if (IsBlockedStatus(status))
        {
            return "Warning";
        }

        return status?.Contains("error", StringComparison.OrdinalIgnoreCase) == true ? "Error" : "Information";
    }

    private static bool IsBlockedStatus(string? status)
        => status?.Contains("bloque", StringComparison.OrdinalIgnoreCase) == true
            || status?.Contains("fail", StringComparison.OrdinalIgnoreCase) == true
            || status?.Contains("error", StringComparison.OrdinalIgnoreCase) == true;

    private static string NormalizeMethod(string? method)
        => method switch
        {
            "Proc_Transacciones" => "ProcTransacciones",
            "Proc_Contrapartidas" => "ProcContrapartidas",
            null or "" => "None",
            _ => method
        };
}
