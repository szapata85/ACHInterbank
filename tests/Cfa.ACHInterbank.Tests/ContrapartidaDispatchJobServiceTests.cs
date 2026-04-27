using Cfa.ACHInterbank.Application.ACH.Interfaces;
using Cfa.ACHInterbank.Application.ACH.Models;
using Cfa.ACHInterbank.Application.External.Connections;
using Cfa.ACHInterbank.Domain.Entities.Transactions.Enums;
using Cfa.ACHInterbank.Domain.Models.ACH;
using Cfa.ACHInterbank.Persistence.ACH.Services.Implementation;
using Cfa.ACHInterbank.Persistence.DataBase;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
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
            NullLogger<ContrapartidaDispatchJobService>.Instance);

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
            NullLogger<ContrapartidaDispatchJobService>.Instance);

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
            TransactionExternalId = "TX-CP-001",
            Reference = "REF-CP-001",
            Type = TransactionTypeEnum.Credit,
            TransactionCode = "22",
            ServiceClassCode = "220",
            CompanyEntryDescriptionId = companyEntryDescriptionId,
            CompanyName = "EMPRESA",
            CompanyIdentification = "900123456",
            OriginatingDFI = "123456780",
            ReceivingDFI = "765432100",
            TraceNumber = "123456780000001",
            TraceSequenceNumber = 1,
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
}
