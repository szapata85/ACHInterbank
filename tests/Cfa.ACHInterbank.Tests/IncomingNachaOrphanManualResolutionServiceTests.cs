using System.Text.Json;
using Cfa.ACHInterbank.Application.ACH.Models;
using Cfa.ACHInterbank.Domain.Entities.Transactions.Enums;
using Cfa.ACHInterbank.Domain.Models.ACH;
using Cfa.ACHInterbank.Persistence.ACH.Services.Implementation;
using Cfa.ACHInterbank.Persistence.DataBase;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Cfa.ACHInterbank.Tests;

public class IncomingNachaOrphanManualResolutionServiceTests
{
    [Fact]
    public async Task ResolveAsync_ShouldCreateManualResolutionEvent_ForNotFoundOrphan()
    {
        using var context = Ctx();
        var link = SeedOrphan(context, IncomingNachaLinkType.NotFound, "NotFound", [10, 11]);
        var sut = new IncomingNachaOrphanManualResolutionService(context);

        var result = await sut.ResolveAsync(new IncomingNachaOrphanManualResolutionRequest
        {
            IncomingNachaTransactionLinkId = link.Id,
            ResolutionAction = IncomingNachaOrphanResolutionAction.MarkAsIgnored,
            ResolutionReason = "Validado manualmente",
            Comment = "Cierre operativo",
            ResolvedBy = "user@cfa.com"
        });

        Assert.True(result.IsResolved);
        var ev = await context.IncomingNachaProcessingEvents.SingleAsync(x => x.EventType == "OrphanManualResolution");
        using var doc = JsonDocument.Parse(ev.EvidenceJson);
        Assert.Equal("IncomingReturnManualResolved", doc.RootElement.GetProperty("eventType").GetString());
        Assert.Equal("MarkAsIgnored", doc.RootElement.GetProperty("resolutionAction").GetString());
        Assert.Equal("user@cfa.com", doc.RootElement.GetProperty("resolvedBy").GetString());
        Assert.False(doc.RootElement.GetProperty("stateChanged").GetBoolean());
        Assert.False(doc.RootElement.GetProperty("applied").GetBoolean());
        Assert.Empty(await context.AchTransactionStateEvents.ToListAsync());
    }

    [Fact]
    public async Task ResolveAsync_ShouldCreateManualResolutionEvent_ForAmbiguousOrphan_WithCandidatesPreserved()
    {
        using var context = Ctx();
        var link = SeedOrphan(context, IncomingNachaLinkType.Ambiguous, "Ambiguous", [10, 11]);
        var sut = new IncomingNachaOrphanManualResolutionService(context);

        var result = await sut.ResolveAsync(new IncomingNachaOrphanManualResolutionRequest
        {
            IncomingNachaTransactionLinkId = link.Id,
            ResolutionAction = IncomingNachaOrphanResolutionAction.MarkAsIgnored,
            ResolvedBy = "qa"
        });

        Assert.True(result.IsResolved);
        var ev = await context.IncomingNachaProcessingEvents.SingleAsync(x => x.EventType == "OrphanManualResolution");
        using var doc = JsonDocument.Parse(ev.EvidenceJson);
        Assert.Equal("Ambiguous", doc.RootElement.GetProperty("previousResolutionReason").GetString());
        Assert.Equal(10, doc.RootElement.GetProperty("candidateTransactionIds")[0].GetInt32());
        Assert.Equal(11, doc.RootElement.GetProperty("candidateTransactionIds")[1].GetInt32());
        Assert.False(doc.RootElement.GetProperty("manualReviewRequired").GetBoolean());
    }

    [Fact]
    public async Task ResolveAsync_ShouldReject_WhenLinkDoesNotExist()
    {
        using var context = Ctx();
        var sut = new IncomingNachaOrphanManualResolutionService(context);

        var result = await sut.ResolveAsync(new IncomingNachaOrphanManualResolutionRequest
        {
            IncomingNachaTransactionLinkId = Guid.NewGuid(),
            ResolutionAction = IncomingNachaOrphanResolutionAction.MarkAsIgnored,
            ResolvedBy = "qa"
        });

        Assert.False(result.IsResolved);
        Assert.Equal("NotFound", result.Status);
        Assert.Empty(await context.IncomingNachaProcessingEvents.ToListAsync());
    }

    [Fact]
    public async Task ResolveAsync_ShouldNotDuplicateManualResolution_WhenCalledTwice()
    {
        using var context = Ctx();
        var link = SeedOrphan(context, IncomingNachaLinkType.NotFound, "NotFound", []);
        var sut = new IncomingNachaOrphanManualResolutionService(context);

        var first = await sut.ResolveAsync(new IncomingNachaOrphanManualResolutionRequest { IncomingNachaTransactionLinkId = link.Id, ResolutionAction = IncomingNachaOrphanResolutionAction.MarkAsIgnored, ResolvedBy = "qa" });
        var second = await sut.ResolveAsync(new IncomingNachaOrphanManualResolutionRequest { IncomingNachaTransactionLinkId = link.Id, ResolutionAction = IncomingNachaOrphanResolutionAction.MarkAsIgnored, ResolvedBy = "qa" });

        Assert.True(first.IsResolved);
        Assert.Equal("AlreadyResolved", second.Status);
        Assert.Equal(1, await context.IncomingNachaProcessingEvents.CountAsync(x => x.EventType == "OrphanManualResolution"));
    }

    [Fact]
    public async Task ResolveAsync_ShouldNotChangeAchTransactionState_ForAuditOnlyResolution()
    {
        using var context = Ctx();
        var tx = new Cfa.ACHInterbank.Domain.Models.ACH.AchTransaction { Id = 99, State = AchTransferStateEnum.Pending, Type = TransactionTypeEnum.Credit, AchCycleId = "C1", EffectiveEntryDate = DateTime.UtcNow, TransactionCode = "22", ReceivingDFI = "1", OriginatingDFI = "1", Amount = 1, Reference = "r", SourceAccountNumber = "1", DestinationAccountNumber = "2" };
        context.AchTransactions.Add(tx);
        var link = SeedOrphan(context, IncomingNachaLinkType.NotFound, "NotFound", []);
        var sut = new IncomingNachaOrphanManualResolutionService(context);

        var result = await sut.ResolveAsync(new IncomingNachaOrphanManualResolutionRequest { IncomingNachaTransactionLinkId = link.Id, ResolutionAction = IncomingNachaOrphanResolutionAction.MarkAsIgnored, ResolvedBy = "qa", ResolvedAchTransactionId = tx.Id });

        Assert.True(result.IsResolved);
        Assert.Equal(AchTransferStateEnum.Pending, (await context.AchTransactions.SingleAsync(x => x.Id == tx.Id)).State);
        Assert.Empty(await context.AchTransactionStateEvents.ToListAsync());
    }

    [Fact]
    public async Task ResolveAsync_ShouldRequireResolvedBy()
    {
        using var context = Ctx();
        var link = SeedOrphan(context, IncomingNachaLinkType.NotFound, "NotFound", []);
        var sut = new IncomingNachaOrphanManualResolutionService(context);

        var result = await sut.ResolveAsync(new IncomingNachaOrphanManualResolutionRequest { IncomingNachaTransactionLinkId = link.Id, ResolutionAction = IncomingNachaOrphanResolutionAction.MarkAsIgnored, ResolvedBy = " " });

        Assert.False(result.IsResolved);
        Assert.Equal("Invalid", result.Status);
        Assert.Empty(await context.IncomingNachaProcessingEvents.ToListAsync());
    }

    static AchDbContext Ctx() => new(new DbContextOptionsBuilder<AchDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);

    static IncomingNachaTransactionLink SeedOrphan(AchDbContext context, IncomingNachaLinkType linkType, string reason, int[] candidates)
    {
        var ingestionId = Guid.NewGuid();
        context.IncomingNachaFileIngestions.Add(new IncomingNachaFileIngestion { Id = ingestionId, FileName = "in.ach", FileHashSha256 = "abc", FileSize = 106, ContentType = "text/plain", UploadedBy = "u", CorrelationId = "c", Notes = "n", ResolvedClearingHouseId = 7001, ResolvedAchCycleId = "C1" });
        var evidence = JsonSerializer.Serialize(new { eventType = "IncomingReturnUnresolved", resolutionReason = reason, returnReasonCode = "R01", originalTraceNumber = "123456789012345", candidateTransactionIds = candidates });
        var link = new IncomingNachaTransactionLink { IncomingNachaFileIngestionId = ingestionId, EntryDetailId = 10, AddendaRecordId = 20, LinkType = linkType, EvidenceJson = evidence, IsFinal = false, LinkedBy = "system" };
        context.IncomingNachaTransactionLinks.Add(link);
        context.IncomingNachaEntryClassifications.Add(new IncomingNachaEntryClassification { IncomingNachaFileIngestionId = ingestionId, EntryDetailId = 10, AddendaRecordId = 20, FunctionalClass = IncomingNachaFunctionalClass.Devolucion, EligibilityStatus = IncomingNachaEligibilityStatus.PendienteResolucion, RequiresManualResolution = true, BusinessMeaning = "dev", ClassifierVersion = "v" });
        context.SaveChanges();
        return link;
    }
}
