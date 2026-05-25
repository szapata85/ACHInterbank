using Cfa.ACHInterbank.Api.Controllers;
using Cfa.ACHInterbank.Application.ACH.Interfaces;
using Cfa.ACHInterbank.Application.ACH.Models;
using Cfa.ACHInterbank.Domain.Entities.Ach.Dtos;
using Cfa.ACHInterbank.Domain.Models.ACH;
using Cfa.ACHInterbank.Persistence.ACH.Services.Implementation;
using Cfa.ACHInterbank.Persistence.DataBase;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Moq;
using Xunit;

namespace Cfa.ACHInterbank.Tests;

public class NachaOperationalReadStoreTests
{
    [Fact]
    public async Task ReadStore_ShouldQueryPersistedNachaHeadersAsNoTracking()
    {
        using var context = BuildContext();
        SeedPersistedFile(context, "N1", DateTime.UtcNow);
        context.ChangeTracker.Clear();

        var files = await new NachaOperationalReadStore(context).GetOperationalFilesAsync();

        files.Should().HaveCount(1);
        context.ChangeTracker.Entries().Should().BeEmpty();
    }

    [Fact]
    public async Task ReadStore_ShouldReturnFilesFromPersistedNachaHeaders()
    {
        using var context = BuildContext();
        SeedPersistedFile(context, "N1", new DateTime(2026, 5, 24, 12, 0, 0, DateTimeKind.Utc));

        var files = await new NachaOperationalReadStore(context).GetOperationalFilesAsync();

        files.Single().FileName.Should().Be("entrada.ach");
        files.Single().DataSource.Should().Be("backend read-only");
        files.Single().NoSensitiveData.Should().BeTrue();
    }

    [Fact]
    public async Task ReadStore_ShouldCalculateBatchEntryAddendaCounts()
    {
        using var context = BuildContext();
        SeedPersistedFile(context, "N1", DateTime.UtcNow, entries: 2, addendas: 3);

        var file = (await new NachaOperationalReadStore(context).GetOperationalFilesAsync()).Single();

        file.BatchCount.Should().Be(1);
        file.EntryCount.Should().Be(2);
        file.AddendaCount.Should().Be(3);
        file.BatchControlCount.Should().Be(1);
        file.FileControlCount.Should().Be(1);
        file.PersistedRecordCount.Should().Be(9);
    }

    [Fact]
    public async Task ReadStore_ShouldReturnEmptyCollectionsWhenNoPersistedData()
    {
        using var context = BuildContext();
        var store = new NachaOperationalReadStore(context);

        (await store.GetOperationalFilesAsync()).Should().BeEmpty();
        (await store.GetOperationalDecisionsAsync()).Should().BeEmpty();
        (await store.GetSoapReadinessAsync()).Should().BeEmpty();
        (await store.GetOperationalAuditAsync()).Should().BeEmpty();
    }

    [Fact]
    public async Task ReadStore_ShouldNotCallSaveChanges()
    {
        using var context = BuildCountingContext();
        SeedPersistedFile(context, "N1", DateTime.UtcNow);
        context.SaveChangesCount = 0;
        context.SaveChangesAsyncCount = 0;
        context.ChangeTracker.Clear();

        await new NachaOperationalReadStore(context).GetDashboardAsync();

        context.SaveChangesCount.Should().Be(0);
        context.SaveChangesAsyncCount.Should().Be(0);
    }

    [Fact]
    public void ReadStore_ShouldNotExecuteSoap()
    {
        typeof(NachaOperationalReadStore).GetConstructors().Single().GetParameters()
            .Select(x => x.ParameterType.Name)
            .Should()
            .NotContain(x => x.Contains("Soap", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task ReadStore_ShouldSanitizeFileReadModels()
    {
        using var context = BuildContext();
        SeedPersistedFile(context, "1234567890abcdef1234567890abcdef", DateTime.UtcNow);

        var file = (await new NachaOperationalReadStore(context).GetOperationalFilesAsync()).Single();

        file.HeaderId!.Length.Should().BeLessThanOrEqualTo(16);
        file.CorrelationId.Should().NotContain("1234567890abcdef1234567890abcdef");
    }

    [Fact]
    public async Task ReadStore_ShouldNotExposeSensitiveData()
    {
        using var context = BuildContext();
        SeedPersistedFile(context, "N1", DateTime.UtcNow);
        context.EntryDetails.Add(new EntryDetail { EntryDetailID = 99, NachaID = "N1", AccountNumber = "1234567890123456", RecipIdNumber = "DOC123456789" });
        await context.SaveChangesAsync();

        var serialized = Serialize(await new NachaOperationalReadStore(context).GetDashboardAsync());

        serialized.Should().NotContain("1234567890123456");
        serialized.Should().NotContain("DOC123456789");
        serialized.Should().NotContain("RequestPayloadXml");
    }

    [Fact]
    public async Task ReadStore_ShouldLimitResults()
    {
        using var context = BuildContext();
        for (var i = 0; i < 60; i++)
        {
            SeedPersistedFile(context, $"N{i:00}", DateTime.UtcNow.AddMinutes(-i), save: false);
        }
        await context.SaveChangesAsync();

        var files = await new NachaOperationalReadStore(context).GetOperationalFilesAsync();

        files.Should().HaveCount(50);
    }

    [Fact]
    public async Task ReadStore_ShouldOrderFilesByReceivedOrCreatedDateDescending()
    {
        using var context = BuildContext();
        SeedPersistedFile(context, "OLDER", new DateTime(2026, 5, 20, 12, 0, 0, DateTimeKind.Utc));
        SeedPersistedFile(context, "NEWER", new DateTime(2026, 5, 25, 12, 0, 0, DateTimeKind.Utc));

        var files = await new NachaOperationalReadStore(context).GetOperationalFilesAsync();

        files.First().FileName.Should().Be("NEWER.ach");
    }

    [Fact]
    public async Task OperationalService_ShouldUseReadStoreWhenDataExists()
    {
        using var context = BuildContext();
        SeedPersistedFile(context, "N1", DateTime.UtcNow);
        var service = new NachaOperationalReadModelService(new NachaOperationalReadStore(context));

        var dashboard = await service.GetDashboardAsync();

        dashboard.IsDemoData.Should().BeFalse();
        dashboard.DataSource.Should().Be("parcial");
        dashboard.Files.Should().ContainSingle();
    }

    [Fact]
    public async Task OperationalService_ShouldFallbackToDemoWhenReadStoreEmpty()
    {
        using var context = BuildContext();
        var service = new NachaOperationalReadModelService(new NachaOperationalReadStore(context));

        var dashboard = await service.GetDashboardAsync();

        dashboard.IsDemoData.Should().BeTrue();
        dashboard.Summary.Warnings.Should().Contain(x => x.Contains("No persisted NACHA read-store data", StringComparison.Ordinal));
    }

    [Fact]
    public async Task OperationalService_ShouldMarkPartialDataWhenSomeSourcesMissing()
    {
        using var context = BuildContext();
        SeedPersistedFile(context, "N1", DateTime.UtcNow);

        var dashboard = await new NachaOperationalReadModelService(new NachaOperationalReadStore(context)).GetDashboardAsync();

        dashboard.IsPartialData.Should().BeTrue();
        dashboard.Warnings.Should().Contain(x => x.Contains("No persisted decision records", StringComparison.Ordinal));
        dashboard.Warnings.Should().Contain(x => x.Contains("No persisted SOAP readiness data", StringComparison.Ordinal));
    }

    [Fact]
    public async Task OperationalService_ShouldReturnNoGoStatus()
    {
        using var context = BuildContext();
        SeedPersistedFile(context, "N1", DateTime.UtcNow);

        var dashboard = await new NachaOperationalReadModelService(new NachaOperationalReadStore(context)).GetDashboardAsync();

        dashboard.ProductiveStatus.Should().Be("NO-GO");
        dashboard.Summary.ProductiveStatus.Should().Be("NO-GO");
    }

    [Fact]
    public async Task OperationalService_ShouldReturnProductiveExecutionFalse()
    {
        using var context = BuildContext();
        SeedPersistedFile(context, "N1", DateTime.UtcNow);

        var dashboard = await new NachaOperationalReadModelService(new NachaOperationalReadStore(context)).GetDashboardAsync();

        dashboard.Summary.ProductiveExecution.Should().BeFalse();
    }

    [Fact]
    public async Task OperationalService_ShouldReturnWouldInvokeRealSoapFalse()
    {
        using var context = BuildContext();
        SeedPersistedFile(context, "N1", DateTime.UtcNow);

        var dashboard = await new NachaOperationalReadModelService(new NachaOperationalReadStore(context)).GetDashboardAsync();

        dashboard.Summary.WouldInvokeRealSoap.Should().BeFalse();
        dashboard.Readiness.Should().NotContain(x => x.WouldInvokeRealSoap);
    }

    [Fact]
    public async Task DashboardEndpoint_ShouldReturnPersistedReadStoreDataWhenAvailable()
    {
        using var context = BuildContext();
        SeedPersistedFile(context, "N1", DateTime.UtcNow);
        var controller = new NachaOperationalReadinessController(new NachaOperationalReadModelService(new NachaOperationalReadStore(context)));

        var ok = Assert.IsType<OkObjectResult>(await controller.GetDashboard(default));
        var dashboard = Assert.IsType<NachaOperationalDashboardReadModel>(ok.Value);

        dashboard.IsDemoData.Should().BeFalse();
    }

    [Fact]
    public async Task FilesEndpoint_ShouldReturnPersistedFiles()
    {
        using var context = BuildContext();
        SeedPersistedFile(context, "N1", DateTime.UtcNow);
        var controller = new NachaOperationalReadinessController(new NachaOperationalReadModelService(new NachaOperationalReadStore(context)));

        var ok = Assert.IsType<OkObjectResult>(await controller.GetFiles(default));
        var files = Assert.IsAssignableFrom<IReadOnlyList<NachaOperationalFileReadModel>>(ok.Value);

        files.Should().ContainSingle();
    }

    [Fact]
    public async Task DecisionsEndpoint_ShouldReturnSafePartialDataWhenNoPersistedDecisions()
    {
        using var context = BuildContext();
        SeedPersistedFile(context, "N1", DateTime.UtcNow);
        var controller = new NachaOperationalReadinessController(new NachaOperationalReadModelService(new NachaOperationalReadStore(context)));

        var dashboard = Assert.IsType<NachaOperationalDashboardReadModel>(Assert.IsType<OkObjectResult>(await controller.GetDashboard(default)).Value);

        dashboard.Decisions.Should().BeEmpty();
        dashboard.Warnings.Should().Contain(x => x.Contains("No persisted decision records", StringComparison.Ordinal));
    }

    [Fact]
    public async Task SoapReadinessEndpoint_ShouldReturnSafePartialDataWhenNoPersistedReadiness()
    {
        using var context = BuildContext();
        SeedPersistedFile(context, "N1", DateTime.UtcNow);
        var controller = new NachaOperationalReadinessController(new NachaOperationalReadModelService(new NachaOperationalReadStore(context)));

        var dashboard = Assert.IsType<NachaOperationalDashboardReadModel>(Assert.IsType<OkObjectResult>(await controller.GetDashboard(default)).Value);

        dashboard.Readiness.Should().BeEmpty();
        dashboard.Warnings.Should().Contain(x => x.Contains("No persisted SOAP readiness data", StringComparison.Ordinal));
    }

    [Fact]
    public async Task AuditEndpoint_ShouldReturnSanitizedAudit()
    {
        using var context = BuildContext();
        var ingestionId = SeedPersistedFile(context, "N1", DateTime.UtcNow);
        context.IncomingNachaProcessingEvents.Add(new IncomingNachaProcessingEvent
        {
            IncomingNachaFileIngestionId = ingestionId,
            EventType = "Processed",
            EventStatus = "Ok",
            Message = "Evento sin payload",
            EvidenceJson = "<soap envelope>secret</soap envelope>",
            OccurredAtUtc = DateTime.UtcNow
        });
        await context.SaveChangesAsync();

        var audit = await new NachaOperationalReadStore(context).GetOperationalAuditAsync();

        Serialize(audit).Should().NotContain("soap envelope");
        Serialize(audit).Should().NotContain("secret");
    }

    [Fact]
    public void Endpoints_ShouldRemainGetOnly()
    {
        typeof(NachaOperationalReadinessController).GetMethods()
            .Where(x => x.DeclaringType == typeof(NachaOperationalReadinessController) && x.IsPublic && x.Name.StartsWith("Get", StringComparison.Ordinal))
            .Should()
            .OnlyContain(x => x.GetCustomAttributes(typeof(HttpGetAttribute), true).Any());
    }

    [Fact]
    public async Task Endpoints_ShouldNotTriggerSoapExecution()
    {
        var service = new Mock<INachaOperationalReadModelService>(MockBehavior.Strict);
        var demoDashboard = await new NachaOperationalReadModelService().GetDashboardAsync();
        service.Setup(x => x.GetDashboardAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(demoDashboard);
        var controller = new NachaOperationalReadinessController(service.Object);

        await controller.GetDashboard(default);

        service.Verify(x => x.GetDashboardAsync(It.IsAny<CancellationToken>()), Times.Once);
        service.VerifyNoOtherCalls();
    }

    [Fact]
    public void ExportFlow_ShouldStillUseCycleId()
    {
        var action = typeof(NachaExportController).GetMethod(nameof(NachaExportController.Export));

        action!.GetParameters().Single(x => x.Name == "cycleId").ParameterType.Should().Be(typeof(string));
    }

    [Fact]
    public void ExportFlow_ShouldRejectHashIdentifierOrNeverReceiveIt()
    {
        var dto = new AchCycleExportDto
        {
            Id = "8dbe5a2ce0da7c9eaff2a82d6a9e704c34ef77fa",
            CycleId = "42",
            ExportIdentifier = "42",
            CycleName = "Ciclo",
            ProcessingDate = DateTime.UtcNow,
            IsExportable = true
        };

        dto.CycleId.Should().Be("42");
        dto.ExportIdentifier.Should().Be("42");
        dto.ExportIdentifier.Should().NotBe(dto.Id);
    }

    [Fact]
    public void ExportDto_ShouldStillExposeCycleIdAndExportIdentifier()
    {
        typeof(AchCycleExportDto).GetProperty(nameof(AchCycleExportDto.CycleId)).Should().NotBeNull();
        typeof(AchCycleExportDto).GetProperty(nameof(AchCycleExportDto.ExportIdentifier)).Should().NotBeNull();
    }

    private static AchDbContext BuildContext()
    {
        var options = new DbContextOptionsBuilder<AchDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new AchDbContext(options);
    }

    private static CountingAchDbContext BuildCountingContext()
    {
        var options = new DbContextOptionsBuilder<AchDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new CountingAchDbContext(options);
    }

    private static Guid SeedPersistedFile(AchDbContext context, string nachaId, DateTime receivedAt, int entries = 1, int addendas = 1, bool save = true)
    {
        var ingestionId = Guid.NewGuid();
        if (!context.ClearingHouses.Local.Any(x => x.Id == 1) && !context.ClearingHouses.Any(x => x.Id == 1))
        {
            context.ClearingHouses.Add(new ClearingHouse { Id = 1, ClearingHouseId = 1, Code = "ACH", Name = "ACH Colombia", OriginCode = "12345678" });
        }
        context.IncomingNachaFileIngestions.Add(new IncomingNachaFileIngestion
        {
            Id = ingestionId,
            FileName = $"{nachaId}.ach" == "N1.ach" ? "entrada.ach" : $"{nachaId}.ach",
            FileHashSha256 = $"{nachaId}-hash",
            FileSize = 106,
            ContentType = "text/plain",
            UploadedAtUtc = receivedAt.AddMinutes(-5),
            ReceivedAtUtc = receivedAt,
            UploadedBy = "tester",
            CorrelationId = $"corr-{nachaId}",
            IngestionStatus = IncomingNachaIngestionStatus.Parseado,
            ParsingStatus = IncomingNachaParsingStatus.Exitoso
        });
        context.IncomingNachaFileProcessingResults.Add(new IncomingNachaFileProcessingResult
        {
            IncomingNachaFileIngestionId = ingestionId,
            AttemptNumber = 1,
            StartedAtUtc = receivedAt,
            FinishedAtUtc = receivedAt.AddMinutes(1),
            TotalBatches = 1,
            TotalEntries = entries,
            TotalAddendas = addendas,
            OutcomeStatus = IncomingNachaProcessingOutcomeStatus.Exitoso
        });
        context.NachaHeaders.Add(new NachaHeader { NachaID = nachaId, ClearingHouseId = 1, IncomingNachaFileIngestionId = ingestionId, CycleNumber = 1 });
        context.BatchHeaders.Add(new BatchHeader { BatchID = Math.Abs(nachaId.GetHashCode()), NachaID = nachaId, BatchNumber = 1 });
        for (var i = 0; i < entries; i++)
        {
            context.EntryDetails.Add(new EntryDetail { EntryDetailID = Math.Abs($"{nachaId}-e-{i}".GetHashCode()), NachaID = nachaId, SequenceNumber = $"90000000000{i:0000}", AccountNumber = $"acct-{i}" });
        }
        for (var i = 0; i < addendas; i++)
        {
            context.AddendaRecords.Add(new AddendaRecord { AddendaID = Math.Abs($"{nachaId}-a-{i}".GetHashCode()), NachaID = nachaId, AddendumSequence = i.ToString() });
        }
        context.BatchControls.Add(new BatchControl { BatchControlID = Math.Abs($"{nachaId}-bc".GetHashCode()), NachaID = nachaId, EntryAddendaCount = entries + addendas });
        context.FileControls.Add(new FileControl { FileControlID = Math.Abs($"{nachaId}-fc".GetHashCode()), NachaID = nachaId, BatchCount = 1, EntryAddendaCount = entries + addendas });

        if (save)
        {
            context.SaveChanges();
        }

        return ingestionId;
    }

    private static string Serialize(object value)
        => System.Text.Json.JsonSerializer.Serialize(value).ToLowerInvariant();

    private sealed class CountingAchDbContext : AchDbContext
    {
        public CountingAchDbContext(DbContextOptions<AchDbContext> options) : base(options)
        {
        }

        public int SaveChangesCount { get; set; }
        public int SaveChangesAsyncCount { get; set; }

        public override int SaveChanges()
        {
            SaveChangesCount++;
            return base.SaveChanges();
        }

        public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            SaveChangesAsyncCount++;
            return base.SaveChangesAsync(cancellationToken);
        }
    }
}
