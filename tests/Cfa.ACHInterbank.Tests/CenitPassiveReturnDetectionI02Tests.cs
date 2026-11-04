using Cfa.ACHInterbank.Api.Controllers;
using Cfa.ACHInterbank.Application.ACH.Models;
using Cfa.ACHInterbank.Application.Security;
using Cfa.ACHInterbank.Domain.Entities.Integrations;
using Cfa.ACHInterbank.Domain.Entities.Transactions.Enums;
using Cfa.ACHInterbank.Domain.Models.ACH;
using Cfa.ACHInterbank.Domain.Models.ACH.Enums;
using Cfa.ACHInterbank.Persistence.ACH.Services.Implementation;
using Cfa.ACHInterbank.Persistence.DataBase;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Cfa.ACHInterbank.Tests;

public sealed class CenitPassiveReturnDetectionI02Tests
{
    private const int CenitId = 2;
    private const int OriginalId = 42;

    [Fact]
    public async Task ConfirmedCenitFailureIsOnlyAPassiveIndication_WhileDisabled_NoWrites()
    {
        await using var db = NewContext();
        await SeedAsync(db);
        var baseline = await db.AuditLogs.CountAsync();
        var findings = await Detector(db).DetectAsync();
        var item = Assert.Single(findings.Findings);
        Assert.Equal(OriginalId, item.OriginalTransactionId);
        Assert.Equal("R02", item.VerifiedReasonCode);
        Assert.Equal("INDICIO_CAUSAL_CENIT", item.Classification);
        Assert.Equal("DESACTIVADO", findings.Mode);
        Assert.False(findings.ProcessingConnected);
        Assert.Equal(1, findings.ExaminedTransactions);
        Assert.Equal(baseline, await db.AuditLogs.CountAsync());
        Assert.Empty(db.ChangeTracker.Entries<AchReturnGenerated>());
        Assert.Empty(db.ChangeTracker.Entries<IncomingNachaProcessingEvent>());
        Assert.Empty(await db.AchReturnsGenerated.ToListAsync());
    }

    [Theory]
    [InlineData("CON_APROBACION")]
    [InlineData("AUTOMATICO")]
    public async Task ModeNeverEnablesExecution_AndAchIsUntouched(string mode)
    {
        await using var db = NewContext();
        await SeedAsync(db);
        var config = await db.ClearingHouseConfigs.SingleAsync(c => c.ClearingHouseId == CenitId);
        config.AutomaticReturnMode = mode;
        await db.SaveChangesAsync();
        var result = await Detector(db).DetectAsync();
        Assert.Equal(mode, result.Mode);
        Assert.False(result.ProcessingConnected);
        Assert.Single(result.Findings);
        Assert.Equal("DESACTIVADO", (await db.ClearingHouseConfigs.SingleAsync(c => c.ClearingHouseId == 1)).AutomaticReturnMode);
        Assert.Empty(await db.AchReturnsGenerated.ToListAsync());
    }

    [Fact]
    public async Task TechnicalOrSimulationOrUnlinkedOrSuccessfulResultNeverBecomesAFinding()
    {
        await using var db = NewContext();
        var execution = await SeedAsync(db);
        var returnCodes = await db.AchReturnCodes.ToListAsync();
        Assert.NotNull(CenitPassiveReturnDetector.Evaluate(execution, CenitId, returnCodes));
        foreach (var invalidate in new Action<IncomingNachaIntegrationExecution>[]
        {
            x => x.TransportStatus = IntegrationTransportStatus.Failed,
            x => x.ExecutionMode = "DRY_RUN",
            x => x.BusinessStatus = IntegrationResponseBusinessStatus.Success,
            x => x.IsTechnicalFailure = true,
            x => x.RetryAllowed = true,
            x => x.ResponseCatalogId = null,
            x => x.DispatchQueue.TransactionLink!.IsFinal = false,
            x => x.DispatchQueue.Classification!.RequiresManualResolution = true,
            x => x.DispatchQueue.QueueStatus = IncomingNachaDispatchQueueStatus.RetryPending,
            x => x.DispatchQueue.AchTransaction!.Direction = AchTransactionDirection.Outgoing,
            x => x.DispatchQueue.AchTransaction!.AchCycle!.ClearingHouse!.Code = "ACHCOL"
        })
        {
            var previousStatus = execution.TransportStatus;
            var previousMode = execution.ExecutionMode;
            var previousBusiness = execution.BusinessStatus;
            var previousTechnical = execution.IsTechnicalFailure;
            var previousRetry = execution.RetryAllowed;
            var previousCatalog = execution.ResponseCatalogId;
            var previousLink = execution.DispatchQueue.TransactionLink!.IsFinal;
            var previousHouse = execution.DispatchQueue.AchTransaction!.AchCycle!.ClearingHouse!.Code;
            var previousDirection = execution.DispatchQueue.AchTransaction.Direction;
            var previousClassificationFlag = execution.DispatchQueue.Classification!.RequiresManualResolution;
            var previousQueueStatus = execution.DispatchQueue.QueueStatus;
            invalidate(execution);
            Assert.Null(CenitPassiveReturnDetector.Evaluate(execution, CenitId, returnCodes));
            execution.TransportStatus = previousStatus;
            execution.ExecutionMode = previousMode;
            execution.BusinessStatus = previousBusiness;
            execution.IsTechnicalFailure = previousTechnical;
            execution.RetryAllowed = previousRetry;
            execution.ResponseCatalogId = previousCatalog;
            execution.DispatchQueue.TransactionLink.IsFinal = previousLink;
            execution.DispatchQueue.AchTransaction.AchCycle!.ClearingHouse!.Code = previousHouse;
            execution.DispatchQueue.AchTransaction.Direction = previousDirection;
            execution.DispatchQueue.Classification.RequiresManualResolution = previousClassificationFlag;
            execution.DispatchQueue.QueueStatus = previousQueueStatus;
        }
    }

    [Fact]
    public async Task OlderRejectionIsNotReportedAfterNewerSuccessfulExecution()
    {
        await using var db = NewContext();
        var previous = await SeedAsync(db);
        db.IncomingNachaIntegrationExecution.Add(new IncomingNachaIntegrationExecution
        {
            Id = Guid.NewGuid(), DispatchQueueId = previous.DispatchQueueId, EntryDetailId = previous.EntryDetailId,
            ClearingHouseId = CenitId, DispatchQueue = previous.DispatchQueue,
            StartedAtUtc = previous.StartedAtUtc.AddMinutes(1), ProcessedAtUtc = previous.ProcessedAtUtc!.Value.AddMinutes(1),
            ExecutionMode = "LIVE", SoapMethodName = "Proc_Transacciones",
            TransportStatus = IntegrationTransportStatus.Succeeded,
            BusinessStatus = IntegrationResponseBusinessStatus.Success,
            BusinessOutcome = IncomingNachaBusinessOutcome.Successful,
            ProcessingStatus = IncomingNachaIndividualProcessingStatus.Completed,
            IsSuccessful = true, ResponseCatalogId = 99, ResultCode = "00"
        });
        await db.SaveChangesAsync();
        Assert.Empty((await Detector(db).DetectAsync()).Findings);
    }

    [Theory]
    [InlineData("D01", "REVISION_CAUSAL", null)]
    [InlineData("R99", "REVISION_CAUSAL", null)]
    [InlineData("R06", "REVISION_SOLICITUD", "R06")]
    [InlineData("R23", "REVISION_CAUSAL", null)] // R23 no aplica a débito monetario
    [InlineData("R34", "REVISION_SOLICITUD", "R34")]
    public async Task CauseCannotBeTakenFromAchRulesOrWithoutCenitApplicability(
        string responseCode, string expectedClassification, string? expectedCause)
    {
        await using var db = NewContext();
        var execution = await SeedAsync(db);
        var tx = execution.DispatchQueue.AchTransaction!;
        tx.Type = TransactionTypeEnum.Debit;
        execution.DispatchQueue.Classification!.FunctionalClass = IncomingNachaFunctionalClass.DebitoEntrante;
        execution.ResultCode = responseCode;
        var code = await db.AchReturnCodes.SingleAsync();
        code.Code = responseCode;
        code.AppliesToDebit = true;
        execution.AchReturnCodeId = code.Id;
        await db.SaveChangesAsync();
        var item = Assert.Single((await Detector(db).DetectAsync()).Findings);
        Assert.Equal(expectedClassification, item.Classification);
        Assert.Equal(expectedCause, item.VerifiedReasonCode);
    }

    [Fact]
    public async Task PrenotificationNeedsAdditionalEvidence_AndReturnAlreadyGeneratedIsIgnored()
    {
        await using var db = NewContext();
        var execution = await SeedAsync(db);
        var tx = execution.DispatchQueue.AchTransaction!;
        tx.Type = TransactionTypeEnum.Prenotification;
        execution.DispatchQueue.Classification!.FunctionalClass = IncomingNachaFunctionalClass.Prenotificacion;
        (await db.AchReturnCodes.SingleAsync()).AppliesToPrenotification = true;
        await db.SaveChangesAsync();
        var item = Assert.Single((await Detector(db).DetectAsync()).Findings);
        Assert.Equal("REVISION_PRENOTIFICACION", item.Classification);
        db.AchReturnsGenerated.Add(new AchReturnGenerated
        {
            OriginalTransactionId = tx.Id, ReturnCycleId = tx.AchCycleId,
            ReturnReasonCode = "R02", Amount = tx.Amount,
            ReturnCycle = tx.AchCycle
        });
        await db.SaveChangesAsync();
        Assert.Empty((await Detector(db).DetectAsync()).Findings);
    }

    [Fact]
    public async Task ForeignAchCodeMappingIsNeverAcceptedAsCenitEvidence()
    {
        await using var db = NewContext();
        var execution = await SeedAsync(db);
        (await db.AchReturnCodes.SingleAsync()).ClearingHouseId = 1;
        await db.SaveChangesAsync();
        var item = Assert.Single((await Detector(db).DetectAsync()).Findings);
        Assert.Equal("REVISION_CAUSAL", item.Classification);
        Assert.Null(item.VerifiedReasonCode);
        Assert.Equal(CenitId, execution.ClearingHouseId);
    }

    [Fact]
    public async Task AQueueBelongingToAchColombiaCannotAppearInCenitDetection()
    {
        await using var db = NewContext();
        var execution = await SeedAsync(db);
        execution.DispatchQueue.ClearingHouseId = 1;
        await db.SaveChangesAsync();
        Assert.Empty((await Detector(db).DetectAsync()).Findings);
        Assert.Equal("DESACTIVADO", (await db.ClearingHouseConfigs
            .SingleAsync(x => x.ClearingHouseId == 1)).AutomaticReturnMode);
    }

    [Fact]
    public async Task RejectsUnboundedSearch_AndHttpEndpointRequiresAuthorizedReadOnlyAccess()
    {
        await using var db = NewContext();
        await SeedAsync(db);
        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() => Detector(db).DetectAsync(101));
        var controller = new CenitPassiveReturnDetectionController(Detector(db));
        Assert.IsType<BadRequestObjectResult>(await controller.Get(0));
        var good = Assert.IsType<OkObjectResult>(await controller.Get(1));
        Assert.IsType<CenitPassiveReturnDetection>(good.Value);
        var cls = typeof(CenitPassiveReturnDetectionController);
        Assert.NotNull(Attribute.GetCustomAttribute(cls, typeof(AuthorizeAttribute)));
        Assert.Equal(P1Policies.ConfigRead,
            cls.GetMethod(nameof(CenitPassiveReturnDetectionController.Get))!
                .GetCustomAttributes(typeof(AuthorizeAttribute), false)
                .Cast<AuthorizeAttribute>().Single().Policy);
        Assert.Equal("api/cenit/devoluciones-automaticas/deteccion",
            cls.GetCustomAttributes(typeof(RouteAttribute), false).Cast<RouteAttribute>().Single().Template);
        Assert.DoesNotContain(cls.GetMethods(), x => x.GetCustomAttributes(typeof(HttpPostAttribute), false).Any()
            || x.GetCustomAttributes(typeof(HttpPutAttribute), false).Any());
    }

    private static CenitPassiveReturnDetector Detector(AchDbContext db) =>
        new(db, new CenitAutomaticReturnSettingsService(db));

    private static AchDbContext NewContext() => new(new DbContextOptionsBuilder<AchDbContext>()
        .UseInMemoryDatabase($"cenit-i02-{Guid.NewGuid():N}").Options);

    private static async Task<IncomingNachaIntegrationExecution> SeedAsync(AchDbContext db)
    {
        var ingestionId = Guid.NewGuid();
        var linkId = Guid.NewGuid();
        var queueId = Guid.NewGuid();
        var now = new DateTime(2026, 11, 2, 15, 0, 0, DateTimeKind.Utc);
        db.ClearingHouseConfigs.AddRange(
            new ClearingHouseConfig { Id = 101, ClearingHouseId = 1, TimeZoneId = "America/Bogota" },
            new ClearingHouseConfig { Id = 102, ClearingHouseId = CenitId, TimeZoneId = "America/Bogota" });
        var achHouse = new ClearingHouse { Id = 1, ClearingHouseId = 101, Code = "ACHCOL", Name = "ACH Colombia", OriginCode = "ACH" };
        var cenitHouse = new ClearingHouse { Id = CenitId, ClearingHouseId = 102, Code = "CENIT", Name = "CENIT", OriginCode = "CEN" };
        db.ClearingHouses.AddRange(achHouse, cenitHouse);
        var cycle = new AchCycle { Id = "CENIT-I02-1", ClearingHouseId = CenitId,
            CycleName = "Ciclo CENIT", ProcessingDate = now.Date, ClearingHouse = cenitHouse };
        var transaction = new AchTransaction { Id = OriginalId, Reference = "I02-CENIT-TEST",
            TransactionExternalId = "I02-CENIT-TEST", Amount = 150m,
            Type = TransactionTypeEnum.Credit, AchCycleId = cycle.Id, AchCycle = cycle,
            Direction = AchTransactionDirection.Incoming, Origin = AchTransactionOrigin.ExternalInstitution,
            ClassificationStatus = AchTransactionClassificationStatus.Determined,
            MonetaryIntegrationRoute = AchMonetaryIntegrationRoute.ProcTransacciones,
            SourceAccountNumber = "0001", DestinationAccountNumber = "0002" };
        var ingestion = new IncomingNachaFileIngestion { Id = ingestionId,
            FileName = "CENIT_IN_TEST", FileHashSha256 = "dummy", UploadedBy = "test" };
        var entry = new EntryDetail { EntryDetailID = 11, TransactionCode = "22", Amount = 150m };
        var link = new IncomingNachaTransactionLink { Id = linkId,
            IncomingNachaFileIngestionId = ingestionId, Ingestion = ingestion,
            EntryDetailId = entry.EntryDetailID, EntryDetail = entry,
            AchTransactionId = OriginalId, AchTransaction = transaction,
            IsFinal = true, LinkType = IncomingNachaLinkType.ExactTrace15 };
        var classification = new IncomingNachaEntryClassification
        {
            Id = Guid.NewGuid(), IncomingNachaFileIngestionId = ingestionId,
            Ingestion = ingestion, EntryDetailId = entry.EntryDetailID, EntryDetail = entry,
            FunctionalClass = IncomingNachaFunctionalClass.CreditoEntrante,
            EligibilityStatus = IncomingNachaEligibilityStatus.Elegible,
            RequiresManualResolution = false
        };
        var queue = new IncomingNachaDispatchQueue { Id = queueId,
            IncomingNachaFileIngestionId = ingestionId, Ingestion = ingestion,
            IncomingNachaEntryClassificationId = classification.Id, Classification = classification,
            IncomingNachaTransactionLinkId = linkId, TransactionLink = link,
            ClearingHouseId = CenitId, AchTransactionId = OriginalId, AchTransaction = transaction,
            AchCycleId = cycle.Id, OperationalDate = now.Date,
            SourceKind = IncomingNachaDispatchSourceKinds.CycleBound,
            QueueStatus = IncomingNachaDispatchQueueStatus.FailedFinal,
            LastErrorCode = "IFUNC" };
        var code = new AchReturnCode { Id = 87, ClearingHouseId = CenitId,
            Code = "R02", AppliesToCredit = true, AppliesToDebit = true,
            AppliesToPrenotification = true, EffectiveFrom = new DateTime(2025, 1, 1),
            IsActive = true };
        var execution = new IncomingNachaIntegrationExecution { Id = Guid.NewGuid(),
            DispatchQueueId = queueId, DispatchQueue = queue, EntryDetailId = 11,
            ClearingHouseId = CenitId, StartedAtUtc = now, ProcessedAtUtc = now,
            ExecutionMode = "LIVE", SoapMethodName = "Proc_Transacciones",
            TransportStatus = IntegrationTransportStatus.Succeeded,
            BusinessStatus = IntegrationResponseBusinessStatus.Rejected,
            BusinessOutcome = IncomingNachaBusinessOutcome.Rejected,
            IsFunctionalRejection = true, IsTechnicalFailure = false,
            IsSuccessful = false, RetryAllowed = false, RequiresManualReview = false,
            ProcessingStatus = IncomingNachaIndividualProcessingStatus.Completed,
            ResponseCatalogId = 99, AchReturnCodeId = code.Id,
            ResultCode = "R02" };
        db.AchReturnCodes.Add(code);
        db.IncomingNachaIntegrationExecution.Add(execution);
        await db.SaveChangesAsync();
        return execution;
    }
}
