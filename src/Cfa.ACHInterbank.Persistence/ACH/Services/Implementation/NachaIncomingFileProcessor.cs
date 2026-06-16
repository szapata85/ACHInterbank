using System.Text;
using Cfa.ACHInterbank.Application.ACH.Interfaces;
using Cfa.ACHInterbank.Application.ACH.Models;
using Cfa.ACHInterbank.Domain.Entities.Transactions.Enums;
using Cfa.ACHInterbank.Domain.Models.ACH;
using Cfa.ACHInterbank.Domain.Models.Configurations;
using Cfa.ACHInterbank.Persistence.DataBase;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Cfa.ACHInterbank.Persistence.ACH.Services.Implementation;

[Scoped]
public sealed class NachaIncomingFileProcessor : INachaIncomingFileProcessor
{
    private const int RecordLength = 106;
    private const int BlockingFactor = 10;
    private static readonly HashSet<string> CreditCodes = new(StringComparer.Ordinal) { "21", "22", "23", "31", "32", "33", "42", "51", "52", "53" };
    private static readonly HashSet<string> DebitCodes = new(StringComparer.Ordinal) { "26", "27", "28", "36", "37", "38", "55", "56", "57" };
    private static readonly HashSet<string> PrenoteCodes = new(StringComparer.Ordinal) { "23", "28", "33", "38", "53", "57" };

    private readonly IIncomingNachaIngestionAppService _ingestionService;
    private readonly IIncomingNachaFunctionalClassifier _classifier;
    private readonly AchDbContext _context;
    private readonly ILogger<NachaIncomingFileProcessor> _logger;

    public NachaIncomingFileProcessor(
        IIncomingNachaIngestionAppService ingestionService,
        IIncomingNachaFunctionalClassifier classifier,
        AchDbContext context,
        ILogger<NachaIncomingFileProcessor> logger)
    {
        _ingestionService = ingestionService;
        _classifier = classifier;
        _context = context;
        _logger = logger;
    }

    public async Task<NachaIncomingFileProcessingResult> ProcessAsync(NachaIncomingFileRequest request, CancellationToken ct = default)
    {
        var correlationId = string.IsNullOrWhiteSpace(request.CorrelationId)
            ? Guid.NewGuid().ToString("N")
            : request.CorrelationId.Trim();
        var fileName = Path.GetFileName(request.FileName ?? string.Empty);
        var errors = new List<string>();
        var warnings = new List<string>();
        var bytes = ResolveBytes(request);
        var content = Encoding.UTF8.GetString(bytes);
        var isReturnFile = fileName.EndsWith(".RET", StringComparison.OrdinalIgnoreCase);

        ValidateRequest(fileName, bytes, content, request.IsSimulation, errors);
        if (errors.Count > 0)
        {
            return BuildFailed(request, fileName, correlationId, isReturnFile, errors, warnings);
        }

        IncomingNachaIngestionResponse ingestionResponse;
        try
        {
            ingestionResponse = await _ingestionService.IngestAsync(new IncomingNachaIngestionRequest
            {
                FileStream = new MemoryStream(bytes),
                FileName = fileName,
                ContentType = isReturnFile ? "application/x-nacha-return" : "application/x-nacha",
                RequestedBy = string.IsNullOrWhiteSpace(request.UploadedBy) ? "system" : request.UploadedBy,
                CorrelationId = correlationId
            }, ct);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Fallo controlado en procesamiento NACHA-M entrante 6B.4 para {FileName}", fileName);
            errors.Add(ex.Message);
            return BuildFailed(request, fileName, correlationId, isReturnFile, errors, warnings);
        }

        if (ingestionResponse.IngestionStatus == IncomingNachaIngestionStatus.Duplicado)
        {
            return BuildDuplicate(request, fileName, correlationId, ingestionResponse, isReturnFile);
        }

        if (ingestionResponse.ErrorCount > 0)
        {
            errors.AddRange(ingestionResponse.Errors);
        }

        var loaded = await LoadParsedGraphAsync(ingestionResponse.IngestionId, ct);
        var decisions = await BuildDecisionsAsync(ingestionResponse.IngestionId, loaded.Entries, loaded.Addendas, isReturnFile, ct);
        var flowType = ResolveFlowType(isReturnFile, loaded.Entries.FirstOrDefault(), loaded.Addendas.FirstOrDefault());
        var validationPassed = errors.Count == 0 && ingestionResponse.ParsingStatus is IncomingNachaParsingStatus.Exitoso or IncomingNachaParsingStatus.ExitosoConAdvertencias;
        var persistencePassed = loaded.Headers.Count > 0 && loaded.Entries.Count > 0 && loaded.FileControls.Count > 0;
        var clearingHouseCode = await ResolveClearingHouseCodeAsync(ingestionResponse.ResolvedClearingHouseId, request.ClearingHouseCode, ct);
        var profileCode = ResolveProfileCode(request, clearingHouseCode);

        return new NachaIncomingFileProcessingResult
        {
            CorrelationId = correlationId,
            FileName = fileName,
            ClearingHouseCode = clearingHouseCode,
            ProfileCode = profileCode,
            FlowType = flowType,
            IsReturnFile = isReturnFile,
            IsDuplicate = false,
            ParsedHeaderId = loaded.Headers.FirstOrDefault()?.NachaID,
            BatchCount = loaded.Batches.Count,
            EntryCount = loaded.Entries.Count,
            AddendaCount = loaded.Addendas.Count,
            BatchControlCount = loaded.BatchControls.Count,
            FileControlCount = loaded.FileControls.Count,
            ValidationPassed = validationPassed,
            PersistencePassed = persistencePassed,
            IngestionId = ingestionResponse.IngestionId,
            Decisions = decisions,
            Errors = errors,
            Warnings = warnings,
            Trace = BuildTrace(fileName, correlationId, clearingHouseCode, profileCode, flowType, isReturnFile, validationPassed, persistencePassed, decisions, errors, warnings, ingestionResponse.IngestionId)
        };
    }

    private static byte[] ResolveBytes(NachaIncomingFileRequest request)
    {
        if (request.ContentBytes is { Length: > 0 })
        {
            return request.ContentBytes;
        }

        return string.IsNullOrEmpty(request.Content)
            ? []
            : Encoding.UTF8.GetBytes(request.Content);
    }

    private static void ValidateRequest(string fileName, byte[] bytes, string content, bool isSimulation, List<string> errors)
    {
        if (string.IsNullOrWhiteSpace(fileName))
        {
            errors.Add("FileName es obligatorio para procesamiento NACHA-M entrante.");
        }

        if (bytes.Length == 0)
        {
            errors.Add("El archivo NACHA-M entrante esta vacio.");
            return;
        }

        if (!fileName.EndsWith(".ach", StringComparison.OrdinalIgnoreCase)
            && !fileName.EndsWith(".RET", StringComparison.OrdinalIgnoreCase)
            && !IsOfficialIncomingName(fileName))
        {
            errors.Add("Extension o nombre no permitido. Solo se aceptan archivos .ach, .RET o RRRRTTT.ZZZ.N.");
        }

        if (!isSimulation && !IsOfficialIncomingName(fileName))
        {
            errors.Add("Nombre NACHA-M invalido. Se esperaba RRRRTTT.ZZZ.N o RRRRTTT.ZZZ.RET; .ach queda solo como fixture interno UAT.");
        }

        var records = SplitRecords(content, errors);
        if (records.Count == 0)
        {
            return;
        }

        ValidateFixedWidth(records, errors);
        ValidateControlTotals(records, errors);
    }

    private static bool IsOfficialIncomingName(string fileName)
    {
        var name = Path.GetFileName(fileName);
        if (name.EndsWith(".RET", StringComparison.OrdinalIgnoreCase))
        {
            var prefix = name[..^4];
            return prefix.Length == 11 && prefix[7] == '.' && prefix.Take(7).All(char.IsDigit) && prefix.Skip(8).All(char.IsDigit);
        }

        return System.Text.RegularExpressions.Regex.IsMatch(name, @"^\d{7}\.\d{3}\.[1-9]\d*$", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
    }

    private static IReadOnlyList<string> SplitRecords(string content, List<string> errors)
    {
        if (content.Contains('\n'))
        {
            var lines = content.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries);
            return lines;
        }

        if (content.Length % RecordLength != 0)
        {
            errors.Add($"Longitud fixed-width invalida. El contenido tiene {content.Length} caracteres y no es multiplo de {RecordLength}.");
            return [];
        }

        return Enumerable.Range(0, content.Length / RecordLength)
            .Select(i => content.Substring(i * RecordLength, RecordLength))
            .ToList();
    }

    private static void ValidateFixedWidth(IReadOnlyList<string> records, List<string> errors)
    {
        for (var i = 0; i < records.Count; i++)
        {
            if (records[i].Length != RecordLength)
            {
                errors.Add($"Registro {i + 1} tiene longitud {records[i].Length}; se esperaban {RecordLength}.");
            }

            if (records[i].Length == 0 || !"156789".Contains(records[i][0]))
            {
                errors.Add($"Registro {i + 1} tiene record type invalido.");
            }
        }

        var firstPadding = records.ToList().FindIndex(IsPaddingRecord);
        if (firstPadding >= 0 && records.Skip(firstPadding).Any(x => !IsPaddingRecord(x)))
        {
            errors.Add("Padding intermedio detectado. Los registros de padding deben aparecer solo al final.");
        }

        if (records.Any(IsPaddingRecord) && records.TakeWhile(x => !IsPaddingRecord(x)).All(x => x[0] != '9'))
        {
            errors.Add("FileControl faltante antes del padding.");
        }

        if (records.Count % BlockingFactor != 0)
        {
            errors.Add($"Cantidad final de registros invalida: {records.Count}. Debe ser multiplo de {BlockingFactor}.");
        }
    }

    private static void ValidateControlTotals(IReadOnlyList<string> records, List<string> errors)
    {
        var businessRecords = records.Where(x => !IsPaddingRecord(x)).ToList();
        var entries = businessRecords.Where(x => x[0] == '6').ToList();
        var addendas = businessRecords.Where(x => x[0] == '7').ToList();
        var fileControl = businessRecords.LastOrDefault(x => x[0] == '9');
        if (businessRecords.All(x => x[0] != '1')) errors.Add("FileHeader faltante.");
        if (businessRecords.All(x => x[0] != '5')) errors.Add("BatchHeader faltante.");
        if (entries.Count == 0) errors.Add("EntryDetail faltante.");
        if (businessRecords.All(x => x[0] != '8')) errors.Add("BatchControl faltante.");
        if (fileControl is null)
        {
            errors.Add("FileControl faltante.");
            return;
        }

        var entryHash = entries.Sum(x => long.Parse(x.Substring(3, 8))) % 10_000_000_000L;
        var debit = entries.Where(x => DebitCodes.Contains(x.Substring(1, 2))).Sum(x => long.Parse(x.Substring(29, 18)));
        var credit = entries.Where(x => CreditCodes.Contains(x.Substring(1, 2))).Sum(x => long.Parse(x.Substring(29, 18)));

        if (long.Parse(fileControl.Substring(21, 10)) != entryHash)
        {
            errors.Add("EntryHash invalido contra FileControl.");
        }

        if (long.Parse(fileControl.Substring(31, 18)) != debit)
        {
            errors.Add("TotalDebitAmount invalido contra FileControl.");
        }

        if (long.Parse(fileControl.Substring(49, 18)) != credit)
        {
            errors.Add("TotalCreditAmount invalido contra FileControl.");
        }

        if (int.Parse(fileControl.Substring(13, 8)) != entries.Count + addendas.Count)
        {
            errors.Add("EntryAddendaCount invalido contra FileControl.");
        }

        if (int.Parse(fileControl.Substring(7, 6)) != records.Count / BlockingFactor)
        {
            errors.Add("BlockCount invalido contra FileControl.");
        }
    }

    private async Task<ParsedGraph> LoadParsedGraphAsync(Guid ingestionId, CancellationToken ct)
    {
        var headers = await _context.NachaHeaders.AsNoTracking()
            .Where(x => x.IncomingNachaFileIngestionId == ingestionId)
            .ToListAsync(ct);
        var headerIds = headers.Select(x => x.NachaID).Where(x => x != null).ToList();

        var batches = await _context.BatchHeaders.AsNoTracking().Where(x => headerIds.Contains(x.NachaID)).ToListAsync(ct);
        var entries = await _context.EntryDetails.AsNoTracking().Where(x => headerIds.Contains(x.NachaID)).ToListAsync(ct);
        var addendas = await _context.AddendaRecords.AsNoTracking().Where(x => headerIds.Contains(x.NachaID)).ToListAsync(ct);
        var batchControls = await _context.BatchControls.AsNoTracking().Where(x => headerIds.Contains(x.NachaID)).ToListAsync(ct);
        var fileControls = await _context.FileControls.AsNoTracking().Where(x => headerIds.Contains(x.NachaID)).ToListAsync(ct);

        return new ParsedGraph(headers, batches, entries, addendas, batchControls, fileControls);
    }

    private async Task<IReadOnlyList<NachaIncomingDecision>> BuildDecisionsAsync(
        Guid ingestionId,
        IReadOnlyList<EntryDetail> entries,
        IReadOnlyList<AddendaRecord> addendas,
        bool isReturnFile,
        CancellationToken ct)
    {
        var links = await _context.IncomingNachaTransactionLinks.AsNoTracking()
            .Where(x => x.IncomingNachaFileIngestionId == ingestionId)
            .ToListAsync(ct);
        var classifications = await _context.IncomingNachaEntryClassifications.AsNoTracking()
            .Where(x => x.IncomingNachaFileIngestionId == ingestionId)
            .ToListAsync(ct);
        var transactionIds = links.Where(x => x.AchTransactionId.HasValue).Select(x => x.AchTransactionId!.Value).Distinct().ToArray();
        var transactions = await _context.AchTransactions.AsNoTracking()
            .Include(x => x.SourceInstitution)
            .Where(x => transactionIds.Contains(x.Id))
            .ToDictionaryAsync(x => x.Id, ct);
        var defaultOriginCodes = await _context.FinancialInstitutions.AsNoTracking()
            .Where(x => x.IsDefaultSource)
            .Select(x => x.RoutingNumber + x.TransitCode)
            .ToListAsync(ct);

        var decisions = new List<NachaIncomingDecision>();
        foreach (var entry in entries)
        {
            var addenda = addendas.FirstOrDefault(x => IsAddendaForEntry(entry, x));
            var persistedClassification = classifications.FirstOrDefault(x => x.EntryDetailId == entry.EntryDetailID);
            var classification = persistedClassification is null
                ? _classifier.Classify(entry, addenda)
                : new IncomingNachaClassificationResult
                {
                    FunctionalClass = persistedClassification.FunctionalClass,
                    EligibilityStatus = persistedClassification.EligibilityStatus,
                    RequiresLink = persistedClassification.RequiresLink,
                    RequiresManualResolution = persistedClassification.RequiresManualResolution,
                    OriginalTraceRef = persistedClassification.OriginalTraceRef,
                    ReturnReasonCode = persistedClassification.ReturnReasonCode,
                    PrenoteStatus = persistedClassification.PrenoteStatus,
                    BusinessMeaning = persistedClassification.BusinessMeaning,
                    ClassificationEvidenceJson = persistedClassification.ClassificationEvidenceJson,
                    ClassifierVersion = persistedClassification.ClassifierVersion
                };
            var link = links.FirstOrDefault(x => x.EntryDetailId == entry.EntryDetailID);
            transactions.TryGetValue(link?.AchTransactionId ?? 0, out var transaction);
            var entryOriginatesFromDefaultSource = defaultOriginCodes.Any(code =>
                !string.IsNullOrWhiteSpace(entry.SequenceNumber)
                && entry.SequenceNumber.StartsWith(code, StringComparison.Ordinal));

            decisions.Add(BuildDecision(entry, addenda, classification, link, transaction, isReturnFile, entryOriginatesFromDefaultSource));
        }

        return decisions;
    }

    private static NachaIncomingDecision BuildDecision(
        EntryDetail entry,
        AddendaRecord? addenda,
        IncomingNachaClassificationResult classification,
        IncomingNachaTransactionLink? link,
        AchTransaction? transaction,
        bool isReturnFile,
        bool entryOriginatesFromDefaultSource)
    {
        if (link?.LinkType is IncomingNachaLinkType.Ambiguous or IncomingNachaLinkType.NotFound)
        {
            return ManualReview(entry, addenda, link.AchTransactionId, "Correlacion no deterministica.");
        }

        return classification.FunctionalClass switch
        {
            IncomingNachaFunctionalClass.CreditoEntrante => new NachaIncomingDecision
            {
                EntryTraceNumber = entry.SequenceNumber ?? string.Empty,
                TransactionId = link?.AchTransactionId,
                DecisionType = NachaIncomingDecisionType.ApplyCreditMovement,
                RequiresMonetaryMovement = true,
                SoapOperation = NachaSoapOperationCandidate.ProcTransacciones,
                NewInternalStatus = "ReadyForCreditMovement",
                AuditMessage = "Credito monetario entrante originado por entidad externa; se prepara Proc_Transacciones sin invocar SOAP real."
            },
            IncomingNachaFunctionalClass.DebitoEntrante when entryOriginatesFromDefaultSource || transaction?.SourceInstitution?.IsDefaultSource == true || transaction?.Type == TransactionTypeEnum.Debit => new NachaIncomingDecision
            {
                EntryTraceNumber = entry.SequenceNumber ?? string.Empty,
                TransactionId = link?.AchTransactionId,
                DecisionType = NachaIncomingDecisionType.ApplyDebitMovement,
                RequiresMonetaryMovement = true,
                SoapOperation = NachaSoapOperationCandidate.ProcContrapartidas,
                NewInternalStatus = "ReadyForDebitMovement",
                AuditMessage = "Debito originado por CFA correlacionado; se prepara Proc_Contrapartidas sin invocar SOAP real."
            },
            IncomingNachaFunctionalClass.DebitoEntrante => ManualReview(entry, addenda, link?.AchTransactionId, "Debito entrante sin evidencia suficiente de origen CFA."),
            IncomingNachaFunctionalClass.Prenotificacion when classification.PrenoteStatus == IncomingNachaPrenoteStatus.RechazaTercero => new NachaIncomingDecision
            {
                EntryTraceNumber = entry.SequenceNumber ?? string.Empty,
                TransactionId = link?.AchTransactionId,
                DecisionType = NachaIncomingDecisionType.RejectPrenotification,
                RequiresMonetaryMovement = false,
                SoapOperation = NachaSoapOperationCandidate.RegistrarRespuestaTransaccion,
                ReasonCode = classification.ReturnReasonCode,
                NewInternalStatus = "PrenotificationRejected",
                AuditMessage = "Respuesta de prenotificacion rechazada; no genera movimiento monetario."
            },
            IncomingNachaFunctionalClass.Prenotificacion => new NachaIncomingDecision
            {
                EntryTraceNumber = entry.SequenceNumber ?? string.Empty,
                TransactionId = link?.AchTransactionId,
                DecisionType = NachaIncomingDecisionType.ApprovePrenotification,
                RequiresMonetaryMovement = false,
                SoapOperation = NachaSoapOperationCandidate.RegistrarRespuestaTransaccion,
                NewInternalStatus = "PrenotificationApproved",
                AuditMessage = "Respuesta de prenotificacion aprobada; no genera movimiento monetario."
            },
            IncomingNachaFunctionalClass.Devolucion or IncomingNachaFunctionalClass.RechazadaOperador or IncomingNachaFunctionalClass.RetornoEpr => new NachaIncomingDecision
            {
                EntryTraceNumber = entry.SequenceNumber ?? string.Empty,
                OriginalTraceNumber = classification.OriginalTraceRef ?? addenda?.OriginalTraceNumber,
                TransactionId = link?.AchTransactionId,
                DecisionType = NachaIncomingDecisionType.RegisterDifferentialResponse,
                RequiresMonetaryMovement = false,
                SoapOperation = NachaSoapOperationCandidate.RegistrarRespuestaTransaccion,
                ReasonCode = classification.ReturnReasonCode ?? addenda?.ReturnReasonCode,
                ReasonDescription = isReturnFile ? "Archivo .RET procesado como respuesta diferencial." : "Respuesta diferencial NACHA-M.",
                NewInternalStatus = "DifferentialResponseRegistered",
                AuditMessage = ".RET/respuesta diferencial no mueve dinero directamente; se prepara RegistrarRespuestaTransaccion."
            },
            _ => ManualReview(entry, addenda, link?.AchTransactionId, "Clase funcional no resoluble automaticamente.")
        };
    }

    private static NachaIncomingDecision ManualReview(EntryDetail entry, AddendaRecord? addenda, int? transactionId, string reason)
        => new()
        {
            EntryTraceNumber = entry.SequenceNumber ?? string.Empty,
            OriginalTraceNumber = addenda?.OriginalTraceNumber,
            TransactionId = transactionId,
            DecisionType = NachaIncomingDecisionType.ManualReviewRequired,
            RequiresMonetaryMovement = false,
            SoapOperation = NachaSoapOperationCandidate.None,
            ReasonDescription = reason,
            NewInternalStatus = "ManualReviewRequired",
            AuditMessage = reason
        };

    private async Task<string> ResolveClearingHouseCodeAsync(int? clearingHouseId, string fallback, CancellationToken ct)
    {
        if (clearingHouseId.HasValue)
        {
            var code = await _context.ClearingHouses.AsNoTracking()
                .Where(x => x.Id == clearingHouseId.Value)
                .Select(x => x.Code)
                .FirstOrDefaultAsync(ct);
            if (!string.IsNullOrWhiteSpace(code))
            {
                return code;
            }
        }

        return string.IsNullOrWhiteSpace(fallback) ? "UNKNOWN" : fallback.Trim();
    }

    private static NachaIncomingFlowType ResolveFlowType(bool isReturnFile, EntryDetail? entry, AddendaRecord? addenda)
    {
        if (isReturnFile || string.Equals(addenda?.CodeTypeAddendumRecord?.Trim(), "99", StringComparison.OrdinalIgnoreCase))
        {
            return NachaIncomingFlowType.ReturnFile;
        }

        var code = entry?.TransactionCode?.Trim() ?? string.Empty;
        if (PrenoteCodes.Contains(code)) return NachaIncomingFlowType.PrenotificationResponse;
        if (CreditCodes.Contains(code)) return NachaIncomingFlowType.IncomingCreditFromExternalOriginator;
        if (DebitCodes.Contains(code)) return NachaIncomingFlowType.IncomingDebitFromExternalOriginator;
        return NachaIncomingFlowType.Unknown;
    }

    private static string ResolveProfileCode(NachaIncomingFileRequest request, string clearingHouseCode)
    {
        if (!string.IsNullOrWhiteSpace(request.ExpectedProfileCode))
        {
            return request.ExpectedProfileCode.Trim();
        }

        return clearingHouseCode.Equals("CENIT", StringComparison.OrdinalIgnoreCase)
            ? "OFFICIAL_CENIT_ENTRADA_ORIGINAL_V1_0"
            : "OFFICIAL_ACH_ENTRADA_ORIGINAL_V1_0";
    }

    private static Dictionary<string, string> BuildTrace(
        string fileName,
        string correlationId,
        string clearingHouseCode,
        string profileCode,
        NachaIncomingFlowType flowType,
        bool isReturnFile,
        bool validationPassed,
        bool persistencePassed,
        IReadOnlyList<NachaIncomingDecision> decisions,
        IReadOnlyList<string> errors,
        IReadOnlyList<string> warnings,
        Guid? ingestionId)
        => new(StringComparer.OrdinalIgnoreCase)
        {
            ["Phase"] = "6B.4",
            ["FileName"] = fileName,
            ["CorrelationId"] = correlationId,
            ["ClearingHouseCode"] = clearingHouseCode,
            ["ProfileCode"] = profileCode,
            ["FlowType"] = flowType.ToString(),
            ["IsReturnFile"] = isReturnFile.ToString(),
            ["ValidationSummary"] = validationPassed ? "Passed" : "Failed",
            ["ParseSummary"] = errors.Count == 0 ? "Parsed" : "BlockedOrFailed",
            ["PersistenceSummary"] = persistencePassed ? "Persisted" : "NotPersisted",
            ["CorrelationSummary"] = decisions.Any(x => x.DecisionType == NachaIncomingDecisionType.ManualReviewRequired) ? "ManualReviewRequired" : "ResolvedOrNotRequired",
            ["DecisionSummary"] = string.Join(",", decisions.Select(x => x.DecisionType).Distinct()),
            ["MonetaryMovementRequired"] = decisions.Any(x => x.RequiresMonetaryMovement).ToString(),
            ["SoapOperationCandidate"] = string.Join(",", decisions.Select(x => x.SoapOperation).Distinct()),
            ["ProductiveExecution"] = "false",
            ["NoGoReason"] = "Productivo permanece NO-GO; fase solo prepara candidatos SOAP.",
            ["IngestionId"] = ingestionId?.ToString("D") ?? string.Empty,
            ["WarningCount"] = warnings.Count.ToString(),
            ["ErrorCount"] = errors.Count.ToString()
        };

    private NachaIncomingFileProcessingResult BuildFailed(
        NachaIncomingFileRequest request,
        string fileName,
        string correlationId,
        bool isReturnFile,
        IReadOnlyList<string> errors,
        IReadOnlyList<string> warnings)
    {
        var clearingHouseCode = string.IsNullOrWhiteSpace(request.ClearingHouseCode) ? "UNKNOWN" : request.ClearingHouseCode;
        var profileCode = ResolveProfileCode(request, clearingHouseCode);
        var trace = BuildTrace(fileName, correlationId, clearingHouseCode, profileCode, isReturnFile ? NachaIncomingFlowType.ReturnFile : NachaIncomingFlowType.Unknown, isReturnFile, false, false, [], errors, warnings, null);
        return new NachaIncomingFileProcessingResult
        {
            CorrelationId = correlationId,
            FileName = fileName,
            ClearingHouseCode = clearingHouseCode,
            ProfileCode = profileCode,
            IsReturnFile = isReturnFile,
            ValidationPassed = false,
            PersistencePassed = false,
            Errors = errors,
            Warnings = warnings,
            Trace = trace
        };
    }

    private static NachaIncomingFileProcessingResult BuildDuplicate(
        NachaIncomingFileRequest request,
        string fileName,
        string correlationId,
        IncomingNachaIngestionResponse response,
        bool isReturnFile)
    {
        var clearingHouseCode = string.IsNullOrWhiteSpace(request.ClearingHouseCode) ? "UNKNOWN" : request.ClearingHouseCode;
        var profileCode = ResolveProfileCode(request, clearingHouseCode);
        var decisions = new[]
        {
            new NachaIncomingDecision
            {
                DecisionType = NachaIncomingDecisionType.IgnoreDuplicate,
                RequiresMonetaryMovement = false,
                SoapOperation = NachaSoapOperationCandidate.None,
                NewInternalStatus = "DuplicateIgnored",
                AuditMessage = "Archivo duplicado detectado por hash/tamano; no se duplican registros ni decisiones."
            }
        };
        return new NachaIncomingFileProcessingResult
        {
            CorrelationId = correlationId,
            FileName = fileName,
            ClearingHouseCode = clearingHouseCode,
            ProfileCode = profileCode,
            FlowType = isReturnFile ? NachaIncomingFlowType.ReturnFile : NachaIncomingFlowType.Unknown,
            IsReturnFile = isReturnFile,
            IsDuplicate = true,
            ValidationPassed = true,
            PersistencePassed = true,
            IngestionId = response.IngestionId,
            Decisions = decisions,
            Errors = response.Errors,
            Trace = BuildTrace(fileName, correlationId, clearingHouseCode, profileCode, isReturnFile ? NachaIncomingFlowType.ReturnFile : NachaIncomingFlowType.Unknown, isReturnFile, true, true, decisions, response.Errors, [], response.IngestionId)
        };
    }

    private static bool IsPaddingRecord(string record) => record.Length == RecordLength && record.All(x => x == '9');

    private static bool IsAddendaForEntry(EntryDetail entry, AddendaRecord addenda)
    {
        var entrySuffix = GetEntrySequenceSuffix(entry.SequenceNumber);
        var addendaSuffix = GetEntrySequenceSuffix(addenda.EntryDetailSequenceNumber);
        return !string.IsNullOrWhiteSpace(entrySuffix)
               && !string.IsNullOrWhiteSpace(addendaSuffix)
               && string.Equals(entrySuffix, addendaSuffix, StringComparison.Ordinal);
    }

    private static string? GetEntrySequenceSuffix(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return null;
        var digits = new string(value.Where(char.IsDigit).ToArray());
        return digits.Length < 7 ? null : digits[^7..];
    }

    private sealed record ParsedGraph(
        IReadOnlyList<NachaHeader> Headers,
        IReadOnlyList<BatchHeader> Batches,
        IReadOnlyList<EntryDetail> Entries,
        IReadOnlyList<AddendaRecord> Addendas,
        IReadOnlyList<BatchControl> BatchControls,
        IReadOnlyList<FileControl> FileControls);
}
