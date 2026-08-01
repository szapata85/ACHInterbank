using Cfa.ACHInterbank.Application.ACH.Models;
using Cfa.ACHInterbank.Domain.Entities.Transactions.Enums;
using Cfa.ACHInterbank.Domain.Models.ACH;
using Cfa.ACHInterbank.Persistence.ACH.Services.Implementation;
using Cfa.ACHInterbank.Persistence.DataBase;
using Microsoft.EntityFrameworkCore;

namespace Cfa.ACHInterbank.Tests;

public class IncomingNachaCommandCenterServiceTests
{
    [Fact]
    public async Task RetryManualAsync_ShouldQueueRetryPendingItem_AndCreateAuditEvent()
    {
        await using var context = await CreateContextAsync();
        var queue = await SeedQueueAsync(context, IncomingNachaDispatchQueueStatus.RetryPending);
        var sut = CreateSut(context);

        var result = await sut.RetryManualAsync(queue.Id, new IncomingNachaManualActionRequest
        {
            IdempotencyKey = "retry-1",
            Justification = "retry manual por incidente"
        }, "ops.user");

        Assert.Equal(IncomingNachaDispatchQueueStatus.RetryPending, result.PreviousStatus);
        Assert.Equal(IncomingNachaDispatchQueueStatus.Queued, result.CurrentStatus);
        Assert.False(result.IsIdempotentReplay);

        var refreshed = await context.IncomingNachaDispatchQueue.FirstAsync(x => x.Id == queue.Id);
        Assert.Equal(IncomingNachaDispatchQueueStatus.Queued, refreshed.QueueStatus);
        Assert.True(await context.IncomingNachaProcessingEvents.AnyAsync(x =>
            x.EventType == "DispatchTransition"
            && x.Message == "Event:ManualRetry;IdempotencyKey:retry-1"));
    }

    [Fact]
    public async Task RetryManualAsync_ShouldRejectConfirmed()
    {
        await using var context = await CreateContextAsync();
        var queue = await SeedQueueAsync(context, IncomingNachaDispatchQueueStatus.Confirmed);
        var sut = CreateSut(context);

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => sut.RetryManualAsync(queue.Id, new IncomingNachaManualActionRequest
        {
            IdempotencyKey = "retry-confirmed",
            Justification = "retry manual no permitido"
        }, "ops.user"));

        Assert.Contains("INCOMING_NACHA_STATE_MACHINE_GUARD_MANUAL_RETRY", ex.Message);
    }

    [Fact]
    public async Task UnblockManualAsync_ShouldRejectNonBlocked()
    {
        await using var context = await CreateContextAsync();
        var queue = await SeedQueueAsync(context, IncomingNachaDispatchQueueStatus.RetryPending);
        var sut = CreateSut(context);

        await Assert.ThrowsAsync<InvalidOperationException>(() => sut.UnblockManualAsync(queue.Id, new IncomingNachaManualActionRequest
        {
            IdempotencyKey = "unblock-1",
            Justification = "desbloqueo"
        }, "ops.user"));
    }

    [Fact]
    public async Task RetryManualAsync_ShouldBeIdempotent_OnRepeatedIdempotencyKey()
    {
        await using var context = await CreateContextAsync();
        var queue = await SeedQueueAsync(context, IncomingNachaDispatchQueueStatus.RetryPending);
        var sut = CreateSut(context);

        _ = await sut.RetryManualAsync(queue.Id, new IncomingNachaManualActionRequest
        {
            IdempotencyKey = "retry-replay",
            Justification = "retry manual por incidente"
        }, "ops.user");

        var replay = await sut.RetryManualAsync(queue.Id, new IncomingNachaManualActionRequest
        {
            IdempotencyKey = "retry-replay",
            Justification = "retry manual por incidente"
        }, "ops.user");

        Assert.True(replay.IsIdempotentReplay);
        Assert.Equal(1, await context.IncomingNachaProcessingEvents.CountAsync(x =>
            x.EventType == "DispatchTransition"
            && x.Message == "Event:ManualRetry;IdempotencyKey:retry-replay"));
    }

    [Fact]
    public async Task GetQueueDetailAsync_ShouldReturnAllowedActions_FromStateMachine()
    {
        await using var context = await CreateContextAsync();
        var queue = await SeedQueueAsync(context, IncomingNachaDispatchQueueStatus.Blocked);
        var sut = CreateSut(context);

        var detail = await sut.GetQueueDetailAsync(queue.Id);

        Assert.NotNull(detail);
        Assert.True(detail!.Queue.AllowedActions.CanUnblock);
        Assert.False(detail.Queue.AllowedActions.CanRetry);
        Assert.Contains("unblock", detail.Queue.AllowedActions.AllowedActions);
        Assert.DoesNotContain("retry", detail.Queue.AllowedActions.AllowedActions);
        Assert.Equal("Requiere atención", detail.Queue.QueueStatusText);
        Assert.Equal("Proc_Transacciones", detail.Queue.SoapOperation);
        Assert.True(detail.Queue.ScheduledAtUtc > DateTime.MinValue);
        Assert.True(detail.Queue.MaxAttempts > 0);
    }

    [Fact]
    public async Task GetQueueDetailAsync_ShouldExposeSoapTechnicalAndFunctionalTrace_InSpanish()
    {
        await using var context = await CreateContextAsync();
        var queue = await SeedQueueAsync(context, IncomingNachaDispatchQueueStatus.RetryPending);
        context.IncomingNachaIntegrationExecution.Add(new IncomingNachaIntegrationExecution
        {
            DispatchQueueId = queue.Id,
            EntryDetailId = 1,
            AttemptNumber = 2,
            ClearingHouseId = queue.ClearingHouseId,
            MethodName = "Proc_Transacciones",
            SoapEndpoint = "WSCFAACH",
            CorrelationId = "corr-intento-2",
            ProcessingStatus = IncomingNachaIndividualProcessingStatus.RetryPending,
            BusinessOutcome = IncomingNachaBusinessOutcome.NotProcessed,
            TransportStatus = Cfa.ACHInterbank.Domain.Entities.Integrations.IntegrationTransportStatus.TimedOut,
            TechnicalErrorCode = "ITIMEOUT",
            TechnicalErrorMessage = "El servicio no respondió dentro del tiempo permitido.",
            ResultCode = string.Empty,
            ResultDescription = string.Empty,
            ResultSource = "SOAP",
            DurationMs = 30000,
            StartedAtUtc = DateTime.UtcNow.AddSeconds(-30),
            FinishedAtUtc = DateTime.UtcNow,
            ProcessedAtUtc = DateTime.UtcNow,
            IsTechnicalFailure = true,
            IsRetryable = true
        });
        await context.SaveChangesAsync();

        var detail = await CreateSut(context).GetQueueDetailAsync(queue.Id);

        var attempt = Assert.Single(detail!.Executions);
        Assert.Equal("Pendiente de reintento", attempt.ProcessingStatusText);
        Assert.Equal("No procesado", attempt.BusinessOutcomeText);
        Assert.Equal("Tiempo de espera agotado", attempt.TransportStatusText);
        Assert.Equal("ITIMEOUT", attempt.TechnicalErrorCode);
        Assert.Equal(30000, attempt.DurationMs);
        Assert.Empty(attempt.ResultCode);
        Assert.Null(attempt.AchReturnCodeId);
    }

    [Fact]
    public async Task GetIngestionValidationsAsync_ShouldReturnPersistedHumanizedIssue_WithExpectedAndFoundValues()
    {
        await using var context = await CreateContextAsync();
        var queue = await SeedQueueAsync(context, IncomingNachaDispatchQueueStatus.Blocked);
        var ingestion = await context.IncomingNachaFileIngestions.SingleAsync(x => x.Id == queue.IncomingNachaFileIngestionId);
        ingestion.Stage = IncomingNachaIngestionStage.Rejected;
        ingestion.RejectionCode = "HEADER_DATE_MISMATCH";
        ingestion.RejectionTitle = "La fecha del archivo no corresponde a la fecha operativa";
        ingestion.RejectionDescription = "El archivo corresponde al 30/07/2026, pero la fecha operativa habilitada es 31/07/2026.";
        ingestion.SuggestedAction = "Seleccione el archivo correspondiente a la fecha operativa vigente.";
        context.IncomingNachaFileProcessingResults.Add(new IncomingNachaFileProcessingResult
        {
            IncomingNachaFileIngestionId = ingestion.Id,
            AttemptNumber = 1,
            ParserErrorsJson = System.Text.Json.JsonSerializer.Serialize(new[]
            {
                new IncomingNachaAdmissionIssue(
                    "HEADER_DATE_MISMATCH",
                    ingestion.RejectionTitle,
                    ingestion.RejectionDescription,
                    ingestion.SuggestedAction,
                    "Functional",
                    "Error",
                    "2026-07-31",
                    "2026-07-30")
            })
        });
        await context.SaveChangesAsync();

        var validations = await CreateSut(context).GetIngestionValidationsAsync(ingestion.Id);

        var validation = Assert.Single(validations!);
        Assert.False(validation.IsSuccessful);
        Assert.Equal("2026-07-31", validation.ExpectedValue);
        Assert.Equal("2026-07-30", validation.FoundValue);
        Assert.Equal("Functional", validation.ErrorType);
        Assert.Contains("Seleccione", validation.SuggestedAction);
    }

    [Fact]
    public async Task RetryManualAsync_ShouldRejectBlocked_AndRegisterRejectedAudit()
    {
        await using var context = await CreateContextAsync();
        var queue = await SeedQueueAsync(context, IncomingNachaDispatchQueueStatus.Blocked);
        var sut = CreateSut(context);

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => sut.RetryManualAsync(queue.Id, new IncomingNachaManualActionRequest
        {
            IdempotencyKey = "retry-blocked-rejected",
            Justification = "retry directo bloqueado"
        }, "ops.user"));

        Assert.Contains("INCOMING_NACHA_STATE_MACHINE_GUARD_MANUAL_RETRY", ex.Message);
        Assert.True(await context.IncomingNachaProcessingEvents.AnyAsync(x =>
            x.EventType == "DispatchTransition"
            && x.EventStatus == "Rejected"
            && x.Message == "Event:ManualRetry;IdempotencyKey:retry-blocked-rejected"));
    }

    [Fact]
    public async Task MarkFailedFinalManualAsync_ShouldRejectConfirmed_ByStateMachineGuard()
    {
        await using var context = await CreateContextAsync();
        var queue = await SeedQueueAsync(context, IncomingNachaDispatchQueueStatus.Confirmed);
        var sut = CreateSut(context);

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => sut.MarkFailedFinalManualAsync(queue.Id, new IncomingNachaManualActionRequest
        {
            IdempotencyKey = "mark-final-confirmed",
            Justification = "cerrar como final"
        }, "ops.user"));

        Assert.Contains("INCOMING_NACHA_STATE_MACHINE_GUARD_MANUAL_MARK_FAILED_FINAL", ex.Message);
    }

    [Fact]
    public async Task RequeueManualAsync_ShouldRejectFailedFinal_ByStateMachineGuard()
    {
        await using var context = await CreateContextAsync();
        var queue = await SeedQueueAsync(context, IncomingNachaDispatchQueueStatus.FailedFinal);
        var sut = CreateSut(context);

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => sut.RequeueManualAsync(queue.Id, new IncomingNachaManualActionRequest
        {
            IdempotencyKey = "requeue-failed-final",
            Justification = "requeue no permitido en terminal"
        }, "ops.user"));

        Assert.Contains("INCOMING_NACHA_STATE_MACHINE_GUARD_MANUAL_REQUEUE", ex.Message);
        Assert.True(await context.IncomingNachaProcessingEvents.AnyAsync(x =>
            x.EventType == "DispatchTransition"
            && x.EventStatus == "Rejected"
            && x.Message == "Event:ManualRequeue;IdempotencyKey:requeue-failed-final"));
    }

    [Fact]
    public async Task GetObservabilitySummaryAsync_ShouldReturnAggregatedOperationalKpis()
    {
        await using var context = await CreateContextAsync();
        var blocked = await SeedQueueAsync(context, IncomingNachaDispatchQueueStatus.Blocked);
        context.IncomingNachaDispatchQueue.Add(new IncomingNachaDispatchQueue
        {
            IncomingNachaFileIngestionId = blocked.IncomingNachaFileIngestionId,
            IncomingNachaEntryClassificationId = blocked.IncomingNachaEntryClassificationId,
            IncomingNachaTransactionLinkId = blocked.IncomingNachaTransactionLinkId,
            AchTransactionId = blocked.AchTransactionId,
            AchCycleId = blocked.AchCycleId,
            ClearingHouseId = blocked.ClearingHouseId,
            OperationalDate = DateTime.UtcNow.Date,
            QueueStatus = IncomingNachaDispatchQueueStatus.RetryPending,
            IdempotencyDispatchKey = Guid.NewGuid().ToString("N"),
            Priority = 15
        });
        await context.SaveChangesAsync();
        var sut = CreateSut(context);

        var summary = await sut.GetObservabilitySummaryAsync(24);

        Assert.True(summary.WindowHours >= 1);
        Assert.True(summary.PipelineHealth.TotalIngestions >= 1);
        Assert.True(summary.PipelineHealth.TotalQueueItems >= 2);
        Assert.True(summary.PipelineHealth.BlockedItems >= 1);
        Assert.True(summary.PipelineHealth.RetryPendingItems >= 1);
        Assert.NotNull(summary.QueueByStatus);
        Assert.NotNull(summary.IngestionsByStatus);
        Assert.NotNull(summary.ByClearingHouseCycle);
        Assert.NotNull(summary.Timeline);
    }

    [Fact]
    public async Task GetIngestionsAsync_ShouldPageAndExposeHumanizedStatuses()
    {
        await using var context = await CreateContextAsync();
        _ = await SeedQueueAsync(context, IncomingNachaDispatchQueueStatus.Queued);

        var page = await CreateSut(context).GetIngestionsAsync(new IncomingNachaIngestionQuery
        {
            Page = 1,
            PageSize = 1,
            FileName = "in.ach"
        });

        var item = Assert.Single(page.Items);
        Assert.Equal(1, page.TotalItems);
        Assert.Equal("Recibido", item.IngestionStatusText);
        Assert.Equal("Received", item.StageCode);
        Assert.Equal("Archivo recibido", item.StageText);
    }

    [Fact]
    public async Task GetTransactionsAsync_ShouldExposeEntryDispatchAttemptAndAchResultProgressively()
    {
        await using var context = await CreateContextAsync();
        var queue = await SeedQueueAsync(context, IncomingNachaDispatchQueueStatus.Confirmed);
        var header = new NachaHeader
        {
            NachaID = "FILE-1",
            IncomingNachaFileIngestionId = queue.IncomingNachaFileIngestionId,
            ClearingHouseId = queue.ClearingHouseId,
            AchCycleId = queue.AchCycleId,
            CycleNumber = 1
        };
        var batch = new BatchHeader { BatchID = 1, BatchNumber = 7, NachaID = header.NachaID, NachaHeader = header };
        var entry = new EntryDetail
        {
            EntryDetailID = 1,
            BatchHeaderId = batch.BatchID,
            BatchHeader = batch,
            BatchNumber = batch.BatchNumber,
            NachaID = header.NachaID,
            NachaHeader = header,
            SequenceNumber = "123456789012345",
            TransactionCode = "22",
            Amount = 1250000.25m
        };
        context.NachaHeaders.Add(header);
        context.BatchHeaders.Add(batch);
        context.EntryDetails.Add(entry);
        context.AddendaRecords.Add(new AddendaRecord
        {
            AddendaID = 1,
            EntryDetailId = entry.EntryDetailID,
            EntryDetail = entry,
            NachaID = header.NachaID,
            NachaHeader = header,
            AddendumSequence = "0001"
        });
        context.IncomingNachaIntegrationExecution.Add(new IncomingNachaIntegrationExecution
        {
            DispatchQueueId = queue.Id,
            EntryDetailId = entry.EntryDetailID,
            AttemptNumber = 1,
            ClearingHouseId = queue.ClearingHouseId,
            MethodName = "Proc_Transacciones",
            CorrelationId = "corr-final",
            ProcessingStatus = IncomingNachaIndividualProcessingStatus.Completed,
            BusinessOutcome = IncomingNachaBusinessOutcome.Returned,
            ResultCode = "R16",
            ResultDescription = "Cuenta congelada",
            ResultSource = "SOAP",
            ExternalTransactionId = "EXT-1",
            StartedAtUtc = DateTime.UtcNow.AddSeconds(-1),
            FinishedAtUtc = DateTime.UtcNow,
            ProcessedAtUtc = DateTime.UtcNow,
            IsSuccess = false
        });
        await context.SaveChangesAsync();

        var page = await CreateSut(context).GetTransactionsAsync(
            queue.IncomingNachaFileIngestionId,
            new IncomingNachaTransactionQuery { Page = 1, PageSize = 10, ResultCode = "R16" });

        var transaction = Assert.Single(page.Items);
        Assert.Equal(1250000.25m, transaction.Amount);
        Assert.Equal(1, transaction.AddendaCount);
        Assert.Equal(queue.ClearingHouseId, transaction.ClearingHouseId);
        Assert.Equal(queue.AchCycleId, transaction.AchCycleId);
        Assert.Equal("Proc_Transacciones", transaction.SoapOperation);
        Assert.Equal("EXT-1", transaction.ExternalTransactionId);
        Assert.Equal("R16", transaction.ResultCode);
        Assert.Equal("Cuenta congelada", transaction.ResultDescription);
        Assert.Equal("Devuelto", transaction.BusinessOutcomeText);
        Assert.NotNull(transaction.ScheduledAtUtc);
    }

    [Fact]
    public async Task GetTransactionsAsync_ShouldMaskSensitiveData_AndKeepTechnicalAndFunctionalResultsSeparate()
    {
        await using var context = await CreateContextAsync();
        var queue = await SeedQueueAsync(context, IncomingNachaDispatchQueueStatus.RetryPending);
        var header = new NachaHeader
        {
            NachaID = "MASKED-FILE",
            IncomingNachaFileIngestionId = queue.IncomingNachaFileIngestionId,
            ClearingHouseId = queue.ClearingHouseId,
            AchCycleId = queue.AchCycleId
        };
        var batch = new BatchHeader
        {
            BatchID = 12,
            BatchNumber = 3,
            NachaID = header.NachaID,
            NachaHeader = header,
            OriginParticipantEntityCode = "12345678",
            EffectiveEntryDate = "260801"
        };
        var entry = new EntryDetail
        {
            EntryDetailID = 12,
            BatchHeaderId = batch.BatchID,
            BatchHeader = batch,
            BatchNumber = batch.BatchNumber,
            NachaID = header.NachaID,
            NachaHeader = header,
            SequenceNumber = "876543210000012",
            TransactionCode = "22",
            AccountNumber = "9876543210",
            ReceivingParticipantEntityCode = "87654321",
            RecipUserName = "PERSONA PRIVADA",
            Amount = 25m
        };
        context.NachaHeaders.Add(header);
        context.BatchHeaders.Add(batch);
        context.EntryDetails.Add(entry);
        context.AddendaRecords.Add(new AddendaRecord
        {
            AddendaID = 12,
            EntryDetailId = entry.EntryDetailID,
            EntryDetail = entry,
            NachaID = header.NachaID,
            NachaHeader = header,
            PaymentRelatedInformation = "REFERENCIA-SENSIBLE-1234",
            OriginalTraceNumber = "123456789012345"
        });
        context.IncomingNachaIntegrationExecution.Add(new IncomingNachaIntegrationExecution
        {
            DispatchQueueId = queue.Id,
            EntryDetailId = entry.EntryDetailID,
            AttemptNumber = 1,
            ClearingHouseId = queue.ClearingHouseId,
            CorrelationId = "corr-timeout",
            ProcessingStatus = IncomingNachaIndividualProcessingStatus.TechnicalFailed,
            BusinessOutcome = IncomingNachaBusinessOutcome.NotProcessed,
            IsTechnicalFailure = true,
            TechnicalErrorCode = "SOAP_TIMEOUT",
            TechnicalErrorMessage = "Tiempo agotado",
            ResultCode = string.Empty,
            ResultDescription = string.Empty
        });
        await context.SaveChangesAsync();

        var transactions = await CreateSut(context).GetTransactionsAsync(queue.IncomingNachaFileIngestionId,
            new IncomingNachaTransactionQuery { HasAddenda = true, HasTechnicalError = true, PageSize = 20 });
        var transaction = Assert.Single(transactions.Items);
        Assert.Equal("****3210", transaction.AccountNumberMasked);
        Assert.Equal("****5678", transaction.OriginInstitution);
        Assert.Equal("****4321", transaction.DestinationInstitution);
        Assert.Equal("P***", transaction.RecipientNameMasked);
        Assert.Equal("Error técnico", transaction.ProcessingStatusText);
        Assert.Equal("No procesado", transaction.BusinessOutcomeText);
        Assert.Empty(transaction.ResultCode);
        Assert.Null(transaction.AchReturnCodeId);

        var addendas = await CreateSut(context).GetAddendasAsync(queue.IncomingNachaFileIngestionId, entry.EntryDetailID);
        var addenda = Assert.Single(addendas);
        Assert.DoesNotContain("REFERENCIA-SENSIBLE", addenda.PaymentInformation);
        Assert.Equal("****2345", addenda.OriginalTraceNumber);
    }

    [Fact]
    public async Task GetIngestionsAsync_ShouldApplyServerFilters_AndExposeOperationalSummaryFields()
    {
        await using var context = await CreateContextAsync();
        var queue = await SeedQueueAsync(context, IncomingNachaDispatchQueueStatus.Queued);
        var ingestion = await context.IncomingNachaFileIngestions.SingleAsync(x => x.Id == queue.IncomingNachaFileIngestionId);
        ingestion.UploadedBy = "operador.prueba";
        ingestion.ResolvedClearingHouseId = 1;
        ingestion.ResolvedAchCycleId = "CC-001";
        ingestion.OperationalDate = new DateTime(2026, 8, 1);
        await context.SaveChangesAsync();

        var result = await CreateSut(context).GetIngestionsAsync(new IncomingNachaIngestionQuery
        {
            ClearingHouseId = 1,
            AchCycleId = "CC-001",
            OperationalDate = new DateTime(2026, 8, 1),
            SortBy = "fileName",
            SortDescending = false
        });

        var item = Assert.Single(result.Items);
        Assert.Equal("operador.prueba", item.UploadedBy);
        Assert.Equal("CENIT", item.ClearingHouseName);
        Assert.Equal("Programado", item.ProcessingStatusText);
        Assert.Equal("Pendiente", item.OverallResultText);
        Assert.NotNull(item.ScheduledAtUtc);
    }

    private static async Task<IncomingNachaDispatchQueue> SeedQueueAsync(AchDbContext context, IncomingNachaDispatchQueueStatus status)
    {
        var clearing = new ClearingHouse { Id = 1, Name = "CENIT", Code = "CENIT", OriginCode = "00000000", ClearingHouseId = 1 };
        var cycle = new AchCycle
        {
            Id = "CC-001",
            CycleName = "Ciclo",
            ProcessingDate = DateTime.UtcNow.Date,
            StartTime = TimeSpan.FromHours(8),
            EndTime = TimeSpan.FromHours(18),
            ClearingHouseId = 1
        };
        var tx = new AchTransaction
        {
            AchCycleId = cycle.Id,
            Type = TransactionTypeEnum.Debit,
            State = AchTransferStateEnum.Pending,
            Amount = 100,
            EffectiveEntryDate = DateTime.UtcNow.Date,
            TraceNumber = "123456789012345",
            OriginatingDFI = "00000000",
            ReceivingDFI = "11111111",
            SourceAccountNumber = "S1",
            DestinationAccountNumber = "D1",
            TransactionCode = "22",
            Reference = "ref"
        };
        var ingestion = new IncomingNachaFileIngestion { FileName = "in.ach", FileHashSha256 = "h", FileSize = 106, ContentType = "text/plain", CorrelationId = Guid.NewGuid().ToString("N") };
        var classification = new IncomingNachaEntryClassification { IncomingNachaFileIngestionId = ingestion.Id, EntryDetailId = 1, FunctionalClass = IncomingNachaFunctionalClass.Devolucion, EligibilityStatus = IncomingNachaEligibilityStatus.Elegible, BusinessMeaning = "x" };
        context.ClearingHouses.Add(clearing);
        context.AchCycles.Add(cycle);
        context.AchTransactions.Add(tx);
        context.IncomingNachaFileIngestions.Add(ingestion);
        context.IncomingNachaEntryClassifications.Add(classification);
        await context.SaveChangesAsync();

        var link = new IncomingNachaTransactionLink { IncomingNachaFileIngestionId = ingestion.Id, EntryDetailId = 1, AchTransactionId = tx.Id, LinkType = IncomingNachaLinkType.ExactTrace15, IsFinal = true, LinkedBy = "sys" };
        context.IncomingNachaTransactionLinks.Add(link);
        await context.SaveChangesAsync();

        var queue = new IncomingNachaDispatchQueue
        {
            IncomingNachaFileIngestionId = ingestion.Id,
            IncomingNachaEntryClassificationId = classification.Id,
            IncomingNachaTransactionLinkId = link.Id,
            AchTransactionId = tx.Id,
            AchCycleId = cycle.Id,
            ClearingHouseId = 1,
            OperationalDate = DateTime.UtcNow.Date,
            QueueStatus = status,
            IdempotencyDispatchKey = Guid.NewGuid().ToString("N"),
            Priority = 10
        };

        context.IncomingNachaDispatchQueue.Add(queue);
        await context.SaveChangesAsync();
        return queue;
    }

    private static Task<AchDbContext> CreateContextAsync()
    {
        var options = new DbContextOptionsBuilder<AchDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
            .EnableSensitiveDataLogging()
            .Options;

        var context = new AchDbContext(options);
        return Task.FromResult(context);
    }

    private static IncomingNachaCommandCenterService CreateSut(AchDbContext context)
        => new(context, new IncomingNachaStateMachineService());
}
