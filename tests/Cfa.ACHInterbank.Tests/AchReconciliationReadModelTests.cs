using Cfa.ACHInterbank.Api.Controllers;
using Cfa.ACHInterbank.Application.ACH.Interfaces;
using Cfa.ACHInterbank.Application.ACH.Models;
using Cfa.ACHInterbank.Domain.Entities.Ach.Dtos;
using Cfa.ACHInterbank.Domain.Entities.Transactions.Enums;
using Cfa.ACHInterbank.Domain.Models.ACH;
using Cfa.ACHInterbank.Domain.Models.ACH.Enums;
using Cfa.ACHInterbank.Persistence.ACH.Services.Implementation;
using Cfa.ACHInterbank.Persistence.DataBase;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Moq;
using Xunit;

namespace Cfa.ACHInterbank.Tests;

public class AchReconciliationReadModelTests
{
    [Fact]
    public async Task Reconciliation_ShouldReturnNoGoAndReadOnlySummary()
    {
        using var context = BuildContext();
        SeedAll(context);

        var dashboard = await new AchReconciliationReadModelService(context).GetDashboardAsync();

        dashboard.ProductiveStatus.Should().Be("NO-GO");
        dashboard.Warnings.Should().Contain(x => x.Contains("read-only", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task Reconciliation_ShouldReturnResponsesReturnsRorPrenotes()
    {
        using var context = BuildContext();
        SeedAll(context);

        var items = await new AchReconciliationReadModelService(context).GetItemsAsync();

        items.Should().Contain(x => x.FlowType == "DifferentialResponse");
        items.Should().Contain(x => x.IsReturnFile);
        items.Should().Contain(x => x.IsRor);
        items.Should().Contain(x => x.IsPrenotification);
    }

    [Fact]
    public async Task Reconciliation_ShouldJoinNachaAndInternalTransactionWhenAvailable()
    {
        using var context = BuildContext();
        SeedAll(context);

        var detail = await new AchReconciliationReadModelService(context).GetItemByCorrelationAsync("corr-response");

        detail.Should().NotBeNull();
        detail!.NachaHeaderSummary.Should().NotBeNull();
        detail.InternalTransactionSummary.Should().NotBeNull();
    }

    [Fact]
    public async Task Reconciliation_ShouldMarkDifferentialResponsesAsNonMonetary()
    {
        using var context = BuildContext();
        SeedAll(context);

        var item = (await new AchReconciliationReadModelService(context).GetItemsAsync()).Single(x => x.FlowType == "DifferentialResponse");

        item.IsNonMonetary.Should().BeTrue();
        item.SoapOperationCandidate.Should().Be("RegistrarRespuestaTransaccion");
    }

    [Fact]
    public async Task Reconciliation_ShouldMarkRetAndPrenoteAsNonMonetary()
    {
        using var context = BuildContext();
        SeedAll(context);

        var items = await new AchReconciliationReadModelService(context).GetItemsAsync();

        items.Where(x => x.IsReturnFile || x.IsPrenotification).Should().OnlyContain(x => x.IsNonMonetary);
    }

    [Fact]
    public async Task Reconciliation_ShouldClassifyManualReviewWhenAmbiguous()
    {
        using var context = BuildContext();
        SeedAll(context);

        var items = await new AchReconciliationReadModelService(context).GetItemsAsync();

        items.Should().Contain(x => x.RequiresManualReview && x.ReconciliationStatus == "RequiereRevisionManual");
    }

    [Fact]
    public async Task Reconciliation_ShouldReturnPartialWarningsWhenSourcesMissing()
    {
        using var context = BuildContext();

        var dashboard = await new AchReconciliationReadModelService(context).GetDashboardAsync();

        dashboard.IsPartialData.Should().BeTrue();
        dashboard.Warnings.Should().Contain(x => x.Contains("No persisted reconciliation", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task Reconciliation_ShouldSanitizeSensitiveData()
    {
        using var context = BuildContext();
        SeedAll(context);

        var serialized = Serialize(await new AchReconciliationReadModelService(context).GetItemByCorrelationAsync("corr-response"));

        serialized.Should().NotContain("1234567890123456");
        serialized.Should().NotContain("doc123456789");
        serialized.Should().NotContain("<soap");
        serialized.Should().Contain("****3456");
    }

    [Fact]
    public async Task Reconciliation_ShouldNotCallSaveChanges()
    {
        using var context = BuildCountingContext();
        SeedAll(context);
        context.SaveChangesCount = 0;
        context.SaveChangesAsyncCount = 0;

        await new AchReconciliationReadModelService(context).GetDashboardAsync();

        context.SaveChangesCount.Should().Be(0);
        context.SaveChangesAsyncCount.Should().Be(0);
    }

    [Fact]
    public void Reconciliation_ShouldNotExecuteSoap()
    {
        typeof(AchReconciliationReadModelService).GetConstructors().Single().GetParameters()
            .Select(x => x.ParameterType.Name)
            .Should().NotContain(x => x.Contains("Soap", StringComparison.OrdinalIgnoreCase) || x.Contains("Gateway", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void ReconciliationEndpoints_ShouldBeGetOnly()
    {
        typeof(AchReconciliationController).GetMethods()
            .Where(x => x.DeclaringType == typeof(AchReconciliationController) && x.IsPublic && x.Name.StartsWith("Get", StringComparison.Ordinal))
            .Should().OnlyContain(x => x.GetCustomAttributes(typeof(HttpGetAttribute), true).Any());
    }

    [Fact]
    public void NachaExport_ShouldStillUseCycleIdNotHash()
    {
        var action = typeof(NachaExportController).GetMethod(nameof(NachaExportController.Export));

        action!.GetParameters().Should().ContainSingle(x => x.Name == "cycleId");
        action.GetParameters().Should().NotContain(x => x.Name!.Contains("hash", StringComparison.OrdinalIgnoreCase));
        typeof(AchCycleExportDto).GetProperty(nameof(AchCycleExportDto.ExportIdentifier)).Should().NotBeNull();
    }

    [Fact]
    public void Reconciliation_ShouldNotUseLegacyLayoutsDefinitions()
    {
        typeof(AchReconciliationController).GetConstructors().Single().GetParameters()
            .Select(x => x.ParameterType.Name)
            .Should().NotContain(x => x.Contains("Layout", StringComparison.OrdinalIgnoreCase) || x.Contains("Definition", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task ReconciliationEndpoint_ShouldReturnItems()
    {
        var service = new Mock<IAchReconciliationReadModelService>();
        service.Setup(x => x.GetItemsAsync(It.IsAny<CancellationToken>())).ReturnsAsync([Item()]);
        var controller = new AchReconciliationController(service.Object);

        var ok = Assert.IsType<OkObjectResult>(await controller.GetItems(default));

        Assert.IsAssignableFrom<IReadOnlyList<AchReconciliationItemReadModel>>(ok.Value).Should().ContainSingle();
    }

    private static AchReconciliationItemReadModel Item() => new()
    {
        ReconciliationId = "resp-1",
        CorrelationId = "corr",
        FileName = "entrada.ach",
        ClearingHouseCode = "ACH",
        FlowType = "DifferentialResponse",
        ResponseType = "Respuesta diferencial",
        TraceNumberMasked = "***0001",
        OriginalTraceNumberMasked = "N/A",
        InternalStatus = "Notificada",
        ReconciliationStatus = "Conciliado",
        IsNonMonetary = true,
        SoapOperationCandidate = "RegistrarRespuestaTransaccion",
        CreatedAt = DateTimeOffset.UtcNow,
        DataSource = "backend read-only",
        IsPersisted = true,
        IsDerived = true
    };

    private static AchDbContext BuildContext()
        => new(new DbContextOptionsBuilder<AchDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);

    private static CountingAchDbContext BuildCountingContext()
        => new(new DbContextOptionsBuilder<AchDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);

    private static void SeedAll(AchDbContext context)
    {
        var tx = new AchTransaction
        {
            Id = 100,
            TransactionExternalId = "TX-100",
            Reference = "REF-100",
            TraceNumber = "900000000001001",
            OriginalTraceRef = "800000000009999",
            SourceAccountNumber = "1234567890123456",
            DestinationAccountNumber = "9876543210123456",
            RecipientIdNumber = "DOC123456789",
            Type = TransactionTypeEnum.Credit,
            State = AchTransferStateEnum.Certified,
            AchCycleId = "C1"
        };
        var retTx = new AchTransaction { Id = 101, TransactionExternalId = "TX-101", Reference = "REF-101", TraceNumber = "900000000001002", Type = TransactionTypeEnum.Return, State = AchTransferStateEnum.ReturnedByOperator, AchCycleId = "C1", SourceAccountNumber = "1", DestinationAccountNumber = "2" };
        var rorTx = new AchTransaction { Id = 102, TransactionExternalId = "TX-102", Reference = "REF-102", TraceNumber = "900000000001003", Type = TransactionTypeEnum.Return, State = AchTransferStateEnum.Pending, AchCycleId = "C1", SourceAccountNumber = "1", DestinationAccountNumber = "2" };
        context.AchTransactions.AddRange(tx, retTx, rorTx);
        context.AchResponses.AddRange(
            new AchResponse { Id = Guid.NewGuid(), TipoRespuesta = TipoRespuestaAch.Transaccion, IdTransaccion = "TX-100", CodigoCamaraCompensacion = "ACH", CodigoEstadoExterno = "00", IdTransaccionServicioExterno = 1, HashIdempotencia = "H1", EstadoProcesamiento = AchResponseProcessingStatus.Notificada, PermiteNotificacion = true, CorrelationId = "corr-response", FechaRecepcion = DateTime.UtcNow, FechaCreacion = DateTime.UtcNow, EstadoInternoNombre = "Notificada" },
            new AchResponse { Id = Guid.NewGuid(), TipoRespuesta = TipoRespuestaAch.Prenota, IdTransaccion = "TX-100", CodigoCamaraCompensacion = "ACH", CodigoEstadoExterno = "05", IdTransaccionServicioExterno = 2, HashIdempotencia = "H2", EstadoProcesamiento = AchResponseProcessingStatus.RequiereRevisionManual, PermiteNotificacion = false, CorrelationId = "corr-prenote", FechaRecepcion = DateTime.UtcNow, FechaCreacion = DateTime.UtcNow, MotivoNoHomologacion = "Ambigua" });
        context.AchReturnsGenerated.Add(new AchReturnGenerated { Id = 1, OriginalTransactionId = 101, OriginalTransaction = retTx, ReturnCycleId = "C1", ReturnReasonCode = "R01", FileName = "return.RET", OriginalSequenceNumber = retTx.TraceNumber, NewSequenceNumber = "900000000001004" });
        context.ReturnOfReturnFlows.Add(new ReturnOfReturnFlow { Id = 1, SourceReturnTransactionId = 101, SourceReturnTransaction = retTx, ReturnOfReturnTransactionId = 102, ReturnOfReturnTransaction = rorTx, ReasonCode = "R02" });
        var ingestionId = Guid.NewGuid();
        context.IncomingNachaFileIngestions.Add(new IncomingNachaFileIngestion { Id = ingestionId, FileName = "entrada.ach", CorrelationId = "corr-response", FileHashSha256 = "hash", UploadedBy = "test" });
        context.NachaHeaders.Add(new NachaHeader { NachaID = "N1", IncomingNachaFileIngestionId = ingestionId, ClearingHouseId = 1, CycleNumber = 1 });
        context.BatchHeaders.Add(new BatchHeader { BatchID = 1, NachaID = "N1", BatchNumber = 1 });
        context.EntryDetails.Add(new EntryDetail { EntryDetailID = 1, NachaID = "N1", SequenceNumber = tx.TraceNumber, AccountNumber = tx.SourceAccountNumber, RecipIdNumber = tx.RecipientIdNumber, Amount = 100 });
        context.AddendaRecords.Add(new AddendaRecord { AddendaID = 1, NachaID = "N1", OriginalTraceNumber = tx.OriginalTraceRef, ReturnReasonCode = "R01" });
        context.BatchControls.Add(new BatchControl { BatchControlID = 1, NachaID = "N1", EntryAddendaCount = 2, TotalCreditAmount = 100 });
        context.FileControls.Add(new FileControl { FileControlID = 1, NachaID = "N1", BatchCount = 1, EntryAddendaCount = 2, TotalCreditAmount = 100 });
        context.IncomingNachaEntryClassifications.Add(new IncomingNachaEntryClassification { IncomingNachaFileIngestionId = ingestionId, EntryDetailId = 1, EntryDetail = context.EntryDetails.Local.First(), FunctionalClass = IncomingNachaFunctionalClass.Ambigua, EligibilityStatus = IncomingNachaEligibilityStatus.RevisionManual, RequiresManualResolution = true, BusinessMeaning = "Ambigua" });
        context.AchTransactionStateEvents.Add(new AchTransactionStateEvent { AchTransactionId = 100, ToState = AchTransferStateEnum.Certified, ReasonCode = "OK" });
        context.SaveChanges();
        context.ChangeTracker.Clear();
    }

    private static string Serialize(object? value) => System.Text.Json.JsonSerializer.Serialize(value).ToLowerInvariant();

    private sealed class CountingAchDbContext : AchDbContext
    {
        public CountingAchDbContext(DbContextOptions<AchDbContext> options) : base(options) { }
        public int SaveChangesCount { get; set; }
        public int SaveChangesAsyncCount { get; set; }
        public override int SaveChanges() { SaveChangesCount++; return base.SaveChanges(); }
        public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default) { SaveChangesAsyncCount++; return base.SaveChangesAsync(cancellationToken); }
    }
}
