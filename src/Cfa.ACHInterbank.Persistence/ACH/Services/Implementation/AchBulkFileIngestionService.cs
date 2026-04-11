using System.Security.Cryptography;
using System.Text.Json;
using Cfa.ACHInterbank.Application.ACH.Interfaces;
using Cfa.ACHInterbank.Application.ACH.Models;
using Cfa.ACHInterbank.Domain.Models.ACH;
using Cfa.ACHInterbank.Domain.Models.Configurations;
using Cfa.ACHInterbank.Persistence.DataBase;
using Microsoft.EntityFrameworkCore;

namespace Cfa.ACHInterbank.Persistence.ACH.Services.Implementation;

[Scoped]
public class AchBulkFileIngestionService : IAchBulkFileIngestionService
{
    private const int MaxSummaryErrors = 20;

    private readonly AchDbContext _context;
    private readonly IEnumerable<IBulkFileParser> _parsers;
    private readonly IBulkFileStructuralValidator _structuralValidator;
    private readonly IBulkIngestionWorkDispatcher _workDispatcher;

    public AchBulkFileIngestionService(
        AchDbContext context,
        IEnumerable<IBulkFileParser> parsers,
        IBulkFileStructuralValidator structuralValidator,
        IBulkIngestionWorkDispatcher workDispatcher)
    {
        _context = context;
        _parsers = parsers;
        _structuralValidator = structuralValidator;
        _workDispatcher = workDispatcher;
    }

    public async Task<BulkFileUploadResponse> UploadAndParseAsync(
        Stream fileStream,
        string fileName,
        string? contentType,
        BulkFileUploadRequest request,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(fileStream);

        var effectiveStream = await EnsureSeekableStreamAsync(fileStream, ct);
        var fileType = ResolveFileType(fileName, contentType);
        var parser = _parsers.FirstOrDefault(p => p.CanParse(fileType))
            ?? throw new NotSupportedException($"No hay parser registrado para formato '{fileType}'.");

        var hash = await ComputeSha256Async(effectiveStream, ct);
        effectiveStream.Position = 0;

        var existing = await TryFindExistingBatchAsync(hash, request.ClientRequestId, ct);
        if (existing is not null)
        {
            return BuildResponse(existing, []);
        }

        var parsed = await parser.ParseAsync(effectiveStream, ct);

        var batch = new BulkIngestionBatch
        {
            BatchReference = string.IsNullOrWhiteSpace(request.BatchReference)
                ? BuildBatchReference(fileType)
                : request.BatchReference!.Trim(),
            FileType = parsed.FileType,
            FileName = fileName,
            ContentType = contentType ?? string.Empty,
            FileHash = hash,
            UploadedBy = string.IsNullOrWhiteSpace(request.RequestedBy) ? "system" : request.RequestedBy!,
            ClientRequestId = request.ClientRequestId,
            UploadedAtUtc = DateTime.UtcNow,
            Status = BulkIngestionBatchStatusEnum.Uploaded
        };

        var summaryErrors = new List<string>();

        foreach (var parsedItem in parsed.Items)
        {
            var validation = _structuralValidator.Validate(parsedItem);
            var normalizedJson = validation.IsValid
                ? JsonSerializer.Serialize(validation.NormalizedItem)
                : null;

            var reference = validation.IsValid
                ? validation.NormalizedItem?.Reference ?? string.Empty
                : parsedItem.Fields.GetValueOrDefault("reference")?.Trim() ?? string.Empty;

            if (!validation.IsValid && !string.IsNullOrWhiteSpace(validation.ErrorMessage))
            {
                summaryErrors.Add($"Fila {parsedItem.Index}: {validation.ErrorMessage}");
            }

            batch.Items.Add(new BulkIngestionItem
            {
                ItemIndex = parsedItem.Index,
                Reference = reference,
                Status = validation.IsValid ? BulkIngestionItemStatusEnum.Ready : BulkIngestionItemStatusEnum.StructuralError,
                Message = validation.ErrorMessage ?? string.Empty,
                RawPayloadJson = JsonSerializer.Serialize(parsedItem.Fields),
                NormalizedPayloadJson = normalizedJson
            });
        }

        batch.TotalRecords = batch.Items.Count;
        batch.TotalValid = batch.Items.Count(i => i.Status == BulkIngestionItemStatusEnum.Ready);
        batch.TotalInvalid = batch.Items.Count(i => i.Status == BulkIngestionItemStatusEnum.StructuralError);
        batch.TotalProcessed = 0;
        batch.ParsedAtUtc = DateTime.UtcNow;
        batch.Status = BulkIngestionBatchStatusEnum.Parsed;

        if (batch.TotalRecords > 0)
        {
            batch.ValidatedAtUtc = DateTime.UtcNow;
            batch.Status = batch.TotalValid > 0
                ? BulkIngestionBatchStatusEnum.Queued
                : BulkIngestionBatchStatusEnum.Failed;
            batch.QueuedAtUtc = batch.TotalValid > 0 ? DateTime.UtcNow : null;
        }

        var topErrors = summaryErrors
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Take(MaxSummaryErrors)
            .ToList();

        batch.SummaryErrorsJson = JsonSerializer.Serialize(topErrors);

        _context.BulkIngestionBatches.Add(batch);
        await _context.SaveChangesAsync(ct);

        if (batch.Status == BulkIngestionBatchStatusEnum.Queued)
        {
            var attempt = new BulkIngestionAttempt
            {
                BatchId = batch.Id,
                AttemptNumber = 1,
                TriggerType = BulkIngestionTriggerTypeEnum.Initial,
                Scope = BulkIngestionRetryScopeEnum.Full,
                TriggeredBy = batch.UploadedBy,
                TriggeredAtUtc = DateTime.UtcNow,
                Status = BulkIngestionAttemptStatusEnum.Queued
            };

            _context.BulkIngestionAttempts.Add(attempt);
            await _context.SaveChangesAsync(ct);

            var jobId = await _workDispatcher.DispatchProcessingAsync(batch.Id, attempt.Id, ct);
            batch.LastJobId = jobId;
            batch.LastJobMessage = "Lote encolado para procesamiento asíncrono.";
            attempt.JobId = jobId;
            await _context.SaveChangesAsync(ct);
        }

        return BuildResponse(batch, topErrors);
    }

    private async Task<BulkIngestionBatch?> TryFindExistingBatchAsync(string hash, string? clientRequestId, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(clientRequestId))
        {
            return null;
        }

        return await _context.BulkIngestionBatches
            .AsNoTracking()
            .OrderByDescending(x => x.UploadedAtUtc)
            .FirstOrDefaultAsync(x => x.ClientRequestId == clientRequestId && x.FileHash == hash, ct);
    }

    private static BulkIngestionFileTypeEnum ResolveFileType(string fileName, string? contentType)
    {
        var extension = Path.GetExtension(fileName).Trim().ToLowerInvariant();

        return extension switch
        {
            ".json" => BulkIngestionFileTypeEnum.Json,
            ".csv" => BulkIngestionFileTypeEnum.Csv,
            ".xlsx" or ".xlsm" => BulkIngestionFileTypeEnum.Excel,
            _ when (contentType ?? string.Empty).Contains("json", StringComparison.OrdinalIgnoreCase) => BulkIngestionFileTypeEnum.Json,
            _ when (contentType ?? string.Empty).Contains("csv", StringComparison.OrdinalIgnoreCase) => BulkIngestionFileTypeEnum.Csv,
            _ when (contentType ?? string.Empty).Contains("excel", StringComparison.OrdinalIgnoreCase)
                   || (contentType ?? string.Empty).Contains("spreadsheet", StringComparison.OrdinalIgnoreCase)
                => BulkIngestionFileTypeEnum.Excel,
            _ => throw new NotSupportedException($"Formato de archivo no soportado: {extension}")
        };
    }

    private static async Task<string> ComputeSha256Async(Stream stream, CancellationToken ct)
    {
        using var sha = SHA256.Create();
        var hash = await sha.ComputeHashAsync(stream, ct);
        return Convert.ToHexString(hash);
    }

    private static async Task<Stream> EnsureSeekableStreamAsync(Stream source, CancellationToken ct)
    {
        if (source.CanSeek)
        {
            source.Position = 0;
            return source;
        }

        var memory = new MemoryStream();
        await source.CopyToAsync(memory, ct);
        memory.Position = 0;
        return memory;
    }

    private static string BuildBatchReference(BulkIngestionFileTypeEnum fileType)
    {
        return $"BULK-{fileType.ToString().ToUpperInvariant()}-{DateTime.UtcNow:yyyyMMddHHmmss}";
    }

    private static BulkFileUploadResponse BuildResponse(BulkIngestionBatch batch, IReadOnlyList<string> parsedSummary)
    {
        var summary = parsedSummary.Count > 0
            ? parsedSummary
            : DeserializeSummary(batch.SummaryErrorsJson);

        return new BulkFileUploadResponse
        {
            BatchId = batch.Id,
            JobId = batch.LastJobId,
            BatchReference = batch.BatchReference,
            Status = batch.Status,
            FileType = batch.FileType,
            TotalRecordsDetected = batch.TotalRecords,
            TotalStructuralErrors = batch.TotalInvalid,
            TotalReadyForProcessing = batch.TotalValid,
            ErrorSummary = summary
        };
    }

    private static IReadOnlyList<string> DeserializeSummary(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return [];
        }

        try
        {
            return JsonSerializer.Deserialize<List<string>>(json) ?? [];
        }
        catch
        {
            return [];
        }
    }
}
