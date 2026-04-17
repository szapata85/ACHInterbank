using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Cfa.ACHInterbank.Application.ACH.Interfaces;
using Cfa.ACHInterbank.Application.ACH.Models;
using Cfa.ACHInterbank.Domain.Models.ACH;
using Cfa.ACHInterbank.Domain.Models.Configurations;
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
    private readonly ILogger<IncomingNachaIngestionAppService> _logger;

    public IncomingNachaIngestionAppService(
        AchDbContext context,
        IIncomingNachaCycleResolver cycleResolver,
        INachaParserService parserService,
        IIncomingNachaPostParseProcessor postParseProcessor,
        ILogger<IncomingNachaIngestionAppService> logger)
    {
        _context = context;
        _cycleResolver = cycleResolver;
        _parserService = parserService;
        _postParseProcessor = postParseProcessor;
        _logger = logger;
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
        var records = ChunkFixed106(fileBytes);

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
            if (!candidatesByFingerprint.Any(x => x.Id == parentIngestionId.Value))
            {
                throw new ArgumentException("ParentIngestionId no corresponde al archivo a reprocesar (hash/tamaño diferente).");
            }

            var alreadyReprocessed = await _context.IncomingNachaFileIngestions.AsNoTracking()
                .AnyAsync(x => x.IsReprocess
                               && x.ParentIngestionId == parentIngestionId.Value
                               && x.FileHashSha256 == fileHash
                               && x.FileSize == fileBytes.LongLength, ct);

            if (alreadyReprocessed)
            {
                throw new ArgumentException("Ya existe un reproceso registrado para este archivo y ParentIngestionId.");
            }
        }

        if (!request.ForceReprocess && canonicalCandidate is not null)
        {
            var nextAttempt = await _context.IncomingNachaFileProcessingResults
                .AsNoTracking()
                .Where(x => x.IncomingNachaFileIngestionId == canonicalCandidate.Id)
                .Select(x => (int?)x.AttemptNumber)
                .MaxAsync(ct) ?? 0;

            _context.IncomingNachaFileProcessingResults.Add(new IncomingNachaFileProcessingResult
            {
                IncomingNachaFileIngestionId = canonicalCandidate.Id,
                AttemptNumber = nextAttempt + 1,
                StartedAtUtc = DateTime.UtcNow,
                FinishedAtUtc = DateTime.UtcNow,
                OutcomeStatus = IncomingNachaProcessingOutcomeStatus.Duplicado,
                FailureStage = "ValidacionDuplicidad",
                ErrorCount = 1,
                ParserErrorsJson = JsonSerializer.Serialize(new[] { "Archivo duplicado" }),
                IsReprocessable = true
            });

            await _context.SaveChangesAsync(ct);
            return new IncomingNachaIngestionResponse
            {
                IngestionId = canonicalCandidate.Id,
                IngestionStatus = IncomingNachaIngestionStatus.Duplicado,
                CycleResolutionStatus = canonicalCandidate.CycleResolutionStatus,
                ParsingStatus = canonicalCandidate.ParsingStatus,
                DetectedClearingHouseId = canonicalCandidate.DetectedClearingHouseId,
                ResolvedClearingHouseId = canonicalCandidate.ResolvedClearingHouseId,
                ResolvedAchCycleId = canonicalCandidate.ResolvedAchCycleId,
                OperationalDate = canonicalCandidate.OperationalDate,
                ErrorCount = 1,
                Errors = new[] { "Archivo duplicado detectado por hash/tamaño." }
            };
        }

        var ingestion = new IncomingNachaFileIngestion
        {
            FileName = request.FileName,
            FileHashSha256 = fileHash,
            FileSize = fileBytes.LongLength,
            ContentType = request.ContentType,
            UploadedBy = string.IsNullOrWhiteSpace(request.RequestedBy) ? "system" : request.RequestedBy,
            ReceivedBy = string.IsNullOrWhiteSpace(request.RequestedBy) ? "system" : request.RequestedBy,
            ReceivedAtUtc = DateTime.UtcNow,
            CorrelationId = correlationId,
            ParentIngestionId = parentIngestionId,
            IsReprocess = request.ForceReprocess,
            IngestionStatus = IncomingNachaIngestionStatus.Recibido,
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
        await _context.SaveChangesAsync(ct);

        ingestion.IngestionStatus = IncomingNachaIngestionStatus.EnValidacion;
        ingestion.Notes = "Validación de resolución de cámara/ciclo en proceso.";
        await _context.SaveChangesAsync(ct);

        var resolution = await _cycleResolver.ResolveAsync(new IncomingNachaCycleResolutionRequest
        {
            FileName = request.FileName,
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

        if (!resolution.IsResolved)
        {
            ingestion.IngestionStatus = resolution.IsAmbiguous
                ? IncomingNachaIngestionStatus.Bloqueado
                : IncomingNachaIngestionStatus.PendienteResolucion;

            ingestion.ParsingStatus = IncomingNachaParsingStatus.NoEjecutado;

            _context.IncomingNachaFileProcessingResults.Add(new IncomingNachaFileProcessingResult
            {
                IncomingNachaFileIngestionId = ingestion.Id,
                AttemptNumber = 1,
                StartedAtUtc = DateTime.UtcNow,
                FinishedAtUtc = DateTime.UtcNow,
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

        ingestion.IngestionStatus = IncomingNachaIngestionStatus.ListoParaParseo;
        ingestion.ParsingStatus = IncomingNachaParsingStatus.EnProceso;
        await _context.SaveChangesAsync(ct);

        var processingResult = new IncomingNachaFileProcessingResult
        {
            IncomingNachaFileIngestionId = ingestion.Id,
            AttemptNumber = 1,
            StartedAtUtc = DateTime.UtcNow,
            OutcomeStatus = IncomingNachaProcessingOutcomeStatus.EnProceso,
            FailureStage = string.Empty
        };
        _context.IncomingNachaFileProcessingResults.Add(processingResult);
        await _context.SaveChangesAsync(ct);

        try
        {
            var parseResult = await _parserService.ParseAndSaveDetailedAsync(
                new MemoryStream(fileBytes),
                request.FileName,
                new NachaParseRequest
                {
                    IncomingNachaFileIngestionId = ingestion.Id,
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
            processingResult.FinishedAtUtc = DateTime.UtcNow;

            ingestion.IngestionStatus = IncomingNachaIngestionStatus.Completado;
            ingestion.ParsingStatus = parseResult.ErrorCount > 0
                ? IncomingNachaParsingStatus.ExitosoConAdvertencias
                : IncomingNachaParsingStatus.Exitoso;

            if (!await _context.IncomingNachaTransactionLinks.AnyAsync(x => x.IncomingNachaFileIngestionId == ingestion.Id, ct))
            {
                _context.IncomingNachaTransactionLinks.Add(new IncomingNachaTransactionLink
                {
                    IncomingNachaFileIngestionId = ingestion.Id,
                    LinkType = IncomingNachaLinkType.NoResuelto,
                    ConfidenceScore = 0,
                    EvidenceJson = "{\"estado\":\"pendiente\"}",
                    LinkedBy = "sistema",
                    IsFinal = false
                });
            }

            await _postParseProcessor.ProcessAsync(ingestion.Id, request.RequestedBy, ct);

            await _context.SaveChangesAsync(ct);
            return BuildResponse(ingestion, processingResult, parseResult.Failures.Select(x => x.Reason).ToList());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Fallo de parseo NACHA-M en la ingesta {IngestionId}", ingestion.Id);
            ingestion.IngestionStatus = IncomingNachaIngestionStatus.Fallido;
            ingestion.ParsingStatus = IncomingNachaParsingStatus.FallidoReprocesable;

            processingResult.OutcomeStatus = IncomingNachaProcessingOutcomeStatus.Fallido;
            processingResult.FailureStage = "Parser";
            processingResult.ErrorCount = 1;
            processingResult.ParserErrorsJson = JsonSerializer.Serialize(new[] { ex.Message });
            processingResult.IsReprocessable = true;
            processingResult.FinishedAtUtc = DateTime.UtcNow;

            await _context.SaveChangesAsync(ct);
            return BuildResponse(ingestion, processingResult, new[] { ex.Message });
        }
    }

    private static IncomingNachaIngestionResponse BuildResponse(
        IncomingNachaFileIngestion ingestion,
        IncomingNachaFileProcessingResult? processing,
        IReadOnlyList<string> errors)
    {
        return new IncomingNachaIngestionResponse
        {
            IngestionId = ingestion.Id,
            IngestionStatus = ingestion.IngestionStatus,
            CycleResolutionStatus = ingestion.CycleResolutionStatus,
            ParsingStatus = ingestion.ParsingStatus,
            DetectedClearingHouseId = ingestion.DetectedClearingHouseId,
            ResolvedClearingHouseId = ingestion.ResolvedClearingHouseId,
            ResolvedAchCycleId = ingestion.ResolvedAchCycleId,
            OperationalDate = ingestion.OperationalDate,
            TotalBatches = processing?.TotalBatches ?? 0,
            TotalEntries = processing?.TotalEntries ?? 0,
            TotalAddendas = processing?.TotalAddendas ?? 0,
            WarningCount = processing?.WarningCount ?? 0,
            ErrorCount = processing?.ErrorCount ?? errors.Count,
            Errors = errors
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
}
