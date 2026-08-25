using System.Text.Json;
using Cfa.ACHInterbank.Api.Controllers;
using Cfa.ACHInterbank.Domain.Entities.Transactions.Enums;
using Cfa.ACHInterbank.Domain.Models.ACH;
using Cfa.ACHInterbank.Persistence.DataBase;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace Cfa.ACHInterbank.Tests;

public sealed class CenitOperationsClearingHouseIsolationTests
{
    [Fact]
    public async Task GetTraceability_ShouldReturnOnlyCenitTransactions_AndUseCenitCatalog()
    {
        await using var context = await CreateContextAsync();
        var cenit = await AddClearingHouseAsync(context, "CENIT", 9101);
        var achColombia = await AddClearingHouseAsync(context, "ACHCOL", 9102);
        await SeedTransactionsAsync(context, cenit, achColombia);

        var controller = new CenitOperationsController(context);

        var action = await controller.GetTraceabilityAsync(null, null, 1, 50, CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(action);
        var payload = JsonSerializer.SerializeToElement(ok.Value);
        Assert.Equal(1, payload.GetProperty("total").GetInt32());
        var item = Assert.Single(payload.GetProperty("items").EnumerateArray());
        Assert.Equal("TX-CENIT", item.GetProperty("TransactionExternalId").GetString());
        Assert.Equal("CENIT", item.GetProperty("ClearingHouseName").GetString());
        Assert.Equal("Causal CENIT", item.GetProperty("CausalDescription").GetString());
    }

    private static async Task<AchDbContext> CreateContextAsync()
    {
        var connection = new SqliteConnection("DataSource=:memory:");
        await connection.OpenAsync();
        var options = new DbContextOptionsBuilder<AchDbContext>()
            .UseSqlite(connection)
            .Options;
        var context = new AchDbContext(options);
        await context.Database.EnsureCreatedAsync();
        return context;
    }

    private static async Task<ClearingHouse> AddClearingHouseAsync(
        AchDbContext context,
        string code,
        int configurationScope)
    {
        var configuration = new ClearingHouseConfig
        {
            ClearingHouseId = configurationScope,
            HolidayStrategy = "Colombian"
        };
        context.ClearingHouseConfigs.Add(configuration);
        await context.SaveChangesAsync();

        var clearingHouse = new ClearingHouse
        {
            Name = code,
            Code = code,
            OriginCode = "000101006",
            ClearingHouseId = configuration.Id
        };
        context.ClearingHouses.Add(clearingHouse);
        await context.SaveChangesAsync();
        return clearingHouse;
    }

    private static async Task SeedTransactionsAsync(
        AchDbContext context,
        ClearingHouse cenit,
        ClearingHouse achColombia)
    {
        var source = new FinancialInstitution
        {
            Id = 7101,
            Name = "Origen",
            RoutingNumber = "00001",
            TransitCode = "007"
        };
        var destination = new FinancialInstitution
        {
            Id = 7102,
            Name = "Destino",
            RoutingNumber = "00001",
            TransitCode = "001"
        };
        source.CalculateCheckDigit();
        destination.CalculateCheckDigit();
        context.FinancialInstitutions.AddRange(source, destination);

        var cenitCycle = NewCycle("CYCLE-CENIT", cenit.Id);
        var achColombiaCycle = NewCycle("CYCLE-ACHCOL", achColombia.Id);
        context.AchCycles.AddRange(cenitCycle, achColombiaCycle);

        var cenitBatch = NewBatch(7201, cenitCycle.Id);
        var achColombiaBatch = NewBatch(7202, achColombiaCycle.Id);
        context.AchBatches.AddRange(cenitBatch, achColombiaBatch);

        context.AchReturnCodes.AddRange(
            new AchReturnCode
            {
                ClearingHouseId = cenit.Id,
                Code = "R01",
                Description = "Causal CENIT",
                FlowType = "Any",
                IsActive = true
            },
            new AchReturnCode
            {
                ClearingHouseId = achColombia.Id,
                Code = "R01",
                Description = "Causal ACH Colombia",
                FlowType = "Any",
                IsActive = true
            });

        context.AchTransactions.AddRange(
            NewTransaction(7301, "TX-CENIT", cenitCycle.Id, cenitBatch.Id, source.Id, destination.Id),
            NewTransaction(7302, "TX-ACHCOL", achColombiaCycle.Id, achColombiaBatch.Id, source.Id, destination.Id));
        await context.SaveChangesAsync();
    }

    private static AchCycle NewCycle(string id, int clearingHouseId) => new()
    {
        Id = id,
        CycleName = id,
        ProcessingDate = new DateTime(2026, 7, 18),
        StartTime = TimeSpan.Zero,
        EndTime = new TimeSpan(23, 59, 0),
        CutoffTime = new TimeSpan(23, 59, 0),
        ClearingHouseId = clearingHouseId
    };

    private static AchBatch NewBatch(int id, string cycleId) => new()
    {
        Id = id,
        AchCycleId = cycleId,
        BatchSequenceNumber = 1,
        CompanyEntryDescriptionId = 1,
        EffectiveEntryDate = new DateTime(2026, 7, 18)
    };

    private static AchTransaction NewTransaction(
        int id,
        string externalId,
        string cycleId,
        int batchId,
        int sourceInstitutionId,
        int destinationInstitutionId) => new()
    {
        Id = id,
        TransactionExternalId = externalId,
        Reference = externalId,
        Amount = 100m,
        Type = TransactionTypeEnum.Credit,
        TransactionCode = "22",
        TraceNumber = $"00001007{id:D7}",
        TraceSequenceNumber = id,
        EffectiveEntryDate = new DateTime(2026, 7, 18),
        State = AchTransferStateEnum.Pending,
        StateChangedAtUtc = new DateTime(2026, 7, 18, 12, 0, 0, DateTimeKind.Utc),
        ReturnReasonCode = "R01",
        SourceAccountNumber = "MASKED-SOURCE",
        DestinationAccountNumber = "MASKED-DESTINATION",
        SourceInstitutionId = sourceInstitutionId,
        DestinationInstitutionId = destinationInstitutionId,
        AchCycleId = cycleId,
        AchBatchId = batchId,
        CompanyEntryDescriptionId = 1
    };
}
