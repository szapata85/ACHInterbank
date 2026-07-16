using AutoMapper;
using Cfa.ACHInterbank.Domain.Models.ACH;
using Cfa.ACHInterbank.Domain.Models.Configurations;
using Cfa.ACHInterbank.Persistence.ACH.Services.Implementation;
using Cfa.ACHInterbank.Persistence.DataBase;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Moq;

namespace Cfa.ACHInterbank.Tests;

public class AchCycleExportReadModelTests
{
    [Fact]
    public async Task GetExecutedWithTransactionsAsync_UsesCycleProcessingDate_NotOldPrenoteDate()
    {
        await using var connection = new SqliteConnection("DataSource=:memory:");
        await connection.OpenAsync();
        var options = new DbContextOptionsBuilder<AchDbContext>()
            .UseSqlite(connection)
            .Options;
        await using var context = new AchDbContext(options);
        await context.Database.EnsureCreatedAsync();
        context.ClearingHouseConfigs.Add(new ClearingHouseConfig { Id = 1, ClearingHouseId = 1, HolidayStrategy = "Colombian" });
        context.ClearingHouses.Add(new ClearingHouse { Id = 1, ClearingHouseId = 1, Name = "CENIT", Code = "CENIT", OriginCode = "00001007" });
        var institution = new FinancialInstitution { Id = 1, Name = "CFA", RoutingNumber = "00012", TransitCode = "283" };
        institution.CalculateCheckDigit();
        context.FinancialInstitutions.Add(institution);
        var cycle = new AchCycle
        {
            Id = "cycle-export",
            CycleName = "Ciclo 1",
            ProcessingDate = new DateTime(2026, 7, 14),
            ClearingHouseId = 1
        };
        var batch = new AchBatch
        {
            Id = 10,
            AchCycleId = cycle.Id,
            AchCycle = cycle,
            BatchSequenceNumber = 1
        };
        var prenote = new AchTransaction
        {
            Id = 20,
            AchCycleId = cycle.Id,
            AchCycle = cycle,
            AchBatchId = batch.Id,
            AchBatch = batch,
            EffectiveEntryDate = new DateTime(2026, 7, 9),
            Reference = "PRENOTE",
            SourceAccountNumber = "source",
            DestinationAccountNumber = "destination",
            SourceInstitutionId = 1,
            DestinationInstitutionId = 1
        };
        context.AchCycles.Add(cycle);
        context.AchBatches.Add(batch);
        context.AchTransactions.Add(prenote);
        await context.SaveChangesAsync();
        Assert.Equal(1, await context.AchCycles.CountAsync());
        Assert.Equal(1, await context.AchBatches.CountAsync());
        Assert.Equal(1, await context.AchTransactions.CountAsync());
        var service = new AchCycleAppService(context, Mock.Of<IMapper>());

        var result = Assert.Single(await service.GetExecutedWithTransactionsAsync());

        Assert.Equal(cycle.Id, result.ExportIdentifier);
        Assert.Equal(cycle.ProcessingDate, result.ProcessingDate);
        Assert.True(result.IsExportable);
    }
}
