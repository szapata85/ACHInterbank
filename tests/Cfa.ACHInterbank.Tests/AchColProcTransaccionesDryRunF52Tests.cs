using Cfa.ACHInterbank.Domain.Entities.Integrations;
using Cfa.ACHInterbank.Domain.Models.ACH;
using Cfa.ACHInterbank.Persistence.ACH.Services.Implementation;
using Cfa.ACHInterbank.Persistence.DataBase;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Cfa.ACHInterbank.Tests;

/// <summary>F5.2: projection only; no SOAP, no Cycle, no database mutation.</summary>
public sealed class AchColProcTransaccionesDryRunF52Tests
{
    [Theory]
    [InlineData(8, 8, "H", false)]
    [InlineData(9, 9, "I", true)]
    [InlineData(8, 10, "H", true)]
    public async Task Preview_UsesEntryWithoutCycle_AndNeverAuthorizesTransport(
        int sequence, int externalCycle, string modifier, bool encrypted)
    {
        await using var db = NewContext();
        var (queue, ingestion, classification) = await SeedAsync(db, sequence, externalCycle, modifier, encrypted);
        var sut = new ProcTransaccionesRequestMapper(db);
        var before = await db.IncomingNachaDispatchQueue.SingleAsync();

        var preview = await sut.PreviewAchColFileBasedAsync(queue, ingestion, classification);

        Assert.False(preview.CanTransmit);
        Assert.Equal("ACHCOL_SOAP_CONTRACT_NOT_HOMOLOGATED_F52", preview.BlockingCode);
        Assert.Equal(queue.Id, preview.DispatchQueueId);
        Assert.Equal("22", preview.ObservedCandidateParameters["TIPTRAN"]);
        Assert.Equal("1500", preview.ObservedCandidateParameters["MONTO"]);
        Assert.Equal("ACCOUNT-1", preview.ObservedCandidateParameters["NCTARECEP"]);
        Assert.Contains("IDTRAN", preview.PendingHomologationParameters);
        Assert.Contains("IDCAMCOMPE", preview.MissingRequiredParameters);
        Assert.Contains("<tem:TIPTRAN>22</tem:TIPTRAN>", preview.SoapPreviewXml);
        Assert.DoesNotContain("<tem:IDCAMCOMPE>", preview.SoapPreviewXml);
        Assert.DoesNotContain("<tem:IDLOTE>", preview.SoapPreviewXml);
        Assert.DoesNotContain($"<tem:IDCAMCOMPE>{externalCycle}</tem:IDCAMCOMPE>", preview.SoapPreviewXml);
        Assert.Null(before.AchCycleId);
        Assert.Equal(IncomingNachaDispatchQueueStatus.Blocked, before.QueueStatus);
        Assert.Equal(0, before.AttemptCount);
        Assert.Empty(db.ChangeTracker.Entries().Where(x => x.State == EntityState.Modified));
        Assert.Empty(await db.IncomingNachaIntegrationExecution.ToListAsync());
    }

    [Fact]
    public async Task Preview_RejectsLegacyOrPrematurelyUnblockedQueue()
    {
        await using var db = NewContext();
        var (queue, ingestion, classification) = await SeedAsync(db);
        var sut = new ProcTransaccionesRequestMapper(db);
        queue.SourceKind = IncomingNachaDispatchSourceKinds.CycleBound;
        await Assert.ThrowsAsync<InvalidOperationException>(() => sut.PreviewAchColFileBasedAsync(queue, ingestion, classification));
        queue.SourceKind = IncomingNachaDispatchSourceKinds.AchColFileBased;
        queue.QueueStatus = IncomingNachaDispatchQueueStatus.Queued;
        await Assert.ThrowsAsync<InvalidOperationException>(() => sut.PreviewAchColFileBasedAsync(queue, ingestion, classification));
    }

    [Fact]
    public async Task Preview_RejectsAlteredBusinessIdentityAndPrenotification()
    {
        await using var db = NewContext();
        var (queue, ingestion, classification) = await SeedAsync(db);
        var sut = new ProcTransaccionesRequestMapper(db);
        queue.IdempotencyDispatchKey = "ALTERED";
        await Assert.ThrowsAsync<InvalidOperationException>(() => sut.PreviewAchColFileBasedAsync(queue, ingestion, classification));
        queue.IdempotencyDispatchKey = await IdentityForAsync(db, queue, ingestion);
        (await db.EntryDetails.SingleAsync()).TransactionCode = "23";
        await Assert.ThrowsAsync<InvalidOperationException>(() => sut.PreviewAchColFileBasedAsync(queue, ingestion, classification));
    }

    [Fact]
    public async Task Preview_RejectsMissingPublishedMapping()
    {
        await using var db = NewContext();
        var (queue, ingestion, classification) = await SeedAsync(db);
        db.IntegrationMappingSets.RemoveRange(db.IntegrationMappingSets);
        await db.SaveChangesAsync();
        var error = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            new ProcTransaccionesRequestMapper(db).PreviewAchColFileBasedAsync(queue, ingestion, classification));
        Assert.Contains("ACHCOL_F52_DRYRUN_MAPPING_INVALID", error.Message);
    }

    private static AchDbContext NewContext() => new(
        new DbContextOptionsBuilder<AchDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString("N")).Options);

    private static async Task<string> IdentityForAsync(AchDbContext db, IncomingNachaDispatchQueue q, IncomingNachaFileIngestion ingestion)
    {
        var entry = await db.EntryDetails.SingleAsync();
        var header = await db.NachaHeaders.SingleAsync();
        Assert.True(IncomingNachaFileBasedDispatchIdentity.TryBuild(q.ClearingHouseId, ingestion.OperationalDate!.Value, header, entry, out var key));
        return key;
    }

    private static async Task<(IncomingNachaDispatchQueue, IncomingNachaFileIngestion, IncomingNachaEntryClassification)> SeedAsync(
        AchDbContext db, int sequence = 8, int cycle = 8, string modifier = "H", bool encrypted = false)
    {
        var date = new DateTime(2026, 7, 31, 0, 0, 0, DateTimeKind.Utc);
        var plain = $"0001283.{sequence:000}.20260731.{cycle}.OUT";
        var ingestion = new IncomingNachaFileIngestion
        {
            FileName = plain + (encrypted ? ".env" : ""),
            CanonicalPlainFileName = plain,
            FileSequence = sequence, ExternalCycleNumber = cycle,
            FileIdModifier = modifier,
            FileProtectionKind = encrypted ? "DigitalEnvelope" : "PlainNacha",
            FileNameDate = date, OperationalDate = date,
            IngestionStatus = IncomingNachaIngestionStatus.Completado,
            ResolutionMode = IncomingNachaDispatchSourceKinds.AchColFileBased,
            ResolvedClearingHouseId = 1,
            UploadedBy = "test", ContentType = "text/plain"
        };
        var header = new NachaHeader
        {
            NachaID = Guid.NewGuid().ToString("N"), ImmediateOrigin = "0001283",
            FileIdModifier = modifier, IncomingNachaFileIngestionId = ingestion.Id
        };
        var entry = new EntryDetail
        {
            EntryDetailID = 101, NachaID = header.NachaID,
            TransactionCode = "22", BatchNumber = 1,
            SequenceNumber = "123456789012345", ReceivingParticipantEntityCode = "22222222",
            AccountNumber = "ACCOUNT-1", Amount = 1500m,
            RecipIdNumber = "900123456", RecipUserName = "EXAMPLE"
        };
        var classification = new IncomingNachaEntryClassification
        {
            IncomingNachaFileIngestionId = ingestion.Id,
            EntryDetailId = entry.EntryDetailID,
            FunctionalClass = IncomingNachaFunctionalClass.CreditoEntrante,
            EligibilityStatus = IncomingNachaEligibilityStatus.Elegible
        };
        var queue = new IncomingNachaDispatchQueue
        {
            IncomingNachaFileIngestionId = ingestion.Id,
            IncomingNachaEntryClassificationId = classification.Id,
            EntryDetailId = entry.EntryDetailID, SourceKind = IncomingNachaDispatchSourceKinds.AchColFileBased,
            ClearingHouseId = 1, OperationalDate = date,
            QueueStatus = IncomingNachaDispatchQueueStatus.Blocked,
            LastErrorCode = "ACHCOL_MAPPING_NOT_READY_F42",
            IdempotencyDispatchKey = "TEMP"
        };
        var method = new IntegrationMethod { Id = 701, Code = "WSCFAACH.Proc_Transacciones", IsActive = true };
        var mapping = new IntegrationMappingSet
        {
            MethodId = method.Id, Name = "F52-DryRun", Version = 1,
            Status = IntegrationMappingSetStatusEnum.Published, IsActive = true
        };
        db.AddRange(new ClearingHouse { Id = 1, ClearingHouseId = 1, Code = "ACHCOL", Name = "ACH Colombia", IsActive = true },
            ingestion, header, entry, classification, queue,
            new BatchHeader { BatchID = 42, NachaID = header.NachaID, BatchNumber = 1, CompanyName = "DEMO" },
            method, mapping,
            new IntegrationMethodParameter { Id = 711, MethodId = method.Id, ParameterPath = "TIPTRAN", Required = true, Direction = IntegrationParameterDirectionEnum.Input },
            new IntegrationMethodParameter { Id = 712, MethodId = method.Id, ParameterPath = "MONTO", Required = true, Direction = IntegrationParameterDirectionEnum.Input },
            new IntegrationMethodParameter { Id = 713, MethodId = method.Id, ParameterPath = "IDCAMCOMPE", Required = true, Direction = IntegrationParameterDirectionEnum.Input });
        await db.SaveChangesAsync();
        queue.IdempotencyDispatchKey = await IdentityForAsync(db, queue, ingestion);
        await db.SaveChangesAsync();
        return (queue, ingestion, classification);
    }
}
