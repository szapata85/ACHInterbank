using Cfa.ACHInterbank.Application.ACH.Interfaces;
using Cfa.ACHInterbank.Application.ACH.Models;
using Cfa.ACHInterbank.Domain.Entities.Transactions.Enums;
using Cfa.ACHInterbank.Domain.Models.ACH;
using Cfa.ACHInterbank.Domain.Models.ACH.Enums;
using Cfa.ACHInterbank.Persistence.DataBase;
using Microsoft.EntityFrameworkCore;

namespace Cfa.ACHInterbank.Persistence.ACH.Services.Implementation;

public sealed class AchReconciliationReadModelService : IAchReconciliationReadModelService
{
    private const int MaxRows = 100;
    private const string Source = "backend read-only";
    private const string PartialSource = "parcial";
    private readonly AchDbContext _context;

    public AchReconciliationReadModelService(AchDbContext context)
    {
        _context = context;
    }

    public async Task<AchReconciliationDashboardReadModel> GetDashboardAsync(CancellationToken cancellationToken = default)
    {
        var items = await GetItemsAsync(cancellationToken);
        var warnings = BuildWarnings(items);
        return new AchReconciliationDashboardReadModel
        {
            ProductiveStatus = "NO-GO",
            TotalResponses = items.Count(x => x.FlowType is "Response" or "DifferentialResponse" or "Prenotification"),
            TotalDifferentialResponses = items.Count(x => x.FlowType == "DifferentialResponse"),
            TotalReturns = items.Count(x => x.FlowType == "Return" || x.IsReturnFile),
            TotalRejections = items.Count(x => x.ResponseType.Contains("Rechazo", StringComparison.OrdinalIgnoreCase)),
            TotalPrenotifications = items.Count(x => x.IsPrenotification),
            TotalRor = items.Count(x => x.IsRor),
            TotalReconciled = items.Count(x => x.ReconciliationStatus == "Conciliado"),
            TotalPending = items.Count(x => x.ReconciliationStatus == "Pendiente"),
            TotalInconsistent = items.Count(x => x.ReconciliationStatus == "Inconsistente"),
            TotalManualReviewRequired = items.Count(x => x.RequiresManualReview),
            TotalNonMonetary = items.Count(x => x.IsNonMonetary),
            TotalMonetaryCandidates = items.Count(x => x.IsMonetaryCandidate),
            LastUpdatedAt = DateTimeOffset.UtcNow,
            DataSource = warnings.Count > 0 ? PartialSource : Source,
            IsPartialData = warnings.Count > 0,
            Warnings = warnings
        };
    }

    public async Task<IReadOnlyList<AchReconciliationItemReadModel>> GetItemsAsync(CancellationToken cancellationToken = default)
    {
        var responses = await _context.AchResponses
            .AsNoTracking()
            .Include(x => x.NotificationAttempts)
            .OrderByDescending(x => x.FechaActualizacion ?? x.FechaCreacion)
            .Take(MaxRows)
            .ToListAsync(cancellationToken);

        var returns = await _context.AchReturnsGenerated
            .AsNoTracking()
            .Include(x => x.OriginalTransaction)
            .OrderByDescending(x => x.GeneratedAtUtc)
            .Take(MaxRows)
            .ToListAsync(cancellationToken);

        var ror = await _context.ReturnOfReturnFlows
            .AsNoTracking()
            .Include(x => x.SourceReturnTransaction)
            .Include(x => x.ReturnOfReturnTransaction)
            .OrderByDescending(x => x.OrchestratedAtUtc)
            .Take(MaxRows)
            .ToListAsync(cancellationToken);

        var classifications = await _context.IncomingNachaEntryClassifications
            .AsNoTracking()
            .Include(x => x.Ingestion)
            .Include(x => x.EntryDetail)
            .Include(x => x.AddendaRecord)
            .OrderByDescending(x => x.CreatedAt)
            .Take(MaxRows)
            .ToListAsync(cancellationToken);

        var ingestionEvents = await _context.IncomingNachaProcessingEvents
            .AsNoTracking()
            .Include(x => x.Ingestion)
            .Where(x => x.EventType == "NachaProfileSelection"
                || x.EventType == "DuplicateUploadAttempt"
                || x.EventType == "FileNameContentConflict")
            .OrderByDescending(x => x.OccurredAtUtc)
            .Take(MaxRows)
            .ToListAsync(cancellationToken);

        var clearingHouseIds = ingestionEvents
            .Select(x => x.Ingestion.ResolvedClearingHouseId ?? x.Ingestion.DetectedClearingHouseId)
            .Where(x => x.HasValue)
            .Select(x => x!.Value)
            .Distinct()
            .ToArray();
        var clearingHouseCodes = await _context.ClearingHouses
            .AsNoTracking()
            .Where(x => clearingHouseIds.Contains(x.Id))
            .ToDictionaryAsync(x => x.Id, x => x.Code, cancellationToken);

        var transactionIds = responses
            .Select(x => ParseInt(x.IdTransaccion))
            .Concat(returns.Select(x => (int?)x.OriginalTransactionId))
            .Concat(ror.SelectMany(x => new int?[] { x.SourceReturnTransactionId, x.ReturnOfReturnTransactionId }))
            .Where(x => x.HasValue)
            .Select(x => x!.Value)
            .Distinct()
            .ToArray();

        var responseTransactionKeys = responses.Select(r => r.IdTransaccion).Distinct().ToArray();
        var transactions = await _context.AchTransactions
            .AsNoTracking()
            .Where(x => transactionIds.Contains(x.Id)
                || responseTransactionKeys.Contains(x.TransactionExternalId)
                || responseTransactionKeys.Contains(x.Reference)
                || responses.Select(r => r.IdTransaccion).Contains(x.TransactionExternalId)
                || responses.Select(r => r.IdTransaccion).Contains(x.Reference))
            .Take(MaxRows)
            .ToListAsync(cancellationToken);

        var headers = await _context.NachaHeaders
            .AsNoTracking()
            .Include(x => x.ClearingHouse)
            .Include(x => x.IncomingNachaFileIngestion)
            .Include(x => x.EntryDetails)
            .Include(x => x.AddendaRecords)
            .OrderByDescending(x => x.IncomingNachaFileIngestion != null ? x.IncomingNachaFileIngestion.UploadedAtUtc : DateTime.MinValue)
            .Take(MaxRows)
            .ToListAsync(cancellationToken);

        var items = new List<AchReconciliationItemReadModel>();
        items.AddRange(responses.Select(x => ProjectResponse(x, FindTransaction(transactions, x.IdTransaccion), headers)));
        items.AddRange(returns.Select(x => ProjectReturn(x, headers)));
        items.AddRange(ror.Select(ProjectRor));
        items.AddRange(classifications.Select(x => ProjectClassification(x, headers)));
        items.AddRange(ingestionEvents.Select(x => ProjectIngestionEvent(x, clearingHouseCodes)));

        return items
            .GroupBy(x => x.ReconciliationId, StringComparer.OrdinalIgnoreCase)
            .Select(x => x.First())
            .OrderByDescending(x => x.UpdatedAt ?? x.CreatedAt)
            .Take(MaxRows)
            .ToArray();
    }

    public async Task<AchReconciliationDetailReadModel?> GetItemAsync(string reconciliationId, CancellationToken cancellationToken = default)
    {
        var item = (await GetItemsAsync(cancellationToken)).FirstOrDefault(x => string.Equals(x.ReconciliationId, reconciliationId, StringComparison.OrdinalIgnoreCase));
        return item is null ? null : await BuildDetailAsync(item, cancellationToken);
    }

    public async Task<AchReconciliationDetailReadModel?> GetItemByCorrelationAsync(string correlationId, CancellationToken cancellationToken = default)
    {
        var item = (await GetItemsAsync(cancellationToken))
            .Where(x => string.Equals(x.CorrelationId, correlationId, StringComparison.OrdinalIgnoreCase))
            .OrderByDescending(x => x.TransactionId.HasValue)
            .ThenByDescending(x => x.UpdatedAt ?? x.CreatedAt)
            .FirstOrDefault();
        return item is null ? null : await BuildDetailAsync(item, cancellationToken);
    }

    private async Task<AchReconciliationDetailReadModel> BuildDetailAsync(AchReconciliationItemReadModel item, CancellationToken ct)
    {
        var header = await _context.NachaHeaders.AsNoTracking()
            .Include(x => x.ClearingHouse)
            .Include(x => x.IncomingNachaFileIngestion)
            .Include(x => x.Batches)
            .Include(x => x.EntryDetails)
            .Include(x => x.AddendaRecords)
            .Include(x => x.BatchControls)
            .Include(x => x.FileControls)
            .FirstOrDefaultAsync(x => item.FileId != null && $"nacha-{SafeToken(x.NachaID)}" == item.FileId, ct);
        var transaction = item.TransactionId.HasValue
            ? await _context.AchTransactions.AsNoTracking().FirstOrDefaultAsync(x => x.Id == item.TransactionId.Value, ct)
            : null;
        var events = item.TransactionId.HasValue
            ? await _context.AchTransactionStateEvents.AsNoTracking()
                .Where(x => x.AchTransactionId == item.TransactionId.Value)
                .OrderByDescending(x => x.CreatedAt)
                .Take(20)
                .ToListAsync(ct)
            : [];
        var responseAttempts = await _context.AchResponseNotificationAttempts.AsNoTracking()
            .Include(x => x.AchResponse)
            .Where(x => x.AchResponse.CorrelationId == item.CorrelationId || x.AchResponse.Id.ToString() == item.ReconciliationId.Replace("resp-", "", StringComparison.OrdinalIgnoreCase))
            .OrderByDescending(x => x.FechaEnvio ?? x.FechaCreacion)
            .Take(20)
            .ToListAsync(ct);

        var entry = header?.EntryDetails?.FirstOrDefault(x => x.EntryDetailID == item.EntryId) ?? header?.EntryDetails?.FirstOrDefault();
        var addenda = header?.AddendaRecords?.FirstOrDefault(x => x.EntryDetailSequenceNumber == entry?.SequenceNumber) ?? header?.AddendaRecords?.FirstOrDefault();
        return new AchReconciliationDetailReadModel
        {
            Item = item,
            NachaHeaderSummary = header is null ? null : new(SafeToken(header.NachaID), ResolveClearingHouse(header), SanitizeFileName(header.IncomingNachaFileIngestion?.FileName, header.NachaID), item.FlowType, SafeCorrelation(header.IncomingNachaFileIngestion?.CorrelationId, header.NachaID ?? item.ReconciliationId)),
            BatchSummary = header?.Batches?.FirstOrDefault() is { } batch ? new(batch.BatchID, SafeText(batch.ServiceClassCode), SafeText(batch.CompanyEntryDescription), batch.BatchNumber) : null,
            EntrySummary = entry is null ? null : new(entry.EntryDetailID, SafeText(entry.TransactionCode), MaskTrace(entry.SequenceNumber), MaskSensitive(entry.AccountNumber), entry.Amount),
            AddendaSummary = addenda is null ? null : new(addenda.AddendaID, SafeText(addenda.ReturnReasonCode), MaskTrace(addenda.OriginalTraceNumber), MaskTrace(addenda.NewTraceNumber)),
            ControlSummary = header is null ? null : new(
                header.BatchControls?.Count ?? 0,
                header.FileControls?.Count ?? 0,
                header.FileControls?.Sum(x => x.EntryAddendaCount) ?? header.BatchControls?.Sum(x => x.EntryAddendaCount ?? 0) ?? 0,
                header.FileControls?.Sum(x => x.TotalDebitAmount) ?? 0,
                header.FileControls?.Sum(x => x.TotalCreditAmount) ?? 0),
            InternalTransactionSummary = transaction is null ? null : new(transaction.Id, MaskSensitive(transaction.TransactionExternalId), MaskSensitive(transaction.Reference), transaction.State.ToString(), transaction.IsPrenotification),
            ResponseHistory = responseAttempts.Select(x => new AchReconciliationHistoryEvent("AchResponseNotificationAttempt", x.EstadoNotificacion.ToString(), SafeText(x.DescripcionError ?? x.DescripcionCausal, "Intento sanitizado")!, ToOffset(x.FechaEnvio ?? x.FechaCreacion), Source)).ToArray(),
            AuditEvents = events.Select(x => new AchReconciliationHistoryEvent("AchTransactionStateEvent", x.ToState.ToString(), SafeText(x.ReasonCode, "Cambio de estado interno")!, x.CreatedAt, Source)).ToArray(),
            Warnings = DetailWarnings(header, transaction),
            NoSensitiveData = true
        };
    }

    private static AchReconciliationItemReadModel ProjectResponse(AchResponse row, AchTransaction? tx, IReadOnlyList<NachaHeader> headers)
    {
        var manual = row.EstadoProcesamiento is AchResponseProcessingStatus.RequiereRevisionManual or AchResponseProcessingStatus.NoHomologada || !string.IsNullOrWhiteSpace(row.MotivoNoHomologacion);
        var prenote = row.TipoRespuesta == TipoRespuestaAch.Prenota;
        var header = FindHeader(headers, row.CorrelationId, tx?.TraceNumber);
        return BaseItem(
            reconciliationId: $"resp-{row.Id:N}",
            correlationId: row.CorrelationId,
            header: header,
            flowType: prenote ? "Prenotification" : "DifferentialResponse",
            responseType: prenote ? "Prenotificacion" : "Respuesta diferencial",
            responseCode: row.CodigoEstadoExterno,
            responseDescription: row.EstadoInternoNombre,
            reasonCode: row.CodigoCausalExterna ?? row.CausalNormalizada,
            reasonDescription: row.DescripcionCausal,
            trace: tx?.TraceNumber ?? row.IdTransaccion,
            originalTrace: tx?.OriginalTraceRef,
            entryId: null,
            transactionId: tx?.Id ?? ParseInt(row.IdTransaccion),
            internalStatus: row.EstadoInternoNombre ?? row.EstadoProcesamiento.ToString(),
            reconciliationStatus: manual ? "RequiereRevisionManual" : row.EstadoProcesamiento == AchResponseProcessingStatus.Notificada ? "Conciliado" : "Pendiente",
            manual,
            isReturnFile: false,
            isRor: false,
            prenote,
            isNonMonetary: true,
            isMonetaryCandidate: false,
            soapOperation: "RegistrarRespuestaTransaccion",
            createdAt: ToOffset(row.FechaCreacion),
            updatedAt: row.FechaActualizacion.HasValue ? ToOffset(row.FechaActualizacion.Value) : null,
            warning: "Respuesta ACH read-only; RegistrarRespuestaTransaccion es no monetario.");
    }

    private static AchReconciliationItemReadModel ProjectReturn(AchReturnGenerated row, IReadOnlyList<NachaHeader> headers)
    {
        var header = FindHeader(headers, null, row.OriginalSequenceNumber);
        return BaseItem(
            $"return-{row.Id}",
            row.OriginalTransaction?.TransactionExternalId,
            header,
            "Return",
            "Devolucion .RET",
            row.ReturnReasonCode,
            "Devolucion ACH",
            row.ReturnReasonCode,
            row.ReturnReasonCode,
            row.NewSequenceNumber,
            row.OriginalSequenceNumber,
            null,
            row.OriginalTransactionId,
            row.OriginalTransaction?.State.ToString() ?? "Returned",
            "Conciliado",
            false,
            true,
            false,
            row.OriginalTransaction?.IsPrenotification == true,
            true,
            false,
            "RegistrarRespuestaTransaccion",
            ToOffset(row.GeneratedAtUtc),
            null,
            "Archivo .RET no mueve dinero directamente.");
    }

    private static AchReconciliationItemReadModel ProjectRor(ReturnOfReturnFlow row)
        => BaseItem(
            $"ror-{row.Id}",
            row.ReturnOfReturnTransaction.TransactionExternalId,
            null,
            "ReturnOfReturn",
            "ROR / Return of Return",
            row.ReasonCode,
            "Return of Return",
            row.ReasonCode,
            row.ReasonCode,
            row.ReturnOfReturnTransaction.TraceNumber,
            row.SourceReturnTransaction.TraceNumber,
            null,
            row.ReturnOfReturnTransactionId,
            row.Status,
            row.Status.Contains("Registered", StringComparison.OrdinalIgnoreCase) ? "Pendiente" : "Conciliado",
            row.Status.Contains("Manual", StringComparison.OrdinalIgnoreCase),
            false,
            true,
            row.ReturnOfReturnTransaction.IsPrenotification,
            true,
            false,
            "RegistrarRespuestaTransaccion",
            ToOffset(row.OrchestratedAtUtc),
            row.UpdatedAt,
            "ROR proyectado como conciliacion read-only.");

    private static AchReconciliationItemReadModel ProjectClassification(IncomingNachaEntryClassification row, IReadOnlyList<NachaHeader> headers)
    {
        var header = headers.FirstOrDefault(x => x.IncomingNachaFileIngestionId == row.IncomingNachaFileIngestionId);
        var monetary = row.FunctionalClass is IncomingNachaFunctionalClass.CreditoEntrante or IncomingNachaFunctionalClass.DebitoEntrante;
        var nonMonetary = row.FunctionalClass is IncomingNachaFunctionalClass.Prenotificacion or IncomingNachaFunctionalClass.Devolucion or IncomingNachaFunctionalClass.RechazadaOperador or IncomingNachaFunctionalClass.RetornoEpr;
        var operation = monetary ? "ProcTransacciones" : "RegistrarRespuestaTransaccion";
        var status = row.RequiresManualResolution || row.EligibilityStatus == IncomingNachaEligibilityStatus.RevisionManual
            ? "RequiereRevisionManual"
            : row.FunctionalClass is IncomingNachaFunctionalClass.Inconsistente or IncomingNachaFunctionalClass.Ambigua ? "Inconsistente" : "Pendiente";
        return BaseItem(
            $"class-{row.Id:N}",
            row.Ingestion?.CorrelationId,
            header,
            row.FunctionalClass.ToString(),
            row.FunctionalClass.ToString(),
            row.PrenoteStatus.ToString(),
            row.BusinessMeaning,
            row.ReturnReasonCode,
            row.BusinessMeaning,
            row.EntryDetail.SequenceNumber,
            row.OriginalTraceRef ?? row.AddendaRecord?.OriginalTraceNumber,
            row.EntryDetailId,
            null,
            row.EligibilityStatus.ToString(),
            status,
            row.RequiresManualResolution || row.EligibilityStatus == IncomingNachaEligibilityStatus.RevisionManual,
            row.FunctionalClass is IncomingNachaFunctionalClass.Devolucion or IncomingNachaFunctionalClass.RechazadaOperador,
            row.FunctionalClass == IncomingNachaFunctionalClass.RetornoEpr,
            row.FunctionalClass == IncomingNachaFunctionalClass.Prenotificacion,
            nonMonetary,
            monetary,
            operation,
            row.CreatedAt,
            row.UpdatedAt,
            "Clasificacion entrante persistida; no ejecuta SOAP ni mutaciones.");
    }

    private static AchReconciliationItemReadModel ProjectIngestionEvent(
        IncomingNachaProcessingEvent row,
        IReadOnlyDictionary<int, string> clearingHouseCodes)
    {
        var duplicate = row.EventType == "DuplicateUploadAttempt";
        var conflict = row.EventType == "FileNameContentConflict";
        var clearingHouseId = row.Ingestion.ResolvedClearingHouseId ?? row.Ingestion.DetectedClearingHouseId;
        var clearingHouseCode = clearingHouseId.HasValue
            && clearingHouseCodes.TryGetValue(clearingHouseId.Value, out var code)
                ? code
                : "N/A";
        var selected = row.EventStatus == NachaProfileSelectionStatus.ProfileSelected.ToString();

        return new AchReconciliationItemReadModel
        {
            ReconciliationId = $"ingestion-{row.IncomingNachaFileIngestionId:N}-event-{row.Id:N}",
            CorrelationId = SafeCorrelation(row.Ingestion.CorrelationId, row.IncomingNachaFileIngestionId.ToString("N")),
            FileId = $"ingestion-{row.IncomingNachaFileIngestionId:N}",
            FileName = SanitizeFileName(row.Ingestion.FileName, row.IncomingNachaFileIngestionId.ToString("N")),
            ClearingHouseCode = clearingHouseCode,
            FlowType = duplicate || conflict ? "FileIngestion" : "DifferentialResponse",
            ResponseType = duplicate
                ? "Doble carga"
                : conflict
                    ? "Conflicto nombre/contenido"
                    : "Seleccion de perfil NACHA-M",
            ResponseCode = SafeText(row.EventStatus),
            ResponseDescription = SafeText(row.Message),
            ReasonCode = SafeText(row.EventType),
            ReasonDescription = SafeText(row.Message),
            TraceNumberMasked = "N/A",
            OriginalTraceNumberMasked = "N/A",
            EntryId = row.EntryDetailId,
            TransactionId = row.AchTransactionId,
            InternalStatus = row.Ingestion.IngestionStatus.ToString(),
            ReconciliationStatus = duplicate ? "Conciliado" : selected ? "Pendiente" : "Inconsistente",
            RequiresManualReview = !duplicate,
            IsReturnFile = !duplicate && !conflict,
            IsRor = false,
            IsPrenotification = false,
            IsNonMonetary = true,
            IsMonetaryCandidate = false,
            SoapOperationCandidate = "None",
            CreatedAt = ToOffset(row.OccurredAtUtc),
            UpdatedAt = row.UpdatedAt,
            DataSource = Source,
            IsPersisted = true,
            IsDerived = true,
            Warning = duplicate
                ? "Carga duplicada auditada sin repetir parsing, evento funcional ni despacho."
                : SafeText(row.Message)
        };
    }

    private static AchReconciliationItemReadModel BaseItem(
        string reconciliationId,
        string? correlationId,
        NachaHeader? header,
        string flowType,
        string responseType,
        string? responseCode,
        string? responseDescription,
        string? reasonCode,
        string? reasonDescription,
        string? trace,
        string? originalTrace,
        int? entryId,
        int? transactionId,
        string internalStatus,
        string reconciliationStatus,
        bool manual,
        bool isReturnFile,
        bool isRor,
        bool prenote,
        bool isNonMonetary,
        bool isMonetaryCandidate,
        string soapOperation,
        DateTimeOffset createdAt,
        DateTimeOffset? updatedAt,
        string? warning)
        => new()
        {
            ReconciliationId = reconciliationId,
            CorrelationId = SafeCorrelation(correlationId, reconciliationId),
            FileId = header is null ? null : $"nacha-{SafeToken(header.NachaID)}",
            FileName = header is null ? "sin-archivo-persistido" : SanitizeFileName(header.IncomingNachaFileIngestion?.FileName, header.NachaID),
            ClearingHouseCode = header is null ? "N/A" : ResolveClearingHouse(header),
            FlowType = flowType,
            ResponseType = responseType,
            ResponseCode = SafeText(responseCode),
            ResponseDescription = SafeText(responseDescription),
            ReasonCode = SafeText(reasonCode),
            ReasonDescription = SafeText(reasonDescription),
            TraceNumberMasked = MaskTrace(trace),
            OriginalTraceNumberMasked = MaskTrace(originalTrace),
            EntryId = entryId,
            TransactionId = transactionId,
            InternalStatus = internalStatus,
            ReconciliationStatus = reconciliationStatus,
            RequiresManualReview = manual,
            IsReturnFile = isReturnFile,
            IsRor = isRor,
            IsPrenotification = prenote,
            IsNonMonetary = isNonMonetary,
            IsMonetaryCandidate = isMonetaryCandidate,
            SoapOperationCandidate = soapOperation,
            CreatedAt = createdAt,
            UpdatedAt = updatedAt,
            DataSource = header is null ? PartialSource : Source,
            IsPersisted = true,
            IsDerived = true,
            Warning = warning
        };

    private static IReadOnlyList<string> BuildWarnings(IReadOnlyList<AchReconciliationItemReadModel> items)
    {
        var warnings = new List<string> { "Productivo permanece NO-GO; consola read-only sin SOAP real ni movimientos." };
        if (items.Count == 0) warnings.Add("No persisted reconciliation sources found; respuesta parcial vacia.");
        if (items.Any(x => x.FileId is null)) warnings.Add("Algunos items no tienen cruce NACHA-M persistido; datos parciales.");
        if (items.All(x => x.TransactionId is null)) warnings.Add("No se encontro cruce con transacciones internas para todos los items.");
        return warnings;
    }

    private static IReadOnlyList<string> DetailWarnings(NachaHeader? header, AchTransaction? transaction)
    {
        var warnings = new List<string> { "Detalle sanitizado; no expone cuentas/documentos completos, XML, secretos ni endpoints reales." };
        if (header is null) warnings.Add("Sin cruce NACHA-M desagregado persistido.");
        if (transaction is null) warnings.Add("Sin cruce de transaccion interna persistida.");
        return warnings;
    }

    private static AchTransaction? FindTransaction(IEnumerable<AchTransaction> txs, string id)
        => txs.FirstOrDefault(x => x.Id.ToString() == id || x.TransactionExternalId == id || x.Reference == id);

    private static NachaHeader? FindHeader(IEnumerable<NachaHeader> headers, string? correlation, string? trace)
        => headers.FirstOrDefault(x => x.IncomingNachaFileIngestion?.CorrelationId == correlation)
            ?? headers.FirstOrDefault(x => x.EntryDetails?.Any(e => e.SequenceNumber == trace) == true)
            ?? headers.FirstOrDefault(x => x.AddendaRecords?.Any(a => a.OriginalTraceNumber == trace || a.NewTraceNumber == trace) == true);

    private static int? ParseInt(string? value) => int.TryParse(value, out var parsed) ? parsed : null;

    private static string ResolveClearingHouse(NachaHeader header)
        => header.ClearingHouse?.Code ?? "NACHA";

    private static DateTimeOffset ToOffset(DateTime value)
        => new(DateTime.SpecifyKind(value, DateTimeKind.Utc));

    private static string SanitizeFileName(string? fileName, string? fallback)
    {
        var value = string.IsNullOrWhiteSpace(fileName) ? $"nacha-{SafeToken(fallback)}.ach" : Path.GetFileName(fileName);
        return value.Length > 120 ? value[..120] : value;
    }

    private static string SafeCorrelation(string? value, string fallback)
        => string.IsNullOrWhiteSpace(value) ? $"corr-{SafeToken(fallback)}" : SafeText(value, $"corr-{SafeToken(fallback)}", 64)!;

    private static string SafeToken(string? value, int maxLength = 16)
    {
        var safe = new string((value ?? "unknown").Where(char.IsLetterOrDigit).Take(maxLength).ToArray());
        return string.IsNullOrWhiteSpace(safe) ? "unknown" : safe;
    }

    private static string? SafeText(string? value, string? fallback = null, int maxLength = 160)
    {
        if (string.IsNullOrWhiteSpace(value)) return fallback;
        var sanitized = value.Replace("\r", " ", StringComparison.Ordinal).Replace("\n", " ", StringComparison.Ordinal).Trim();
        return sanitized.Length > maxLength ? sanitized[..maxLength] : sanitized;
    }

    private static string MaskTrace(string? value)
    {
        var digits = new string((value ?? string.Empty).Where(char.IsDigit).ToArray());
        return digits.Length <= 4 ? "N/A" : $"***{digits[^4..]}";
    }

    private static string MaskSensitive(string? value)
    {
        var normalized = new string((value ?? string.Empty).Where(char.IsLetterOrDigit).ToArray());
        return normalized.Length <= 4 ? "N/A" : $"****{normalized[^4..]}";
    }
}
