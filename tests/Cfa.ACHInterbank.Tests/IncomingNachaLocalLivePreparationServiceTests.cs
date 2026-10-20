using System.Security.Cryptography;
using System.Text;
using Cfa.ACHInterbank.Domain.Entities.Transactions.Enums;
using Cfa.ACHInterbank.Domain.Models.ACH;
using Cfa.ACHInterbank.Domain.Models.Configurations;
using Cfa.ACHInterbank.Persistence.ACH.Services.Implementation;
using Cfa.ACHInterbank.Persistence.DataBase;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Moq;

namespace Cfa.ACHInterbank.Tests;

public sealed class IncomingNachaLocalLivePreparationServiceTests
{
    [Fact]
    public async Task EnsureAsync_PreparesIdempotentTransaction_ForCenitIngestion()
    {
        await AssertSupportedClearingHouseAsync("CENIT");
    }

    [Fact]
    public async Task EnsureAsync_PreparesIdempotentTransaction_ForAchColombiaIngestion()
    {
        await AssertSupportedClearingHouseAsync("ACHCOL");
    }

    [Fact]
    public async Task EnsureAsync_DoesNotPrepareTransactions_ForUnsupportedClearingHouse()
    {
        await using var context = CreateContext();
        context.ClearingHouses.Add(new ClearingHouse
        {
            Id = 7,
            ClearingHouseId = 7,
            Code = "OTHER",
            Name = "Unsupported clearing house",
            OriginCode = "0000000"
        });
        await context.SaveChangesAsync();

        var sut = CreateService(context);

        await sut.EnsureAsync(
            new IncomingNachaFileIngestion { ResolvedClearingHouseId = 7 },
            new EntryDetail { TransactionCode = "32" },
            IncomingNachaFunctionalClass.CreditoEntrante);

        Assert.Empty(await context.AchTransactions.ToListAsync());
        Assert.Empty(await context.IncomingNachaProcessingEvents.ToListAsync());
    }

    [Fact]
    public async Task EnsureAsync_DoesNotAlterSuccessfulExistingTransaction()
    {
        await using var context = CreateContext();
        var scenario = await SeedSupportedScenarioAsync(context, "ACHCOL");
        var marker = BuildMarker(scenario.Entry.SequenceNumber!);
        var existingBatch = new AchBatch
        {
            Id = 91,
            AchCycleId = scenario.Ingestion.ResolvedAchCycleId,
            CompanyIdentification = marker,
            CompanyName = "PRESERVE SUCCESS",
            CompanyEntryDescriptionId = 1
        };
        var existing = new AchTransaction
        {
            Id = 92,
            TraceNumber = scenario.Entry.SequenceNumber!,
            TransactionExternalId = marker,
            Reference = marker,
            CompanyIdentification = "PRESERVE SUCCESS",
            CompanyName = "PRESERVE SUCCESS",
            State = AchTransferStateEnum.AppliedTacitly,
            SourceAccountNumber = "PRESERVE-SOURCE",
            DestinationAccountNumber = "PRESERVE-DESTINATION",
            AchBatch = existingBatch,
            AchCycleId = scenario.Ingestion.ResolvedAchCycleId!,
            SourceInstitutionId = 1,
            DestinationInstitutionId = 1
        };
        context.AchTransactions.Add(existing);
        await context.SaveChangesAsync();

        await CreateService(context).EnsureAsync(
            scenario.Ingestion,
            scenario.Entry,
            IncomingNachaFunctionalClass.CreditoEntrante);

        Assert.Equal("PRESERVE SUCCESS", existing.CompanyIdentification);
        Assert.Equal(marker, existingBatch.CompanyIdentification);
        Assert.Empty(await context.IncomingNachaProcessingEvents.ToListAsync());
    }

    private static async Task AssertSupportedClearingHouseAsync(string clearingHouseCode)
    {
        await using var context = CreateContext();
        var scenario = await SeedSupportedScenarioAsync(context, clearingHouseCode);
        var sut = CreateService(context);

        await sut.EnsureAsync(
            scenario.Ingestion,
            scenario.Entry,
            IncomingNachaFunctionalClass.CreditoEntrante);
        await sut.EnsureAsync(
            scenario.Ingestion,
            scenario.Entry,
            IncomingNachaFunctionalClass.CreditoEntrante);

        var transaction = Assert.Single(await context.AchTransactions
            .Include(x => x.SourceInstitution)
            .Include(x => x.AchBatch)
            .ToListAsync());
        Assert.Equal("LOCAL LIVE " + clearingHouseCode, transaction.CompanyName);
        Assert.Equal("LOCAL LIVE " + clearingHouseCode, transaction.AchBatch.CompanyName);
        Assert.StartsWith(clearingHouseCode + " LOCAL EXTERNAL ", transaction.SourceInstitution.Name);
        Assert.Equal(scenario.Ingestion.ResolvedAchCycleId, transaction.AchCycleId);
        Assert.Equal(scenario.Entry.SequenceNumber, transaction.TraceNumber);
        Assert.Equal(AchTransferStateEnum.Pending, transaction.State);

        var processingEvent = Assert.Single(await context.IncomingNachaProcessingEvents.ToListAsync());
        Assert.Equal("LocalLiveTransactionPrepared", processingEvent.EventType);
        Assert.Equal(
            $"{{\"createdBy\":\"local-live-proc-transacciones\",\"clearingHouse\":\"{clearingHouseCode}\"}}",
            processingEvent.EvidenceJson);
        Assert.DoesNotContain(scenario.Entry.AccountNumber!, processingEvent.EvidenceJson, StringComparison.Ordinal);
    }

    private static async Task<(IncomingNachaFileIngestion Ingestion, EntryDetail Entry)> SeedSupportedScenarioAsync(
        AchDbContext context,
        string clearingHouseCode)
    {
        var clearingHouse = new ClearingHouse
        {
            Id = 1,
            ClearingHouseId = 1,
            Code = clearingHouseCode,
            Name = clearingHouseCode,
            OriginCode = "0001283"
        };
        var cycle = new AchCycle
        {
            Id = clearingHouseCode + "-20260731-01",
            CycleName = "Local live cycle",
            ProcessingDate = new DateTime(2026, 7, 31),
            StartTime = TimeSpan.Zero,
            EndTime = new TimeSpan(23, 59, 59),
            CutoffTime = new TimeSpan(23, 59, 59),
            ClearingHouseId = clearingHouse.Id,
            ClearingHouse = clearingHouse
        };
        var destination = new FinancialInstitution
        {
            Id = 1,
            Name = "CFA",
            RoutingNumber = "00001",
            TransitCode = "283",
            IsDefaultSource = true,
            Status = FinancialInstitutionStatus.Active
        };
        destination.CalculateCheckDigit();

        var ingestion = new IncomingNachaFileIngestion
        {
            Id = Guid.NewGuid(),
            FileName = "authorized-fixture",
            FileHashSha256 = "SAFE-HASH",
            ResolvedClearingHouseId = clearingHouse.Id,
            ResolvedAchCycleId = cycle.Id
        };
        var entry = new EntryDetail
        {
            EntryDetailID = 10,
            NachaID = "NACHA-LOCAL-LIVE",
            BatchNumber = 1,
            TransactionCode = "32",
            ReceivingParticipantEntityCode = "00001283",
            CheckDigit = destination.CheckDigit,
            SequenceNumber = "999999000000001",
            AccountNumber = "TEST-ACCOUNT",
            Amount = 100m,
            AddendumIndicator = "1"
        };

        context.ClearingHouses.Add(clearingHouse);
        context.AchCycles.Add(cycle);
        context.FinancialInstitutions.Add(destination);
        context.IncomingNachaFileIngestions.Add(ingestion);
        context.BatchHeaders.Add(new BatchHeader
        {
            BatchID = 11,
            NachaID = entry.NachaID,
            BatchNumber = entry.BatchNumber,
            ServiceClassCode = "220",
            OriginParticipantEntityCode = "99999900"
        });
        context.EntryDetails.Add(entry);
        context.CompanyEntryDescriptionCatalogs.Add(new CompanyEntryDescriptionCatalog
        {
            Id = 1,
            Term = "PAGOS",
            Description = "Test"
        });
        await context.SaveChangesAsync();
        return (ingestion, entry);
    }

    private static IncomingNachaLocalLivePreparationService CreateService(AchDbContext context)
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["RUN_LOCAL_SOAP_PROC_TRANSACCIONES_E2E"] = "true",
                ["ALLOW_LOCAL_MONETARY_SOAP_E2E"] = "true",
                ["ProcTransacciones:Mode"] = "Live"
            })
            .Build();
        var environment = new Mock<IHostEnvironment>();
        environment.SetupGet(x => x.EnvironmentName).Returns(Environments.Development);
        return new IncomingNachaLocalLivePreparationService(context, configuration, environment.Object);
    }

    private static AchDbContext CreateContext()
        => new(new DbContextOptionsBuilder<AchDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options);

    private static string BuildMarker(string trace)
        => "local-live-" + Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(trace))).ToLowerInvariant()[..20];
}
