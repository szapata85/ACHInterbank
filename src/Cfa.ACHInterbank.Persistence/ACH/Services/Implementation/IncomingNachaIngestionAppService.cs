using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Cfa.ACHInterbank.Application.ACH.Interfaces;
using Cfa.ACHInterbank.Application.ACH.Models;
using Cfa.ACHInterbank.Application.ACH.Interfaces.ExternalFileNames;
using Cfa.ACHInterbank.Application.ACH.Models.ExternalFileNames;
using Cfa.ACHInterbank.Application.ACHSobreDigital.ManagedDigitalEnvelope;
using Cfa.ACHInterbank.Domain.Models.ACH;
using Cfa.ACHInterbank.Domain.Models.Configurations;
using Cfa.ACHInterbank.Persistence.ACH.Services.Implementation.ExternalFileNames;
using Cfa.ACHInterbank.Persistence.DataBase;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Cfa.ACHInterbank.Persistence.ACH.Services.Implementation;

[Scoped]
public class IncomingNachaIngestionAppService : IIncomingNachaIngestionAppService
{
    private readonly AchDbContext _context;
    private readonly IIncomingNachaCycleResolver _cycleResolver;
    private readonly INachaParserService _parserService;
    private readonly IIncomingNachaPostParseProcessor _postParseProcessor;
    private readonly IExternalFileNamePolicy _externalFileNamePolicy;
    private readonly ILogger<IncomingNachaIngestionAppService> _logger;
    private readonly INachaConfigResolver _profileResolver;
    private readonly IManagedDigitalEnvelopeService? _digitalEnvelopeService;
    private readonly IIncomingNachaAdmissionValidator _admissionValidator;
    private readonly TimeProvider _timeProvider;

    public IncomingNachaIngestionAppService(
        AchDbContext context,
        IIncomingNachaCycleResolver cycleResolver,
        INachaParserService parserService,
        IIncomingNachaPostParseProcessor postParseProcessor,
        IExternalFileNamePolicy externalFileNamePolicy,
        ILogger<IncomingNachaIngestionAppService> logger,
        INachaConfigResolver? profileResolver = null,
        IManagedDigitalEnvelopeService? digitalEnvelopeService = null,
        IIncomingNachaAdmissionValidator? admissionValidator = null,
        TimeProvider? timeProvider = null)
    {
        _context = context;
        _cycleResolver = cycleResolver;
        _parserService = parserService;
        _postParseProcessor = postParseProcessor;
        _externalFileNamePolicy = externalFileNamePolicy;
        _logger = logger;
        _profileResolver = profileResolver ?? new NachaConfigResolver(context);
        _digitalEnvelopeService = digitalEnvelopeService;
        _timeProvider = timeProvider ?? TimeProvider.System;
        // El runtime DI siempre resuelve la política persistente. El fallback conserva
        // compatibilidad de constructores usados por pruebas/hosts embebidos legados.
        _admissionValidator = admissionValidator ?? new LegacyCompatibleAdmissionValidator();
    }

    public async Task<IncomingNachaIngestionResponse> IngestAsync(IncomingNachaIngestionRequest request, CancellationToken ct = default)
    {
        var correlationId = string.IsNullOrWhiteSpace(request.CorrelationId)
            ? Guid.NewGuid().ToString("N")
            : request.CorrelationId.Trim();

        byte[] fileBytes;
        await using (var ms = new MemoryStream())
        {
            await request.FileStream.CopyToAsync(ms, ct);
            fileBytes = ms.ToArray();
        }

        var fileHash = ComputeSha256(fileBytes);
        var sameNameDifferentContent = await _context.IncomingNachaFileIngestions
            .AsNoTracking()
            .AnyAsync(x => x.FileName == request.FileName
                           && (x.FileHashSha256 != fileHash || x.FileSize != fileBytes.LongLength), ct);

        var candidatesByFingerprint = await _context.IncomingNachaFileIngestions.AsNoTracking()
            .Where(x => x.FileHashSha256 == fileHash && x.FileSize == fileBytes.LongLength)
            .OrderByDescending(x => x.UploadedAtUtc)
            .ToListAsync(ct);

        var canonicalCandidate = candidatesByFingerprint.FirstOrDefault(x => !x.IsReprocess) ?? candidatesByFingerprint.FirstOrDefault();
        var parentIngestionId = request.ParentIngestionId;
        if (request.ForceReprocess)
        {
            if (canonicalCandidate is null)
            {
                throw new ArgumentException("No existe una ingesta base para reprocesar el archivo indicado por hash/tamaño.");
            }

            parentIngestionId ??= canonicalCandidate.Id;
            if (canonicalCandidate.IsReprocess || canonicalCandidate.Id != parentIngestionId.Value)
            {
                throw new ArgumentException("ParentIngestionId debe corresponder a la ingesta canónica original del archivo.");
            }

            var effectiveCandidate = SelectEffectiveCandidate(candidatesByFingerprint, canonicalCandidate);
            var latestEffectiveResultIsReprocessable = await _context.IncomingNachaFileProcessingResults
                .AsNoTracking()
                .Where(x => x.IncomingNachaFileIngestionId == effectiveCandidate.Id)
                .OrderByDescending(x => x.AttemptNumber)
                .ThenByDescending(x => x.StartedAtUtc)
                .Select(x => (bool?)x.IsReprocessable)
                .FirstOrDefaultAsync(ct);
            if (latestEffectiveResultIsReprocessable == false)
            {
                throw new ArgumentException("La última ejecución efectiva del archivo no está autorizada para reproceso.");
            }

            var ingestionIds = candidatesByFingerprint.Select(x => x.Id).ToArray();
            var hasPersistedProcessing = await _context.NachaHeaders.AsNoTracking()
                    .AnyAsync(x => x.IncomingNachaFileIngestionId.HasValue
                                   && ingestionIds.Contains(x.IncomingNachaFileIngestionId.Value), ct)
                || await _context.IncomingNachaTransactionLinks.AsNoTracking()
                    .AnyAsync(x => ingestionIds.Contains(x.IncomingNachaFileIngestionId), ct)
                || await _context.IncomingNachaEntryClassifications.AsNoTracking()
                    .AnyAsync(x => ingestionIds.Contains(x.IncomingNachaFileIngestionId), ct)
                || await _context.IncomingNachaDispatchQueue.AsNoTracking()
                    .AnyAsync(x => ingestionIds.Contains(x.IncomingNachaFileIngestionId), ct);
            if (hasPersistedProcessing)
            {
                throw new ArgumentException(
                    "El reproceso de la ingesta base está bloqueado porque ya existe persistencia canónica o despacho asociado.");
            }
        }

        if (!request.ForceReprocess && canonicalCandidate is not null)
        {
            var effectiveCandidate = SelectEffectiveCandidate(candidatesByFingerprint, canonicalCandidate);
            var effectiveProcessing = await GetLatestNonDuplicateProcessingAsync(effectiveCandidate.Id, ct);
            var canReprocess = CanReprocessDuplicate(candidatesByFingerprint, effectiveCandidate);
            var nextAttempt = await _context.IncomingNachaFileProcessingResults
                .AsNoTracking()
                .Where(x => x.IncomingNachaFileIngestionId == canonicalCandidate.Id)
                .Select(x => (int?)x.AttemptNumber)
                .MaxAsync(ct) ?? 0;

            _context.IncomingNachaFileProcessingResults.Add(new IncomingNachaFileProcessingResult
            {
                IncomingNachaFileIngestionId = canonicalCandidate.Id,
                AttemptNumber = nextAttempt + 1,
                StartedAtUtc = UtcNow,
                FinishedAtUtc = UtcNow,
                OutcomeStatus = IncomingNachaProcessingOutcomeStatus.Duplicado,
                FailureStage = "ValidacionDuplicidad",
                ErrorCount = 1,
                ParserErrorsJson = JsonSerializer.Serialize(new[] { "Archivo duplicado" }),
                IsReprocessable = canReprocess
            });
            _context.IncomingNachaProcessingEvents.Add(new IncomingNachaProcessingEvent
            {
                IncomingNachaFileIngestionId = canonicalCandidate.Id,
                EventType = "DuplicateUploadAttempt",
                EventStatus = IncomingNachaIngestionStatus.Duplicado.ToString(),
                Message = "Segundo intento auditado; no se repitieron parsing, evento funcional ni despacho.",
                EvidenceJson = JsonSerializer.Serialize(new
                {
                    attemptedFileName = request.FileName,
                    samePhysicalName = string.Equals(request.FileName, canonicalCandidate.FileName, StringComparison.OrdinalIgnoreCase),
                    contentFingerprintMatched = true
                }),
                RaisedBy = "IncomingNachaIngestionAppService"
            });

            await _context.SaveChangesAsync(ct);
            return new IncomingNachaIngestionResponse
            {
                IngestionId = effectiveCandidate.Id,
                OriginalFileName = effectiveCandidate.FileName,
                FileHash = effectiveCandidate.FileHashSha256,
                CorrelationId = effectiveCandidate.CorrelationId,
                IngestionStatus = IncomingNachaIngestionStatus.Duplicado,
                CycleResolutionStatus = effectiveCandidate.CycleResolutionStatus,
                ParsingStatus = effectiveCandidate.ParsingStatus,
                DetectedClearingHouseId = effectiveCandidate.DetectedClearingHouseId,
                ResolvedClearingHouseId = effectiveCandidate.ResolvedClearingHouseId,
                ResolvedAchCycleId = effectiveCandidate.ResolvedAchCycleId,
                OperationalDate = effectiveCandidate.OperationalDate,
                SelectedProfileCode = effectiveCandidate.ProfileCode,
                SelectedProfileVersion = effectiveCandidate.ProfileVersion,
                TotalBatches = effectiveProcessing?.TotalBatches ?? 0,
                TotalEntries = effectiveProcessing?.TotalEntries ?? 0,
                TotalAddendas = effectiveProcessing?.TotalAddendas ?? 0,
                WarningCount = effectiveProcessing?.WarningCount ?? 0,
                ErrorCount = 1,
                Errors = new[] { "Archivo duplicado detectado por hash/tamaño." }
            };
        }

        var ingestionId = Guid.NewGuid();
        var ingestion = new IncomingNachaFileIngestion
        {
            Id = ingestionId,
            FileName = request.FileName,
            FileHashSha256 = fileHash,
            FileSize = fileBytes.LongLength,
            ContentType = request.ContentType,
            FileExtension = Path.GetExtension(request.FileName),
            UploadedBy = string.IsNullOrWhiteSpace(request.RequestedBy) ? "system" : request.RequestedBy,
            ReceivedBy = string.IsNullOrWhiteSpace(request.RequestedBy) ? "system" : request.RequestedBy,
            ReceivedAtUtc = UtcNow,
            CorrelationId = correlationId,
            ParentIngestionId = parentIngestionId,
            IsReprocess = request.ForceReprocess,
            IngestionStatus = IncomingNachaIngestionStatus.Recibido,
            Stage = IncomingNachaIngestionStage.Received,
            ParsingStatus = IncomingNachaParsingStatus.NoEjecutado,
            CycleResolutionStatus = IncomingNachaCycleResolutionStatus.NoIntentado,
            Notes = request.ForceReprocess
                ? $"Reproceso autorizado. ParentIngestionId={parentIngestionId}"
                : "Archivo recibido.",
            ResolutionEvidenceJson = request.ForceReprocess
                    ? JsonSerializer.Serialize(new
                {
                    eventType = "ReprocesoAutorizado",
                    parentIngestionId,
                    fileHash,
                    fileSize = fileBytes.LongLength
                })
                : "{}"
        };

        _context.IncomingNachaFileIngestions.Add(ingestion);
        if (sameNameDifferentContent)
        {
            _context.IncomingNachaProcessingEvents.Add(new IncomingNachaProcessingEvent
            {
                IncomingNachaFileIngestionId = ingestion.Id,
                EventType = "FileNameContentConflict",
                EventStatus = "Detected",
                Message = "El nombre físico ya existe con contenido diferente; se conserva como ingesta independiente.",
                EvidenceJson = JsonSerializer.Serialize(new
                {
                    fileName = request.FileName,
                    contentFingerprintMatched = false,
                    overwritePrevented = true
                }),
                RaisedBy = "IncomingNachaIngestionAppService"
            });
        }

        try
        {
            await _context.SaveChangesAsync(ct);
        }
        catch (DbUpdateException)
        {
            // Hardening DB-first: carrera multiinstancia/multinodo para el mismo hash+tamaño.
            if (!request.ForceReprocess)
            {
                var existing = await _context.IncomingNachaFileIngestions.AsNoTracking()
                    .Where(x => !x.IsReprocess && x.FileHashSha256 == fileHash && x.FileSize == fileBytes.LongLength)
                    .OrderByDescending(x => x.UploadedAtUtc)
                    .FirstOrDefaultAsync(ct);
                if (existing is not null)
                {
                    var nextAttempt = await _context.IncomingNachaFileProcessingResults
                        .AsNoTracking()
                        .Where(x => x.IncomingNachaFileIngestionId == existing.Id)
                        .Select(x => (int?)x.AttemptNumber)
                        .MaxAsync(ct) ?? 0;

                    _context.IncomingNachaFileProcessingResults.Add(new IncomingNachaFileProcessingResult
                    {
                        IncomingNachaFileIngestionId = existing.Id,
                        AttemptNumber = nextAttempt + 1,
                StartedAtUtc = UtcNow,
                FinishedAtUtc = UtcNow,
                        OutcomeStatus = IncomingNachaProcessingOutcomeStatus.Duplicado,
                        FailureStage = "ValidacionDuplicidadDbFirst",
                        ErrorCount = 1,
                        ParserErrorsJson = JsonSerializer.Serialize(new[] { "Archivo duplicado (DB-first)." }),
                        IsReprocessable = true
                    });
                    await _context.SaveChangesAsync(ct);
                    return new IncomingNachaIngestionResponse
                    {
                        IngestionId = existing.Id,
                        OriginalFileName = existing.FileName,
                        FileHash = existing.FileHashSha256,
                        CorrelationId = existing.CorrelationId,
                        IngestionStatus = IncomingNachaIngestionStatus.Duplicado,
                        CycleResolutionStatus = existing.CycleResolutionStatus,
                        ParsingStatus = existing.ParsingStatus,
                        DetectedClearingHouseId = existing.DetectedClearingHouseId,
                        ResolvedClearingHouseId = existing.ResolvedClearingHouseId,
                        ResolvedAchCycleId = existing.ResolvedAchCycleId,
                        OperationalDate = existing.OperationalDate,
                        ErrorCount = 1,
                        Errors = new[] { "Archivo duplicado detectado por hash/tamaño (DB-first)." }
                    };
                }
            }

            throw;
        }

        var processingFileBytes = fileBytes;
        var processingFileName = request.FileName;
        var processingFileHash = fileHash;
        var containsDecryptedPlaintext = false;

        try
        {
            if (IsDigitalEnvelopeFile(request.FileName))
            {
                IncomingNachaStageTransitions.MoveTo(ingestion, IncomingNachaIngestionStage.Decrypting);
                await _context.SaveChangesAsync(ct);
                if (!request.RequestedClearingHouseId.HasValue || request.RequestedClearingHouseId.Value <= 0)
                {
                    throw new ManagedDigitalEnvelopeException(
                        "DIGITAL_ENVELOPE_CLEARING_HOUSE_REQUIRED",
                        "ClearingHouseId es obligatorio para descifrar un archivo .env.");
                }

                var selectedClearingHouse = await _context.ClearingHouses
                    .AsNoTracking()
                    .FirstOrDefaultAsync(x => x.Id == request.RequestedClearingHouseId.Value, ct)
                    ?? throw new ManagedDigitalEnvelopeException(
                        "DIGITAL_ENVELOPE_CLEARING_HOUSE_NOT_FOUND",
                        "La cámara seleccionada no existe.");

                if (!string.Equals(selectedClearingHouse.Code, "ACHCOL", StringComparison.OrdinalIgnoreCase))
                {
                    throw new ManagedDigitalEnvelopeException(
                        "DIGITAL_ENVELOPE_CLEARING_HOUSE_INVALID",
                        "Los archivos .env sólo se admiten para la cámara ACH Colombia.");
                }

                if (_digitalEnvelopeService is null)
                {
                    throw new ManagedDigitalEnvelopeException(
                        "DIGITAL_ENVELOPE_SERVICE_UNAVAILABLE",
                        "El servicio canónico de sobre digital no está disponible.");
                }

                var decrypted = await _digitalEnvelopeService.DecryptAsync(
                    new ManagedDigitalEnvelopeRequest(
                        CertificateVersionId: 0,
                        FileName: request.FileName,
                        Content: fileBytes,
                        Actor: string.IsNullOrWhiteSpace(request.RequestedBy) ? "system" : request.RequestedBy,
                        ClearingHouseId: selectedClearingHouse.Id,
                        OperationMode: "LIVE"),
                    ct);

                processingFileBytes = decrypted.Content;
                processingFileName = decrypted.FileName;
                processingFileHash = ComputeSha256(processingFileBytes);
                containsDecryptedPlaintext = true;

                _context.IncomingNachaProcessingEvents.Add(new IncomingNachaProcessingEvent
                {
                    IncomingNachaFileIngestionId = ingestion.Id,
                    EventType = "DigitalEnvelopeDecrypted",
                    EventStatus = "Applied",
                    Message = "Sobre digital ACH Colombia descifrado en memoria y entregado al pipeline NACHA-M canónico.",
                    EvidenceJson = JsonSerializer.Serialize(new
                    {
                        sourceFileName = request.FileName,
                        canonicalFileName = processingFileName,
                        sourceHashSha256 = fileHash,
                        canonicalHashSha256 = processingFileHash,
                        certificateVersionId = decrypted.CertificateVersionId,
                        cryptographicProfile = decrypted.CryptographicProfile,
                        plaintextPersistedToDisk = false
                    }),
                    RaisedBy = "IncomingNachaIngestionAppService"
                });
                await _context.SaveChangesAsync(ct);
            }

            var records = ChunkFixed106(processingFileBytes);

            ingestion.IngestionStatus = IncomingNachaIngestionStatus.EnValidacion;
            IncomingNachaStageTransitions.MoveTo(ingestion, IncomingNachaIngestionStage.HeaderParsing);
            ingestion.Notes = "Validación de resolución de cámara/ciclo en proceso.";
            await _context.SaveChangesAsync(ct);

            var resolution = await _cycleResolver.ResolveAsync(new IncomingNachaCycleResolutionRequest
            {
                FileName = processingFileName,
                Records = records
            }, ct);

        ingestion.DetectedClearingHouseId = resolution.DetectedClearingHouseId;
        ingestion.ResolvedClearingHouseId = resolution.ClearingHouseId;
        ingestion.OperationalDate = resolution.OperationalDate;
        ingestion.ResolvedAchCycleId = resolution.AchCycleId;
        ingestion.CycleResolutionStatus = resolution.Status;
        ingestion.ResolutionMode = resolution.ResolutionMode;
        ingestion.ResolutionConfidence = resolution.Confidence;
        ingestion.ResolutionEvidenceJson = resolution.EvidenceJson;
        ingestion.WarningsJson = JsonSerializer.Serialize(resolution.Warnings);

        if (request.RequestedClearingHouseId.HasValue
            && resolution.ClearingHouseId.HasValue
            && request.RequestedClearingHouseId.Value != resolution.ClearingHouseId.Value)
        {
            const string mismatch = "La cámara seleccionada no coincide con la cámara detectada en el contenido NACHA-M.";
            ingestion.IngestionStatus = IncomingNachaIngestionStatus.Bloqueado;
            ingestion.ParsingStatus = IncomingNachaParsingStatus.NoEjecutado;
            ingestion.Notes = "CLEARING_HOUSE_SELECTION_MISMATCH";
            IncomingNachaStageTransitions.MoveTo(ingestion, IncomingNachaIngestionStage.Rejected);
            _context.IncomingNachaFileProcessingResults.Add(new IncomingNachaFileProcessingResult
            {
                IncomingNachaFileIngestionId = ingestion.Id,
                AttemptNumber = 1,
                StartedAtUtc = UtcNow,
                FinishedAtUtc = UtcNow,
                OutcomeStatus = IncomingNachaProcessingOutcomeStatus.BloqueadoAmbiguo,
                FailureStage = "ValidacionCamaraSeleccionada",
                ErrorCount = 1,
                ParserErrorsJson = JsonSerializer.Serialize(new[] { "CLEARING_HOUSE_SELECTION_MISMATCH" }),
                IsReprocessable = true
            });
            await _context.SaveChangesAsync(ct);
            return BuildResponse(ingestion, null, [mismatch]);
        }

        if (!resolution.IsResolved)
        {
            ingestion.IngestionStatus = resolution.IsAmbiguous
                ? IncomingNachaIngestionStatus.Bloqueado
                : IncomingNachaIngestionStatus.PendienteResolucion;

            ingestion.ParsingStatus = IncomingNachaParsingStatus.NoEjecutado;
            IncomingNachaStageTransitions.MoveTo(ingestion, IncomingNachaIngestionStage.Rejected);

            _context.IncomingNachaFileProcessingResults.Add(new IncomingNachaFileProcessingResult
            {
                IncomingNachaFileIngestionId = ingestion.Id,
                AttemptNumber = 1,
                StartedAtUtc = UtcNow,
                FinishedAtUtc = UtcNow,
                OutcomeStatus = resolution.IsAmbiguous ? IncomingNachaProcessingOutcomeStatus.BloqueadoAmbiguo : IncomingNachaProcessingOutcomeStatus.Fallido,
                FailureStage = "ResolucionCamaraCiclo",
                WarningCount = resolution.Warnings.Count,
                ErrorCount = resolution.Errors.Count,
                ParserWarningsJson = JsonSerializer.Serialize(resolution.Warnings),
                ParserErrorsJson = JsonSerializer.Serialize(resolution.Errors),
                IsReprocessable = true
            });

            await _context.SaveChangesAsync(ct);
            return BuildResponse(ingestion, null, resolution.Errors);
        }

        IncomingNachaStageTransitions.MoveTo(ingestion, IncomingNachaIngestionStage.ValidatingCycle);
        var admission = await _admissionValidator.ValidateAsync(
            new IncomingNachaAdmissionRequest(processingFileName, records, resolution, request.ForceReprocess),
            ct);
        ingestion.FileNameDate = admission.FileNameDate?.ToDateTime(TimeOnly.MinValue);
        ingestion.HeaderDate = admission.Header?.FileCreationDate.ToDateTime(TimeOnly.MinValue);
        ingestion.EffectiveDate = admission.EffectiveDate?.ToDateTime(TimeOnly.MinValue);
        ingestion.DetectedCycleNumber = admission.CycleNumber;

        if (!admission.IsAccepted)
        {
            var issue = admission.Issue!;
            ingestion.IngestionStatus = IncomingNachaIngestionStatus.Bloqueado;
            ingestion.ParsingStatus = IncomingNachaParsingStatus.NoEjecutado;
            IncomingNachaStageTransitions.MoveTo(ingestion, IncomingNachaIngestionStage.Rejected);
            ingestion.RejectionCode = issue.Code;
            ingestion.RejectionTitle = issue.Title;
            ingestion.RejectionDescription = issue.Message;
            ingestion.SuggestedAction = issue.SuggestedAction;
            ingestion.Notes = issue.Code;
            _context.IncomingNachaFileProcessingResults.Add(new IncomingNachaFileProcessingResult
            {
                IncomingNachaFileIngestionId = ingestion.Id,
                AttemptNumber = 1,
                StartedAtUtc = UtcNow,
                FinishedAtUtc = UtcNow,
                OutcomeStatus = IncomingNachaProcessingOutcomeStatus.Fallido,
                FailureStage = ingestion.Stage.ToString(),
                ErrorCount = 1,
                ParserErrorsJson = JsonSerializer.Serialize(new[] { issue }),
                IsReprocessable = true
            });
            await _context.SaveChangesAsync(ct);
            return BuildResponse(ingestion, null, [issue.Message]);
        }

        NachaConfigResolutionResult? profileResolution = null;
        var isDifferentialCandidate = IsDifferentialCandidate(records);
        var clearingHouseCode = await ResolveClearingHouseCodeAsync(ingestion.ResolvedClearingHouseId, ct);
        var configClearingHouseCode = ToConfigClearingHouseCode(clearingHouseCode);
        var requiresExplicitProfile = isDifferentialCandidate
                                      || string.Equals(configClearingHouseCode, "ACH", StringComparison.OrdinalIgnoreCase);
        if (requiresExplicitProfile)
        {
            var isCenitRor = string.Equals(configClearingHouseCode, "CENIT", StringComparison.OrdinalIgnoreCase)
                             && IsCenitReturnOfReturnCandidate(records);
            var flowTypeCode = isDifferentialCandidate
                ? isCenitRor ? CenitReturnOfReturn2026Layout.FlowTypeCode : "RETORNO"
                : ResolveOrdinaryFlowTypeCode(records);
            var isAchColOrdinary = !isDifferentialCandidate
                                   && string.Equals(configClearingHouseCode, "ACH", StringComparison.OrdinalIgnoreCase);
            profileResolution = await _profileResolver.ResolveAsync(new NachaConfigResolutionRequest
            {
                ClearingHouseCode = configClearingHouseCode,
                FlowTypeCode = flowTypeCode,
                DirectionCode = "ENTRADA",
                ProcessDateUtc = ingestion.OperationalDate ?? UtcNow.Date,
                RequestedVersionMajor = isAchColOrdinary ? AchColOfficialNachaLayout.ProfileVersionMajor : null,
                RequestedVersionMinor = isAchColOrdinary ? AchColOfficialNachaLayout.ProfileVersionMinor : null,
                RecordCodes = records
                    .Where(record => record.Length == 106 && !record.All(character => character == '9'))
                    .Select(record => record[0].ToString())
                    .Distinct(StringComparer.Ordinal)
                    .ToArray(),
                SelectionContext = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
                {
                    ["MessageType"] = isDifferentialCandidate
                        ? isCenitRor ? "ReturnOfReturn" : "DifferentialResponse"
                        : flowTypeCode == "PRENOTIFICACION" ? "Prenotification" : "Original",
                    ["AddendaType"] = isDifferentialCandidate ? "99" : "05"
                },
                RequireHomologated = isDifferentialCandidate
            }, ct);

            _context.IncomingNachaProcessingEvents.Add(new IncomingNachaProcessingEvent
            {
                IncomingNachaFileIngestionId = ingestion.Id,
                EventType = "NachaProfileSelection",
                EventStatus = profileResolution.SelectionStatus.ToString(),
                Message = profileResolution.Success
                    ? "Perfil NACHA-M explícito seleccionado."
                    : "Procesamiento bloqueado antes del parser por selección de perfil.",
                EvidenceJson = JsonSerializer.Serialize(new
                {
                    clearingHouseCode,
                    flowType = flowTypeCode,
                    direction = "ENTRADA",
                    selectionStatus = profileResolution.SelectionStatus.ToString(),
                    profileCode = profileResolution.Profile?.ProfileCode,
                    profileVersion = profileResolution.Profile is null
                        ? null
                        : $"{profileResolution.Profile.VersionMajor}.{profileResolution.Profile.VersionMinor}",
                    requireHomologated = isDifferentialCandidate
                }),
                RaisedBy = "IncomingNachaIngestionAppService"
            });

            if (!profileResolution.Success)
            {
                var diagnostic = profileResolution.SelectionStatus.ToString();
                ingestion.IngestionStatus = IncomingNachaIngestionStatus.Bloqueado;
                ingestion.ParsingStatus = IncomingNachaParsingStatus.NoEjecutado;
                ingestion.Notes = $"PROFILE_SELECTION_BLOCKED;Status={diagnostic}";
                IncomingNachaStageTransitions.MoveTo(ingestion, IncomingNachaIngestionStage.Rejected);

                _context.IncomingNachaFileProcessingResults.Add(new IncomingNachaFileProcessingResult
                {
                    IncomingNachaFileIngestionId = ingestion.Id,
                    AttemptNumber = 1,
                StartedAtUtc = UtcNow,
                FinishedAtUtc = UtcNow,
                    OutcomeStatus = profileResolution.SelectionStatus == NachaProfileSelectionStatus.ProfileAmbiguous
                        ? IncomingNachaProcessingOutcomeStatus.BloqueadoAmbiguo
                        : IncomingNachaProcessingOutcomeStatus.Fallido,
                    FailureStage = "SeleccionPerfilNacha",
                    WarningCount = profileResolution.Warnings.Count,
                    ErrorCount = 1,
                    ParserWarningsJson = JsonSerializer.Serialize(profileResolution.Warnings),
                    ParserErrorsJson = JsonSerializer.Serialize(new[] { diagnostic }),
                    IsReprocessable = true
                });

                await _context.SaveChangesAsync(ct);
                return BuildResponse(ingestion, null, [diagnostic], profileResolution);
            }

        }

        if (profileResolution?.Profile is { } selectedProfile)
        {
            ingestion.ProfileCode = selectedProfile.ProfileCode;
            ingestion.ProfileVersion = $"{selectedProfile.VersionMajor}.{selectedProfile.VersionMinor}";
        }

        ingestion.IngestionStatus = IncomingNachaIngestionStatus.ListoParaParseo;
        ingestion.ParsingStatus = IncomingNachaParsingStatus.EnProceso;
        IncomingNachaStageTransitions.MoveTo(ingestion, IncomingNachaIngestionStage.Parsing);
        await _context.SaveChangesAsync(ct);

        int? cycleNumber = null;
        if (!string.IsNullOrWhiteSpace(ingestion.ResolvedAchCycleId))
        {
            var resolvedCycle = await _context.AchCycles.AsNoTracking().FirstOrDefaultAsync(x => x.Id == ingestion.ResolvedAchCycleId, ct);
            if (resolvedCycle is not null && ExternalFileNameSupport.TryExtractPositiveCycleNumber(resolvedCycle.CycleName, out var parsedCycleNumber))
            {
                cycleNumber = parsedCycleNumber;
            }
        }

        var policyContext = new ExternalFileNameContext
        {
            ClearingHouseId = ingestion.ResolvedClearingHouseId ?? 0,
            ClearingHouseCode = await ResolveClearingHouseCodeAsync(ingestion.ResolvedClearingHouseId, ct),
            ProcessingDate = ingestion.OperationalDate ?? UtcNow.Date,
            ExternalFileType = ExternalFileType.NachaIn,
            Flow = ExternalFileFlow.Recepcion,
            Direction = ExternalFileDirection.Inbound,
            ProvidedExternalFileName = processingFileName,
            InternalFileName = processingFileName,
            NachaContent = Encoding.UTF8.GetString(processingFileBytes),
            FileHash = processingFileHash,
            FileSize = processingFileBytes.LongLength,
            CycleId = ingestion.ResolvedAchCycleId,
            CycleNumber = cycleNumber,
            RequestedBy = request.RequestedBy ?? "system"
        };

        var policyResult = await _externalFileNamePolicy.GenerateExternalNameAsync(policyContext, ct);
        if (policyResult.Validation.IsHardBlocked)
        {
            ingestion.IngestionStatus = IncomingNachaIngestionStatus.Bloqueado;
            ingestion.ParsingStatus = IncomingNachaParsingStatus.NoEjecutado;
            IncomingNachaStageTransitions.MoveTo(ingestion, IncomingNachaIngestionStage.Rejected);
            _context.IncomingNachaFileProcessingResults.Add(new IncomingNachaFileProcessingResult
            {
                IncomingNachaFileIngestionId = ingestion.Id,
                AttemptNumber = 1,
                StartedAtUtc = UtcNow,
                FinishedAtUtc = UtcNow,
                OutcomeStatus = IncomingNachaProcessingOutcomeStatus.BloqueadoAmbiguo,
                FailureStage = "ExternalFileNamePolicy",
                ErrorCount = policyResult.Validation.Issues.Count,
                ParserErrorsJson = JsonSerializer.Serialize(policyResult.Validation.Issues.Select(x => x.Message).ToArray()),
                IsReprocessable = true
            });
            await _context.SaveChangesAsync(ct);
            return BuildResponse(ingestion, null, policyResult.Validation.Issues.Select(x => x.Message).ToArray());
        }

        var processingResult = new IncomingNachaFileProcessingResult
        {
            IncomingNachaFileIngestionId = ingestion.Id,
            AttemptNumber = 1,
                StartedAtUtc = UtcNow,
            OutcomeStatus = IncomingNachaProcessingOutcomeStatus.EnProceso,
            FailureStage = string.Empty
        };
        _context.IncomingNachaFileProcessingResults.Add(processingResult);
        await _context.SaveChangesAsync(ct);

        var usesRelationalTransaction = _context.Database.IsRelational();
        try
        {
            var executionStrategy = _context.Database.CreateExecutionStrategy();
            return await executionStrategy.ExecuteAsync(async () =>
            {
                await using var persistenceTransaction = usesRelationalTransaction
                    ? await _context.Database.BeginTransactionAsync(ct)
                    : null;
                try
                {
                    IncomingNachaStageTransitions.MoveTo(ingestion, IncomingNachaIngestionStage.ValidatingContent);
                    var parseResult = await _parserService.ParseAndSaveDetailedAsync(
                        new MemoryStream(processingFileBytes),
                        processingFileName,
                        new NachaParseRequest
                        {
                            IncomingNachaFileIngestionId = ingestion.Id,
                            SelectedProfileId = profileResolution?.Profile?.Id,
                            SelectedProfileCode = profileResolution?.Profile?.ProfileCode,
                            ResolvedAchCycleId = ingestion.ResolvedAchCycleId,
                            ResolvedClearingHouseId = ingestion.ResolvedClearingHouseId,
                            OperationalDate = ingestion.OperationalDate,
                            CorrelationId = correlationId
                        },
                        ct);

                    processingResult.TotalBatches = parseResult.TotalBatches;
                    processingResult.TotalEntries = parseResult.TotalEntries;
                    processingResult.TotalAddendas = parseResult.TotalAddendas;
                    processingResult.WarningCount = parseResult.WarningCount;
                    processingResult.ErrorCount = parseResult.ErrorCount;
                    processingResult.ValidCount = Math.Max(0, parseResult.TotalEntries - parseResult.ErrorCount);
                    processingResult.InvalidCount = parseResult.ErrorCount;
                    processingResult.ParserWarningsJson = JsonSerializer.Serialize(parseResult.Failures.Select(x => x.Reason));
                    processingResult.ParserErrorsJson = JsonSerializer.Serialize(parseResult.Failures.Select(x => x.Reason));
                    processingResult.OutcomeStatus = parseResult.ErrorCount > 0
                        ? IncomingNachaProcessingOutcomeStatus.ExitosoConAdvertencias
                        : IncomingNachaProcessingOutcomeStatus.Exitoso;
                    processingResult.IsReprocessable = parseResult.ErrorCount > 0;
                    processingResult.FinishedAtUtc = UtcNow;

                    ingestion.IngestionStatus = IncomingNachaIngestionStatus.Completado;
                    ingestion.ParsingStatus = parseResult.ErrorCount > 0
                        ? IncomingNachaParsingStatus.ExitosoConAdvertencias
                        : IncomingNachaParsingStatus.Exitoso;

                    var executedBy = string.IsNullOrWhiteSpace(request.RequestedBy) ? "sistema" : request.RequestedBy.Trim();
                    IncomingNachaStageTransitions.MoveTo(ingestion, IncomingNachaIngestionStage.Persisting);
                    await _postParseProcessor.ProcessAsync(ingestion.Id, executedBy, ct);

                    IncomingNachaStageTransitions.MoveTo(ingestion, IncomingNachaIngestionStage.Persisted);
                    await _context.SaveChangesAsync(ct);
                    if (persistenceTransaction is not null)
                    {
                        await persistenceTransaction.CommitAsync(ct);
                    }

                    return BuildResponse(ingestion, processingResult, parseResult.Failures.Select(x => x.Reason).ToList(), profileResolution);
                }
                catch
                {
                    if (persistenceTransaction is not null)
                    {
                        await persistenceTransaction.RollbackAsync(CancellationToken.None);
                    }

                    throw;
                }
            });
        }
        catch (Exception ex)
        {
            if (usesRelationalTransaction)
            {
                _context.ChangeTracker.Clear();
                ingestion = await _context.IncomingNachaFileIngestions
                    .SingleAsync(x => x.Id == ingestion.Id, CancellationToken.None);
                processingResult = await _context.IncomingNachaFileProcessingResults
                    .SingleAsync(x => x.Id == processingResult.Id, CancellationToken.None);
            }

            _logger.LogError(ex, "Fallo de parseo NACHA-M en la ingesta {IngestionId}", ingestion.Id);
            ingestion.IngestionStatus = IncomingNachaIngestionStatus.Fallido;
            IncomingNachaStageTransitions.MoveTo(ingestion, IncomingNachaIngestionStage.Failed);
            ingestion.ParsingStatus = IncomingNachaParsingStatus.FallidoReprocesable;

            processingResult.OutcomeStatus = IncomingNachaProcessingOutcomeStatus.Fallido;
            processingResult.FailureStage = "Parser";
            processingResult.ErrorCount = 1;
            processingResult.ParserErrorsJson = JsonSerializer.Serialize(new[] { ex.Message });
            processingResult.IsReprocessable = true;
            processingResult.FinishedAtUtc = UtcNow;

            // El estado terminal de auditoría debe persistirse aun si el cliente canceló la solicitud.
            await _context.SaveChangesAsync(CancellationToken.None);
            return BuildResponse(ingestion, processingResult, new[] { ex.Message }, profileResolution);
        }
        }
        catch (ManagedDigitalEnvelopeException ex)
        {
            ingestion.IngestionStatus = IncomingNachaIngestionStatus.Bloqueado;
            ingestion.ParsingStatus = IncomingNachaParsingStatus.NoEjecutado;
            ingestion.Notes = $"DIGITAL_ENVELOPE_DECRYPTION_FAILED;Code={ex.ErrorCode}";
            IncomingNachaStageTransitions.MoveTo(ingestion, IncomingNachaIngestionStage.Rejected);
            _context.IncomingNachaFileProcessingResults.Add(new IncomingNachaFileProcessingResult
            {
                IncomingNachaFileIngestionId = ingestion.Id,
                AttemptNumber = 1,
                StartedAtUtc = UtcNow,
                FinishedAtUtc = UtcNow,
                OutcomeStatus = IncomingNachaProcessingOutcomeStatus.Fallido,
                FailureStage = "DigitalEnvelopeDecryption",
                ErrorCount = 1,
                ParserErrorsJson = JsonSerializer.Serialize(new[] { ex.ErrorCode }),
                IsReprocessable = true
            });
            _context.IncomingNachaProcessingEvents.Add(new IncomingNachaProcessingEvent
            {
                IncomingNachaFileIngestionId = ingestion.Id,
                EventType = "DigitalEnvelopeDecryptionFailed",
                EventStatus = "Blocked",
                Message = "El sobre digital no superó el descifrado o la validación criptográfica.",
                EvidenceJson = JsonSerializer.Serialize(new { errorCode = ex.ErrorCode, plaintextPersistedToDisk = false }),
                RaisedBy = "IncomingNachaIngestionAppService"
            });
            await _context.SaveChangesAsync(ct);
            return BuildResponse(ingestion, null, [ex.ErrorCode]);
        }
        finally
        {
            if (containsDecryptedPlaintext)
            {
                CryptographicOperations.ZeroMemory(processingFileBytes);
            }
        }
    }

    private static IncomingNachaFileIngestion SelectEffectiveCandidate(
        IReadOnlyList<IncomingNachaFileIngestion> candidates,
        IncomingNachaFileIngestion canonicalCandidate)
        => candidates.FirstOrDefault(x => x.IsReprocess && x.ParentIngestionId == canonicalCandidate.Id)
           ?? canonicalCandidate;

    private async Task<IncomingNachaFileProcessingResult?> GetLatestNonDuplicateProcessingAsync(
        Guid ingestionId,
        CancellationToken ct)
        => await _context.IncomingNachaFileProcessingResults
            .AsNoTracking()
            .Where(x => x.IncomingNachaFileIngestionId == ingestionId
                        && x.OutcomeStatus != IncomingNachaProcessingOutcomeStatus.Duplicado)
            .OrderByDescending(x => x.AttemptNumber)
            .ThenByDescending(x => x.StartedAtUtc)
            .FirstOrDefaultAsync(ct);

    private static bool CanReprocessDuplicate(
        IReadOnlyList<IncomingNachaFileIngestion> candidates,
        IncomingNachaFileIngestion effectiveCandidate)
        => candidates.All(x => !x.IsReprocess)
           && effectiveCandidate.ParsingStatus is IncomingNachaParsingStatus.EnProceso
               or IncomingNachaParsingStatus.FallidoReprocesable;

    private static bool IsDigitalEnvelopeFile(string fileName)
        => Path.GetFileName(fileName).EndsWith(".env", StringComparison.OrdinalIgnoreCase);

    private static IncomingNachaIngestionResponse BuildResponse(
        IncomingNachaFileIngestion ingestion,
        IncomingNachaFileProcessingResult? processing,
        IReadOnlyList<string> errors,
        NachaConfigResolutionResult? profileSelection = null)
    {
        return new IncomingNachaIngestionResponse
        {
            IngestionId = ingestion.Id,
            OriginalFileName = ingestion.FileName,
            FileHash = ingestion.FileHashSha256,
            CorrelationId = ingestion.CorrelationId,
            IngestionStatus = ingestion.IngestionStatus,
            CycleResolutionStatus = ingestion.CycleResolutionStatus,
            ParsingStatus = ingestion.ParsingStatus,
            DetectedClearingHouseId = ingestion.DetectedClearingHouseId,
            ResolvedClearingHouseId = ingestion.ResolvedClearingHouseId,
            ResolvedAchCycleId = ingestion.ResolvedAchCycleId,
            OperationalDate = ingestion.OperationalDate,
            ProfileSelectionStatus = profileSelection?.SelectionStatus,
            SelectedProfileCode = profileSelection?.Profile?.ProfileCode ?? ingestion.ProfileCode,
            SelectedProfileVersion = profileSelection?.Profile is null
                ? ingestion.ProfileVersion
                : $"{profileSelection.Profile.VersionMajor}.{profileSelection.Profile.VersionMinor}",
            TotalBatches = processing?.TotalBatches ?? 0,
            TotalEntries = processing?.TotalEntries ?? 0,
            TotalAddendas = processing?.TotalAddendas ?? 0,
            WarningCount = processing?.WarningCount ?? 0,
            ErrorCount = processing?.ErrorCount ?? errors.Count,
            Errors = errors,
            OperationalIssue = string.IsNullOrWhiteSpace(ingestion.RejectionCode)
                ? null
                : new IncomingNachaAdmissionIssue(
                    ingestion.RejectionCode,
                    ingestion.RejectionTitle ?? "No fue posible admitir el archivo",
                    ingestion.RejectionDescription ?? errors.FirstOrDefault() ?? "El archivo no superó las validaciones operativas.",
                    ingestion.SuggestedAction ?? "Verifique el archivo y vuelva a intentarlo.",
                    string.IsNullOrWhiteSpace(ingestion.TechnicalErrorCode) ? "Functional" : "Technical",
                    "Error")
        };
    }

    private static string ComputeSha256(byte[] bytes)
    {
        var hash = SHA256.HashData(bytes);
        var sb = new StringBuilder(hash.Length * 2);
        foreach (var b in hash)
        {
            sb.Append(b.ToString("x2"));
        }
        return sb.ToString();
    }

    private static IReadOnlyList<string> ChunkFixed106(byte[] bytes)
    {
        var content = Encoding.UTF8.GetString(bytes);
        if (content.Contains('\n'))
        {
            return content.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries).ToList();
        }

        if (content.Length % 106 == 0)
        {
            return Enumerable.Range(0, content.Length / 106)
                .Select(i => content.Substring(i * 106, 106))
                .ToList();
        }

        return [content];
    }

    private static bool IsDifferentialCandidate(IReadOnlyList<string> records)
        => records.Any(record =>
            record.Length == 106
            && record[0] == '7'
            && string.Equals(record.Substring(1, 2), "99", StringComparison.Ordinal));

    private static string ResolveOrdinaryFlowTypeCode(IReadOnlyList<string> records)
    {
        var transactionCodes = records
            .Where(record => record.Length == AchColOfficialNachaLayout.RecordLength && record[0] == '6')
            .Select(record => record.Substring(1, 2))
            .Distinct(StringComparer.Ordinal)
            .ToArray();
        string[] monetaryCodes = ["22", "27", "32", "37", "52", "55"];
        string[] prenotificationCodes = ["23", "28", "33", "38", "53", "57"];

        if (transactionCodes.Length > 0 && transactionCodes.All(prenotificationCodes.Contains))
        {
            return "PRENOTIFICACION";
        }

        if (transactionCodes.Length > 0
            && transactionCodes.All(code => monetaryCodes.Contains(code, StringComparer.Ordinal)
                                            || prenotificationCodes.Contains(code, StringComparer.Ordinal)))
        {
            return "ORIGINAL";
        }

        return "UNSUPPORTED";
    }

    private static bool IsCenitReturnOfReturnCandidate(IReadOnlyList<string> records)
        => records.Any(record =>
            record.Length == CenitReturnOfReturn2026Layout.RecordLength
            && record[0] == '7'
            && string.Equals(record.Substring(1, 2), "99", StringComparison.Ordinal)
            && CenitReturnOfReturn2026Layout.IsCause(record.Substring(3, 3)));

    private static string ToConfigClearingHouseCode(string clearingHouseCode)
        => clearingHouseCode.Contains("CENIT", StringComparison.OrdinalIgnoreCase)
            ? "CENIT"
            : clearingHouseCode.Contains("ACH", StringComparison.OrdinalIgnoreCase)
                ? "ACH"
                : string.Empty;

    private async Task<string> ResolveClearingHouseCodeAsync(int? clearingHouseId, CancellationToken ct)
    {
        if (!clearingHouseId.HasValue)
        {
            return string.Empty;
        }

        return await _context.ClearingHouses
            .AsNoTracking()
            .Where(x => x.Id == clearingHouseId.Value)
            .Select(x => x.Code)
            .FirstOrDefaultAsync(ct) ?? string.Empty;
    }

    private DateTime UtcNow => _timeProvider.GetUtcNow().UtcDateTime;

    private sealed class LegacyCompatibleAdmissionValidator : IIncomingNachaAdmissionValidator
    {
        public Task<IncomingNachaAdmissionResult> ValidateAsync(IncomingNachaAdmissionRequest request, CancellationToken ct = default)
        {
            var date = DateOnly.FromDateTime(request.Resolution.OperationalDate!.Value);
            var header = new NachaHeaderPreview(string.Empty, string.Empty, date, null, null, null, request.FileName);
            return Task.FromResult(IncomingNachaAdmissionResult.Accepted(header, null, null, null));
        }
    }
}
