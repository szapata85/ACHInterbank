using Cfa.ACHInterbank.Application.ACH.Interfaces;
using Cfa.ACHInterbank.Application.ACH.Models;
using Cfa.ACHInterbank.Application.External.Connections;
using Cfa.ACHInterbank.Domain.Entities.Transactions.Enums;
using Cfa.ACHInterbank.Domain.Models.ACH;
using Cfa.ACHInterbank.Domain.Models.Configurations;
using Cfa.ACHInterbank.Persistence.ACH.Services.Implementation;
using Cfa.ACHInterbank.Persistence.DataBase;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

namespace Cfa.ACHInterbank.Tests;

public class ContrapartidaDispatchJobServiceTests
{
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
            LiveDispatchOptions());

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

        var sut = new ContrapartidaDispatchJobService(
            context,
            soap.Object,
            mapper.Object,
            parser.Object,
            NullLogger<ContrapartidaDispatchJobService>.Instance,
            LiveDispatchOptions());

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

        var batch = await context.ContrapartidaDispatchBatches.SingleAsync();
        Assert.Equal(ContrapartidaDispatchBatchStatusEnum.Completed, batch.Status);
        Assert.Equal(1, batch.TotalSucceeded);
        Assert.Equal(0, batch.TotalFailed);
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
        await SembrarTransaccionYItemPendienteAsync(context, cycleId);

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
            LiveDispatchOptions());

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

        var batch = await context.ContrapartidaDispatchBatches.SingleAsync();
        Assert.Equal(ContrapartidaDispatchBatchStatusEnum.Failed, batch.Status);
    }

    [Fact]
    public async Task ProcessCycleAsync_DebeMarcarParcial_CuandoItemsTienenResultadoMixto()
    {
        using var connection = new SqliteConnection("DataSource=:memory:");
        await connection.OpenAsync();

        var options = new DbContextOptionsBuilder<AchDbContext>()
            .UseSqlite(connection)
            .Options;

        await using var context = new AchDbContext(options);
        await context.Database.EnsureCreatedAsync();

        var cycleId = await SembrarEstructuraBaseAsync(context);
        var transactionIds = await SembrarVariasTransaccionesEItemsPendientesAsync(context, cycleId, 2);

        var mapper = new Mock<IProcContrapartidasRequestMapper>();
        mapper
            .Setup(x => x.ResolveAsync(It.IsAny<AchCycle>(), It.IsAny<IReadOnlyCollection<AchTransaction>>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ProcContrapartidasRequestResolution
            {
                Contract = ContratoValido(),
                MappingSnapshotHash = "hash-parcial",
                UsedFallback = false
            });
        mapper
            .Setup(x => x.BuildSoapBody(It.IsAny<ProcContrapartidasRequestContract>()))
            .Returns("<request-parcial/>");

        var soap = new Mock<IWscfaachSoapClient>();
        soap
            .Setup(x => x.ProcContrapartidasAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync("<Envelope><Body><ok/></Body></Envelope>");

        var parser = new Mock<IProcContrapartidasResponseParser>();
        parser
            .Setup(x => x.Parse(It.IsAny<string>()))
            .Returns(new ProcContrapartidasParsedResponse(
                IsSuccess: false,
                IsSoapFault: false,
                IsRetryable: false,
                IsFunctionalRejection: true,
                ErrorCode: "R10",
                ErrorMessage: "Mixto",
                RawResponse: "<mixed/>",
                ResponseCode: "R10",
                ItemResults: new Dictionary<int, ProcContrapartidasParsedItemResponse>
                {
                    [transactionIds[0]] = new(transactionIds[0], true, false, "R96", "Aplicado"),
                    [transactionIds[1]] = new(transactionIds[1], false, false, "R10", "Rechazo funcional")
                }));

        var sut = new ContrapartidaDispatchJobService(
            context,
            soap.Object,
            mapper.Object,
            parser.Object,
            NullLogger<ContrapartidaDispatchJobService>.Instance,
            LiveDispatchOptions());

        var result = await sut.ProcessCycleAsync(cycleId, 1, "qa-soap-2b", 100, CancellationToken.None);

        Assert.Equal(2, result.Processed);
        Assert.Equal(1, result.Succeeded);
        Assert.Equal(1, result.Failed);
        Assert.Equal(1, result.Partial);

        var items = await context.ContrapartidaDispatchItems.OrderBy(x => x.Id).ToListAsync();
        Assert.Equal(ContrapartidaDispatchItemStateEnum.ReportedToContrapartida, items[0].State);
        Assert.Equal(ContrapartidaDispatchItemStateEnum.ContrapartidaReportFailed, items[1].State);

        var attempts = await context.ContrapartidaDispatchAttempts.OrderBy(x => x.Id).ToListAsync();
        Assert.Contains(attempts, x => x.Result == ContrapartidaDispatchAttemptResultEnum.Success);
        Assert.Contains(attempts, x => x.Result == ContrapartidaDispatchAttemptResultEnum.Partial);

        var batch = await context.ContrapartidaDispatchBatches.SingleAsync();
        Assert.Equal(ContrapartidaDispatchBatchStatusEnum.CompletedWithErrors, batch.Status);
        Assert.Equal(1, batch.TotalSucceeded);
        Assert.Equal(1, batch.TotalFailed);
        Assert.Equal(1, batch.TotalPartial);
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
        await SembrarTransaccionYItemPendienteAsync(context, cycleId);

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
            Options.Create(new ProcContrapartidasDispatchOptions { Mode = "DryRun" }));

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
            Options.Create(new ProcContrapartidasDispatchOptions { Mode = "DryRun" }));

        var result = await sut.ProcessCycleAsync(cycleId, 1, "qa-no-fallback", 100, CancellationToken.None);

        Assert.Equal(1, result.Processed);
        Assert.Equal(1, result.Failed);

        var attempt = await context.ContrapartidaDispatchAttempts.SingleAsync();
        Assert.Equal(ContrapartidaDispatchAttemptResultEnum.Failed, attempt.Result);
        Assert.Equal(string.Empty, attempt.RequestPayloadXml);
        Assert.Contains("REQUIRED_MAPPING_USES_FALLBACK", attempt.ResponsePayloadXml);

        mapper.Verify(x => x.BuildSoapBody(It.IsAny<ProcContrapartidasRequestContract>()), Times.Never);
        soap.Verify(x => x.ProcContrapartidasAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
        parser.Verify(x => x.Parse(It.IsAny<string>()), Times.Never);
    }

    private static async Task<string> SembrarEstructuraBaseAsync(AchDbContext context)
    {
        context.ClearingHouseConfigs.Add(new ClearingHouseConfig
        {
            Id = 1,
            HolidayStrategy = "Colombian"
        });

        context.ClearingHouses.Add(new ClearingHouse
        {
            Id = 1,
            Name = "ACH Colombia",
            Code = "ACH",
            OriginCode = "12345678",
            ClearingHouseId = 1
        });

        var cycleId = "bbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbb";
        context.AchCycles.Add(new AchCycle
        {
            Id = cycleId,
            ClearingHouseId = 1,
            CycleName = "CICLO-QA",
            ProcessingDate = DateTime.Today,
            StartTime = TimeSpan.Zero,
            EndTime = new TimeSpan(23, 59, 0),
            CutoffTime = new TimeSpan(23, 0, 0)
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
            EffectiveEntryDate = DateTime.Today,
            ServiceClassCode = "220",
            BatchSequenceNumber = 1
        };

        var tx = new AchTransaction
        {
            Amount = 1000m,
            TransactionExternalId = $"TX-CP-{sufijo}",
            Reference = $"REF-CP-{sufijo}",
            Type = TransactionTypeEnum.Credit,
            TransactionCode = "22",
            ServiceClassCode = "220",
            CompanyEntryDescriptionId = companyEntryDescriptionId,
            CompanyName = "EMPRESA",
            CompanyIdentification = "900123456",
            OriginatingDFI = "123456780",
            ReceivingDFI = "765432100",
            TraceNumber = $"12345678{sufijo}",
            TraceSequenceNumber = consecutivo,
            EffectiveEntryDate = DateTime.Today,
            AddendaRecordIndicator = true,
            IsPrenotification = false,
            SourceAccountNumber = "111122223333",
            DestinationAccountNumber = "999988887777",
            SourceInstitutionId = 1,
            DestinationInstitutionId = 2,
            AchCycleId = cycleId,
            AchBatch = batch
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
}
