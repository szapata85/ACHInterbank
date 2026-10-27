using Cfa.ACHInterbank.Application.ACH.Interfaces;
using Cfa.ACHInterbank.Application.External.Connections;
using Cfa.ACHInterbank.Domain.Models.ACH;
using Cfa.ACHInterbank.Persistence.ACH.Services.Implementation;
using Cfa.ACHInterbank.Persistence.DataBase;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using Cfa.ACHInterbank.Domain.Models.Configurations;
using Moq;
using Xunit;

namespace Cfa.ACHInterbank.Tests;

/// <summary>
/// F4.2 covers worker preflight and its mandatory no-SOAP boundary. InMemory is
/// sufficient here; relational constraints/concurrency remain pending for F6.
/// </summary>
public sealed class IncomingNachaFileBasedOrchestrationF42Tests
{
    [Theory]
    [InlineData(8, 8, "H", true)]
    [InlineData(9, 9, "I", false)]
    [InlineData(8, 10, "H", false)]
    public async Task ReadyEntry_PrevalidatedWithoutCycleOrTransport(
        int sequence, int cycle, string modifier, bool clearText)
    {
        await using var context = NewContext();
        var queue = await SeedAndPlan(context, sequence, cycle, modifier, clearText);
        var soap = new Mock<IWscfaachSoapClient>(MockBehavior.Strict);
        var mapper = new Mock<IProcTransaccionesRequestMapper>(MockBehavior.Strict);
        var sut = NewOrchestrator(context, mapper, soap, featureEnabled: true);

        var result = await sut.ExecuteAsync(50, "f42-tester");
        context.ChangeTracker.Clear();
        var persisted = await context.IncomingNachaDispatchQueue.SingleAsync();

        Assert.Equal(queue.Id, persisted.Id);
        Assert.Equal(IncomingNachaDispatchSourceKinds.AchColFileBased, persisted.SourceKind);
        Assert.Null(persisted.AchCycleId);
        Assert.Null(persisted.AchTransactionId);
        Assert.Equal(IncomingNachaDispatchQueueStatus.Blocked, persisted.QueueStatus);
        Assert.Equal("ACHCOL_MAPPING_NOT_READY_F42", persisted.LastErrorCode);
        Assert.Null(persisted.NextAttemptAtUtc);
        Assert.Equal(0, persisted.AttemptCount);
        Assert.Equal(0, result.Picked);
        Assert.Contains("0 envios CFA", result.Summary);
        Assert.Single(await context.IncomingNachaProcessingEvents
            .Where(e => e.EventType == "AchColFileBasedPreflightReady").ToListAsync());
        Assert.Empty(await context.IncomingNachaIntegrationExecution.ToListAsync());
        mapper.VerifyNoOtherCalls();
        soap.VerifyNoOtherCalls();

        // Scheduler recurrence must not emit duplicate audit or unlock the row.
        await sut.ExecuteAsync(50, "f42-tester");
        Assert.Equal(1, await context.IncomingNachaProcessingEvents
            .CountAsync(e => e.EventType == "AchColFileBasedPreflightReady"));
    }

    [Fact]
    public async Task FlagOff_LeavesWorkBlockedAndUnchanged()
    {
        await using var context = NewContext();
        await SeedAndPlan(context);
        var soap = new Mock<IWscfaachSoapClient>(MockBehavior.Strict);
        var mapper = new Mock<IProcTransaccionesRequestMapper>(MockBehavior.Strict);
        await NewOrchestrator(context, mapper, soap, featureEnabled: false)
            .ExecuteAsync(100, "tester");
        var queue = await context.IncomingNachaDispatchQueue.SingleAsync();
        Assert.Equal("ACHCOL_DISPATCH_NOT_ENABLED_F33", queue.LastErrorCode);
        Assert.Equal(IncomingNachaDispatchQueueStatus.Blocked, queue.QueueStatus);
        Assert.Empty(await context.IncomingNachaProcessingEvents
            .Where(x => x.EventType.StartsWith("AchColFileBasedPreflight")).ToListAsync());
        soap.VerifyNoOtherCalls();
    }

    [Theory]
    [InlineData(true, false)]
    [InlineData(false, true)]
    public async Task ChangedEntryOrIdentity_FailsClosed_NoSoap(bool prenote, bool corruptKey)
    {
        await using var context = NewContext();
        await SeedAndPlan(context);
        if (prenote)
            (await context.EntryDetails.SingleAsync()).TransactionCode = "23";
        if (corruptKey)
            (await context.IncomingNachaDispatchQueue.SingleAsync()).IdempotencyDispatchKey = "CORRUPTED";
        await context.SaveChangesAsync();

        var soap = new Mock<IWscfaachSoapClient>(MockBehavior.Strict);
        var mapper = new Mock<IProcTransaccionesRequestMapper>(MockBehavior.Strict);
        await NewOrchestrator(context, mapper, soap, featureEnabled: true)
            .ExecuteAsync(100, "tester");
        var queue = await context.IncomingNachaDispatchQueue.SingleAsync();
        Assert.Equal("ACHCOL_PREFLIGHT_REJECTED_F42", queue.LastErrorCode);
        Assert.Equal(IncomingNachaDispatchQueueStatus.Blocked, queue.QueueStatus);
        Assert.Null(queue.NextAttemptAtUtc);
        Assert.Empty(await context.IncomingNachaIntegrationExecution.ToListAsync());
        soap.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task FileBased_PreflightInLiveMode_RemainsLocalSimulationWithoutSoap()
    {
        await using var context = NewContext();
        await SeedAndPlan(context, sequence: 8, cycle: 10, modifier: "H", clearText: false);
        var mapper = new Mock<IProcTransaccionesRequestMapper>(MockBehavior.Strict);
        var soap = new Mock<IWscfaachSoapClient>(MockBehavior.Strict);
        var sut = new IncomingNachaPostProcessingOrchestrator(
            context, mapper.Object, new ProcTransaccionesResponseParser(), soap.Object,
            dispatchOptions: Options.Create(new ProcTransaccionesDispatchOptions { Mode = "Live" }),
            configuration: NewConfiguration(true));

        var run = await sut.ExecuteAsync(20, "f43-simulacion-local");
        var item = await context.IncomingNachaDispatchQueue.SingleAsync();
        Assert.Equal(IncomingNachaDispatchQueueStatus.Blocked, item.QueueStatus);
        Assert.Equal("ACHCOL_MAPPING_NOT_READY_F42", item.LastErrorCode);
        Assert.Equal(0, item.AttemptCount);
        Assert.Null(item.NextAttemptAtUtc);
        Assert.Equal(0, run.Picked);
        Assert.Empty(await context.IncomingNachaIntegrationExecution.ToListAsync());
        mapper.VerifyNoOtherCalls();
        soap.VerifyNoOtherCalls();
    }

    private static IncomingNachaPostProcessingOrchestrator NewOrchestrator(
        AchDbContext context, Mock<IProcTransaccionesRequestMapper> mapper,
        Mock<IWscfaachSoapClient> soap, bool featureEnabled)
        => new(context, mapper.Object, new ProcTransaccionesResponseParser(), soap.Object,
            configuration: NewConfiguration(featureEnabled));

    private static IConfiguration NewConfiguration(bool enabled) => new ConfigurationBuilder()
        .AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["AchCol:AllowCycleIndependentInboundDispatch"] = enabled.ToString()
        }).Build();

    private static AchDbContext NewContext() => new(
        new DbContextOptionsBuilder<AchDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString("N")).Options);

    private static async Task<IncomingNachaDispatchQueue> SeedAndPlan(
        AchDbContext context, int sequence = 8, int cycle = 8,
        string modifier = "H", bool clearText = true)
    {
        var ingestion = new IncomingNachaFileIngestion
        {
            Id = Guid.NewGuid(),
            FileName = $"0001283.{sequence:000}.20260731.{cycle}.OUT" + (clearText ? "" : ".env"),
            CanonicalPlainFileName = $"0001283.{sequence:000}.20260731.{cycle}.OUT",
            FileIdModifier = modifier,
            FileProtectionKind = clearText ? "PlainNacha" : "DigitalEnvelope",
            FileSequence = sequence, ExternalCycleNumber = cycle,
            FileHashSha256 = Guid.NewGuid().ToString("N"), FileSize = 1,
            ContentType = "text/plain", UploadedBy = "tester",
            CorrelationId = Guid.NewGuid().ToString("N"),
            IngestionStatus = IncomingNachaIngestionStatus.Completado,
            CycleResolutionStatus = IncomingNachaCycleResolutionStatus.ResueltoSinCiclo,
            ResolutionMode = "AchColFileBased", ResolvedClearingHouseId = 1,
            OperationalDate = new DateTime(2026, 7, 31, 0, 0, 0, DateTimeKind.Utc)
        };
        var header = new NachaHeader
        {
            NachaID = Guid.NewGuid().ToString("N"), ImmediateOrigin = "0001283",
            IncomingNachaFileIngestionId = ingestion.Id, FileIdModifier = modifier
        };
        var entry = new EntryDetail
        {
            EntryDetailID = 101, NachaID = header.NachaID, TransactionCode = "22",
            SequenceNumber = "123456789012345", BatchNumber = 1, Amount = 100m,
            ReceivingParticipantEntityCode = "22222222", AccountNumber = "ACCOUNT-1",
            RecipIdNumber = "900123456"
        };
        var cls = new IncomingNachaEntryClassification
        {
            Id = Guid.NewGuid(), IncomingNachaFileIngestionId = ingestion.Id,
            EntryDetailId = entry.EntryDetailID,
            FunctionalClass = IncomingNachaFunctionalClass.CreditoEntrante,
            EligibilityStatus = IncomingNachaEligibilityStatus.Elegible
        };
        context.AddRange(new ClearingHouse
        {
            Id = 1, ClearingHouseId = 1, Name = "ACH Colombia", Code = "ACHCOL",
            OriginCode = "0001283"
        }, ingestion, header, entry, cls);
        await context.SaveChangesAsync();
        var planner = new IncomingNachaDispatchPlanner(context,
            new IncomingNachaDispatchEligibilityPolicy(), configuration: NewConfiguration(true));
        Assert.Equal(1, await planner.PlanForIngestionAsync(ingestion.Id, "tester"));
        return await context.IncomingNachaDispatchQueue.SingleAsync();
    }
}
