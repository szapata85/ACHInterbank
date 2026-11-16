using Cfa.ACHInterbank.Application.ACH.Interfaces;
using Cfa.ACHInterbank.Application.ACH.Models;
using Cfa.ACHInterbank.Application.External.Connections;
using Cfa.ACHInterbank.Application.Security.Dtos;
using Cfa.ACHInterbank.Application.Security.Interfaces;
using Cfa.ACHInterbank.Application.Integrations.Models;
using Cfa.ACHInterbank.Application.Integrations.Interfaces;
using Cfa.ACHInterbank.Domain.Entities.Integrations;
using Cfa.ACHInterbank.Domain.Entities.Transactions.Enums;
using Cfa.ACHInterbank.Domain.Models.ACH;
using Cfa.ACHInterbank.Domain.Models.ACH.Enums;
using Cfa.ACHInterbank.Domain.Models.Configurations;
using Cfa.ACHInterbank.Persistence.ACH.Services.Implementation;
using Cfa.ACHInterbank.Persistence.DataBase;
using Cfa.ACHInterbank.Persistence.Integrations.Services;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;
using Cfa.ACHInterbank.Tests.TestSupport;

namespace Cfa.ACHInterbank.Tests;

public class ContrapartidaDispatchJobServiceTests
{
    [Fact]
    public async Task ProcessCycleAsync_BlockedByOperationalCalendar_DoesNotInvokeSoapOrCreateBatch()
    {
        using var connection = new SqliteConnection("DataSource=:memory:");
        await connection.OpenAsync();
        await using var context = new AchDbContext(
            new DbContextOptionsBuilder<AchDbContext>().UseSqlite(connection).Options);
        await context.Database.EnsureCreatedAsync();
        var cycleId = await SembrarEstructuraBaseAsync(context);

        var soap = new Mock<IWscfaachSoapClient>(MockBehavior.Strict);
        var mapper = new Mock<IProcContrapartidasRequestMapper>(MockBehavior.Strict);
        var parser = new Mock<IProcContrapartidasResponseParser>(MockBehavior.Strict);
        var guard = new Mock<ICycleCalendarGuard>(MockBehavior.Strict);
        guard.Setup(x => x.EnsureExecutableAsync(It.IsAny<AchCycle>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new CycleCalendarGuardResult(
                false,
                true,
                new DateOnly(2026, 7, 13),
                new DateOnly(2026, 7, 14),
                "Festivo de Chiquinquira trasladado por Ley Emiliani"));

        var sut = new ContrapartidaDispatchJobService(
            context,
            soap.Object,
            mapper.Object,
            parser.Object,
            NullLogger<ContrapartidaDispatchJobService>.Instance,
            LiveDispatchOptions(),
            timeProvider: TestClock.Create(),
            calendarGuard: guard.Object);

        var result = await sut.ProcessCycleAsync(cycleId, 1, "qa-calendar", 100);

        Assert.Equal(0, result.Processed);
        Assert.Contains("SOAP no invocado", result.Summary);
        Assert.Empty(await context.ContrapartidaDispatchBatches.ToListAsync());
        Assert.Empty(await context.ContrapartidaDispatchAttempts.ToListAsync());
        soap.Verify(x => x.ProcContrapartidasAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
        mapper.VerifyNoOtherCalls();
        parser.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task ProcessCycleAsync_DebeRetornarSinProcesados_CuandoNoHayItemsElegibles()
    {
        using var connection = new SqliteConnection("DataSource=:memory:");
        await connection.OpenAsync();

        var options = new DbContextOptionsBuilder<AchDbContext>()
            .UseSqlite(connection)
            .Options;

        await using var context = new AchDbContext(options);
        await context.Database.EnsureCreatedAsync();

        var cycleId = await SembrarEstructuraBaseAsync(context);

        var mapper = new Mock<IProcContrapartidasRequestMapper>(MockBehavior.Strict);
        var parser = new Mock<IProcContrapartidasResponseParser>(MockBehavior.Strict);
        var soap = new Mock<IWscfaachSoapClient>(MockBehavior.Strict);

        var sut = new ContrapartidaDispatchJobService(
            context,
            soap.Object,
            mapper.Object,
            parser.Object,
            NullLogger<ContrapartidaDispatchJobService>.Instance,
            LiveDispatchOptions(),
            soapIntegrationSettingsService: SoapSettingsService("http://localhost:7083/WSCFAACH.svc"),
            responseCatalogResolver: Catalog(Success("R96", "Débito aplicado correctamente")),
            timeProvider: TestClock.Create());

        var result = await sut.ProcessCycleAsync(cycleId, 1, "qa-soap-2b", 100, CancellationToken.None);

        Assert.Equal(0, result.Processed);
        Assert.Equal(0, result.Succeeded);
        Assert.Equal(0, result.Failed);
        Assert.Equal(0, result.Partial);
        Assert.Equal(0, result.Chunks);
        Assert.Empty(context.ContrapartidaDispatchBatches);
    }

    [Fact]
    public async Task ProcessCycleAsync_DebeMarcarItemReportado_CuandoRespuestaEsExitosa()
    {
        using var connection = new SqliteConnection("DataSource=:memory:");
        await connection.OpenAsync();

        var options = new DbContextOptionsBuilder<AchDbContext>()
            .UseSqlite(connection)
            .Options;

        await using var context = new AchDbContext(options);
        await context.Database.EnsureCreatedAsync();

        var cycleId = await SembrarEstructuraBaseAsync(context);
        var txId = await SembrarTransaccionYItemPendienteAsync(context, cycleId);

        var contract = ContratoValido();
        var resolution = new ProcContrapartidasRequestResolution
        {
            Contract = contract,
            MappingSetId = Guid.NewGuid(),
            MappingVersion = 3,
            MappingSnapshotHash = "hash-publicado-qa",
            UsedFallback = false
        };

        var mapper = new Mock<IProcContrapartidasRequestMapper>();
        mapper
            .Setup(x => x.ResolveAsync(It.IsAny<AchCycle>(), It.IsAny<IReadOnlyCollection<AchTransaction>>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(resolution);
        mapper
            .Setup(x => x.BuildSoapBody(It.IsAny<ProcContrapartidasRequestContract>()))
            .Returns("<request/>\n");

        var soap = new Mock<IWscfaachSoapClient>();
        soap
            .Setup(x => x.ProcContrapartidasAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync("<Envelope><Body><ok/></Body></Envelope>");

        var parser = new Mock<IProcContrapartidasResponseParser>();
        parser
            .Setup(x => x.Parse(It.IsAny<string>()))
            .Returns(new ProcContrapartidasParsedResponse(
                IsSuccess: true,
                IsSoapFault: false,
                IsRetryable: false,
                IsFunctionalRejection: false,
                ErrorCode: string.Empty,
                ErrorMessage: string.Empty,
                RawResponse: "<Envelope><Body><ok/></Body></Envelope>",
                ResponseCode: "R96",
                ItemResults: new Dictionary<int, ProcContrapartidasParsedItemResponse>
                {
                    [txId] = new(txId, true, false, "R96", "Aplicado")
                }));
        await new IntegrationCatalogBootstrapper(context).EnsureAsync();

        var sut = new ContrapartidaDispatchJobService(
            context,
            soap.Object,
            mapper.Object,
            parser.Object,
            NullLogger<ContrapartidaDispatchJobService>.Instance,
            LiveDispatchOptions(),
            soapIntegrationSettingsService: SoapSettingsService("http://localhost:7083/WSCFAACH.svc"),
            responseCatalogResolver: new IntegrationResponseCatalogResolver(context),
            timeProvider: TestClock.Create());

        var result = await sut.ProcessCycleAsync(cycleId, 1, "qa-soap-2b", 100, CancellationToken.None);

        Assert.Equal(1, result.Processed);
        Assert.Equal(1, result.Succeeded);
        Assert.Equal(0, result.Failed);
        Assert.Equal(0, result.Partial);
        Assert.Equal(1, result.Chunks);

        var item = await context.ContrapartidaDispatchItems.SingleAsync();
        Assert.Equal(ContrapartidaDispatchItemStateEnum.ReportedToContrapartida, item.State);
        Assert.Equal(1, item.AttemptCount);
        Assert.Equal("R96", item.LastResponseCode);

        var attempt = await context.ContrapartidaDispatchAttempts.SingleAsync();
        Assert.Equal(ContrapartidaDispatchAttemptResultEnum.Success, attempt.Result);
        Assert.False(attempt.RetryEligible);
        Assert.Equal("Proc_Contrapartidas", attempt.SoapMethodName);
        Assert.Equal("http://localhost:7083/WSCFAACH.svc", attempt.SoapEndpoint);
        Assert.Equal("Live", attempt.ExecutionMode);
        Assert.Equal("R96", attempt.SoapResponseCode);
        Assert.Equal("Débito aplicado correctamente", attempt.SoapResponseDescription);
        Assert.NotNull(attempt.ResponseCatalogId);
        Assert.Equal(IntegrationTransportStatus.Succeeded, attempt.TransportStatus);
        Assert.Equal(IntegrationResponseBusinessStatus.Success, attempt.BusinessStatus);
        Assert.Equal("Succeeded", attempt.SoapTechnicalStatus);
        Assert.True(attempt.IsSuccessful);
        Assert.False(attempt.IsFunctionalRejection);
        Assert.False(attempt.IsTechnicalFailure);
        Assert.Equal("<request/>\n", attempt.RequestPayloadXml);
        Assert.Equal("<Envelope><Body><ok/></Body></Envelope>", attempt.ResponsePayloadXml);
        Assert.DoesNotContain("<METODO>", attempt.RequestPayloadXml, StringComparison.OrdinalIgnoreCase);

        var readModel = await new TransactionIntegrationResultService(context).GetAsync(txId);
        Assert.NotNull(readModel?.Latest);
        Assert.Equal("Proc_Contrapartidas", readModel.Latest.Method);
        Assert.Equal("R96", readModel.Latest.ResponseCode);
        Assert.Equal("Débito aplicado correctamente", readModel.Latest.ResponseDescription);
        Assert.Equal("Succeeded", readModel.Latest.TransportStatus);
        Assert.Equal("Success", readModel.Latest.BusinessStatus);
        Assert.Single(readModel.History);

        var batch = await context.ContrapartidaDispatchBatches.SingleAsync();
        Assert.Equal(ContrapartidaDispatchBatchStatusEnum.Completed, batch.Status);
        Assert.Equal(1, batch.TotalSucceeded);
        Assert.Equal(0, batch.TotalFailed);

        var replayError = await Assert.ThrowsAsync<InvalidOperationException>(() => sut.RetryBatchAsync(
            new ContrapartidaDispatchRetryRequest(batch.Id, ContrapartidaDispatchRetryScope.Full, "qa-replay", AllowReplaySucceeded: true)));
        Assert.Contains("No existen items elegibles", replayError.Message);
        soap.Verify(x => x.ProcContrapartidasAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ProcessTransactionAsync_DebeAislarLaTransaccionObjetivo()
    {
        using var connection = new SqliteConnection("DataSource=:memory:");
        await connection.OpenAsync();
        var options = new DbContextOptionsBuilder<AchDbContext>().UseSqlite(connection).Options;
        await using var context = new AchDbContext(options);
        await context.Database.EnsureCreatedAsync();

        var cycleId = await SembrarEstructuraBaseAsync(context);
        var targetId = await SembrarTransaccionYItemPendienteAsync(context, cycleId);
        var unrelatedId = await SembrarTransaccionYItemPendienteAsync(context, cycleId);

        var mapper = new Mock<IProcContrapartidasRequestMapper>();
        mapper
            .Setup(x => x.ResolveAsync(
                It.IsAny<AchCycle>(),
                It.Is<IReadOnlyCollection<AchTransaction>>(items => items.Count == 1 && items.Single().Id == targetId),
                It.IsAny<DateTime>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ProcContrapartidasRequestResolution
            {
                Contract = ContratoValido(),
                MappingSetId = Guid.NewGuid(),
                MappingVersion = 1,
                MappingSnapshotHash = "targeted-hash",
                UsedFallback = false
            });
        mapper.Setup(x => x.BuildSoapBody(It.IsAny<ProcContrapartidasRequestContract>())).Returns("<request/>");

        var soap = new Mock<IWscfaachSoapClient>();
        soap.Setup(x => x.ProcContrapartidasAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync("<response/>");
        var parser = new Mock<IProcContrapartidasResponseParser>();
        parser.Setup(x => x.Parse("<response/>"))
            .Returns(new ProcContrapartidasParsedResponse(
                true, false, false, false, string.Empty, string.Empty, "<response/>", "00",
                new Dictionary<int, ProcContrapartidasParsedItemResponse>
                {
                    [targetId] = new(targetId, true, false, "00", "Aplicado")
                }));

        var sut = new ContrapartidaDispatchJobService(
            context,
            soap.Object,
            mapper.Object,
            parser.Object,
            NullLogger<ContrapartidaDispatchJobService>.Instance,
            LiveDispatchOptions(),
            soapIntegrationSettingsService: SoapSettingsService("http://localhost:7083/WSCFAACH.svc"),
            responseCatalogResolver: Catalog(Success("00", "Aplicado")),
            timeProvider: TestClock.Create());

        var result = await sut.ProcessTransactionAsync(cycleId, 1, targetId, "uat-targeted", CancellationToken.None);

        Assert.Equal(1, result.Processed);
        var items = await context.ContrapartidaDispatchItems.OrderBy(x => x.AchTransactionId).ToListAsync();
        Assert.Equal(ContrapartidaDispatchItemStateEnum.ReportedToContrapartida,
            items.Single(x => x.AchTransactionId == targetId).State);
        Assert.Equal(ContrapartidaDispatchItemStateEnum.PendingContrapartidaReport,
            items.Single(x => x.AchTransactionId == unrelatedId).State);
        soap.Verify(x => x.ProcContrapartidasAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ProcessTransactionAsync_DebeBloquearAntesDelTransporte_CuandoYaFueReportada()
    {
        using var connection = new SqliteConnection("DataSource=:memory:");
        await connection.OpenAsync();
        var options = new DbContextOptionsBuilder<AchDbContext>().UseSqlite(connection).Options;
        await using var context = new AchDbContext(options);
        await context.Database.EnsureCreatedAsync();

        var cycleId = await SembrarEstructuraBaseAsync(context);
        var transactionId = await SembrarTransaccionYItemPendienteAsync(context, cycleId);
        var item = await context.ContrapartidaDispatchItems.SingleAsync();
        item.State = ContrapartidaDispatchItemStateEnum.ReportedToContrapartida;
        await context.SaveChangesAsync();

        var mapper = new Mock<IProcContrapartidasRequestMapper>(MockBehavior.Strict);
        var soap = new Mock<IWscfaachSoapClient>(MockBehavior.Strict);
        var parser = new Mock<IProcContrapartidasResponseParser>(MockBehavior.Strict);
        var sut = new ContrapartidaDispatchJobService(
            context,
            soap.Object,
            mapper.Object,
            parser.Object,
            NullLogger<ContrapartidaDispatchJobService>.Instance,
            LiveDispatchOptions(),
            timeProvider: TestClock.Create());

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            sut.ProcessTransactionAsync(transactionId, "uat-duplicate-gate", CancellationToken.None));

        Assert.StartsWith("CONTRAPARTIDA_ALREADY_SUCCEEDED:", exception.Message, StringComparison.Ordinal);
        Assert.Empty(await context.ContrapartidaDispatchAttempts.ToListAsync());
        soap.Verify(x => x.ProcContrapartidasAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
        mapper.VerifyNoOtherCalls();
        parser.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task ProcessCycleAsync_DebeEnviarAReintento_CuandoRespuestaEsRetryable()
    {
        using var connection = new SqliteConnection("DataSource=:memory:");
        await connection.OpenAsync();

        var options = new DbContextOptionsBuilder<AchDbContext>()
            .UseSqlite(connection)
            .Options;

        await using var context = new AchDbContext(options);
        await context.Database.EnsureCreatedAsync();

        var cycleId = await SembrarEstructuraBaseAsync(context);
        var txId = await SembrarTransaccionYItemPendienteAsync(context, cycleId);
        await SeedIncomingNachaDispatchQueueAsync(context, cycleId, txId, "retryable");

        var mapper = new Mock<IProcContrapartidasRequestMapper>();
        mapper
            .Setup(x => x.ResolveAsync(It.IsAny<AchCycle>(), It.IsAny<IReadOnlyCollection<AchTransaction>>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ProcContrapartidasRequestResolution
            {
                Contract = ContratoValido(),
                MappingSnapshotHash = "hash-retry",
                UsedFallback = false
            });
        mapper
            .Setup(x => x.BuildSoapBody(It.IsAny<ProcContrapartidasRequestContract>()))
            .Returns("<request-retry/>");

        var soap = new Mock<IWscfaachSoapClient>();
        soap
            .Setup(x => x.ProcContrapartidasAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync("<Envelope><Body><Fault><faultstring>timeout</faultstring></Fault></Body></Envelope>");

        var parser = new Mock<IProcContrapartidasResponseParser>();
        parser
            .Setup(x => x.Parse(It.IsAny<string>()))
            .Returns(new ProcContrapartidasParsedResponse(
                IsSuccess: false,
                IsSoapFault: true,
                IsRetryable: true,
                IsFunctionalRejection: false,
                ErrorCode: "R98",
                ErrorMessage: "Temporal",
                RawResponse: "<fault/>",
                ResponseCode: "R98",
                ItemResults: new Dictionary<int, ProcContrapartidasParsedItemResponse>()));

        var sut = new ContrapartidaDispatchJobService(
            context,
            soap.Object,
            mapper.Object,
            parser.Object,
            NullLogger<ContrapartidaDispatchJobService>.Instance,
            LiveDispatchOptions(),
            timeProvider: TestClock.Create());

        var result = await sut.ProcessCycleAsync(cycleId, 1, "qa-soap-2b", 100, CancellationToken.None);

        Assert.Equal(1, result.Processed);
        Assert.Equal(0, result.Succeeded);
        Assert.Equal(1, result.Failed);
        Assert.Equal(0, result.Partial);

        var item = await context.ContrapartidaDispatchItems.SingleAsync();
        Assert.Equal(ContrapartidaDispatchItemStateEnum.RetryPending, item.State);
        Assert.NotNull(item.NextAttemptAtUtc);

        var attempt = await context.ContrapartidaDispatchAttempts.SingleAsync();
        Assert.Equal(ContrapartidaDispatchAttemptResultEnum.Failed, attempt.Result);
        Assert.True(attempt.RetryEligible);
        Assert.Equal("R98", attempt.SoapResponseCode);
        Assert.Equal("Temporal", attempt.SoapResponseDescription);
        Assert.Equal("SoapFault", attempt.SoapTechnicalStatus);
        Assert.False(attempt.IsFunctionalRejection);
        Assert.True(attempt.IsTechnicalFailure);

        var batch = await context.ContrapartidaDispatchBatches.SingleAsync();
        Assert.Equal(ContrapartidaDispatchBatchStatusEnum.Failed, batch.Status);
    }

    [Fact]
    public async Task ProcessCycleAsync_DebePersistirRechazoFuncional_CuandoSoapRetornaR01()
    {
        using var connection = new SqliteConnection("DataSource=:memory:");
        await connection.OpenAsync();

        var options = new DbContextOptionsBuilder<AchDbContext>()
            .UseSqlite(connection)
            .Options;

        await using var context = new AchDbContext(options);
        await context.Database.EnsureCreatedAsync();

        var cycleId = await SembrarEstructuraBaseAsync(context);
        var txId = await SembrarTransaccionYItemPendienteAsync(context, cycleId);

        var mapper = new Mock<IProcContrapartidasRequestMapper>();
        mapper
            .Setup(x => x.ResolveAsync(It.IsAny<AchCycle>(), It.IsAny<IReadOnlyCollection<AchTransaction>>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ProcContrapartidasRequestResolution
            {
                Contract = ContratoValido(),
                MappingSnapshotHash = "hash-r01",
                UsedFallback = false
            });
        mapper
            .Setup(x => x.BuildSoapBody(It.IsAny<ProcContrapartidasRequestContract>()))
            .Returns("<Proc_Contrapartidas><OFNIT>900123456</OFNIT></Proc_Contrapartidas>");

        var soap = new Mock<IWscfaachSoapClient>();
        soap
            .Setup(x => x.ProcContrapartidasAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync("<Envelope><Body><Proc_ContrapartidasResponse><ANSST>R01</ANSST><ANCLC>R01</ANCLC></Proc_ContrapartidasResponse></Body></Envelope>");

        var parser = new ProcContrapartidasResponseParser();

        var sut = new ContrapartidaDispatchJobService(
            context,
            soap.Object,
            mapper.Object,
            parser,
            NullLogger<ContrapartidaDispatchJobService>.Instance,
            LiveDispatchOptions(),
            soapIntegrationSettingsService: SoapSettingsService("http://localhost:7083/WSCFAACH.svc"),
            responseCatalogResolver: Catalog(Rejected("R01", "Rechazo parametrizado")),
            timeProvider: TestClock.Create());

        var result = await sut.ProcessCycleAsync(cycleId, 1, "qa-r01", 100, CancellationToken.None);

        Assert.Equal(1, result.Processed);
        Assert.Equal(0, result.Succeeded);
        Assert.Equal(1, result.Failed);

        var attempt = await context.ContrapartidaDispatchAttempts.SingleAsync();
        Assert.Equal("R01", attempt.SoapResponseCode);
        Assert.Equal("Succeeded", attempt.SoapTechnicalStatus);
        Assert.Equal(IntegrationTransportStatus.Succeeded, attempt.TransportStatus);
        Assert.Equal(IntegrationResponseBusinessStatus.Rejected, attempt.BusinessStatus);
        Assert.False(attempt.IsSuccessful);
        Assert.True(attempt.IsFunctionalRejection);
        Assert.False(attempt.IsTechnicalFailure);
        Assert.NotEmpty(attempt.ResponsePayloadXml);
        Assert.Contains("R01", attempt.ResponsePayloadXml);

        var item = await context.ContrapartidaDispatchItems.SingleAsync();
        Assert.Equal("R01", item.LastResponseCode);
        Assert.Equal(ContrapartidaDispatchItemStateEnum.ContrapartidaReportFailed, item.State);
    }

    [Fact]
    public async Task ProcessCycleAsync_DebeEnviarRequestIndependientePorTransaccion_YNoCruzarRespuestas()
    {
        using var connection = new SqliteConnection("DataSource=:memory:");
        await connection.OpenAsync();
        await using var context = new AchDbContext(
            new DbContextOptionsBuilder<AchDbContext>().UseSqlite(connection).Options);
        await context.Database.EnsureCreatedAsync();

        var cycleId = await SembrarEstructuraBaseAsync(context);
        var transactionIds = await SembrarVariasTransaccionesEItemsPendientesAsync(context, cycleId, 2);
        var firstTx = await context.AchTransactions.SingleAsync(x => x.Id == transactionIds[0]);
        var secondTx = await context.AchTransactions.SingleAsync(x => x.Id == transactionIds[1]);
        secondTx.SourceAccountNumber = "000000000022";
        await context.SaveChangesAsync();

        var mapper = new Mock<IProcContrapartidasRequestMapper>(MockBehavior.Strict);
        mapper.Setup(x => x.ResolveAsync(
                It.IsAny<AchCycle>(),
                It.IsAny<IReadOnlyCollection<AchTransaction>>(),
                It.IsAny<DateTime>(),
                It.IsAny<CancellationToken>()))
            .Returns((AchCycle cycle, IReadOnlyCollection<AchTransaction> selected, DateTime now, CancellationToken ct) =>
            {
                var tx = Assert.Single(selected);
                return Task.FromResult(new ProcContrapartidasRequestResolution
                {
                    Contract = ContratoValido() with { OFIDTX = tx.Reference, OFCTA = tx.SourceAccountNumber },
                    MappingSnapshotHash = $"mapping-{tx.Id}",
                    UsedFallback = false
                });
            });
        mapper.Setup(x => x.BuildSoapBody(It.IsAny<ProcContrapartidasRequestContract>()))
            .Returns((ProcContrapartidasRequestContract contract) =>
                $"<request id='{contract.OFIDTX}' account='{contract.OFCTA}'/>");

        var sentRequests = new List<string>();
        var soap = new Mock<IWscfaachSoapClient>(MockBehavior.Strict);
        soap.Setup(x => x.ProcContrapartidasAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns((string body, CancellationToken ct) =>
            {
                sentRequests.Add(body);
                var code = body.Contains(firstTx.Reference, StringComparison.Ordinal) ? "R96" : "R10";
                return Task.FromResult($"<Envelope><Body><Proc_ContrapartidasResponse><ANSST>{code}</ANSST><ANCLC>{code}</ANCLC></Proc_ContrapartidasResponse></Body></Envelope>");
            });

        var sut = new ContrapartidaDispatchJobService(
            context,
            soap.Object,
            mapper.Object,
            new ProcContrapartidasResponseParser(),
            NullLogger<ContrapartidaDispatchJobService>.Instance,
            LiveDispatchOptions(),
            responseCatalogResolver: Catalog(
                Success("R96", "Aplicado"),
                Rejected("R10", "Rechazo funcional")),
            timeProvider: TestClock.Create());

        var result = await sut.ProcessCycleAsync(cycleId, 1, "qa-correlacion", 100, CancellationToken.None);

        Assert.Equal(2, result.Processed);
        Assert.Equal(1, result.Succeeded);
        Assert.Equal(1, result.Failed);
        Assert.Equal(0, result.Partial);
        Assert.Equal(2, result.Chunks);
        Assert.Equal(2, sentRequests.Count);
        Assert.Contains(sentRequests, x => x.Contains(firstTx.Reference) && x.Contains(firstTx.SourceAccountNumber));
        Assert.Contains(sentRequests, x => x.Contains(secondTx.Reference) && x.Contains(secondTx.SourceAccountNumber));
        Assert.DoesNotContain(sentRequests, x => x.Contains(firstTx.Reference) && x.Contains(secondTx.SourceAccountNumber));
        mapper.Verify(x => x.ResolveAsync(
            It.IsAny<AchCycle>(),
            It.Is<IReadOnlyCollection<AchTransaction>>(txs => txs.Count == 1),
            It.IsAny<DateTime>(),
            It.IsAny<CancellationToken>()), Times.Exactly(2));
        soap.Verify(x => x.ProcContrapartidasAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Exactly(2));

        var attempts = await context.ContrapartidaDispatchAttempts.ToListAsync();
        var dispatchItems = await context.ContrapartidaDispatchItems.ToListAsync();
        var firstItemId = dispatchItems.Single(x => x.AchTransactionId == transactionIds[0]).Id;
        var secondItemId = dispatchItems.Single(x => x.AchTransactionId == transactionIds[1]).Id;
        Assert.Equal(2, attempts.Count);
        Assert.Equal(2, attempts.Select(x => x.DispatchBatchId).Distinct().Count());
        var firstAttempt = Assert.Single(attempts.Where(x => x.DispatchItemId == firstItemId));
        var secondAttempt = Assert.Single(attempts.Where(x => x.DispatchItemId == secondItemId));
        Assert.Equal("R96", firstAttempt.ExternalResponseCode);
        Assert.True(firstAttempt.IsSuccessful);
        Assert.Contains(firstTx.Reference, firstAttempt.RequestPayloadXml);
        Assert.Equal("R10", secondAttempt.ExternalResponseCode);
        Assert.False(secondAttempt.IsSuccessful);
        Assert.Contains(secondTx.Reference, secondAttempt.RequestPayloadXml);
        Assert.DoesNotContain(firstTx.Reference, secondAttempt.RequestPayloadXml);

        var batches = await context.ContrapartidaDispatchBatches.ToListAsync();
        Assert.Equal(2, batches.Count);
        Assert.All(batches, batch => Assert.Equal(1, batch.TotalItems));

        var repeat = await sut.ProcessCycleAsync(cycleId, 1, "qa-no-duplicar", 100, CancellationToken.None);
        Assert.Equal(0, repeat.Processed);
        Assert.Equal(2, sentRequests.Count);
    }

    [Fact]
    public async Task ProcessCycleAsync_TimeoutIncierto_NoReintentaYRequiereConciliacion()
    {
        using var connection = new SqliteConnection("DataSource=:memory:");
        await connection.OpenAsync();
        await using var context = new AchDbContext(
            new DbContextOptionsBuilder<AchDbContext>().UseSqlite(connection).Options);
        await context.Database.EnsureCreatedAsync();
        var cycleId = await SembrarEstructuraBaseAsync(context);
        await SembrarTransaccionYItemPendienteAsync(context, cycleId);

        var mapper = new Mock<IProcContrapartidasRequestMapper>();
        mapper.Setup(x => x.ResolveAsync(
                It.IsAny<AchCycle>(),
                It.IsAny<IReadOnlyCollection<AchTransaction>>(),
                It.IsAny<DateTime>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ProcContrapartidasRequestResolution
            {
                Contract = ContratoValido(), MappingSnapshotHash = "timeout", UsedFallback = false
            });
        mapper.Setup(x => x.BuildSoapBody(It.IsAny<ProcContrapartidasRequestContract>()))
            .Returns("<request-timeout/>");

        var soap = new Mock<IWscfaachSoapClient>();
        soap.Setup(x => x.ProcContrapartidasAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new TimeoutException("Timeout de prueba, resultado desconocido"));
        var sut = new ContrapartidaDispatchJobService(
            context, soap.Object, mapper.Object, Mock.Of<IProcContrapartidasResponseParser>(),
            NullLogger<ContrapartidaDispatchJobService>.Instance,
            LiveDispatchOptions(), timeProvider: TestClock.Create());

        var result = await sut.ProcessCycleAsync(cycleId, 1, "qa-timeout", 100);
        Assert.Equal(1, result.Processed);
        Assert.Equal(1, result.Failed);
        var attempt = await context.ContrapartidaDispatchAttempts.SingleAsync();
        Assert.Equal(IntegrationTransportStatus.TimedOut, attempt.TransportStatus);
        Assert.True(attempt.RequiresManualReview);
        Assert.False(attempt.RetryEligible);
        var item = await context.ContrapartidaDispatchItems.SingleAsync();
        Assert.Equal(ContrapartidaDispatchItemStateEnum.ContrapartidaReportFailed, item.State);
        Assert.Null(item.NextAttemptAtUtc);

        var repeat = await sut.ProcessCycleAsync(cycleId, 1, "qa-timeout-replay", 100);
        Assert.Equal(0, repeat.Processed);
        var batch = await context.ContrapartidaDispatchBatches.SingleAsync();
        var retryError = await Assert.ThrowsAsync<InvalidOperationException>(() => sut.RetryBatchAsync(
            new ContrapartidaDispatchRetryRequest(batch.Id, ContrapartidaDispatchRetryScope.Full, "qa-manual", AllowReplaySucceeded: true)));
        Assert.Contains("No existen items elegibles", retryError.Message);
        soap.Verify(x => x.ProcContrapartidasAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ProcessCycleAsync_DryRun_GeneraPayloadSinInvocarSoap()
    {
        using var connection = new SqliteConnection("DataSource=:memory:");
        await connection.OpenAsync();

        var options = new DbContextOptionsBuilder<AchDbContext>()
            .UseSqlite(connection)
            .Options;

        await using var context = new AchDbContext(options);
        await context.Database.EnsureCreatedAsync();

        var cycleId = await SembrarEstructuraBaseAsync(context);
        var txId = await SembrarTransaccionYItemPendienteAsync(context, cycleId);
        await SeedIncomingNachaDispatchQueueAsync(context, cycleId, txId, "dry-run");

        var mapper = new Mock<IProcContrapartidasRequestMapper>();
        mapper
            .Setup(x => x.ResolveAsync(It.IsAny<AchCycle>(), It.IsAny<IReadOnlyCollection<AchTransaction>>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ProcContrapartidasRequestResolution
            {
                Contract = ContratoValido(),
                MappingSnapshotHash = "hash-dry-run",
                UsedFallback = false
            });
        mapper
            .Setup(x => x.BuildSoapBody(It.IsAny<ProcContrapartidasRequestContract>()))
            .Returns("<request-dry-run/>");

        var soap = new Mock<IWscfaachSoapClient>(MockBehavior.Strict);
        var parser = new Mock<IProcContrapartidasResponseParser>(MockBehavior.Strict);

        var sut = new ContrapartidaDispatchJobService(
            context,
            soap.Object,
            mapper.Object,
            parser.Object,
            NullLogger<ContrapartidaDispatchJobService>.Instance,
            Options.Create(new ProcContrapartidasDispatchOptions { Mode = "DryRun" }),
            timeProvider: TestClock.Create());

        var result = await sut.ProcessCycleAsync(cycleId, 1, "qa-soap-dry-run", 100, CancellationToken.None);

        Assert.Equal(1, result.Processed);
        Assert.Equal(0, result.Succeeded);
        Assert.Equal(1, result.Failed);

        var item = await context.ContrapartidaDispatchItems.SingleAsync();
        Assert.Equal(ContrapartidaDispatchItemStateEnum.ContrapartidaReportFailed, item.State);
        Assert.Null(item.NextAttemptAtUtc);
        Assert.Equal("PROC_DRY_RUN", item.LastErrorCode);

        var attempt = await context.ContrapartidaDispatchAttempts.SingleAsync();
        Assert.Equal(ContrapartidaDispatchAttemptResultEnum.Failed, attempt.Result);
        Assert.False(attempt.RetryEligible);
        Assert.Equal("<request-dry-run/>", attempt.RequestPayloadXml);
        Assert.Contains("dry-run", attempt.ResponsePayloadXml, StringComparison.OrdinalIgnoreCase);
        Assert.Equal("DryRun", attempt.ExecutionMode);
        Assert.Equal("PROC_DRY_RUN", attempt.SoapResponseCode);
        Assert.Equal("DryRun", attempt.SoapTechnicalStatus);
        Assert.False(attempt.IsSuccessful);
        Assert.False(attempt.IsFunctionalRejection);
        Assert.False(attempt.IsTechnicalFailure);

        var execution = await context.IncomingNachaIntegrationExecution.SingleAsync();
        Assert.Equal(IntegrationGuaranteeConstants.ProcContrapartidas, execution.MethodName);
        Assert.Equal("<request-dry-run/>", execution.RequestPayloadXml);
        Assert.Contains("dry-run", execution.ResponsePayloadXml, StringComparison.OrdinalIgnoreCase);
        Assert.Equal("PROC_DRY_RUN", execution.ResponseCode);

        soap.Verify(x => x.ProcContrapartidasAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
        parser.Verify(x => x.Parse(It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task ProcessCycleAsync_ShouldFailBeforeXml_WhenMapperResolutionUsesFallback()
    {
        using var connection = new SqliteConnection("DataSource=:memory:");
        await connection.OpenAsync();

        var options = new DbContextOptionsBuilder<AchDbContext>()
            .UseSqlite(connection)
            .Options;

        await using var context = new AchDbContext(options);
        await context.Database.EnsureCreatedAsync();

        var cycleId = await SembrarEstructuraBaseAsync(context);
        await SembrarTransaccionYItemPendienteAsync(context, cycleId);

        var mapper = new Mock<IProcContrapartidasRequestMapper>();
        mapper
            .Setup(x => x.ResolveAsync(It.IsAny<AchCycle>(), It.IsAny<IReadOnlyCollection<AchTransaction>>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ProcContrapartidasRequestResolution
            {
                Contract = ContratoValido(),
                MappingSnapshotHash = string.Empty,
                UsedFallback = true
            });

        var soap = new Mock<IWscfaachSoapClient>(MockBehavior.Strict);
        var parser = new Mock<IProcContrapartidasResponseParser>(MockBehavior.Strict);

        var sut = new ContrapartidaDispatchJobService(
            context,
            soap.Object,
            mapper.Object,
            parser.Object,
            NullLogger<ContrapartidaDispatchJobService>.Instance,
            Options.Create(new ProcContrapartidasDispatchOptions { Mode = "DryRun" }),
            timeProvider: TestClock.Create());

        var result = await sut.ProcessCycleAsync(cycleId, 1, "qa-no-fallback", 100, CancellationToken.None);

        Assert.Equal(1, result.Processed);
        Assert.Equal(1, result.Failed);

        var attempt = await context.ContrapartidaDispatchAttempts.SingleAsync();
        Assert.Equal(ContrapartidaDispatchAttemptResultEnum.Failed, attempt.Result);
        Assert.Equal(string.Empty, attempt.RequestPayloadXml);
        Assert.Contains("REQUIRED_MAPPING_USES_FALLBACK", attempt.ResponsePayloadXml);
        Assert.Equal("SOAP_EXCEPTION", attempt.SoapResponseCode);
        Assert.Equal("TechnicalException", attempt.SoapTechnicalStatus);
        Assert.True(attempt.IsTechnicalFailure);
        Assert.Contains("REQUIRED_MAPPING_USES_FALLBACK", attempt.TechnicalException);

        mapper.Verify(x => x.BuildSoapBody(It.IsAny<ProcContrapartidasRequestContract>()), Times.Never);
        soap.Verify(x => x.ProcContrapartidasAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
        parser.Verify(x => x.Parse(It.IsAny<string>()), Times.Never);
    }

    [Theory]
    [InlineData("Preparing", false)]
    [InlineData("InFlight", true)]
    public async Task E2E02_ExpiredLease_SeparatesPreSendFromUnknownAfterRestart(
        string phase, bool expectReconciliation)
    {
        using var connection = new SqliteConnection("DataSource=:memory:");
        await connection.OpenAsync();
        await using var context = new AchDbContext(new DbContextOptionsBuilder<AchDbContext>()
            .UseSqlite(connection).Options);
        await context.Database.EnsureCreatedAsync();
        var cycleId = await SembrarEstructuraBaseAsync(context);
        var txId = await SembrarTransaccionYItemPendienteAsync(context, cycleId);
        var item = await context.ContrapartidaDispatchItems.SingleAsync(x => x.AchTransactionId == txId);
        var nowUtc = new DateTime(2026, 11, 15, 20, 0, 0, DateTimeKind.Utc);
        item.State = ContrapartidaDispatchItemStateEnum.ReportingContrapartida;
        item.DispatchLeaseToken = "worker:abandoned";
        item.DispatchLeasePhase = phase;
        item.DispatchLeaseExpiresAtUtc = nowUtc.AddSeconds(-1);
        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();

        Assert.Equal(1, await ContrapartidaDispatchRecovery.RecoverStaleAsync(context, nowUtc));
        Assert.Equal(0, await ContrapartidaDispatchRecovery.RecoverStaleAsync(context, nowUtc));
        var recovered = await context.ContrapartidaDispatchItems.AsNoTracking().SingleAsync();
        Assert.Equal(expectReconciliation, recovered.RequiresReconciliation);
        Assert.Equal(expectReconciliation
            ? ContrapartidaDispatchItemStateEnum.ContrapartidaReportFailed
            : ContrapartidaDispatchItemStateEnum.RetryPending, recovered.State);
        Assert.Equal(0, recovered.AttemptCount); // Lease recovery is not an additional SOAP attempt.
        Assert.Null(recovered.DispatchLeaseExpiresAtUtc);
        Assert.Equal(expectReconciliation ? (DateTime?)null : nowUtc, recovered.NextAttemptAtUtc);
        var audit = await context.ContrapartidaDispatchAttempts.SingleAsync();
        Assert.Equal(expectReconciliation, audit.RequiresManualReview);
        Assert.Equal(!expectReconciliation, audit.RetryEligible);
        Assert.Contains("recovery:e2e02", audit.TriggeredBy);
        Assert.Equal(-1, audit.AttemptNumber); // Recovery-only audit numbering cannot consume a SOAP sequence.
    }

    [Fact]
    public async Task E2E02_RepeatedRecoveryClaimsOnce_AndOldWorkerCannotStartSoap()
    {
        using var connection = new SqliteConnection("DataSource=:memory:");
        await connection.OpenAsync();
        await using var context = new AchDbContext(new DbContextOptionsBuilder<AchDbContext>()
            .UseSqlite(connection).Options);
        await context.Database.EnsureCreatedAsync();
        var cycleId = await SembrarEstructuraBaseAsync(context);
        var txId = await SembrarTransaccionYItemPendienteAsync(context, cycleId);
        var item = await context.ContrapartidaDispatchItems.SingleAsync(x => x.AchTransactionId == txId);
        var nowUtc = new DateTime(2026, 11, 15, 20, 0, 0, DateTimeKind.Utc);
        item.State = ContrapartidaDispatchItemStateEnum.ReportingContrapartida;
        item.DispatchLeasePhase = ContrapartidaDispatchRecovery.InFlight;
        item.DispatchLeaseToken = "old-worker";
        item.DispatchLeaseExpiresAtUtc = nowUtc.AddMinutes(-1);
        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();

        // Serial recovery calls simulate overlapping Quartz instances after the same claim.
        Assert.Equal(1, await ContrapartidaDispatchRecovery.RecoverStaleAsync(context, nowUtc));
        Assert.Equal(0, await ContrapartidaDispatchRecovery.RecoverStaleAsync(context, nowUtc));
        Assert.Single(await context.ContrapartidaDispatchAttempts.ToListAsync());
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            ContrapartidaDispatchRecovery.MarkBeforeTransportAsync(
                context, [item.Id], "old-worker", nowUtc, CancellationToken.None));
        Assert.True((await context.ContrapartidaDispatchItems.AsNoTracking().SingleAsync()).RequiresReconciliation);
    }

    [Fact]
    public async Task E2E02_TwoQuartzWorkers_CannotClaimSameExpiredDispatch()
    {
        var dbFile = Path.Combine(Path.GetTempPath(), $"ach-e2e02-{Guid.NewGuid():N}.sqlite");
        var options = new DbContextOptionsBuilder<AchDbContext>()
            .UseSqlite($"Data Source={dbFile};Cache=Shared;Default Timeout=30;Pooling=False").Options;
        var nowUtc = new DateTime(2026, 11, 15, 20, 0, 0, DateTimeKind.Utc);
        try
        {
            await using (var seed = new AchDbContext(options))
            {
                await seed.Database.EnsureCreatedAsync();
                var cycleId = await SembrarEstructuraBaseAsync(seed);
                var txId = await SembrarTransaccionYItemPendienteAsync(seed, cycleId);
                var item = await seed.ContrapartidaDispatchItems.SingleAsync(x => x.AchTransactionId == txId);
                item.State = ContrapartidaDispatchItemStateEnum.ReportingContrapartida;
                item.DispatchLeasePhase = ContrapartidaDispatchRecovery.InFlight;
                item.DispatchLeaseToken = "abandoned-worker";
                item.DispatchLeaseExpiresAtUtc = nowUtc.AddMinutes(-1);
                await seed.SaveChangesAsync();
            }

            async Task<int> RecoverAsync()
            {
                await using var db = new AchDbContext(options);
                try { return await ContrapartidaDispatchRecovery.RecoverStaleAsync(db, nowUtc); }
                catch (SqliteException ex) when (ex.SqliteErrorCode is 5 or 6)
                {
                    // SQLite's coarse writer locks can reject one contender; SQL Server/PG use CAS.
                    return 0;
                }
            }
            var results = await Task.WhenAll(RecoverAsync(), RecoverAsync());
            Assert.Equal(1, results.Sum());
            await using var verify = new AchDbContext(options);
            Assert.Single(await verify.ContrapartidaDispatchAttempts.ToListAsync());
            var recovered = await verify.ContrapartidaDispatchItems.SingleAsync();
            Assert.Equal(ContrapartidaDispatchItemStateEnum.ContrapartidaReportFailed, recovered.State);
            Assert.True(recovered.RequiresReconciliation);
            Assert.Equal(0, recovered.AttemptCount);
        }
        finally
        {
            try { File.Delete(dbFile); } catch (IOException) { /* Windows may retain SQLite handles. */ }
        }
    }

    [Fact]
    public async Task E2E02_ActiveLease_PreventsPrematureRecovery_AndAdvancesBoundaryOnce()
    {
        using var connection = new SqliteConnection("DataSource=:memory:");
        await connection.OpenAsync();
        await using var context = new AchDbContext(new DbContextOptionsBuilder<AchDbContext>()
            .UseSqlite(connection).Options);
        await context.Database.EnsureCreatedAsync();
        var cycleId = await SembrarEstructuraBaseAsync(context);
        var txId = await SembrarTransaccionYItemPendienteAsync(context, cycleId);
        var item = await context.ContrapartidaDispatchItems.SingleAsync(x => x.AchTransactionId == txId);
        var nowUtc = new DateTime(2026, 11, 15, 20, 0, 0, DateTimeKind.Utc);
        item.State = ContrapartidaDispatchItemStateEnum.ReportingContrapartida;
        item.DispatchLeasePhase = ContrapartidaDispatchRecovery.Preparing;
        item.DispatchLeaseToken = "live-worker";
        item.DispatchLeaseExpiresAtUtc = nowUtc.AddMinutes(15);
        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();

        Assert.Equal(0, await ContrapartidaDispatchRecovery.RecoverStaleAsync(context, nowUtc));
        await ContrapartidaDispatchRecovery.MarkBeforeTransportAsync(
            context, [item.Id], "live-worker", nowUtc, CancellationToken.None);
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            ContrapartidaDispatchRecovery.MarkBeforeTransportAsync(
                context, [item.Id], "live-worker", nowUtc, CancellationToken.None));
        Assert.Equal(ContrapartidaDispatchRecovery.InFlight,
            (await context.ContrapartidaDispatchItems.AsNoTracking().SingleAsync()).DispatchLeasePhase);
    }

    [Fact]
    public async Task AUD_E2E_02_PhasePromotedBeforeExpiredRecovery_NeverReturnsToRetryPending()
    {
        using var connection = new SqliteConnection("DataSource=:memory:");
        await connection.OpenAsync();
        var options = new DbContextOptionsBuilder<AchDbContext>().UseSqlite(connection).Options;
        await using var db = new AchDbContext(options);
        await db.Database.EnsureCreatedAsync();
        var cycleId = await SembrarEstructuraBaseAsync(db);
        var txId = await SembrarTransaccionYItemPendienteAsync(db, cycleId);
        var item = await db.ContrapartidaDispatchItems.SingleAsync(x => x.AchTransactionId == txId);
        var now = new DateTime(2026, 11, 15, 20, 0, 0, DateTimeKind.Utc);
        item.State = ContrapartidaDispatchItemStateEnum.ReportingContrapartida;
        item.DispatchLeaseToken = "worker:phase-race";
        item.DispatchLeasePhase = ContrapartidaDispatchRecovery.Preparing;
        item.DispatchLeaseExpiresAtUtc = now.AddMinutes(1);
        await db.SaveChangesAsync();
        db.ChangeTracker.Clear();

        // A competing worker crosses the transport fence; recovery cannot treat the claim as NotSent.
        await ContrapartidaDispatchRecovery.MarkBeforeTransportAsync(
            db, [item.Id], "worker:phase-race", now, CancellationToken.None);
        await db.ContrapartidaDispatchItems.Where(x => x.Id == item.Id)
            .ExecuteUpdateAsync(u => u.SetProperty(x => x.DispatchLeaseExpiresAtUtc, now.AddSeconds(-1)));
        Assert.Equal(1, await ContrapartidaDispatchRecovery.RecoverStaleAsync(db, now));
        var result = await db.ContrapartidaDispatchItems.AsNoTracking().SingleAsync();
        Assert.Equal(ContrapartidaDispatchItemStateEnum.ContrapartidaReportFailed, result.State);
        Assert.True(result.RequiresReconciliation);
        Assert.False((await db.ContrapartidaDispatchAttempts.SingleAsync()).RetryAllowed);
    }

    [Fact]
    public async Task AUD_E2E_04_IntentCommittedBeforeSoap_RestartPreservesCorrelationHashAndMapper()
    {
        using var connection = new SqliteConnection("DataSource=:memory:");
        await connection.OpenAsync();
        var options = new DbContextOptionsBuilder<AchDbContext>().UseSqlite(connection).Options;
        await using var db = new AchDbContext(options);
        await db.Database.EnsureCreatedAsync();
        var cycleId = await SembrarEstructuraBaseAsync(db);
        var txId = await SembrarTransaccionYItemPendienteAsync(db, cycleId);
        var item = await db.ContrapartidaDispatchItems.SingleAsync(x => x.AchTransactionId == txId);
        var batch = new ContrapartidaDispatchBatch
        {
            AchCycleId = cycleId, ClearingHouseId = 1,
            Status = ContrapartidaDispatchBatchStatusEnum.Processing
        };
        db.ContrapartidaDispatchBatches.Add(batch);
        var now = new DateTime(2026, 11, 15, 20, 0, 0, DateTimeKind.Utc);
        item.State = ContrapartidaDispatchItemStateEnum.ReportingContrapartida;
        item.DispatchLeaseToken = $"scheduled:{batch.Id:N}";
        item.DispatchLeasePhase = ContrapartidaDispatchRecovery.Preparing;
        item.DispatchLeaseExpiresAtUtc = now.AddMinutes(1);
        await db.SaveChangesAsync();
        db.ChangeTracker.Clear();
        var mapperId = Guid.NewGuid();
        const string payload = "<soap><debit>1</debit></soap>";
        var hash = Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(
            System.Text.Encoding.UTF8.GetBytes(payload)));

        await ContrapartidaDispatchRecovery.PersistIntentBeforeTransportAsync(
            db, [item.Id], $"scheduled:{batch.Id:N}", batch.Id, now,
            payload, hash, mapperId, 7, "mapping-snapshot", "https://core.invalid/soap", "Live",
            CancellationToken.None);

        // Simulate process crash: reconstruct evidence using fresh tracking state, without a SOAP invocation.
        db.ChangeTracker.Clear();
        var stored = await db.ContrapartidaDispatchBatches.AsNoTracking().SingleAsync();
        Assert.Equal($"scheduled:{batch.Id:N}", stored.DispatchCorrelationId);
        Assert.Equal(hash, stored.RequestPayloadHash);
        Assert.Equal(payload, stored.RequestPayloadXml);
        Assert.Equal(mapperId, stored.MappingSetId);
        Assert.Equal(7, stored.MappingVersion);
        Assert.Equal("mapping-snapshot", stored.MappingSnapshotHash);
        Assert.Equal("Live", stored.DispatchExecutionMode);
        Assert.Equal("https://core.invalid/soap", stored.DispatchSoapEndpoint);
        Assert.Equal(now, stored.DispatchIntentAtUtc);
        Assert.Equal(ContrapartidaDispatchRecovery.InFlight,
            (await db.ContrapartidaDispatchItems.AsNoTracking().SingleAsync()).DispatchLeasePhase);

        // A repeated worker cannot create a second intent or re-enter SOAP.
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            ContrapartidaDispatchRecovery.PersistIntentBeforeTransportAsync(
                db, [item.Id], $"scheduled:{batch.Id:N}", batch.Id, now,
                payload, hash, mapperId, 7, "mapping-snapshot", "https://core.invalid/soap", "Live",
                CancellationToken.None));
        db.ChangeTracker.Clear();
        Assert.Equal(hash, (await db.ContrapartidaDispatchBatches.AsNoTracking().SingleAsync()).RequestPayloadHash);

        await db.ContrapartidaDispatchItems.Where(x => x.Id == item.Id)
            .ExecuteUpdateAsync(u => u.SetProperty(x => x.DispatchLeaseExpiresAtUtc, now.AddSeconds(-1)));
        Assert.Equal(1, await ContrapartidaDispatchRecovery.RecoverStaleAsync(db, now));
        Assert.True((await db.ContrapartidaDispatchItems.AsNoTracking().SingleAsync()).RequiresReconciliation);
        Assert.Equal(hash, (await db.ContrapartidaDispatchBatches.AsNoTracking().SingleAsync()).RequestPayloadHash);
    }

    [Fact]
    public async Task AUD_E2E_04_ManualMultiItemBatch_WritesIndependentDurableIntents()
    {
        using var connection = new SqliteConnection("DataSource=:memory:");
        await connection.OpenAsync();
        await using var db = new AchDbContext(new DbContextOptionsBuilder<AchDbContext>()
            .UseSqlite(connection).Options);
        await db.Database.EnsureCreatedAsync();
        var cycleId = await SembrarEstructuraBaseAsync(db);
        var tx1 = await SembrarTransaccionYItemPendienteAsync(db, cycleId);
        var tx2 = await SembrarTransaccionYItemPendienteAsync(db, cycleId);
        var batch = new ContrapartidaDispatchBatch
        {
            AchCycleId = cycleId, ClearingHouseId = 1, TotalItems = 2,
            Status = ContrapartidaDispatchBatchStatusEnum.Processing
        };
        db.ContrapartidaDispatchBatches.Add(batch);
        var now = new DateTime(2026, 11, 15, 20, 0, 0, DateTimeKind.Utc);
        var items = await db.ContrapartidaDispatchItems.OrderBy(x => x.Id).ToListAsync();
        var token = $"manual-retry:{batch.Id:N}";
        foreach (var item in items)
        {
            item.State = ContrapartidaDispatchItemStateEnum.Retrying;
            item.DispatchLeaseToken = token;
            item.DispatchLeasePhase = ContrapartidaDispatchRecovery.Preparing;
            item.DispatchLeaseExpiresAtUtc = now.AddMinutes(1);
        }
        await db.SaveChangesAsync();
        db.ChangeTracker.Clear();
        var mapperId = Guid.NewGuid();
        foreach (var item in items)
        {
            var xml = $"<debit id=\"{item.AchTransactionId}\"/>";
            var hash = Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(
                System.Text.Encoding.UTF8.GetBytes(xml)));
            await ContrapartidaDispatchRecovery.PersistIntentBeforeTransportAsync(
                db, [item.Id], token, batch.Id, now,
                xml, hash, mapperId, 2, "mapping-2", "https://core.invalid/soap", "Live",
                CancellationToken.None);
        }
        db.ChangeTracker.Clear();
        var persisted = await db.ContrapartidaDispatchItems.AsNoTracking().OrderBy(x => x.Id).ToListAsync();
        Assert.Equal(2, persisted.Count);
        Assert.All(persisted, x =>
        {
            Assert.Equal(ContrapartidaDispatchRecovery.InFlight, x.DispatchLeasePhase);
            Assert.Equal(token, x.DispatchIntentCorrelationId);
            Assert.Equal(2, x.DispatchIntentMappingVersion);
            Assert.Equal(mapperId, x.DispatchIntentMappingSetId);
            Assert.NotNull(x.DispatchIntentAtUtc);
            Assert.False(string.IsNullOrWhiteSpace(x.DispatchIntentPayloadHash));
        });
        Assert.NotEqual(persisted[0].DispatchIntentPayloadHash, persisted[1].DispatchIntentPayloadHash);
        Assert.Null((await db.ContrapartidaDispatchBatches.AsNoTracking().SingleAsync()).DispatchIntentAtUtc);
    }

    [Fact]
    public async Task AUD_E2E_04_ExpiredLeaseCannotWriteDispatchIntentOrTransmit()
    {
        using var connection = new SqliteConnection("DataSource=:memory:");
        await connection.OpenAsync();
        await using var db = new AchDbContext(new DbContextOptionsBuilder<AchDbContext>()
            .UseSqlite(connection).Options);
        await db.Database.EnsureCreatedAsync();
        var cycleId = await SembrarEstructuraBaseAsync(db);
        var txId = await SembrarTransaccionYItemPendienteAsync(db, cycleId);
        var item = await db.ContrapartidaDispatchItems.SingleAsync(x => x.AchTransactionId == txId);
        var batch = new ContrapartidaDispatchBatch
        {
            AchCycleId = cycleId, ClearingHouseId = 1,
            Status = ContrapartidaDispatchBatchStatusEnum.Processing
        };
        db.ContrapartidaDispatchBatches.Add(batch);
        var now = new DateTime(2026, 11, 15, 20, 0, 0, DateTimeKind.Utc);
        item.State = ContrapartidaDispatchItemStateEnum.ReportingContrapartida;
        item.DispatchLeaseToken = "expired-token";
        item.DispatchLeasePhase = ContrapartidaDispatchRecovery.Preparing;
        item.DispatchLeaseExpiresAtUtc = now.AddSeconds(-1);
        await db.SaveChangesAsync();
        db.ChangeTracker.Clear();
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            ContrapartidaDispatchRecovery.PersistIntentBeforeTransportAsync(
                db, [item.Id], "expired-token", batch.Id, now,
                "<request/>", "validhash", null, null, "snapshot", "endpoint", "Live",
                CancellationToken.None));
        Assert.Null((await db.ContrapartidaDispatchBatches.AsNoTracking().SingleAsync()).DispatchIntentAtUtc);
        Assert.Equal(ContrapartidaDispatchRecovery.Preparing,
            (await db.ContrapartidaDispatchItems.AsNoTracking().SingleAsync()).DispatchLeasePhase);
    }

    [Fact]
    public async Task AUD_E2E_03_ClosedOriginalCycle_ReassignsOnlyPreSendRecoveryToOpenCycle()
    {
        using var connection = new SqliteConnection("DataSource=:memory:");
        await connection.OpenAsync();
        await using var db = new AchDbContext(new DbContextOptionsBuilder<AchDbContext>()
            .UseSqlite(connection).Options);
        await db.Database.EnsureCreatedAsync();
        var originalCycle = await SembrarEstructuraBaseAsync(db);
        var txId = await SembrarTransaccionYItemPendienteAsync(db, originalCycle);
        var original = await db.AchCycles.SingleAsync(x => x.Id == originalCycle);
        original.OperationalStatus = AchCycleOperationalStatus.Closed;
        var newCycleId = "cccccccccccccccccccccccccccccccccccccccc";
        db.AchCycles.Add(new AchCycle
        {
            Id = newCycleId, ClearingHouseId = 1, CycleName = "CICLO-QA",
            ClearingHouseCycleConfigId = original.ClearingHouseCycleConfigId,
            ProcessingDate = TestClock.OperationalDate,
            StartTime = TimeSpan.Zero, EndTime = new TimeSpan(23, 59, 0),
            CutoffTime = new TimeSpan(23, 0, 0), OutputReleaseTime = new TimeSpan(23, 59, 0),
            OperationalStatus = AchCycleOperationalStatus.Open
        });
        var item = await db.ContrapartidaDispatchItems.SingleAsync(x => x.AchTransactionId == txId);
        item.State = ContrapartidaDispatchItemStateEnum.RetryPending;
        item.LastErrorCode = "E2E02_PRE_SEND_CRASH";
        item.DispatchLeasePhase = ContrapartidaDispatchRecovery.Idle;
        item.DispatchLeaseToken = "recovery:old";
        item.AttemptCount = 1;
        item.NextAttemptAtUtc = null;
        await db.SaveChangesAsync();
        db.ChangeTracker.Clear();

        // Mapper deliberately fails: reassignment happens before request construction; no SOAP is allowed.
        var soap = new Mock<IWscfaachSoapClient>(MockBehavior.Strict);
        var mapper = new Mock<IProcContrapartidasRequestMapper>(MockBehavior.Strict);
        var parser = new Mock<IProcContrapartidasResponseParser>(MockBehavior.Strict);
        var sut = new ContrapartidaDispatchJobService(db, soap.Object, mapper.Object, parser.Object,
            NullLogger<ContrapartidaDispatchJobService>.Instance, LiveDispatchOptions(),
            timeProvider: TestClock.Create());
        await sut.ProcessCycleAsync(newCycleId, 1, "qa-restart", 1);
        db.ChangeTracker.Clear();
        var routed = await db.ContrapartidaDispatchItems.AsNoTracking().SingleAsync();
        Assert.Equal(newCycleId, routed.AchCycleId);
        Assert.Equal(originalCycle, routed.LastReassignedFromCycleId);
        Assert.NotNull(routed.LastReassignedAtUtc);
        Assert.Equal(originalCycle,
            (await db.AchTransactions.AsNoTracking().SingleAsync()).AchCycleId);
        soap.Verify(x => x.ProcContrapartidasAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task AUD_E2E_03_DeferredOperationalCalendar_DoesNotReassignPreSendRecovery()
    {
        using var connection = new SqliteConnection("DataSource=:memory:");
        await connection.OpenAsync();
        await using var db = new AchDbContext(new DbContextOptionsBuilder<AchDbContext>()
            .UseSqlite(connection).Options);
        await db.Database.EnsureCreatedAsync();
        var sourceId = await SembrarEstructuraBaseAsync(db);
        var txId = await SembrarTransaccionYItemPendienteAsync(db, sourceId);
        var source = await db.AchCycles.SingleAsync(x => x.Id == sourceId);
        source.OperationalStatus = AchCycleOperationalStatus.Closed;
        const string targetId = "dddddddddddddddddddddddddddddddddddddddd";
        db.AchCycles.Add(new AchCycle
        {
            Id = targetId, ClearingHouseId = 1, CycleName = "CICLO-QA",
            ClearingHouseCycleConfigId = source.ClearingHouseCycleConfigId,
            ProcessingDate = TestClock.OperationalDate,
            StartTime = TimeSpan.Zero, EndTime = TimeSpan.FromHours(23),
            CutoffTime = TimeSpan.FromHours(22), OutputReleaseTime = TimeSpan.FromHours(23)
        });
        var item = await db.ContrapartidaDispatchItems.SingleAsync(x => x.AchTransactionId == txId);
        item.State = ContrapartidaDispatchItemStateEnum.RetryPending;
        item.LastErrorCode = "E2E02_PRE_SEND_CRASH";
        item.DispatchLeasePhase = ContrapartidaDispatchRecovery.Idle;
        await db.SaveChangesAsync();
        db.ChangeTracker.Clear();
        var guard = new Mock<ICycleCalendarGuard>(MockBehavior.Strict);
        guard.Setup(g => g.EnsureExecutableAsync(It.IsAny<AchCycle>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new CycleCalendarGuardResult(false, true,
                new DateOnly(2026, 7, 13), new DateOnly(2026, 7, 14), "calendario bloqueado"));
        var soap = new Mock<IWscfaachSoapClient>(MockBehavior.Strict);
        var sut = new ContrapartidaDispatchJobService(db, soap.Object,
            new Mock<IProcContrapartidasRequestMapper>(MockBehavior.Strict).Object,
            new Mock<IProcContrapartidasResponseParser>(MockBehavior.Strict).Object,
            NullLogger<ContrapartidaDispatchJobService>.Instance,
            LiveDispatchOptions(), timeProvider: TestClock.Create(), calendarGuard: guard.Object);
        var result = await sut.ProcessCycleAsync(targetId, 1, "qa-calendar", 1);
        Assert.Equal(0, result.Processed);
        Assert.Contains("diferido por calendario", result.Summary);
        Assert.Equal(sourceId,
            (await db.ContrapartidaDispatchItems.AsNoTracking().SingleAsync()).AchCycleId);
        soap.Verify(x => x.ProcContrapartidasAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task AUD_E2E_03_UnknownResult_ClosedCycleNeverReassignsOrResends()
    {
        using var connection = new SqliteConnection("DataSource=:memory:");
        await connection.OpenAsync();
        await using var db = new AchDbContext(new DbContextOptionsBuilder<AchDbContext>()
            .UseSqlite(connection).Options);
        await db.Database.EnsureCreatedAsync();
        var originalCycle = await SembrarEstructuraBaseAsync(db);
        var txId = await SembrarTransaccionYItemPendienteAsync(db, originalCycle);
        var original = await db.AchCycles.SingleAsync(x => x.Id == originalCycle);
        original.OperationalStatus = AchCycleOperationalStatus.Closed;
        var openCycleId = "cccccccccccccccccccccccccccccccccccccccc";
        db.AchCycles.Add(new AchCycle
        {
            Id = openCycleId, ClearingHouseId = 1, CycleName = "CICLO-QA",
            ClearingHouseCycleConfigId = original.ClearingHouseCycleConfigId,
            ProcessingDate = TestClock.OperationalDate,
            StartTime = TimeSpan.Zero, EndTime = new TimeSpan(23, 59, 0),
            CutoffTime = new TimeSpan(23, 0, 0), OutputReleaseTime = new TimeSpan(23, 59, 0)
        });
        var item = await db.ContrapartidaDispatchItems.SingleAsync(x => x.AchTransactionId == txId);
        item.State = ContrapartidaDispatchItemStateEnum.ContrapartidaReportFailed;
        item.RequiresReconciliation = true;
        item.DispatchLeasePhase = ContrapartidaDispatchRecovery.Idle;
        item.LastErrorCode = "E2E02_OUTCOME_UNKNOWN";
        await db.SaveChangesAsync();
        db.ChangeTracker.Clear();
        var soap = new Mock<IWscfaachSoapClient>(MockBehavior.Strict);
        var sut = new ContrapartidaDispatchJobService(db, soap.Object,
            new Mock<IProcContrapartidasRequestMapper>(MockBehavior.Strict).Object,
            new Mock<IProcContrapartidasResponseParser>(MockBehavior.Strict).Object,
            NullLogger<ContrapartidaDispatchJobService>.Instance, LiveDispatchOptions(),
            timeProvider: TestClock.Create());
        await sut.ProcessCycleAsync(openCycleId, 1, "qa-restart", 1);
        var after = await db.ContrapartidaDispatchItems.AsNoTracking().SingleAsync();
        Assert.Equal(originalCycle, after.AchCycleId);
        Assert.True(after.RequiresReconciliation);
        Assert.Empty(await db.ContrapartidaDispatchBatches.ToListAsync());
        soap.Verify(x => x.ProcContrapartidasAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(4)]
    [InlineData(5)]
    public async Task H01_ExpiredPreparingLease_PreservesSoapCounterAndHonorsLimit(int soapAttempts)
    {
        using var connection = new SqliteConnection("DataSource=:memory:");
        await connection.OpenAsync();
        await using var db = new AchDbContext(new DbContextOptionsBuilder<AchDbContext>()
            .UseSqlite(connection).Options);
        await db.Database.EnsureCreatedAsync();
        var cycleId = await SembrarEstructuraBaseAsync(db);
        await SembrarTransaccionYItemPendienteAsync(db, cycleId);
        var item = await db.ContrapartidaDispatchItems.SingleAsync();
        var now = TestClock.UtcNow.UtcDateTime;
        item.State = ContrapartidaDispatchItemStateEnum.ReportingContrapartida;
        item.AttemptCount = soapAttempts;
        item.DispatchLeasePhase = ContrapartidaDispatchRecovery.Preparing;
        item.DispatchLeaseToken = "worker:h01";
        item.DispatchLeaseExpiresAtUtc = now.AddSeconds(-1);
        await db.SaveChangesAsync();
        db.ChangeTracker.Clear();

        Assert.Equal(1, await ContrapartidaDispatchRecovery.RecoverStaleAsync(db, now));
        Assert.Equal(0, await ContrapartidaDispatchRecovery.RecoverStaleAsync(db, now));
        var result = await db.ContrapartidaDispatchItems.AsNoTracking().SingleAsync();
        Assert.Equal(soapAttempts, result.AttemptCount);
        Assert.Equal(soapAttempts == 5, result.RequiresReconciliation);
        Assert.Equal(soapAttempts == 5
            ? ContrapartidaDispatchItemStateEnum.ContrapartidaReportFailed
            : ContrapartidaDispatchItemStateEnum.RetryPending, result.State);
        Assert.Equal(soapAttempts == 5 ? (DateTime?)null : now, result.NextAttemptAtUtc);
        var audit = await db.ContrapartidaDispatchAttempts.AsNoTracking().SingleAsync();
        Assert.Equal(-1, audit.AttemptNumber);
        Assert.Equal(IntegrationTransportStatus.NotExecuted, audit.TransportStatus);
        Assert.Null(result.DispatchLeaseExpiresAtUtc);
    }

    [Fact]
    public async Task H01_LegacyRetryPendingAtLimit_QuartzRecoveryMovesToReconciliationWithoutSoap()
    {
        using var connection = new SqliteConnection("DataSource=:memory:");
        await connection.OpenAsync();
        await using var db = new AchDbContext(new DbContextOptionsBuilder<AchDbContext>()
            .UseSqlite(connection).Options);
        await db.Database.EnsureCreatedAsync();
        var cycleId = await SembrarEstructuraBaseAsync(db);
        await SembrarTransaccionYItemPendienteAsync(db, cycleId);
        var item = await db.ContrapartidaDispatchItems.SingleAsync();
        item.State = ContrapartidaDispatchItemStateEnum.RetryPending;
        item.AttemptCount = 5; // Legacy Preparing recovery incorrectly consumed the last slot.
        item.LastErrorCode = "E2E02_PRE_SEND_CRASH";
        item.DispatchLeasePhase = ContrapartidaDispatchRecovery.Idle;
        item.DispatchLeaseToken = "recovery:legacy";
        await db.SaveChangesAsync();
        db.ChangeTracker.Clear();

        Assert.Equal(1, await ContrapartidaDispatchRecovery.RecoverStaleAsync(db, TestClock.UtcNow.UtcDateTime));
        Assert.Equal(0, await ContrapartidaDispatchRecovery.RecoverStaleAsync(db, TestClock.UtcNow.UtcDateTime));
        var after = await db.ContrapartidaDispatchItems.AsNoTracking().SingleAsync();
        Assert.Equal(ContrapartidaDispatchItemStateEnum.ContrapartidaReportFailed, after.State);
        Assert.True(after.RequiresReconciliation);
        Assert.Null(after.NextAttemptAtUtc);
        Assert.Equal(5, after.AttemptCount);
        Assert.Equal("H01_RETRY_LIMIT_RECONCILIATION", after.LastErrorCode);
        var audit = await db.ContrapartidaDispatchAttempts.AsNoTracking().SingleAsync();
        Assert.Equal(-1, audit.AttemptNumber);
        Assert.Equal("recovery:h01", audit.TriggeredBy);
    }

    [Fact]
    public async Task H01_RepeatedRestart_RecoveryAuditsCannotConsumeSoapAttemptsOrCollide()
    {
        using var connection = new SqliteConnection("DataSource=:memory:");
        await connection.OpenAsync();
        await using var db = new AchDbContext(new DbContextOptionsBuilder<AchDbContext>()
            .UseSqlite(connection).Options);
        await db.Database.EnsureCreatedAsync();
        var cycleId = await SembrarEstructuraBaseAsync(db);
        await SembrarTransaccionYItemPendienteAsync(db, cycleId);
        var now = TestClock.UtcNow.UtcDateTime;
        var item = await db.ContrapartidaDispatchItems.SingleAsync();
        item.AttemptCount = 4;
        item.State = ContrapartidaDispatchItemStateEnum.ReportingContrapartida;
        item.DispatchLeasePhase = ContrapartidaDispatchRecovery.Preparing;
        item.DispatchLeaseToken = "worker:first";
        item.DispatchLeaseExpiresAtUtc = now.AddSeconds(-2);
        await db.SaveChangesAsync();
        db.ChangeTracker.Clear();
        Assert.Equal(1, await ContrapartidaDispatchRecovery.RecoverStaleAsync(db, now));
        // A new Quartz worker claims Preparation but crashes before the InFlight fence.
        await db.ContrapartidaDispatchItems.Where(x => x.AchCycleId == cycleId)
            .ExecuteUpdateAsync(u => u
                .SetProperty(x => x.State, ContrapartidaDispatchItemStateEnum.ReportingContrapartida)
                .SetProperty(x => x.DispatchLeasePhase, ContrapartidaDispatchRecovery.Preparing)
                .SetProperty(x => x.DispatchLeaseToken, "worker:second")
                .SetProperty(x => x.DispatchLeaseExpiresAtUtc, now.AddSeconds(-1)));
        Assert.Equal(1, await ContrapartidaDispatchRecovery.RecoverStaleAsync(db, now));
        Assert.Equal(4, (await db.ContrapartidaDispatchItems.AsNoTracking().SingleAsync()).AttemptCount);
        var events = await db.ContrapartidaDispatchAttempts.AsNoTracking().ToListAsync();
        Assert.Equal(2, events.Count);
        Assert.Equal(new[] { -2, -1 }, events.Select(x => x.AttemptNumber).OrderBy(x => x).ToArray());
    }

    [Fact]
    public async Task H02_ManualRetry_ClosedSourceUsesOpenPermittedCycleAndPersistsAudit()
    {
        using var connection = new SqliteConnection("DataSource=:memory:");
        await connection.OpenAsync();
        await using var db = new AchDbContext(new DbContextOptionsBuilder<AchDbContext>()
            .UseSqlite(connection).Options);
        await db.Database.EnsureCreatedAsync();
        var sourceId = await SembrarEstructuraBaseAsync(db);
        var txId = await SembrarTransaccionYItemPendienteAsync(db, sourceId);
        var source = await db.AchCycles.SingleAsync(x => x.Id == sourceId);
        source.OperationalStatus = AchCycleOperationalStatus.Closed;
        const string targetId = "eeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeee";
        db.AchCycles.Add(new AchCycle
        {
            Id = targetId, ClearingHouseId = 1, CycleName = "CICLO-QA",
            ClearingHouseCycleConfigId = source.ClearingHouseCycleConfigId,
            ProcessingDate = TestClock.OperationalDate,
            StartTime = TimeSpan.Zero, EndTime = TimeSpan.FromHours(23),
            CutoffTime = TimeSpan.FromHours(22), OutputReleaseTime = TimeSpan.FromHours(23),
            OperationalStatus = AchCycleOperationalStatus.Open
        });
        var sourceBatch = await SeedManualRetrySourceAsync(db, sourceId);
        await db.SaveChangesAsync();
        db.ChangeTracker.Clear();

        var mapper = new Mock<IProcContrapartidasRequestMapper>();
        mapper.Setup(x => x.ResolveAsync(It.IsAny<AchCycle>(), It.IsAny<IReadOnlyCollection<AchTransaction>>(),
            It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ProcContrapartidasRequestResolution
            {
                Contract = ContratoValido(), MappingSetId = Guid.NewGuid(),
                MappingVersion = 2, MappingSnapshotHash = "h02-qa", UsedFallback = false
            });
        mapper.Setup(x => x.BuildSoapBody(It.IsAny<ProcContrapartidasRequestContract>())).Returns("<manual/>");
        var soap = new Mock<IWscfaachSoapClient>(MockBehavior.Strict);
        soap.Setup(x => x.ProcContrapartidasAsync("<manual/>", It.IsAny<CancellationToken>()))
            .ReturnsAsync("<response/>");
        var parser = new Mock<IProcContrapartidasResponseParser>();
        parser.Setup(x => x.Parse("<response/>"))
            .Returns(new ProcContrapartidasParsedResponse(
                true, false, false, false, string.Empty, string.Empty, "<response/>", "R96",
                new Dictionary<int, ProcContrapartidasParsedItemResponse>
                { [txId] = new(txId, true, false, "R96", "Aplicado") }));
        var sut = new ContrapartidaDispatchJobService(db, soap.Object, mapper.Object, parser.Object,
            NullLogger<ContrapartidaDispatchJobService>.Instance, LiveDispatchOptions(),
            soapIntegrationSettingsService: SoapSettingsService("http://localhost:7083/WSCFAACH.svc"),
            responseCatalogResolver: Catalog(Success("R96", "Aplicado")),
            timeProvider: TestClock.Create());
        var result = await sut.RetryBatchAsync(new ContrapartidaDispatchRetryRequest(
            sourceBatch.Id, ContrapartidaDispatchRetryScope.FailedOnly, "qa-h02"));
        Assert.Equal(targetId, result.CycleId);
        Assert.Equal(1, result.Processed);
        Assert.Equal(1, result.Succeeded);
        var after = await db.ContrapartidaDispatchItems.AsNoTracking().SingleAsync();
        Assert.Equal(targetId, after.AchCycleId);
        Assert.Equal(sourceId, after.LastReassignedFromCycleId);
        Assert.NotNull(after.LastReassignedAtUtc);
        Assert.Equal(sourceId, (await db.AchTransactions.AsNoTracking().SingleAsync()).AchCycleId);
        Assert.Equal(targetId, (await db.ContrapartidaDispatchBatches.AsNoTracking()
            .SingleAsync(x => x.Id == result.NewBatchId)).AchCycleId);
        Assert.Equal(2, after.AttemptCount);
        Assert.Equal(ContrapartidaDispatchItemStateEnum.ReportedToContrapartida, after.State);
        soap.Verify(x => x.ProcContrapartidasAsync("<manual/>", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task H02_ManualRetry_NoOpenCycle_DefersWithoutSoap()
    {
        using var connection = new SqliteConnection("DataSource=:memory:");
        await connection.OpenAsync();
        await using var db = new AchDbContext(new DbContextOptionsBuilder<AchDbContext>()
            .UseSqlite(connection).Options);
        await db.Database.EnsureCreatedAsync();
        var sourceId = await SembrarEstructuraBaseAsync(db);
        await SembrarTransaccionYItemPendienteAsync(db, sourceId);
        var sourceBatch = await SeedManualRetrySourceAsync(db, sourceId);
        var source = await db.AchCycles.SingleAsync(x => x.Id == sourceId);
        source.OperationalStatus = AchCycleOperationalStatus.Closed;
        await db.SaveChangesAsync();
        db.ChangeTracker.Clear();
        var soap = new Mock<IWscfaachSoapClient>(MockBehavior.Strict);
        var sut = new ContrapartidaDispatchJobService(db, soap.Object,
            new Mock<IProcContrapartidasRequestMapper>(MockBehavior.Strict).Object,
            new Mock<IProcContrapartidasResponseParser>(MockBehavior.Strict).Object,
            NullLogger<ContrapartidaDispatchJobService>.Instance, LiveDispatchOptions(),
            timeProvider: TestClock.Create());
        var noCycle = await sut.RetryBatchAsync(new ContrapartidaDispatchRetryRequest(
            sourceBatch.Id, ContrapartidaDispatchRetryScope.FailedOnly, "qa-no-cycle"));
        Assert.Equal(0, noCycle.Processed);
        Assert.Contains("SOAP no invocado", noCycle.Summary);
        Assert.Single(await db.ContrapartidaDispatchBatches.ToListAsync());
        Assert.Equal(ContrapartidaDispatchItemStateEnum.RetryPending,
            (await db.ContrapartidaDispatchItems.AsNoTracking().SingleAsync()).State);
        soap.Verify(x => x.ProcContrapartidasAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task H02_ManualRetry_UnknownOutcomeNeverRetransmitsEvenWithOpenCycle()
    {
        using var connection = new SqliteConnection("DataSource=:memory:");
        await connection.OpenAsync();
        await using var db = new AchDbContext(new DbContextOptionsBuilder<AchDbContext>()
            .UseSqlite(connection).Options);
        await db.Database.EnsureCreatedAsync();
        var sourceId = await SembrarEstructuraBaseAsync(db);
        await SembrarTransaccionYItemPendienteAsync(db, sourceId);
        var sourceBatch = await SeedManualRetrySourceAsync(db, sourceId);
        var item = await db.ContrapartidaDispatchItems.SingleAsync();
        item.RequiresReconciliation = true;
        item.State = ContrapartidaDispatchItemStateEnum.ContrapartidaReportFailed;
        await db.SaveChangesAsync();
        db.ChangeTracker.Clear();
        var soap = new Mock<IWscfaachSoapClient>(MockBehavior.Strict);
        var sut = new ContrapartidaDispatchJobService(db, soap.Object,
            new Mock<IProcContrapartidasRequestMapper>(MockBehavior.Strict).Object,
            new Mock<IProcContrapartidasResponseParser>(MockBehavior.Strict).Object,
            NullLogger<ContrapartidaDispatchJobService>.Instance, LiveDispatchOptions(),
            timeProvider: TestClock.Create());
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => sut.RetryBatchAsync(
            new ContrapartidaDispatchRetryRequest(sourceBatch.Id,
                ContrapartidaDispatchRetryScope.Full, "qa-unknown")));
        Assert.Contains("No existen items elegibles", exception.Message);
        soap.Verify(x => x.ProcContrapartidasAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task H01_FourActualAttempts_RecoveryThenFinalSoapUsesFifthSlotOnce()
    {
        using var connection = new SqliteConnection("DataSource=:memory:");
        await connection.OpenAsync();
        await using var db = new AchDbContext(new DbContextOptionsBuilder<AchDbContext>()
            .UseSqlite(connection).Options);
        await db.Database.EnsureCreatedAsync();
        var cycleId = await SembrarEstructuraBaseAsync(db);
        var txId = await SembrarTransaccionYItemPendienteAsync(db, cycleId);
        var item = await db.ContrapartidaDispatchItems.SingleAsync();
        var now = TestClock.UtcNow.UtcDateTime;
        item.State = ContrapartidaDispatchItemStateEnum.ReportingContrapartida;
        item.AttemptCount = 4;
        item.DispatchLeaseToken = "worker:pre-send-crash";
        item.DispatchLeasePhase = ContrapartidaDispatchRecovery.Preparing;
        item.DispatchLeaseExpiresAtUtc = now.AddSeconds(-1);
        for (var n = 1; n <= 4; n++)
        {
            db.ContrapartidaDispatchAttempts.Add(new ContrapartidaDispatchAttempt
            {
                DispatchItemId = item.Id, AttemptNumber = n,
                StartedAtUtc = now.AddMinutes(-30 - n), FinishedAtUtc = now.AddMinutes(-29 - n),
                Result = ContrapartidaDispatchAttemptResultEnum.Failed,
                CorrelationId = $"previous-soap-{n}", TriggeredBy = "previous:worker",
                TransportStatus = IntegrationTransportStatus.Succeeded,
                BusinessStatus = IntegrationResponseBusinessStatus.Rejected
            });
        }
        await db.SaveChangesAsync();
        db.ChangeTracker.Clear();
        Assert.Equal(1, await ContrapartidaDispatchRecovery.RecoverStaleAsync(db, now));
        Assert.Equal(4, (await db.ContrapartidaDispatchItems.AsNoTracking().SingleAsync()).AttemptCount);

        var mapper = new Mock<IProcContrapartidasRequestMapper>();
        mapper.Setup(x => x.ResolveAsync(It.IsAny<AchCycle>(), It.IsAny<IReadOnlyCollection<AchTransaction>>(),
                It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ProcContrapartidasRequestResolution
            {
                Contract = ContratoValido(), MappingSetId = Guid.NewGuid(),
                MappingVersion = 1, MappingSnapshotHash = "h01-fifth", UsedFallback = false
            });
        mapper.Setup(x => x.BuildSoapBody(It.IsAny<ProcContrapartidasRequestContract>())).Returns("<final/>");
        var soap = new Mock<IWscfaachSoapClient>(MockBehavior.Strict);
        soap.Setup(x => x.ProcContrapartidasAsync("<final/>", It.IsAny<CancellationToken>()))
            .ReturnsAsync("<response/>");
        var parser = new Mock<IProcContrapartidasResponseParser>();
        parser.Setup(x => x.Parse("<response/>"))
            .Returns(new ProcContrapartidasParsedResponse(
                true, false, false, false, string.Empty, string.Empty, "<response/>", "R96",
                new Dictionary<int, ProcContrapartidasParsedItemResponse>
                { [txId] = new(txId, true, false, "R96", "Aplicado") }));
        var sut = new ContrapartidaDispatchJobService(db, soap.Object, mapper.Object, parser.Object,
            NullLogger<ContrapartidaDispatchJobService>.Instance, LiveDispatchOptions(),
            soapIntegrationSettingsService: SoapSettingsService("http://localhost:7083/WSCFAACH.svc"),
            responseCatalogResolver: Catalog(Success("R96", "Aplicado")),
            timeProvider: TestClock.Create());
        var result = await sut.ProcessCycleAsync(cycleId, 1, "h01-fifth", 1);
        Assert.Equal(1, result.Processed);
        Assert.Equal(1, result.Succeeded);
        Assert.Equal(5, (await db.ContrapartidaDispatchItems.AsNoTracking().SingleAsync()).AttemptCount);
        Assert.Equal(ContrapartidaDispatchItemStateEnum.ReportedToContrapartida,
            (await db.ContrapartidaDispatchItems.AsNoTracking().SingleAsync()).State);
        var numbers = await db.ContrapartidaDispatchAttempts.AsNoTracking()
            .OrderBy(x => x.AttemptNumber).Select(x => x.AttemptNumber).ToListAsync();
        Assert.Equal(new[] { -1, 1, 2, 3, 4, 5 }, numbers);
        Assert.Equal(0, (await sut.ProcessCycleAsync(cycleId, 1, "h01-restart", 1)).Processed);
        soap.Verify(x => x.ProcContrapartidasAsync("<final/>", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task H02_ManualRetry_BlockedCalendarWithOpenCycle_DefersWithoutSoap()
    {
        using var connection = new SqliteConnection("DataSource=:memory:");
        await connection.OpenAsync();
        await using var db = new AchDbContext(new DbContextOptionsBuilder<AchDbContext>()
            .UseSqlite(connection).Options);
        await db.Database.EnsureCreatedAsync();
        var sourceId = await SembrarEstructuraBaseAsync(db);
        await SembrarTransaccionYItemPendienteAsync(db, sourceId);
        var sourceBatch = await SeedManualRetrySourceAsync(db, sourceId);
        db.ChangeTracker.Clear();
        var guard = new Mock<ICycleCalendarGuard>(MockBehavior.Strict);
        guard.Setup(x => x.EnsureExecutableAsync(It.IsAny<AchCycle>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new CycleCalendarGuardResult(false, true,
                DateOnly.FromDateTime(TestClock.OperationalDate),
                DateOnly.FromDateTime(TestClock.OperationalDate.AddDays(3)),
                "Calendario bancario cerrado"));
        var soap = new Mock<IWscfaachSoapClient>(MockBehavior.Strict);
        var sut = new ContrapartidaDispatchJobService(db, soap.Object,
            new Mock<IProcContrapartidasRequestMapper>(MockBehavior.Strict).Object,
            new Mock<IProcContrapartidasResponseParser>(MockBehavior.Strict).Object,
            NullLogger<ContrapartidaDispatchJobService>.Instance, LiveDispatchOptions(),
            timeProvider: TestClock.Create(), calendarGuard: guard.Object);
        var result = await sut.RetryBatchAsync(new ContrapartidaDispatchRetryRequest(
            sourceBatch.Id, ContrapartidaDispatchRetryScope.FailedOnly, "qa-holiday"));
        Assert.Equal(0, result.Processed);
        Assert.Equal(Guid.Empty, result.NewBatchId);
        Assert.Equal(ContrapartidaDispatchItemStateEnum.RetryPending,
            (await db.ContrapartidaDispatchItems.AsNoTracking().SingleAsync()).State);
        Assert.Single(await db.ContrapartidaDispatchBatches.AsNoTracking().ToListAsync());
        soap.Verify(x => x.ProcContrapartidasAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task H02_CompetingManualWorkers_CannotBothClaimTheSameDebit()
    {
        var dbFile = Path.Combine(Path.GetTempPath(), $"h02-concurrency-{Guid.NewGuid():N}.sqlite");
        var options = new DbContextOptionsBuilder<AchDbContext>()
            .UseSqlite($"Data Source={dbFile};Cache=Shared;Default Timeout=30;Pooling=False").Options;
        try
        {
            Guid sourceBatchId;
            await using (var seed = new AchDbContext(options))
            {
                await seed.Database.EnsureCreatedAsync();
                var sourceCycleId = await SembrarEstructuraBaseAsync(seed);
                await SembrarTransaccionYItemPendienteAsync(seed, sourceCycleId);
                var sourceBatch = await SeedManualRetrySourceAsync(seed, sourceCycleId);
                sourceBatchId = sourceBatch.Id;
            }
            var soap = new Mock<IWscfaachSoapClient>();
            soap.Setup(x => x.ProcContrapartidasAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync("<response/>");
            var mapper = new Mock<IProcContrapartidasRequestMapper>();
            mapper.Setup(x => x.ResolveAsync(It.IsAny<AchCycle>(), It.IsAny<IReadOnlyCollection<AchTransaction>>(),
                    It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new ProcContrapartidasRequestResolution
                {
                    Contract = ContratoValido(), MappingSetId = Guid.NewGuid(),
                    MappingVersion = 1, MappingSnapshotHash = "h02-race", UsedFallback = false
                });
            mapper.Setup(x => x.BuildSoapBody(It.IsAny<ProcContrapartidasRequestContract>()))
                .Returns("<concurrent/>");
            var parser = new Mock<IProcContrapartidasResponseParser>();
            parser.Setup(x => x.Parse("<response/>"))
                .Returns(new ProcContrapartidasParsedResponse(
                    false, false, false, true, string.Empty, "Rechazo funcional", "<response/>", "R01",
                    new Dictionary<int, ProcContrapartidasParsedItemResponse>()));
            async Task TryRetryAsync()
            {
                await using var db = new AchDbContext(options);
                var sut = new ContrapartidaDispatchJobService(db, soap.Object, mapper.Object, parser.Object,
                    NullLogger<ContrapartidaDispatchJobService>.Instance, LiveDispatchOptions(),
                    soapIntegrationSettingsService: SoapSettingsService("http://localhost:7083/WSCFAACH.svc"),
                    responseCatalogResolver: Catalog(Rejected("R01", "Rechazo funcional")),
                    timeProvider: TestClock.Create());
                try
                {
                    await sut.RetryBatchAsync(new ContrapartidaDispatchRetryRequest(
                        sourceBatchId, ContrapartidaDispatchRetryScope.FailedOnly, "qa-race"));
                }
                catch (InvalidOperationException) { /* Second worker lost CAS or saw active batch. */ }
                catch (SqliteException ex) when (ex.SqliteErrorCode is 5 or 6)
                { /* SQLite uses a database-wide writer lock instead of row-level CAS. */ }
                catch (DbUpdateException ex) when (ex.InnerException is SqliteException sql
                    && sql.SqliteErrorCode is 5 or 6)
                { /* Provider-level lock outcome is an acceptable losing worker. */ }
            }
            await Task.WhenAll(TryRetryAsync(), TryRetryAsync());
            await using var verify = new AchDbContext(options);
            var finalItem = await verify.ContrapartidaDispatchItems.AsNoTracking().SingleAsync();
            Assert.Equal(2, finalItem.AttemptCount);
            Assert.Equal(1, await verify.ContrapartidaDispatchAttempts.CountAsync(x => x.AttemptNumber == 2));
            soap.Verify(x => x.ProcContrapartidasAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Once);
        }
        finally
        {
            try { File.Delete(dbFile); } catch (IOException) { /* Windows file handle may persist. */ }
        }
    }

    [Fact]
    public async Task H02_ManualRetry_CycleClosesAfterFirstSoap_LeavesRemainingDebitUnsent()
    {
        using var connection = new SqliteConnection("DataSource=:memory:");
        await connection.OpenAsync();
        await using var db = new AchDbContext(new DbContextOptionsBuilder<AchDbContext>()
            .UseSqlite(connection).Options);
        await db.Database.EnsureCreatedAsync();
        var sourceId = await SembrarEstructuraBaseAsync(db);
        await SembrarTransaccionYItemPendienteAsync(db, sourceId);
        await SembrarTransaccionYItemPendienteAsync(db, sourceId);
        var source = await db.AchCycles.SingleAsync(x => x.Id == sourceId);
        source.OperationalStatus = AchCycleOperationalStatus.Closed;
        const string targetId = "ffffffffffffffffffffffffffffffffffffffff";
        db.AchCycles.Add(new AchCycle
        {
            Id = targetId, ClearingHouseId = 1, CycleName = "CICLO-QA",
            ClearingHouseCycleConfigId = source.ClearingHouseCycleConfigId,
            ProcessingDate = TestClock.OperationalDate,
            StartTime = TimeSpan.Zero, EndTime = TimeSpan.FromHours(23),
            CutoffTime = TimeSpan.FromHours(22), OutputReleaseTime = TimeSpan.FromHours(23),
            OperationalStatus = AchCycleOperationalStatus.Open
        });
        var batch = new ContrapartidaDispatchBatch
        {
            AchCycleId = sourceId, ClearingHouseId = 1,
            Status = ContrapartidaDispatchBatchStatusEnum.Failed,
            TotalItems = 2, TotalFailed = 2
        };
        db.ContrapartidaDispatchBatches.Add(batch);
        var items = await db.ContrapartidaDispatchItems.OrderBy(x => x.Id).ToListAsync();
        foreach (var item in items)
        {
            item.AttemptCount = 1;
            item.State = ContrapartidaDispatchItemStateEnum.RetryPending;
            item.DispatchLeasePhase = ContrapartidaDispatchRecovery.Idle;
            db.ContrapartidaDispatchAttempts.Add(new ContrapartidaDispatchAttempt
            {
                DispatchItemId = item.Id, DispatchBatchId = batch.Id,
                AttemptNumber = 1, StartedAtUtc = TestClock.UtcNow.UtcDateTime.AddMinutes(-2),
                FinishedAtUtc = TestClock.UtcNow.UtcDateTime.AddMinutes(-1),
                Result = ContrapartidaDispatchAttemptResultEnum.Failed,
                CorrelationId = $"previous-{item.Id}", TriggeredBy = "seed:previous",
                RetryEligible = true, RetryAllowed = true,
                TransportStatus = IntegrationTransportStatus.Succeeded,
                BusinessStatus = IntegrationResponseBusinessStatus.Rejected
            });
        }
        await db.SaveChangesAsync();
        db.ChangeTracker.Clear();
        var mapper = new Mock<IProcContrapartidasRequestMapper>();
        mapper.Setup(x => x.ResolveAsync(It.IsAny<AchCycle>(), It.IsAny<IReadOnlyCollection<AchTransaction>>(),
            It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ProcContrapartidasRequestResolution
            {
                Contract = ContratoValido(), MappingSetId = Guid.NewGuid(),
                MappingVersion = 2, MappingSnapshotHash = "h02-boundary", UsedFallback = false
            });
        mapper.Setup(x => x.BuildSoapBody(It.IsAny<ProcContrapartidasRequestContract>()))
            .Returns("<one-request/>");
        var soap = new Mock<IWscfaachSoapClient>();
        soap.Setup(x => x.ProcContrapartidasAsync("<one-request/>", It.IsAny<CancellationToken>()))
            .Returns(async () =>
            {
                // Deterministic operational status switch while a multi-item retry runs.
                await db.AchCycles.Where(x => x.Id == targetId)
                    .ExecuteUpdateAsync(u => u.SetProperty(x => x.OperationalStatus,
                        AchCycleOperationalStatus.Closed));
                return "<response/>";
            });
        var parser = new Mock<IProcContrapartidasResponseParser>();
        parser.Setup(x => x.Parse("<response/>"))
            .Returns(new ProcContrapartidasParsedResponse(
                true, false, false, false, string.Empty, string.Empty, "<response/>", "R96",
                new Dictionary<int, ProcContrapartidasParsedItemResponse>()));
        var sut = new ContrapartidaDispatchJobService(db, soap.Object, mapper.Object, parser.Object,
            NullLogger<ContrapartidaDispatchJobService>.Instance, LiveDispatchOptions(),
            soapIntegrationSettingsService: SoapSettingsService("http://localhost:7083/WSCFAACH.svc"),
            responseCatalogResolver: Catalog(Success("R96", "Aplicado")),
            timeProvider: TestClock.Create());
        var result = await sut.RetryBatchAsync(new ContrapartidaDispatchRetryRequest(
            batch.Id, ContrapartidaDispatchRetryScope.FailedOnly, "qa-midbatch-close"));
        Assert.Equal(2, result.Selected);
        Assert.Equal(1, result.Processed);
        Assert.Equal(1, result.Succeeded);
        Assert.Contains("pending=1", result.Summary);
        var all = await db.ContrapartidaDispatchItems.AsNoTracking().OrderBy(x => x.Id).ToListAsync();
        Assert.Single(all.Where(x => x.State == ContrapartidaDispatchItemStateEnum.ReportedToContrapartida));
        Assert.Single(all.Where(x => x.State == ContrapartidaDispatchItemStateEnum.RetryPending));
        Assert.Equal(ContrapartidaDispatchBatchStatusEnum.CompletedWithErrors,
            (await db.ContrapartidaDispatchBatches.AsNoTracking().SingleAsync(x => x.Id == result.NewBatchId)).Status);
        soap.Verify(x => x.ProcContrapartidasAsync("<one-request/>", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task RES_H02_ScheduledCycleClosesDuringMappingOrAfterIntent_NoSoapAndNoAttempt(bool afterIntent)
    {
        using var connection = new SqliteConnection("DataSource=:memory:");
        await connection.OpenAsync();
        await using var db = new AchDbContext(new DbContextOptionsBuilder<AchDbContext>()
            .UseSqlite(connection).Options);
        await db.Database.EnsureCreatedAsync();
        var cycleId = await SembrarEstructuraBaseAsync(db);
        await SembrarTransaccionYItemPendienteAsync(db, cycleId);
        var mapper = new Mock<IProcContrapartidasRequestMapper>();
        mapper.Setup(x => x.ResolveAsync(It.IsAny<AchCycle>(),
                It.IsAny<IReadOnlyCollection<AchTransaction>>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .Returns(async () =>
            {
                if (!afterIntent)
                    await db.AchCycles.Where(x => x.Id == cycleId).ExecuteUpdateAsync(u =>
                        u.SetProperty(x => x.OperationalStatus, AchCycleOperationalStatus.Closed));
                return new ProcContrapartidasRequestResolution
                {
                    Contract = ContratoValido(), MappingVersion = 5,
                    MappingSnapshotHash = "res-h02-test", UsedFallback = false
                };
            });
        mapper.Setup(x => x.BuildSoapBody(It.IsAny<ProcContrapartidasRequestContract>()))
            .Returns("<request/>");
        var soap = new Mock<IWscfaachSoapClient>(MockBehavior.Strict);
        var parser = new Mock<IProcContrapartidasResponseParser>(MockBehavior.Strict);
        var calendar = new Mock<ICycleCalendarGuard>();
        var guardCalls = 0;
        calendar.Setup(x => x.EnsureExecutableAsync(It.IsAny<AchCycle>(), It.IsAny<CancellationToken>()))
            .Returns(async () =>
            {
                guardCalls++;
                if (afterIntent && guardCalls == 3)
                    await db.AchCycles.Where(x => x.Id == cycleId).ExecuteUpdateAsync(u =>
                        u.SetProperty(x => x.OperationalStatus, AchCycleOperationalStatus.Closed));
                return new CycleCalendarGuardResult(true, false,
                    DateOnly.FromDateTime(TestClock.OperationalDate), null, "Operable");
            });
        var sut = new ContrapartidaDispatchJobService(db, soap.Object, mapper.Object, parser.Object,
            NullLogger<ContrapartidaDispatchJobService>.Instance, LiveDispatchOptions(),
            calendarGuard: calendar.Object, timeProvider: TestClock.Create());
        var result = await sut.ProcessCycleAsync(cycleId, 1, "qa-res-h02-close", 1);
        db.ChangeTracker.Clear();
        var item = await db.ContrapartidaDispatchItems.AsNoTracking().SingleAsync();
        Assert.Equal(0, result.Processed);
        Assert.Equal(ContrapartidaDispatchItemStateEnum.RetryPending, item.State);
        Assert.Equal(0, item.AttemptCount);
        Assert.False(item.RequiresReconciliation);
        Assert.Equal(ContrapartidaDispatchRecovery.UnsentCycleCode, item.LastErrorCode);
        Assert.Equal(ContrapartidaDispatchRecovery.Idle, item.DispatchLeasePhase);
        Assert.Equal(afterIntent, item.DispatchIntentAtUtc.HasValue);
        Assert.Equal(ContrapartidaDispatchBatchStatusEnum.Cancelled,
            (await db.ContrapartidaDispatchBatches.AsNoTracking().SingleAsync()).Status);
        var audit = await db.ContrapartidaDispatchAttempts.AsNoTracking().SingleAsync();
        Assert.True(audit.AttemptNumber < 0);
        Assert.Equal(IntegrationTransportStatus.NotExecuted, audit.TransportStatus);
        Assert.Equal("NotSent", audit.SoapTechnicalStatus);
        soap.Verify(x => x.ProcContrapartidasAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task RES_H02_ManualCycleClosesDuringPreparationOrAtFinalFence_SkipsDebit(bool afterIntent)
    {
        using var connection = new SqliteConnection("DataSource=:memory:");
        await connection.OpenAsync();
        await using var db = new AchDbContext(new DbContextOptionsBuilder<AchDbContext>()
            .UseSqlite(connection).Options);
        await db.Database.EnsureCreatedAsync();
        var cycleId = await SembrarEstructuraBaseAsync(db);
        await SembrarTransaccionYItemPendienteAsync(db, cycleId);
        var source = await SeedManualRetrySourceAsync(db, cycleId);
        db.ChangeTracker.Clear();
        var mapper = new Mock<IProcContrapartidasRequestMapper>();
        mapper.Setup(x => x.ResolveAsync(It.IsAny<AchCycle>(),
                It.IsAny<IReadOnlyCollection<AchTransaction>>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .Returns(async () =>
            {
                if (!afterIntent)
                    await db.AchCycles.Where(x => x.Id == cycleId).ExecuteUpdateAsync(u =>
                        u.SetProperty(x => x.OperationalStatus, AchCycleOperationalStatus.Closed));
                return new ProcContrapartidasRequestResolution
                {
                    Contract = ContratoValido(), MappingVersion = 5,
                    MappingSnapshotHash = "res-h02-manual", UsedFallback = false
                };
            });
        mapper.Setup(x => x.BuildSoapBody(It.IsAny<ProcContrapartidasRequestContract>()))
            .Returns("<manual/>");
        var soap = new Mock<IWscfaachSoapClient>(MockBehavior.Strict);
        var parser = new Mock<IProcContrapartidasResponseParser>(MockBehavior.Strict);
        var calendar = new Mock<ICycleCalendarGuard>();
        var guardCalls = 0;
        calendar.Setup(x => x.EnsureExecutableAsync(It.IsAny<AchCycle>(), It.IsAny<CancellationToken>()))
            .Returns(async () =>
            {
                guardCalls++;
                if (afterIntent && guardCalls == 5)
                    await db.AchCycles.Where(x => x.Id == cycleId).ExecuteUpdateAsync(u =>
                        u.SetProperty(x => x.OperationalStatus, AchCycleOperationalStatus.Closed));
                return new CycleCalendarGuardResult(true, false,
                    DateOnly.FromDateTime(TestClock.OperationalDate), null, "Operable");
            });
        var sut = new ContrapartidaDispatchJobService(db, soap.Object, mapper.Object, parser.Object,
            NullLogger<ContrapartidaDispatchJobService>.Instance, LiveDispatchOptions(),
            calendarGuard: calendar.Object, timeProvider: TestClock.Create());
        var result = await sut.RetryBatchAsync(new ContrapartidaDispatchRetryRequest(
            source.Id, ContrapartidaDispatchRetryScope.FailedOnly, "qa-res-h02"));
        db.ChangeTracker.Clear();
        var item = await db.ContrapartidaDispatchItems.AsNoTracking().SingleAsync();
        Assert.Equal(1, result.Selected);
        Assert.Equal(0, result.Processed);
        Assert.Equal(1, item.AttemptCount); // Only the original verified SOAP attempt.
        Assert.Equal(ContrapartidaDispatchItemStateEnum.RetryPending, item.State);
        Assert.Equal(ContrapartidaDispatchRecovery.UnsentCycleCode, item.LastErrorCode);
        Assert.False(item.RequiresReconciliation);
        Assert.Equal(afterIntent, item.DispatchIntentAtUtc.HasValue);
        Assert.Single(await db.ContrapartidaDispatchAttempts.AsNoTracking()
            .Where(a => a.AttemptNumber < 0).ToListAsync());
        soap.Verify(x => x.ProcContrapartidasAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task RES_H02_OperationalWindowExpiresDuringMapping_SkipsSoap()
    {
        using var connection = new SqliteConnection("DataSource=:memory:");
        await connection.OpenAsync();
        await using var db = new AchDbContext(new DbContextOptionsBuilder<AchDbContext>()
            .UseSqlite(connection).Options);
        await db.Database.EnsureCreatedAsync();
        var cycleId = await SembrarEstructuraBaseAsync(db);
        var cycle = await db.AchCycles.SingleAsync(x => x.Id == cycleId);
        cycle.EndTime = new TimeSpan(12, 1, 0); // Clock begins at 12:00 America/Bogota.
        await db.SaveChangesAsync();
        await SembrarTransaccionYItemPendienteAsync(db, cycleId);
        var clock = new AdvancingDispatchTimeProvider();
        var mapper = new Mock<IProcContrapartidasRequestMapper>();
        mapper.Setup(x => x.ResolveAsync(It.IsAny<AchCycle>(),
                It.IsAny<IReadOnlyCollection<AchTransaction>>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .Returns(() =>
            {
                clock.Advance(TimeSpan.FromMinutes(2));
                return Task.FromResult(new ProcContrapartidasRequestResolution
                {
                    Contract = ContratoValido(), MappingVersion = 5,
                    MappingSnapshotHash = "res-h02-window", UsedFallback = false
                });
            });
        mapper.Setup(x => x.BuildSoapBody(It.IsAny<ProcContrapartidasRequestContract>()))
            .Returns("<request/>");
        var soap = new Mock<IWscfaachSoapClient>(MockBehavior.Strict);
        var sut = new ContrapartidaDispatchJobService(db, soap.Object, mapper.Object,
            new Mock<IProcContrapartidasResponseParser>(MockBehavior.Strict).Object,
            NullLogger<ContrapartidaDispatchJobService>.Instance, LiveDispatchOptions(),
            timeProvider: clock);
        var result = await sut.ProcessCycleAsync(cycleId, 1, "qa-res-h02-window", 1);
        db.ChangeTracker.Clear();
        Assert.Equal(0, result.Processed);
        var item = await db.ContrapartidaDispatchItems.AsNoTracking().SingleAsync();
        Assert.Equal(ContrapartidaDispatchItemStateEnum.RetryPending, item.State);
        Assert.Equal(0, item.AttemptCount);
        Assert.Equal(ContrapartidaDispatchRecovery.UnsentCycleCode, item.LastErrorCode);
        soap.Verify(x => x.ProcContrapartidasAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    private sealed class AdvancingDispatchTimeProvider : TimeProvider
    {
        private DateTimeOffset _now = TestClock.UtcNow;
        public void Advance(TimeSpan by) => _now = _now.Add(by);
        public override DateTimeOffset GetUtcNow() => _now;
        public override TimeZoneInfo LocalTimeZone => TestClock.BogotaTimeZone;
    }

    [Fact]
    public async Task RES_H02_CycleReassignedBetweenSelectionAndClaim_CasRejectsStaleWorker()
    {
        // Provider-executed CAS model for the dispatcher race. Both providers
        // must use the same AchCycleId predicate in the real ExecuteUpdate.
        using var connection = new SqliteConnection("DataSource=:memory:");
        await connection.OpenAsync();
        await using var db = new AchDbContext(new DbContextOptionsBuilder<AchDbContext>()
            .UseSqlite(connection).Options);
        await db.Database.EnsureCreatedAsync();
        var previousCycle = await SembrarEstructuraBaseAsync(db);
        await SembrarTransaccionYItemPendienteAsync(db, previousCycle);
        var item = await db.ContrapartidaDispatchItems.AsNoTracking().SingleAsync();
        var newCycle = "res-h02-reassigned-cycle";
        db.AchCycles.Add(new AchCycle
        {
            Id = newCycle, ClearingHouseId = 1, CycleName = "CICLO-QA",
            ProcessingDate = TestClock.OperationalDate, StartTime = TimeSpan.Zero,
            EndTime = TimeSpan.FromHours(23), CutoffTime = TimeSpan.FromHours(22),
            OutputReleaseTime = TimeSpan.FromHours(23), OperationalStatus = AchCycleOperationalStatus.Open
        });
        await db.SaveChangesAsync();
        await db.ContrapartidaDispatchItems.Where(x => x.Id == item.Id && x.AchCycleId == previousCycle)
            .ExecuteUpdateAsync(u => u.SetProperty(x => x.AchCycleId, newCycle));
        var lostClaim = await db.ContrapartidaDispatchItems
            .Where(x => x.Id == item.Id && x.AchCycleId == previousCycle
                && x.ClearingHouseId == 1 && x.DispatchLeasePhase == ContrapartidaDispatchRecovery.Idle)
            .ExecuteUpdateAsync(u => u.SetProperty(x => x.DispatchLeaseToken, "obsolete-worker"));
        Assert.Equal(0, lostClaim);
        var persisted = await db.ContrapartidaDispatchItems.AsNoTracking().SingleAsync();
        Assert.Equal(newCycle, persisted.AchCycleId);
        Assert.NotEqual("obsolete-worker", persisted.DispatchLeaseToken);
    }

    private static async Task<ContrapartidaDispatchBatch> SeedManualRetrySourceAsync(
        AchDbContext db, string cycleId)
    {
        var item = await db.ContrapartidaDispatchItems.SingleAsync();
        item.AttemptCount = 1;
        item.State = ContrapartidaDispatchItemStateEnum.RetryPending;
        item.DispatchLeasePhase = ContrapartidaDispatchRecovery.Idle;
        item.RequiresReconciliation = false;
        item.NextAttemptAtUtc = null;
        var batch = new ContrapartidaDispatchBatch
        {
            AchCycleId = cycleId, ClearingHouseId = 1,
            Status = ContrapartidaDispatchBatchStatusEnum.Failed,
            TotalItems = 1, TotalFailed = 1
        };
        db.ContrapartidaDispatchBatches.Add(batch);
        db.ContrapartidaDispatchAttempts.Add(new ContrapartidaDispatchAttempt
        {
            DispatchItemId = item.Id,
            DispatchBatchId = batch.Id,
            AttemptNumber = 1,
            StartedAtUtc = TestClock.UtcNow.UtcDateTime.AddMinutes(-2),
            FinishedAtUtc = TestClock.UtcNow.UtcDateTime.AddMinutes(-1),
            Result = ContrapartidaDispatchAttemptResultEnum.Failed,
            CorrelationId = "prior-verified-rejection",
            TriggeredBy = "seed:previous",
            RetryEligible = true,
            RetryAllowed = true,
            TransportStatus = IntegrationTransportStatus.Succeeded,
            BusinessStatus = IntegrationResponseBusinessStatus.Rejected,
            SoapResponseCode = "R01",
            SoapMethodName = "Proc_Contrapartidas"
        });
        await db.SaveChangesAsync();
        return batch;
    }

    private static async Task<string> SembrarEstructuraBaseAsync(AchDbContext context)
    {
        context.ClearingHouseConfigs.Add(new ClearingHouseConfig
        {
            Id = 1,
            HolidayStrategy = "Colombian",
            TimeZoneId = "America/Bogota"
        });

        context.ClearingHouses.Add(new ClearingHouse
        {
            Id = 1,
            Name = "ACH Colombia",
            Code = "ACH",
            OriginCode = "12345678",
            ClearingHouseId = 1
        });

        var cycleConfig = new ClearingHouseCycleConfig
        {
            ClearingHouseId = 1,
            PolicyVersion = "TEST-V1",
            CycleName = "CICLO-QA",
            StartTime = TimeSpan.Zero,
            EndTime = new TimeSpan(23, 59, 0),
            CutoffTime = new TimeSpan(23, 0, 0),
            OutputReleaseTime = new TimeSpan(23, 59, 0),
            EffectiveFrom = TestClock.OperationalDate,
            EffectiveTo = TestClock.OperationalDate,
            IsActive = true
        };
        context.ClearingHouseCycleConfigs.Add(cycleConfig);
        await context.SaveChangesAsync();

        var cycleId = "bbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbb";
        context.AchCycles.Add(new AchCycle
        {
            Id = cycleId,
            ClearingHouseId = 1,
            CycleName = "CICLO-QA",
            ProcessingDate = TestClock.OperationalDate,
            StartTime = TimeSpan.Zero,
            EndTime = new TimeSpan(23, 59, 0),
            CutoffTime = new TimeSpan(23, 0, 0),
            OutputReleaseTime = new TimeSpan(23, 59, 0),
            ClearingHouseCycleConfigId = cycleConfig.Id
        });

        var sourceFi = new FinancialInstitution
        {
            Id = 1,
            Name = "Banco Origen",
            Status = FinancialInstitutionStatus.Active,
            IsDefaultSource = true,
            RoutingNumber = "12345",
            TransitCode = "678"
        };
        sourceFi.CalculateCheckDigit();

        var destinationFi = new FinancialInstitution
        {
            Id = 2,
            Name = "Banco Destino",
            Status = FinancialInstitutionStatus.Active,
            IsDefaultSource = false,
            RoutingNumber = "76543",
            TransitCode = "210"
        };
        destinationFi.CalculateCheckDigit();

        context.FinancialInstitutions.AddRange(sourceFi, destinationFi);

        await context.SaveChangesAsync();
        return cycleId;
    }

    private static async Task<int> SembrarTransaccionYItemPendienteAsync(AchDbContext context, string cycleId)
    {
        var consecutivo = await context.AchTransactions.CountAsync() + 1;
        var sufijo = consecutivo.ToString("D6");

        var companyEntryDescriptionId = await context.CompanyEntryDescriptionCatalogs
            .Where(x => x.Term == "NOMINAS" && x.IsActive)
            .Select(x => x.Id)
            .FirstAsync();

        var batch = new AchBatch
        {
            AchCycleId = cycleId,
            CompanyName = "EMPRESA",
            CompanyIdentification = "900123456",
            CompanyEntryDescription = "NOMINAS",
            CompanyEntryDescriptionId = companyEntryDescriptionId,
            OriginOrOdfi = "12345678",
            EffectiveEntryDate = TestClock.OperationalDate,
            ServiceClassCode = "220",
            BatchSequenceNumber = 1
        };

        var tx = new AchTransaction
        {
            Amount = 1000m,
            TransactionExternalId = $"TX-CP-{sufijo}",
            Reference = $"REF-CP-{sufijo}",
            Type = TransactionTypeEnum.Debit,
            TransactionCode = "27",
            ServiceClassCode = "220",
            CompanyEntryDescriptionId = companyEntryDescriptionId,
            CompanyName = "EMPRESA",
            CompanyIdentification = "900123456",
            OriginatingDFI = "123456780",
            ReceivingDFI = "765432100",
            TraceNumber = $"12345678{sufijo}",
            TraceSequenceNumber = consecutivo,
            EffectiveEntryDate = TestClock.OperationalDate,
            AddendaRecordIndicator = true,
            IsPrenotification = false,
            SourceAccountNumber = "111122223333",
            DestinationAccountNumber = "999988887777",
            SourceInstitutionId = 1,
            DestinationInstitutionId = 2,
            AchCycleId = cycleId,
            AchBatch = batch,
            Direction = AchTransactionDirection.Outgoing,
            Origin = AchTransactionOrigin.Cfa,
            MonetaryIntegrationRoute = AchMonetaryIntegrationRoute.ProcContrapartidas,
            ClassificationStatus = AchTransactionClassificationStatus.Determined,
            SourceInstitutionWasDefaultAtCreation = true,
            ClassifiedAtUtc = DateTime.UtcNow,
            ClassificationVersion = 1
        };

        context.AchTransactions.Add(tx);
        await context.SaveChangesAsync();

        context.ContrapartidaDispatchItems.Add(new ContrapartidaDispatchItem
        {
            AchTransactionId = tx.Id,
            AchCycleId = cycleId,
            ClearingHouseId = 1,
            AchBatchId = tx.AchBatchId,
            State = ContrapartidaDispatchItemStateEnum.PendingContrapartidaReport,
            AttemptCount = 0,
            NextAttemptAtUtc = null
        });

        await context.SaveChangesAsync();
        return tx.Id;
    }

    private static async Task SeedIncomingNachaDispatchQueueAsync(AchDbContext context, string cycleId, int txId, string suffix)
    {
        var ingestionId = Guid.NewGuid();
        var entryDetailId = 10_000 + txId;

        context.EntryDetails.Add(new EntryDetail
        {
            EntryDetailID = entryDetailId,
            TransactionCode = "22",
            ReceivingParticipantEntityCode = "76543210",
            AccountNumber = "999988887777",
            Amount = 1000m,
            RecipUserName = "Receiver"
        });

        context.IncomingNachaFileIngestions.Add(new IncomingNachaFileIngestion
        {
            Id = ingestionId,
            FileName = $"qa-{suffix}.ach",
            FileHashSha256 = $"hash-{suffix}",
            FileSize = 1,
            ContentType = "text/plain",
            UploadedBy = "tester",
            CorrelationId = $"qa-{suffix}",
            Notes = "qa"
        });

        var classificationId = Guid.NewGuid();
        context.IncomingNachaEntryClassifications.Add(new IncomingNachaEntryClassification
        {
            Id = classificationId,
            IncomingNachaFileIngestionId = ingestionId,
            EntryDetailId = entryDetailId
        });

        var linkId = Guid.NewGuid();
        context.IncomingNachaTransactionLinks.Add(new IncomingNachaTransactionLink
        {
            Id = linkId,
            IncomingNachaFileIngestionId = ingestionId,
            EntryDetailId = entryDetailId,
            AchTransactionId = txId,
            LinkType = IncomingNachaLinkType.ExactTrace15,
            ConfidenceScore = 1m,
            LinkedBy = "tester",
            IsFinal = true
        });

        context.IncomingNachaDispatchQueue.Add(new IncomingNachaDispatchQueue
        {
            Id = Guid.NewGuid(),
            IncomingNachaFileIngestionId = ingestionId,
            IncomingNachaEntryClassificationId = classificationId,
            IncomingNachaTransactionLinkId = linkId,
            AchTransactionId = txId,
            AchCycleId = cycleId,
            ClearingHouseId = 1,
            OperationalDate = TestClock.OperationalDate,
            QueueStatus = IncomingNachaDispatchQueueStatus.Queued,
            Priority = 100,
            IdempotencyDispatchKey = $"qa-contrapartida-{suffix}-{txId}",
            NextAttemptAtUtc = TestClock.UtcNow.UtcDateTime.AddMinutes(-1)
        });

        await context.SaveChangesAsync();
    }

    private static async Task<List<int>> SembrarVariasTransaccionesEItemsPendientesAsync(AchDbContext context, string cycleId, int cantidad)
    {
        var ids = new List<int>();
        for (var i = 0; i < cantidad; i++)
        {
            ids.Add(await SembrarTransaccionYItemPendienteAsync(context, cycleId));
        }

        return ids;
    }

    private static ProcContrapartidasRequestContract ContratoValido() => new()
    {
        OFNIT = "900123456",
        OFEMP = "EMPRESA",
        OFCTA = "111122223333",
        OFDD = "D",
        OFFECHEFEC = "20260427",
        OFMONDEB = 1000,
        OFMONCRE = 1000,
        OFIDARCH = 1,
        OFIDLOT = 1,
        OFST = "00",
        OFIDTX = "TX-CP-001",
        OFIDREVER = 0,
        OFIDEBAPLI = 0,
        OFIDCAMCOMPE = 1,
        OFDIRECCIONIP = "127.0.0.1",
        OFLIBRE = "QA",
        OFLIBRE1 = 0,
        ANSIDLOTE = 1,
        ANSST = "00",
        ANCLC = "00",
        ANSIDTX = "TX-CP-001",
        ANSIDREVER = 0
    };

    private static IOptions<ProcContrapartidasDispatchOptions> LiveDispatchOptions()
        => Options.Create(new ProcContrapartidasDispatchOptions { Mode = "Live" });

    private static ISoapIntegrationSettingsService SoapSettingsService(string endpoint)
    {
        var settings = new Mock<ISoapIntegrationSettingsService>();
        settings
            .Setup(x => x.GetAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new SoapIntegrationSettingsDto
            {
                WscfaachMappings =
                [
                    new SoapEndpointMethodMappingDto
                    {
                        MethodName = "Proc_Contrapartidas",
                        Endpoint = endpoint,
                        SoapAction = "http://tempuri.org/IWSCFAACH/Proc_Contrapartidas",
                        Enabled = true
                    }
                ]
            });

        return settings.Object;
    }

    private static IIntegrationResponseCatalogResolver Catalog(params IntegrationResponseCatalogResult[] results)
        => new StubResponseCatalogResolver(results);

    private static IntegrationResponseCatalogResult Success(string code, string description)
        => Known(code, description, IntegrationResponseBusinessStatus.Success);

    private static IntegrationResponseCatalogResult Rejected(string code, string description)
        => Known(code, description, IntegrationResponseBusinessStatus.Rejected);

    private static IntegrationResponseCatalogResult Known(
        string code,
        string description,
        IntegrationResponseBusinessStatus status)
        => new(
            null,
            code,
            description,
            IntegrationResponseCategory.CoreSoapResponse,
            IntegrationResponseCategory.CoreSoapResponse,
            "Proc_Contrapartidas",
            status,
            false,
            false,
            true,
            "AppliedTacitly",
            true);

    private sealed class StubResponseCatalogResolver(IEnumerable<IntegrationResponseCatalogResult> results)
        : IIntegrationResponseCatalogResolver
    {
        private readonly IReadOnlyDictionary<string, IntegrationResponseCatalogResult> _results =
            results.ToDictionary(x => x.Code, StringComparer.OrdinalIgnoreCase);

        public Task<IntegrationResponseCatalogResult> ResolveAsync(
            string source,
            string method,
            string? responseCode,
            DateTime processedAtUtc,
            CancellationToken ct = default)
        {
            var code = responseCode?.Trim() ?? string.Empty;
            return Task.FromResult(_results.TryGetValue(code, out var result)
                ? result
                : new IntegrationResponseCatalogResult(
                    null,
                    code,
                    "Código pendiente de parametrización",
                    source,
                    IntegrationResponseCategory.CoreSoapResponse,
                    method,
                    IntegrationResponseBusinessStatus.PendingCatalog,
                    false,
                    true,
                    false,
                    string.Empty,
                    false));
        }
    }
}
