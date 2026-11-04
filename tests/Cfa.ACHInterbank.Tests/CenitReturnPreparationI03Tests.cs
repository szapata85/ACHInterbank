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

/// <summary>I03-CENIT: pruebas incorporadas. Hasta ejecución con SDK y BD real, no certifican concurrencia SQL.</summary>
public sealed class CenitReturnPreparationI03Tests
{
    private const int CenitId = 2;
    private const int OriginalId = 42;
    private static readonly DateTimeOffset OperationNow = new(2026, 11, 3, 16, 30, 0, TimeSpan.Zero);

    [Fact]
    public async Task DisabledModeNeverPersistsEvenWithRealConfirmedEvidence()
    {
        await using var db = NewContext();
        var evidence = await SeedAsync(db);
        var baseline = await db.AuditLogs.CountAsync();
        await Assert.ThrowsAsync<CenitReturnPreparationConflictException>(() =>
            Service(db).PrepareAsync(new(OriginalId, evidence.Id)));
        Assert.Empty(await db.CenitReturnPreparations.ToListAsync());
        Assert.Equal(baseline, await db.AuditLogs.CountAsync());
        Assert.Equal("DESACTIVADO", (await db.ClearingHouseConfigs.SingleAsync(x => x.ClearingHouseId == 1)).AutomaticReturnMode);
    }

    [Theory]
    [InlineData("CON_APROBACION")]
    [InlineData("AUTOMATICO")]
    public async Task ExplicitPreparationIsAuditedAndIdempotentWithoutFinancialSideEffects(string mode)
    {
        await using var db = NewContext();
        var evidence = await SeedAsync(db);
        (await db.ClearingHouseConfigs.SingleAsync(c => c.ClearingHouseId == CenitId)).AutomaticReturnMode = mode;
        await db.SaveChangesAsync();
        var prepared = await Service(db).PrepareAsync(new(OriginalId, evidence.Id));
        Assert.Equal("PREPARADA_SIN_ENVIO", prepared.Status);
        Assert.Equal("CENIT-I03-2", prepared.SuggestedReturnCycleId);
        Assert.Equal("R02", prepared.ReasonCode);
        Assert.Equal(new DateTime(2026,11,3), prepared.SuggestedValueDate);
        Assert.False(prepared.ProcessingConnected);
        Assert.Single(await Service(db).ListAsync());
        Assert.Single(await db.CenitReturnPreparations.ToListAsync());
        Assert.Contains(db.AuditLogs, x => x.EntityName == nameof(CenitReturnPreparation));
        await Assert.ThrowsAsync<CenitReturnPreparationConflictException>(() => Service(db).PrepareAsync(new(OriginalId, evidence.Id)));
        Assert.Empty(await db.AchReturnsGenerated.ToListAsync());
        Assert.Empty(await db.AchColombiaReturnPreparations.ToListAsync());
        Assert.Equal("DESACTIVADO", (await db.ClearingHouseConfigs.SingleAsync(x => x.ClearingHouseId == 1)).AutomaticReturnMode);
    }

    [Fact]
    public async Task StaleExecutionAndSpecialCausesNeverBecomePreparations()
    {
        await using var db = NewContext();
        var evidence = await SeedAsync(db);
        await Enable(db);
        await Assert.ThrowsAsync<CenitReturnPreparationConflictException>(() =>
            Service(db).PrepareAsync(new(OriginalId, Guid.NewGuid())));
        evidence.ResultCode = "R06";
        (await db.AchReturnCodes.SingleAsync()).Code = "R06";
        await db.SaveChangesAsync();
        await Assert.ThrowsAsync<CenitReturnPreparationConflictException>(() =>
            Service(db).PrepareAsync(new(OriginalId, evidence.Id)));
        Assert.Empty(await db.CenitReturnPreparations.ToListAsync());
    }

    [Fact]
    public async Task LaterSuccessfulExecutionInvalidatesOldRejection()
    {
        await using var db = NewContext();
        var previous = await SeedAsync(db);
        await Enable(db);
        db.IncomingNachaIntegrationExecution.Add(new IncomingNachaIntegrationExecution
        {
            Id = Guid.NewGuid(), DispatchQueueId = previous.DispatchQueueId,
            EntryDetailId = previous.EntryDetailId, ClearingHouseId = CenitId,
            DispatchQueue = previous.DispatchQueue,
            StartedAtUtc = previous.StartedAtUtc.AddMinutes(1),
            ProcessedAtUtc = previous.ProcessedAtUtc!.Value.AddMinutes(1),
            ExecutionMode = "LIVE", SoapMethodName = "Proc_Transacciones",
            TransportStatus = IntegrationTransportStatus.Succeeded,
            BusinessStatus = IntegrationResponseBusinessStatus.Success,
            BusinessOutcome = IncomingNachaBusinessOutcome.Successful,
            ProcessingStatus = IncomingNachaIndividualProcessingStatus.Completed,
            IsSuccessful = true, ResponseCatalogId = 99, ResultCode = "00"
        });
        await db.SaveChangesAsync();
        await Assert.ThrowsAsync<CenitReturnPreparationConflictException>(() =>
            Service(db).PrepareAsync(new(OriginalId, previous.Id)));
        Assert.Empty(await db.CenitReturnPreparations.ToListAsync());
    }

    [Fact]
    public async Task WrongHouseAndWrongDateAndExpiredWindowFailClosed()
    {
        await using var db = NewContext();
        var evidence = await SeedAsync(db);
        await Enable(db);
        evidence.DispatchQueue.ClearingHouseId = 1;
        await db.SaveChangesAsync();
        await Assert.ThrowsAsync<CenitReturnPreparationConflictException>(() =>
            Service(db).PrepareAsync(new(OriginalId, evidence.Id)));
        evidence.DispatchQueue.ClearingHouseId = CenitId;
        evidence.DispatchQueue.AchTransaction!.EffectiveEntryDate = new DateTime(2026,11,2);
        await db.SaveChangesAsync();
        await Assert.ThrowsAsync<CenitReturnPreparationConflictException>(() =>
            Service(db).PrepareAsync(new(OriginalId, evidence.Id)));
        evidence.DispatchQueue.AchTransaction.EffectiveEntryDate = new DateTime(2026,11,3);
        await db.SaveChangesAsync();
        await Assert.ThrowsAsync<CenitReturnPreparationConflictException>(() =>
            Service(db, new DateTimeOffset(2026,11,3,18,0,0,TimeSpan.Zero))
                .PrepareAsync(new(OriginalId, evidence.Id)));
        Assert.Empty(await db.CenitReturnPreparations.ToListAsync());
    }

    [Fact]
    public async Task MissingOrClosedTargetCycleHolidayAndGeneratedReturnAreBlocked()
    {
        await using var db = NewContext();
        var evidence = await SeedAsync(db);
        await Enable(db);
        var target = await db.AchCycles.SingleAsync(x => x.Id == "CENIT-I03-2");
        target.OperationalStatus = AchCycleOperationalStatus.Closed;
        await db.SaveChangesAsync();
        await Assert.ThrowsAsync<CenitReturnPreparationConflictException>(() =>
            Service(db).PrepareAsync(new(OriginalId, evidence.Id)));
        target.OperationalStatus = AchCycleOperationalStatus.Open;
        db.BankHolidays.Add(new BankHolidayModel { Date = new DateOnly(2026,11,3), CountryCode = "CO", Description = "Prueba" });
        await db.SaveChangesAsync();
        await Assert.ThrowsAsync<CenitReturnPreparationConflictException>(() =>
            Service(db).PrepareAsync(new(OriginalId, evidence.Id)));
        db.BankHolidays.Remove(await db.BankHolidays.SingleAsync());
        db.AchReturnsGenerated.Add(new AchReturnGenerated
        {
            OriginalTransactionId = OriginalId, ReturnCycleId = target.Id, ReturnCycle = target,
            ReturnReasonCode = "R02", Amount = 150m
        });
        await db.SaveChangesAsync();
        await Assert.ThrowsAsync<CenitReturnPreparationConflictException>(() =>
            Service(db).PrepareAsync(new(OriginalId, evidence.Id)));
        Assert.Empty(await db.CenitReturnPreparations.ToListAsync());
    }

    [Fact]
    public async Task ControllerEnforcesPermissionsAndOnlyExposesPreparationAndList()
    {
        await using var db = NewContext();
        var evidence = await SeedAsync(db);
        var controller = new CenitReturnPreparationController(Service(db));
        Assert.IsType<BadRequestObjectResult>(await controller.Get(101));
        Assert.IsType<BadRequestObjectResult>(await controller.Prepare(new(0, Guid.Empty), default));
        Assert.IsType<ConflictObjectResult>(await controller.Prepare(new(OriginalId, evidence.Id), default));
        var cls = typeof(CenitReturnPreparationController);
        Assert.NotNull(Attribute.GetCustomAttribute(cls, typeof(AuthorizeAttribute)));
        Assert.Equal(P1Policies.ConfigRead, cls.GetMethod(nameof(CenitReturnPreparationController.Get))!
            .GetCustomAttributes(typeof(AuthorizeAttribute), false).Cast<AuthorizeAttribute>().Single().Policy);
        Assert.Equal(P1Policies.ConfigManage, cls.GetMethod(nameof(CenitReturnPreparationController.Prepare))!
            .GetCustomAttributes(typeof(AuthorizeAttribute), false).Cast<AuthorizeAttribute>().Single().Policy);
        Assert.DoesNotContain(cls.GetMethods(), m => m.GetCustomAttributes(typeof(HttpPutAttribute), false).Any()
            || m.GetCustomAttributes(typeof(HttpDeleteAttribute), false).Any());
    }

    private static CenitReturnPreparationService Service(AchDbContext db, DateTimeOffset? now = null) =>
        new(db, new FixedClock(now ?? OperationNow));
    private sealed class FixedClock(DateTimeOffset now) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => now;
    }
    private static AchDbContext NewContext() => new(new DbContextOptionsBuilder<AchDbContext>()
        .UseInMemoryDatabase($"cenit-i03-{Guid.NewGuid():N}").Options);
    private static async Task Enable(AchDbContext db)
    {
        (await db.ClearingHouseConfigs.SingleAsync(x => x.ClearingHouseId == CenitId))
            .AutomaticReturnMode = "CON_APROBACION";
        await db.SaveChangesAsync();
    }

    internal static async Task<IncomingNachaIntegrationExecution> SeedAsync(AchDbContext db)
    {
        var ingestionId = Guid.NewGuid();
        var linkId = Guid.NewGuid();
        var queueId = Guid.NewGuid();
        var now = new DateTime(2026, 11, 3, 15, 0, 0, DateTimeKind.Utc);
        db.ClearingHouseConfigs.AddRange(
            new ClearingHouseConfig { Id = 101, ClearingHouseId = 1, TimeZoneId = "America/Bogota" },
            new ClearingHouseConfig { Id = 102, ClearingHouseId = CenitId, TimeZoneId = "America/Bogota" });
        var achHouse = new ClearingHouse { Id = 1, ClearingHouseId = 101, Code = "ACHCOL", Name = "ACH Colombia", OriginCode = "ACH" };
        var cenitHouse = new ClearingHouse { Id = CenitId, ClearingHouseId = 102, Code = "CENIT", Name = "CENIT", OriginCode = "CEN" };
        db.ClearingHouses.AddRange(achHouse, cenitHouse);
        var cycle = new AchCycle { Id = "CENIT-I02-1", ClearingHouseId = CenitId,
            CycleName = "Ciclo 1 CENIT", ProcessingDate = now.Date, ClearingHouse = cenitHouse,
            StartTime = new TimeSpan(7,30,0), EndTime = new TimeSpan(10,30,0), CutoffTime = new TimeSpan(10,30,0),
            OperationalStatus = AchCycleOperationalStatus.Closed };
        var transaction = new AchTransaction { Id = OriginalId, Reference = "I02-CENIT-TEST",
            TransactionExternalId = "I02-CENIT-TEST", Amount = 150m,
            Type = TransactionTypeEnum.Credit, AchCycleId = cycle.Id, AchCycle = cycle,
            EffectiveEntryDate = now.Date,
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
        var policy = new ClearingHouseCycleConfig
        {
            ClearingHouseId = CenitId, CycleName = "Ciclo 2 CENIT", PolicyVersion = "REGULATORY-2025",
            EffectiveFrom = new DateTime(2025,1,1), IsActive = true, AllowsReturn = true
        };
        db.ClearingHouseCycleConfigs.Add(policy);
        db.AchCycles.Add(new AchCycle
        {
            Id = "CENIT-I03-2", ClearingHouseId = CenitId, ClearingHouse = cenitHouse,
            CycleName = "Ciclo 2 CENIT", ProcessingDate = now.Date,
            StartTime = new TimeSpan(11,0,0), EndTime = new TimeSpan(13,0,0),
            CutoffTime = new TimeSpan(13,0,0), OperationalStatus = AchCycleOperationalStatus.Open,
            ClearingHouseCycleConfig = policy
        });
        await db.SaveChangesAsync();
        return execution;
    }
}
