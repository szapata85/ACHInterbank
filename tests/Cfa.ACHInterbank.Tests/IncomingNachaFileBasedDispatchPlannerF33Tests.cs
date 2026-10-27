using Cfa.ACHInterbank.Domain.Models.ACH;
using Cfa.ACHInterbank.Persistence.ACH.Services.Implementation;
using Cfa.ACHInterbank.Persistence.DataBase;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Xunit;

namespace Cfa.ACHInterbank.Tests;

/// <summary>F3.3 planning tests; none authorizes transmission to CFA.</summary>
public class IncomingNachaFileBasedDispatchPlannerF33Tests
{
    [Theory]
    [InlineData(8, 8, "H")]
    [InlineData(9, 9, "I")]
    [InlineData(8, 10, "H")]
    public async Task FileBasedEntry_NoCycleOrLinkedTransaction_CreatesBlockedWork(int fileSequence, int externalCycle, string modifier)
    {
        await using var context = NewContext();
        var ingestion = Seed(context, fileSequence, externalCycle, modifier);
        var created = await NewPlanner(context, enabled: true).PlanForIngestionAsync(ingestion.Id, "tester");

        Assert.Equal(1, created);
        var item = await context.IncomingNachaDispatchQueue.SingleAsync();
        Assert.Equal(IncomingNachaDispatchSourceKinds.AchColFileBased, item.SourceKind);
        Assert.Null(item.AchTransactionId);
        Assert.Null(item.AchCycleId);
        Assert.Null(item.IncomingNachaTransactionLinkId);
        Assert.NotNull(item.EntryDetailId);
        Assert.Equal(IncomingNachaDispatchQueueStatus.Blocked, item.QueueStatus);
        Assert.Equal("ACHCOL_DISPATCH_NOT_ENABLED_F33", item.LastErrorCode);
        Assert.Null(item.NextAttemptAtUtc);
        Assert.Equal(64, item.IdempotencyDispatchKey.Length);
        Assert.Equal(0, await NewPlanner(context, enabled: true).PlanForIngestionAsync(ingestion.Id, "tester"));
    }

    [Fact]
    public async Task FeatureFlagOff_DoesNotCreateFileBasedWork()
    {
        await using var context = NewContext();
        var ingestion = Seed(context);
        Assert.Equal(0, await NewPlanner(context, enabled: false).PlanForIngestionAsync(ingestion.Id, "tester"));
        Assert.Empty(context.IncomingNachaDispatchQueue);
    }

    [Fact]
    public async Task RepeatedBusinessEntry_FromDifferentFileCycleOrEnvelope_UsesSameKey()
    {
        await using var context = NewContext();
        var first = Seed(context, 8, 8, "H");
        Assert.Equal(1, await NewPlanner(context, enabled: true).PlanForIngestionAsync(first.Id, "tester"));
        var key = (await context.IncomingNachaDispatchQueue.SingleAsync()).IdempotencyDispatchKey;
        var second = Seed(context, 8, 10, "H", clearText: false);
        Assert.Equal(0, await NewPlanner(context, enabled: true).PlanForIngestionAsync(second.Id, "tester"));
        Assert.Equal(key, (await context.IncomingNachaDispatchQueue.SingleAsync()).IdempotencyDispatchKey);
    }

    [Theory]
    [InlineData("", "22", 100, true)]
    [InlineData("123", "22", 100, true)]
    [InlineData("123456789012345", "22", 0, true)]
    [InlineData("123456789012345", "22", 100, false)]
    public async Task InvalidOrIneligibleEntry_IsNeverPlanned(string trace, string code, int amount, bool eligible)
    {
        await using var context = NewContext();
        var ingestion = Seed(context);
        var entry = await context.EntryDetails.SingleAsync();
        entry.SequenceNumber = trace;
        entry.TransactionCode = code;
        entry.Amount = amount;
        var cls = await context.IncomingNachaEntryClassifications.SingleAsync();
        cls.EligibilityStatus = eligible ? IncomingNachaEligibilityStatus.Elegible : IncomingNachaEligibilityStatus.Bloqueada;
        await context.SaveChangesAsync();

        Assert.Equal(0, await NewPlanner(context, enabled: true).PlanForIngestionAsync(ingestion.Id, "tester"));
        Assert.Empty(context.IncomingNachaDispatchQueue);
    }

    [Fact]
    public async Task PrenotificationOrReturn_IsNotPlanned()
    {
        await using var context = NewContext();
        var ingestion = Seed(context);
        var cls = await context.IncomingNachaEntryClassifications.SingleAsync();
        cls.FunctionalClass = IncomingNachaFunctionalClass.Prenotificacion;
        await context.SaveChangesAsync();
        Assert.Equal(0, await NewPlanner(context, enabled: true).PlanForIngestionAsync(ingestion.Id, "tester"));
        Assert.Empty(context.IncomingNachaDispatchQueue);
    }

    [Theory]
    [InlineData("23", IncomingNachaFunctionalClass.CreditoEntrante)]
    [InlineData("28", IncomingNachaFunctionalClass.DebitoEntrante)]
    [InlineData("27", IncomingNachaFunctionalClass.CreditoEntrante)]
    public async Task F41_FailClosedEvenIfClassifierLabelsWrongCode(
        string transactionCode, IncomingNachaFunctionalClass claimedClass)
    {
        await using var context = NewContext();
        var ingestion = Seed(context);
        (await context.EntryDetails.SingleAsync()).TransactionCode = transactionCode;
        (await context.IncomingNachaEntryClassifications.SingleAsync()).FunctionalClass = claimedClass;
        await context.SaveChangesAsync();
        Assert.Equal(0, await NewPlanner(context, enabled: true).PlanForIngestionAsync(ingestion.Id, "tester"));
        Assert.Empty(context.IncomingNachaDispatchQueue);
    }

    [Fact]
    public async Task NotAchColClearingHouse_IsNotPlanned()
    {
        await using var context = NewContext();
        var ingestion = Seed(context);
        (await context.ClearingHouses.SingleAsync()).Code = "CENIT";
        await context.SaveChangesAsync();
        Assert.Equal(0, await NewPlanner(context, enabled: true).PlanForIngestionAsync(ingestion.Id, "tester"));
    }

    private static IncomingNachaDispatchPlanner NewPlanner(AchDbContext context, bool enabled) => new(
        context, new IncomingNachaDispatchEligibilityPolicy(),
        configuration: new ConfigurationBuilder().AddInMemoryCollection(
            new Dictionary<string, string?> { ["AchCol:AllowCycleIndependentInboundDispatch"] = enabled.ToString() }).Build());

    private static AchDbContext NewContext() => new(
        new DbContextOptionsBuilder<AchDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString("N")).Options);

    private static IncomingNachaFileIngestion Seed(
        AchDbContext context, int sequence = 8, int cycle = 8, string modifier = "H", bool clearText = true)
    {
        if (!context.ClearingHouses.Any())
        {
            context.ClearingHouses.Add(new ClearingHouse
            {
                Id = 1, ClearingHouseId = 1, Name = "ACH Colombia", Code = "ACHCOL", OriginCode = "0001283"
            });
        }

        var ingestion = new IncomingNachaFileIngestion
        {
            Id = Guid.NewGuid(),
            FileName = $"0001283.{sequence:000}.20260731.{cycle}.OUT" + (clearText ? "" : ".env"),
            CanonicalPlainFileName = $"0001283.{sequence:000}.20260731.{cycle}.OUT",
            FileIdModifier = modifier,
            FileProtectionKind = clearText ? "PlainNacha" : "DigitalEnvelope",
            FileSequence = sequence,
            ExternalCycleNumber = cycle,
            FileHashSha256 = Guid.NewGuid().ToString("N"),
            FileSize = 1,
            ContentType = "text/plain",
            UploadedBy = "tester", CorrelationId = Guid.NewGuid().ToString("N"),
            Notes = "F3.3 unit fixture", IngestionStatus = IncomingNachaIngestionStatus.Completado,
            CycleResolutionStatus = IncomingNachaCycleResolutionStatus.ResueltoSinCiclo,
            ResolutionMode = "AchColFileBased", ResolvedClearingHouseId = 1,
            OperationalDate = new DateTime(2026, 7, 31, 0, 0, 0, DateTimeKind.Utc)
        };
        var header = new NachaHeader
        {
            NachaID = Guid.NewGuid().ToString("N"),
            ImmediateOrigin = "0001283",
            IncomingNachaFileIngestionId = ingestion.Id,
            FileIdModifier = modifier
        };
        var entry = new EntryDetail
        {
            EntryDetailID = context.EntryDetails.Count() + 101,
            NachaID = header.NachaID,
            TransactionCode = "22", SequenceNumber = "123456789012345",
            BatchNumber = 1, Amount = 100m,
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
        context.AddRange(ingestion, header, entry, cls);
        context.SaveChanges();
        return ingestion;
    }
}
