using Cfa.ACHInterbank.Domain.Entities.Transactions.Enums;
using Cfa.ACHInterbank.Domain.Models.ACH;
using Cfa.ACHInterbank.Persistence.ACH.Services.Implementation;
using Cfa.ACHInterbank.Persistence.DataBase;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Cfa.ACHInterbank.Tests;

public sealed class IncomingNachaHistoricalCorrectionServiceTests
{
    private static readonly Guid HistoricalIngestionId = Guid.Parse("604c10d7-ec07-4a44-ad0c-81180c5a8228");

    [Fact]
    public async Task ApplyAsync_CycleResolution_ShouldCorrectHistoricalCyclePreserveR96AndRemainIdempotent()
    {
        await using var fixture = await HistoricalFixture.CreateAsync();
        var sut = new IncomingNachaHistoricalCorrectionService(fixture.Context);

        await sut.ApplyAsync(HistoricalIngestionId);

        Assert.Equal("CENIT-20260713-02", await fixture.Context.IncomingNachaFileIngestions
            .Where(x => x.Id == HistoricalIngestionId)
            .Select(x => x.ResolvedAchCycleId)
            .SingleAsync());

        Assert.Equal(4, await fixture.Context.AchTransactions.CountAsync(x => x.AchCycleId == "CENIT-20260713-02"));
        Assert.Equal(4, await fixture.Context.AchBatches.CountAsync(x => x.AchCycleId == "CENIT-20260713-02"));
        Assert.Equal(4, await fixture.Context.IncomingNachaDispatchQueue.CountAsync(x => x.IncomingNachaFileIngestionId == HistoricalIngestionId && x.AchCycleId == "CENIT-20260713-02"));
        Assert.All(await fixture.Context.IncomingNachaDispatchQueue
            .Where(x => x.IncomingNachaFileIngestionId == HistoricalIngestionId)
            .ToListAsync(), x => Assert.Equal(IncomingNachaDispatchQueueStatus.Confirmed, x.QueueStatus));

        var executions = await fixture.Context.IncomingNachaIntegrationExecution
            .AsNoTracking()
            .Where(x => x.SoapResponseCode == "R96")
            .ToListAsync();
        Assert.Equal(6, executions.Count);
        Assert.All(executions, x => Assert.Equal("R96", x.SoapResponseCode));

        Assert.Equal(1, await fixture.Context.IncomingNachaProcessingEvents.CountAsync(x =>
            x.IncomingNachaFileIngestionId == HistoricalIngestionId
            && x.EventType == "CycleResolutionCorrected"
            && x.EventStatus == "Completed"));
        Assert.Equal(1, await fixture.Context.IncomingNachaProcessingEvents.CountAsync(x =>
            x.IncomingNachaFileIngestionId == HistoricalIngestionId
            && x.EventType == "ProcTransaccionesMappingCorrection"
            && x.EventStatus == "Completed"));

        await sut.ApplyAsync(HistoricalIngestionId);

        Assert.Equal(1, await fixture.Context.IncomingNachaProcessingEvents.CountAsync(x =>
            x.IncomingNachaFileIngestionId == HistoricalIngestionId
            && x.EventType == "CycleResolutionCorrected"
            && x.EventStatus == "Completed"));
        Assert.Equal(1, await fixture.Context.IncomingNachaProcessingEvents.CountAsync(x =>
            x.IncomingNachaFileIngestionId == HistoricalIngestionId
            && x.EventType == "ProcTransaccionesMappingCorrection"
            && x.EventStatus == "Completed"));
    }

    private sealed class HistoricalFixture : IAsyncDisposable
    {
        private readonly SqliteConnection _connection;

        public AchDbContext Context { get; }

        private HistoricalFixture(SqliteConnection connection, AchDbContext context)
        {
            _connection = connection;
            Context = context;
        }

        public static async Task<HistoricalFixture> CreateAsync()
        {
            var connection = new SqliteConnection("DataSource=:memory:");
            await connection.OpenAsync();

            using (var command = connection.CreateCommand())
            {
                command.CommandText = "PRAGMA foreign_keys = ON;";
                command.ExecuteNonQuery();
            }

            var options = new DbContextOptionsBuilder<AchDbContext>()
                .UseSqlite(connection)
                .Options;

            var context = new AchDbContext(options);
            await context.Database.EnsureCreatedAsync();

            var fixture = new HistoricalFixture(connection, context);
            await fixture.SeedAsync();
            return fixture;
        }

        public async ValueTask DisposeAsync()
        {
            await Context.DisposeAsync();
            await _connection.DisposeAsync();
        }

        private async Task SeedAsync()
        {
            var clearingHouseConfig = new ClearingHouseConfig
            {
                Id = 2,
                ClearingHouseId = 2,
                HolidayStrategy = "Colombian"
            };
            Context.ClearingHouseConfigs.Add(clearingHouseConfig);
            Context.ClearingHouses.Add(new ClearingHouse
            {
                Id = 2,
                Name = "CENIT",
                Code = "CENIT",
                OriginCode = "0001283",
                ClearingHouseId = 2
            });

            var oldCycle = new AchCycle
            {
                Id = "CENIT-20260713-01",
                CycleName = "Ciclo 1",
                ClearingHouseId = 2,
                ProcessingDate = new DateTime(2026, 7, 13),
                CutoffTime = new TimeSpan(8, 0, 0),
                StartTime = new TimeSpan(7, 0, 0),
                EndTime = new TimeSpan(9, 0, 0)
            };
            var correctedCycle = new AchCycle
            {
                Id = "CENIT-20260713-02",
                CycleName = "Ciclo 2",
                ClearingHouseId = 2,
                ProcessingDate = new DateTime(2026, 7, 13),
                CutoffTime = new TimeSpan(10, 0, 0),
                StartTime = new TimeSpan(9, 0, 0),
                EndTime = new TimeSpan(11, 0, 0)
            };
            Context.AchCycles.AddRange(oldCycle, correctedCycle);

            var sourceInstitution = new FinancialInstitution
            {
                Id = 1,
                Name = "Banco Externo",
                RoutingNumber = "99999",
                TransitCode = "900",
                IsDefaultSource = false,
                Status = FinancialInstitutionStatus.Active
            };
            sourceInstitution.CalculateCheckDigit();

            var destinationInstitution = new FinancialInstitution
            {
                Id = 2,
                Name = "CFA",
                RoutingNumber = "00001",
                TransitCode = "283",
                IsDefaultSource = true,
                Status = FinancialInstitutionStatus.Active
            };
            destinationInstitution.CalculateCheckDigit();
            Context.FinancialInstitutions.AddRange(sourceInstitution, destinationInstitution);

            var ingestion = new IncomingNachaFileIngestion
            {
                Id = HistoricalIngestionId,
                FileName = "0001283.002.20260713.1",
                FileHashSha256 = "HISTORICAL-HASH",
                FileSize = 4096,
                ContentType = "text/plain",
                UploadedBy = "historical-loader",
                CorrelationId = "historical-correlation-id",
                IngestionStatus = IncomingNachaIngestionStatus.Completado,
                ParsingStatus = IncomingNachaParsingStatus.Exitoso,
                CycleResolutionStatus = IncomingNachaCycleResolutionStatus.ResueltoInferido,
                ResolvedClearingHouseId = 2,
                OperationalDate = new DateTime(2026, 7, 13),
                ResolvedAchCycleId = oldCycle.Id,
                ResolutionMode = "HistoricalCorrectionSeed",
                ResolutionConfidence = 0.95m,
                ResolutionEvidenceJson = "{}",
                Notes = "seeded historical ingestion for correction tests"
            };
            Context.IncomingNachaFileIngestions.Add(ingestion);

            var header = new NachaHeader
            {
                NachaID = "NACHA-HIST-001",
                IncomingNachaFileIngestionId = HistoricalIngestionId,
                ImmediateOrigin = "0001283",
                ImmediateDestination = "0000000000",
                FileIdModifier = "1",
                ReferenceCode = "HIST-0001",
                ClearingHouseId = 2,
                AchCycleId = oldCycle.Id,
                CycleNumber = 1
            };
            Context.NachaHeaders.Add(header);

            var queues = new List<IncomingNachaDispatchQueue>();

            for (var index = 1; index <= 4; index++)
            {
                var batchNumber = index;
                var batchId = 100 + index;
                var entryId = 200 + index;
                var addendaId = 300 + index;
                var transactionId = 400 + index;
                var accountNumber = $"00000032{index:00}";
                var traceNumber = $"0001283{index:0000000}";
                var companyIdentification = $"L{index:0000000}";

                Context.BatchHeaders.Add(new BatchHeader
                {
                    BatchID = 500 + index,
                    NachaID = header.NachaID,
                    CompanyId = companyIdentification,
                    CompanyName = "LOCAL LIVE CENIT",
                    StandardEntryClassCode = "PPD",
                    CompanyEntryDescription = "PAGOS",
                    EffectiveEntryDate = "20260713",
                    OriginParticipantEntityCode = "0001283",
                    BatchNumber = batchNumber
                });

                Context.EntryDetails.Add(new EntryDetail
                {
                    EntryDetailID = entryId,
                    NachaID = header.NachaID,
                    TransactionCode = "32",
                    ReceivingParticipantEntityCode = "00001283",
                    AccountNumber = accountNumber,
                    Amount = 100m + index,
                    RecipIdNumber = $"90000000{index}",
                    RecipUserName = $"Beneficiario {index}",
                    SequenceNumber = traceNumber,
                    BatchNumber = batchNumber
                });

                Context.AddendaRecords.Add(new AddendaRecord
                {
                    AddendaID = addendaId,
                    NachaID = header.NachaID,
                    CodeTypeAddendumRecord = "05",
                    InfofromOriginator = $"INFO-{index}",
                    InvoiceOrAccountNumber = $"INV-{index}",
                    EntryDetailSequenceNumber = traceNumber
                });

                var batch = new AchBatch
                {
                    Id = batchId,
                    AchCycleId = oldCycle.Id,
                    ServiceClassCode = "220",
                    CompanyName = "LOCAL LIVE CENIT",
                    CompanyIdentification = companyIdentification,
                    CompanyEntryDescription = "PAGOS",
                    CompanyEntryDescriptionId = 1,
                    OriginOrOdfi = "0001283",
                    EffectiveEntryDate = new DateTime(2026, 7, 13),
                    BatchSequenceNumber = index,
                    TotalDebitAmount = 0m,
                    TotalCreditAmount = 100m + index
                };
                Context.AchBatches.Add(batch);

                var transaction = new AchTransaction
                {
                    Id = transactionId,
                    Amount = 100m + index,
                    TransactionExternalId = $"local-live-proc-transacciones-{index}",
                    Reference = $"local-live-proc-transacciones-{index}",
                    Type = TransactionTypeEnum.Credit,
                    TransactionCode = "32",
                    ServiceClassCode = "220",
                    CompanyEntryDescriptionId = 1,
                    CompanyName = "LOCAL LIVE CENIT",
                    CompanyIdentification = companyIdentification,
                    OriginatingDFI = "99999900",
                    ReceivingDFI = "00001283",
                    TraceNumber = traceNumber,
                    TraceSequenceNumber = 1_000_000 + index,
                    EffectiveEntryDate = new DateTime(2026, 7, 13),
                    AddendaRecordIndicator = true,
                    IsPrenotification = false,
                    State = AchTransferStateEnum.Pending,
                    StateChangedAtUtc = DateTime.UtcNow,
                    SourceAccountNumber = $"old-source-{index}",
                    DestinationAccountNumber = accountNumber,
                    SourceInstitutionId = 1,
                    DestinationInstitutionId = 2,
                    AchCycleId = oldCycle.Id,
                    AchBatchId = batchId,
                    OriginalTraceRef = traceNumber,
                    RecipientIdNumber = $"90000000{index}",
                    DiscretionaryData = string.Empty
                };
                Context.AchTransactions.Add(transaction);

                var classification = new IncomingNachaEntryClassification
                {
                    Id = Guid.NewGuid(),
                    IncomingNachaFileIngestionId = HistoricalIngestionId,
                    EntryDetailId = entryId,
                    AddendaRecordId = addendaId,
                    FunctionalClass = IncomingNachaFunctionalClass.CreditoEntrante,
                    EligibilityStatus = IncomingNachaEligibilityStatus.Elegible,
                    RequiresLink = true,
                    RequiresManualResolution = false,
                    OriginalTraceRef = traceNumber,
                    BusinessMeaning = "local-live-proc-transacciones"
                };
                Context.IncomingNachaEntryClassifications.Add(classification);

                var link = new IncomingNachaTransactionLink
                {
                    Id = Guid.NewGuid(),
                    IncomingNachaFileIngestionId = HistoricalIngestionId,
                    EntryDetailId = entryId,
                    AddendaRecordId = addendaId,
                    AchTransactionId = transactionId,
                    LinkType = IncomingNachaLinkType.ExactTrace15,
                    ConfidenceScore = 1m,
                    EvidenceJson = "{}",
                    LinkedBy = "local-live-proc-transacciones",
                    IsFinal = true
                };
                Context.IncomingNachaTransactionLinks.Add(link);

                var queue = new IncomingNachaDispatchQueue
                {
                    Id = Guid.NewGuid(),
                    IncomingNachaFileIngestionId = HistoricalIngestionId,
                    IncomingNachaEntryClassificationId = classification.Id,
                    IncomingNachaTransactionLinkId = link.Id,
                    AchTransactionId = transactionId,
                    AchCycleId = oldCycle.Id,
                    ClearingHouseId = 2,
                    OperationalDate = new DateTime(2026, 7, 13),
                    QueueStatus = IncomingNachaDispatchQueueStatus.Confirmed,
                    Priority = index,
                    IdempotencyDispatchKey = $"historical-{index}",
                    AttemptCount = 1,
                    LastResponseCode = "R96",
                    ConfirmedAtUtc = DateTime.UtcNow
                };
                Context.IncomingNachaDispatchQueue.Add(queue);
                queues.Add(queue);
            }

            var queueIds = queues.Select(x => x.Id).ToArray();
            for (var index = 1; index <= 6; index++)
            {
                Context.IncomingNachaIntegrationExecution.Add(new IncomingNachaIntegrationExecution
                {
                    Id = Guid.NewGuid(),
                    DispatchQueueId = queueIds[(index - 1) % queueIds.Length],
                    MethodName = "Proc_Transacciones",
                    SoapMethodName = "Proc_Transacciones",
                    SoapEndpoint = "http://localhost:7083/WSCFAACH.svc",
                    ExecutionMode = "Live",
                    MappingVersion = 1,
                    MappingSnapshotHash = "snapshot-historical",
                    RequestHash = $"request-{index}",
                    ResponseHash = $"response-{index}",
                    RequestPayloadXml = "<request />",
                    ResponsePayloadXml = "<response />",
                    SoapResponseCode = "R96",
                    SoapResponseDescription = "Rechazo funcional",
                    SoapTechnicalStatus = "Completed",
                    IsSuccessful = false,
                    IsFunctionalRejection = true,
                    IsTechnicalFailure = false,
                    TechnicalException = string.Empty,
                    DurationMs = 120,
                    ResponseCode = "R96",
                    ResponseMessage = "R96",
                    IsSuccess = false,
                    IsRetryable = false,
                    StartedAtUtc = DateTime.UtcNow.AddMinutes(-index),
                    FinishedAtUtc = DateTime.UtcNow.AddMinutes(-index).AddSeconds(1),
                    CorrelationId = $"corr-{index}"
                });
            }

            await Context.SaveChangesAsync();
        }
    }
}
