using System.Text.Json;
using Cfa.ACHInterbank.Application.DataBase;
using Cfa.ACHInterbank.Domain.Models.ACH;
using Cfa.ACHInterbank.Persistence.DataBase;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;

namespace Cfa.ACHInterbank.Persistence.ACH.Services.Implementation.Seeders;

[Scoped]
public sealed class BulkIngestionTrackingScenarioSeeder : IDbSeeder
{
    private const string BatchReferencePrefix = "SEED-INGEST-";

    private readonly AchDbContext _context;
    private readonly IHostEnvironment _environment;

    public BulkIngestionTrackingScenarioSeeder(AchDbContext context, IHostEnvironment environment)
    {
        _context = context;
        _environment = environment;
    }

    public int Order => 7;

    public async Task SeedAsync()
    {
        if (!CanSeedCurrentEnvironment())
        {
            return;
        }

        var alreadySeeded = await _context.BulkIngestionBatches
            .AsNoTracking()
            .AnyAsync(x => x.BatchReference.StartsWith(BatchReferencePrefix));

        if (alreadySeeded)
        {
            return;
        }

        var seedNow = DateTime.UtcNow;
        var batches = BuildBatchScenarios(seedNow);
        var items = BuildItemsForScenarios(batches, seedNow);
        var attempts = BuildAttemptsForScenarios(batches, seedNow);

        _context.BulkIngestionBatches.AddRange(batches);
        _context.BulkIngestionItems.AddRange(items);
        _context.BulkIngestionAttempts.AddRange(attempts);

        await _context.SaveChangesAsync();
    }

    private bool CanSeedCurrentEnvironment()
    {
        return _environment.IsDevelopment()
               || _environment.IsEnvironment("Testing")
               || _environment.IsEnvironment("Demo")
               || _environment.IsEnvironment("Demonstration");
    }

    private static List<BulkIngestionBatch> BuildBatchScenarios(DateTime seedNow)
    {
        return
        [
            new BulkIngestionBatch
            {
                Id = Guid.NewGuid(),
                BatchReference = $"{BatchReferencePrefix}UPLOADED",
                FileType = BulkIngestionFileTypeEnum.Json,
                FileName = "seed_uploaded.json",
                ContentType = "application/json",
                FileHash = "seed-hash-uploaded",
                UploadedAtUtc = seedNow.AddMinutes(-75),
                UploadedBy = "seed-system",
                ClientRequestId = "REQ-SEED-UPLOADED",
                TotalRecords = 12,
                TotalValid = 0,
                TotalInvalid = 0,
                Status = BulkIngestionBatchStatusEnum.Uploaded,
                LastJobMessage = "Archivo registrado y pendiente de validación.",
                SummaryErrorsJson = "[]"
            },
            new BulkIngestionBatch
            {
                Id = Guid.NewGuid(),
                BatchReference = $"{BatchReferencePrefix}QUEUED",
                FileType = BulkIngestionFileTypeEnum.Csv,
                FileName = "seed_queued.csv",
                ContentType = "text/csv",
                FileHash = "seed-hash-queued",
                UploadedAtUtc = seedNow.AddMinutes(-70),
                ParsedAtUtc = seedNow.AddMinutes(-68),
                ValidatedAtUtc = seedNow.AddMinutes(-66),
                QueuedAtUtc = seedNow.AddMinutes(-65),
                UploadedBy = "seed-system",
                ClientRequestId = "REQ-SEED-QUEUED",
                TotalRecords = 20,
                TotalValid = 18,
                TotalInvalid = 2,
                Status = BulkIngestionBatchStatusEnum.Queued,
                LastJobId = "job-seed-queued",
                LastJobMessage = "Lote en cola para procesamiento.",
                SummaryErrorsJson = JsonSerializer.Serialize(new[]
                {
                    "Fila 4: cuenta destino incompleta.",
                    "Fila 11: monto inválido."
                })
            },
            new BulkIngestionBatch
            {
                Id = Guid.NewGuid(),
                BatchReference = $"{BatchReferencePrefix}PROCESSING",
                FileType = BulkIngestionFileTypeEnum.Excel,
                FileName = "seed_processing.xlsx",
                ContentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                FileHash = "seed-hash-processing",
                UploadedAtUtc = seedNow.AddMinutes(-55),
                ParsedAtUtc = seedNow.AddMinutes(-53),
                ValidatedAtUtc = seedNow.AddMinutes(-52),
                QueuedAtUtc = seedNow.AddMinutes(-51),
                ProcessingStartedAtUtc = seedNow.AddMinutes(-49),
                UploadedBy = "seed-system",
                ClientRequestId = "REQ-SEED-PROCESSING",
                TotalRecords = 30,
                TotalValid = 28,
                TotalInvalid = 2,
                TotalProcessed = 14,
                TotalSucceeded = 11,
                TotalFailed = 3,
                Status = BulkIngestionBatchStatusEnum.Processing,
                LastJobId = "job-seed-processing",
                LastJobMessage = "Procesamiento en ejecución, lote parcial.",
                SummaryErrorsJson = JsonSerializer.Serialize(new[]
                {
                    "Fila 8: tercero no habilitado.",
                    "Fila 17: transacción duplicada por referencia."
                })
            },
            new BulkIngestionBatch
            {
                Id = Guid.NewGuid(),
                BatchReference = $"{BatchReferencePrefix}COMPLETED",
                FileType = BulkIngestionFileTypeEnum.Json,
                FileName = "seed_completed.json",
                ContentType = "application/json",
                FileHash = "seed-hash-completed",
                UploadedAtUtc = seedNow.AddMinutes(-40),
                ParsedAtUtc = seedNow.AddMinutes(-39),
                ValidatedAtUtc = seedNow.AddMinutes(-38),
                QueuedAtUtc = seedNow.AddMinutes(-37),
                ProcessingStartedAtUtc = seedNow.AddMinutes(-36),
                ProcessingFinishedAtUtc = seedNow.AddMinutes(-34),
                UploadedBy = "seed-system",
                ClientRequestId = "REQ-SEED-COMPLETED",
                TotalRecords = 16,
                TotalValid = 16,
                TotalInvalid = 0,
                TotalProcessed = 16,
                TotalSucceeded = 16,
                TotalFailed = 0,
                Status = BulkIngestionBatchStatusEnum.Completed,
                LastJobId = "job-seed-completed",
                LastJobMessage = "Lote procesado sin errores.",
                SummaryErrorsJson = "[]"
            },
            new BulkIngestionBatch
            {
                Id = Guid.NewGuid(),
                BatchReference = $"{BatchReferencePrefix}PARTIAL",
                FileType = BulkIngestionFileTypeEnum.Csv,
                FileName = "seed_partial.csv",
                ContentType = "text/csv",
                FileHash = "seed-hash-partial",
                UploadedAtUtc = seedNow.AddMinutes(-30),
                ParsedAtUtc = seedNow.AddMinutes(-29),
                ValidatedAtUtc = seedNow.AddMinutes(-28),
                QueuedAtUtc = seedNow.AddMinutes(-27),
                ProcessingStartedAtUtc = seedNow.AddMinutes(-26),
                ProcessingFinishedAtUtc = seedNow.AddMinutes(-25),
                UploadedBy = "seed-system",
                ClientRequestId = "REQ-SEED-PARTIAL",
                TotalRecords = 25,
                TotalValid = 23,
                TotalInvalid = 2,
                TotalProcessed = 23,
                TotalSucceeded = 17,
                TotalFailed = 6,
                Status = BulkIngestionBatchStatusEnum.PartiallyProcessed,
                RetryCount = 1,
                LastJobId = "job-seed-partial",
                LastJobMessage = "Lote completado con errores funcionales.",
                SummaryErrorsJson = JsonSerializer.Serialize(new[]
                {
                    "6 transacciones rechazadas por validación funcional.",
                    "2 filas inválidas por estructura."
                })
            },
            new BulkIngestionBatch
            {
                Id = Guid.NewGuid(),
                BatchReference = $"{BatchReferencePrefix}FAILED",
                FileType = BulkIngestionFileTypeEnum.Excel,
                FileName = "seed_failed.xlsx",
                ContentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                FileHash = "seed-hash-failed",
                UploadedAtUtc = seedNow.AddMinutes(-20),
                ParsedAtUtc = seedNow.AddMinutes(-19),
                ValidatedAtUtc = seedNow.AddMinutes(-18),
                QueuedAtUtc = seedNow.AddMinutes(-17),
                ProcessingStartedAtUtc = seedNow.AddMinutes(-16),
                ProcessingFinishedAtUtc = seedNow.AddMinutes(-15),
                UploadedBy = "seed-system",
                ClientRequestId = "REQ-SEED-FAILED",
                TotalRecords = 10,
                TotalValid = 10,
                TotalInvalid = 0,
                TotalProcessed = 10,
                TotalSucceeded = 0,
                TotalFailed = 10,
                Status = BulkIngestionBatchStatusEnum.Failed,
                RetryCount = 2,
                LastJobId = "job-seed-failed",
                LastJobMessage = "Error crítico durante procesamiento del lote.",
                SummaryErrorsJson = JsonSerializer.Serialize(new[]
                {
                    "No se pudo persistir transacciones por conflicto de integridad.",
                    "El lote requiere reintento completo."
                })
            },
            new BulkIngestionBatch
            {
                Id = Guid.NewGuid(),
                BatchReference = $"{BatchReferencePrefix}RETRYING",
                FileType = BulkIngestionFileTypeEnum.Json,
                FileName = "seed_retrying.json",
                ContentType = "application/json",
                FileHash = "seed-hash-retrying",
                UploadedAtUtc = seedNow.AddMinutes(-12),
                ParsedAtUtc = seedNow.AddMinutes(-11),
                ValidatedAtUtc = seedNow.AddMinutes(-10),
                QueuedAtUtc = seedNow.AddMinutes(-9),
                ProcessingStartedAtUtc = seedNow.AddMinutes(-8),
                UploadedBy = "seed-system",
                ClientRequestId = "REQ-SEED-RETRYING",
                TotalRecords = 14,
                TotalValid = 14,
                TotalInvalid = 0,
                TotalProcessed = 6,
                TotalSucceeded = 5,
                TotalFailed = 1,
                Status = BulkIngestionBatchStatusEnum.Retrying,
                RetryCount = 3,
                LastJobId = "job-seed-retrying",
                LastJobMessage = "Reintentando solo transacciones fallidas.",
                SummaryErrorsJson = JsonSerializer.Serialize(new[]
                {
                    "Reintento en curso para referencias duplicadas."
                })
            }
        ];
    }

    private static List<BulkIngestionItem> BuildItemsForScenarios(IReadOnlyList<BulkIngestionBatch> batches, DateTime seedNow)
    {
        var items = new List<BulkIngestionItem>();

        foreach (var batch in batches)
        {
            var scenarioItems = BuildScenarioItems(batch, seedNow);
            items.AddRange(scenarioItems);
        }

        return items;
    }

    private static List<BulkIngestionAttempt> BuildAttemptsForScenarios(IReadOnlyList<BulkIngestionBatch> batches, DateTime seedNow)
    {
        var attempts = new List<BulkIngestionAttempt>();

        foreach (var batch in batches)
        {
            attempts.AddRange(BuildAttempts(batch, seedNow));
        }

        return attempts;
    }

    private static IEnumerable<BulkIngestionItem> BuildScenarioItems(BulkIngestionBatch batch, DateTime seedNow)
    {
        var items = new List<BulkIngestionItem>();
        var total = Math.Max(batch.TotalRecords, 6);

        for (var index = 1; index <= total; index++)
        {
            var status = ResolveItemStatus(batch.Status, index);
            var reference = $"{batch.BatchReference}-ITEM-{index:000}";
            var rawPayload = JsonSerializer.Serialize(new
            {
                Reference = reference,
                Amount = 100000 + (index * 250),
                SourceAccountNumber = $"1020{index:000000}",
                DestinationAccountNumber = $"2040{index:000000}",
                DestinationInstitutionId = (index % 4) + 1
            });

            items.Add(new BulkIngestionItem
            {
                BatchId = batch.Id,
                ItemIndex = index,
                Reference = reference,
                Status = status,
                Message = ResolveItemMessage(status, index),
                TransactionId = status == BulkIngestionItemStatusEnum.Processed ? 900000 + index : null,
                RawPayloadJson = rawPayload,
                NormalizedPayloadJson = status == BulkIngestionItemStatusEnum.StructuralError ? null : rawPayload,
                CreatedAt = seedNow,
                UpdatedAt = seedNow
            });
        }

        return items;
    }

    private static IEnumerable<BulkIngestionAttempt> BuildAttempts(BulkIngestionBatch batch, DateTime seedNow)
    {
        var attempts = new List<BulkIngestionAttempt>();

        if (batch.Status == BulkIngestionBatchStatusEnum.Uploaded)
        {
            return attempts;
        }

        attempts.Add(new BulkIngestionAttempt
        {
            BatchId = batch.Id,
            AttemptNumber = 1,
            TriggerType = BulkIngestionTriggerTypeEnum.Initial,
            Scope = BulkIngestionRetryScopeEnum.Full,
            TriggeredBy = "seed-system",
            TriggeredAtUtc = batch.QueuedAtUtc ?? batch.UploadedAtUtc,
            Status = ResolveAttemptStatus(batch.Status, isRetry: false),
            JobId = batch.LastJobId,
            StartedAtUtc = batch.ProcessingStartedAtUtc,
            FinishedAtUtc = batch.ProcessingFinishedAtUtc,
            TotalProcessed = batch.TotalProcessed,
            TotalSucceeded = batch.TotalSucceeded,
            TotalFailed = batch.TotalFailed,
            ResultMessage = batch.LastJobMessage,
            CreatedAt = seedNow,
            UpdatedAt = seedNow
        });

        if (batch.Status is BulkIngestionBatchStatusEnum.PartiallyProcessed or BulkIngestionBatchStatusEnum.Failed or BulkIngestionBatchStatusEnum.Retrying)
        {
            attempts.Add(new BulkIngestionAttempt
            {
                BatchId = batch.Id,
                AttemptNumber = 2,
                TriggerType = BulkIngestionTriggerTypeEnum.Retry,
                Scope = BulkIngestionRetryScopeEnum.FailedOnly,
                TriggeredBy = "ops.user",
                TriggeredAtUtc = seedNow.AddMinutes(-5),
                Status = batch.Status == BulkIngestionBatchStatusEnum.Retrying
                    ? BulkIngestionAttemptStatusEnum.Processing
                    : BulkIngestionAttemptStatusEnum.PartiallyProcessed,
                JobId = $"retry-{batch.BatchReference.ToLowerInvariant()}",
                StartedAtUtc = seedNow.AddMinutes(-5),
                FinishedAtUtc = batch.Status == BulkIngestionBatchStatusEnum.Retrying ? null : seedNow.AddMinutes(-3),
                TotalProcessed = Math.Max(1, batch.TotalFailed),
                TotalSucceeded = batch.Status == BulkIngestionBatchStatusEnum.Retrying ? 0 : Math.Max(0, batch.TotalFailed - 1),
                TotalFailed = batch.Status == BulkIngestionBatchStatusEnum.Retrying ? 1 : 1,
                ResultMessage = batch.Status == BulkIngestionBatchStatusEnum.Retrying
                    ? "Reintento en ejecución para errores de negocio y duplicados."
                    : "Reintento completado parcialmente, queda 1 pendiente.",
                CreatedAt = seedNow,
                UpdatedAt = seedNow
            });
        }

        return attempts;
    }

    private static BulkIngestionItemStatusEnum ResolveItemStatus(BulkIngestionBatchStatusEnum batchStatus, int index)
    {
        return batchStatus switch
        {
            BulkIngestionBatchStatusEnum.Uploaded => BulkIngestionItemStatusEnum.Ready,
            BulkIngestionBatchStatusEnum.Queued => index % 5 == 0 ? BulkIngestionItemStatusEnum.StructuralError : BulkIngestionItemStatusEnum.Ready,
            BulkIngestionBatchStatusEnum.Processing => index <= 10
                ? BulkIngestionItemStatusEnum.Processed
                : index % 4 == 0 ? BulkIngestionItemStatusEnum.ProcessingError : BulkIngestionItemStatusEnum.Ready,
            BulkIngestionBatchStatusEnum.Completed => BulkIngestionItemStatusEnum.Processed,
            BulkIngestionBatchStatusEnum.PartiallyProcessed => index % 6 switch
            {
                0 => BulkIngestionItemStatusEnum.StructuralError,
                1 => BulkIngestionItemStatusEnum.ProcessingError,
                _ => BulkIngestionItemStatusEnum.Processed
            },
            BulkIngestionBatchStatusEnum.Failed => index % 3 == 0
                ? BulkIngestionItemStatusEnum.StructuralError
                : BulkIngestionItemStatusEnum.ProcessingError,
            BulkIngestionBatchStatusEnum.Retrying => index <= 5
                ? BulkIngestionItemStatusEnum.Processed
                : index % 4 == 0 ? BulkIngestionItemStatusEnum.ProcessingError : BulkIngestionItemStatusEnum.Ready,
            _ => BulkIngestionItemStatusEnum.Ready
        };
    }

    private static string ResolveItemMessage(BulkIngestionItemStatusEnum status, int index)
    {
        return status switch
        {
            BulkIngestionItemStatusEnum.Ready => "Registro listo para procesamiento.",
            BulkIngestionItemStatusEnum.Processed => "Transacción registrada correctamente.",
            BulkIngestionItemStatusEnum.StructuralError => index % 2 == 0
                ? "Error estructural: campo amount inválido o faltante."
                : "Error estructural: longitud de cuenta destino no válida.",
            BulkIngestionItemStatusEnum.ProcessingError => index % 2 == 0
                ? "Error funcional: tercero no autorizado para débito."
                : "Error funcional: referencia duplicada detectada.",
            _ => "Sin mensaje"
        };
    }

    private static BulkIngestionAttemptStatusEnum ResolveAttemptStatus(BulkIngestionBatchStatusEnum batchStatus, bool isRetry)
    {
        if (isRetry)
        {
            return batchStatus == BulkIngestionBatchStatusEnum.Retrying
                ? BulkIngestionAttemptStatusEnum.Processing
                : BulkIngestionAttemptStatusEnum.PartiallyProcessed;
        }

        return batchStatus switch
        {
            BulkIngestionBatchStatusEnum.Queued => BulkIngestionAttemptStatusEnum.Queued,
            BulkIngestionBatchStatusEnum.Processing => BulkIngestionAttemptStatusEnum.Processing,
            BulkIngestionBatchStatusEnum.Completed => BulkIngestionAttemptStatusEnum.Completed,
            BulkIngestionBatchStatusEnum.PartiallyProcessed => BulkIngestionAttemptStatusEnum.PartiallyProcessed,
            BulkIngestionBatchStatusEnum.Failed => BulkIngestionAttemptStatusEnum.Failed,
            BulkIngestionBatchStatusEnum.Retrying => BulkIngestionAttemptStatusEnum.PartiallyProcessed,
            _ => BulkIngestionAttemptStatusEnum.Queued
        };
    }
}
