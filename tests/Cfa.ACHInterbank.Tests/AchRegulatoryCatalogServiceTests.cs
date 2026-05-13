using Cfa.ACHInterbank.Domain.Entities.Transactions.Enums;
using Cfa.ACHInterbank.Domain.Models.ACH;
using Cfa.ACHInterbank.Persistence.ACH.Services.Implementation;
using Cfa.ACHInterbank.Persistence.DataBase;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace Cfa.ACHInterbank.Tests;

public class AchRegulatoryCatalogServiceTests
{
    [Fact]
    public async Task ValidateReturnCode_AllowsConfiguredCode()
    {
        await using var context = await CreateContextAsync();
        var clearingHouse = await EnsureClearingHouseAsync(context);
        context.AchReturnCodes.Add(new AchReturnCode { ClearingHouseId = clearingHouse.Id, Code = "R01", Description = "Fondos insuficientes", AppliesToDebit = true, IsActive = true });
        await context.SaveChangesAsync();

        var sut = new AchRegulatoryCatalogService(context);
        var result = await sut.ValidateReturnCodeAsync(clearingHouse.Id, "R01", TransactionTypeEnum.Debit, DateTime.UtcNow.Date, DateTime.UtcNow.Date, CancellationToken.None);

        Assert.True(result.IsAllowed);
    }

    [Fact]
    public async Task ValidateReturnCode_RejectsInvalidCode()
    {
        await using var context = await CreateContextAsync();
        var sut = new AchRegulatoryCatalogService(context);

        var result = await sut.ValidateReturnCodeAsync(1, "R99", TransactionTypeEnum.Debit, DateTime.UtcNow.Date, DateTime.UtcNow.Date, CancellationToken.None);

        Assert.False(result.IsAllowed);
    }

    [Fact]
    public async Task ReturnOfReturnPolicy_EnforcesMaxDays()
    {
        await using var context = await CreateContextAsync();
        var clearingHouse = await EnsureClearingHouseAsync(context);
        context.AchReturnOfReturnPolicies.Add(new AchReturnOfReturnPolicy
        {
            ClearingHouseId = clearingHouse.Id,
            OriginalReturnCode = "R01",
            AllowedNewReturnCodesCsv = "R02",
            MaxDays = 2,
            RequiredOriginalState = "ReturnedByOperator",
            IsUniquePerTransaction = true,
            IsActive = true
        });
        await context.SaveChangesAsync();

        var sut = new AchRegulatoryCatalogService(context);
        var result = await sut.ValidateReturnOfReturnAsync(clearingHouse.Id, "R01", "R02", "ReturnedByOperator", DateTime.UtcNow.Date.AddDays(-5), DateTime.UtcNow.Date, CancellationToken.None);

        Assert.False(result.IsAllowed);
    }

    [Fact]
    public async Task ReturnPolicy_ValidatesAllowedCodeAndAddenda()
    {
        await using var context = await CreateContextAsync();
        var clearingHouse = await EnsureClearingHouseAsync(context);
        context.AchReturnPolicies.Add(new AchReturnPolicy
        {
            ClearingHouseId = clearingHouse.Id,
            TransactionType = "Debit",
            AllowedReturnCodesCsv = "R01,R02",
            MaxDays = 5,
            RequiredOriginalTransactionState = "Pending",
            RequiresAddenda = true,
            IsActive = true
        });
        await context.SaveChangesAsync();

        var sut = new AchRegulatoryCatalogService(context);
        var result = await sut.ValidateReturnPolicyAsync(clearingHouse.Id, TransactionTypeEnum.Debit, "R03", DateTime.UtcNow.Date, DateTime.UtcNow.Date, hasAddenda: false, originalState: "Pending", CancellationToken.None);

        Assert.False(result.IsAllowed);
    }

    [Fact]
    public async Task Priority_IsReadFromCatalog()
    {
        await using var context = await CreateContextAsync();
        context.AchTransactionTypePolicies.Add(new AchTransactionTypePolicy { TransactionType = "Debit", PriorityOrder = 123, IsActive = true });
        await context.SaveChangesAsync();

        var sut = new AchRegulatoryCatalogService(context);
        var priority = await sut.GetPriorityAsync(TransactionTypeEnum.Debit, CancellationToken.None);

        Assert.Equal(123, priority);
    }

    [Fact]
    public async Task PrenotificationRequirement_IsReadFromCatalog()
    {
        await using var context = await CreateContextAsync();
        context.AchPrenotificationPolicies.Add(new AchPrenotificationPolicy { TransactionType = "Debit", IsRequired = true, IsActive = true });
        await context.SaveChangesAsync();

        var sut = new AchRegulatoryCatalogService(context);
        var required = await sut.IsPrenotificationRequiredAsync(TransactionTypeEnum.Debit, CancellationToken.None);

        Assert.True(required);
    }

    [Fact]
    public async Task FileRejectionCode_IsResolvedFromCatalog()
    {
        await using var context = await CreateContextAsync();
        context.AchFileRejectionCodes.Add(new AchFileRejectionCode { Code = "D01", Description = "Archivo duplicado", AppliesToStage = "Validation", Severity = "Fatal", IsActive = true });
        await context.SaveChangesAsync();

        var sut = new AchRegulatoryCatalogService(context);
        var dxx = await sut.ResolveFileRejectionCodeAsync("Validation", "D01", CancellationToken.None);

        Assert.NotNull(dxx);
        Assert.Equal("D01", dxx!.Code);
    }

    private static async Task<AchDbContext> CreateContextAsync()
    {
        var connection = new SqliteConnection("DataSource=:memory:");
        await connection.OpenAsync();
        var options = new DbContextOptionsBuilder<AchDbContext>()
            .UseSqlite(connection)
            .EnableSensitiveDataLogging()
            .Options;

        var context = new AchDbContext(options);
        context.Database.EnsureCreated();
        await EnsureClearingHouseAsync(context);
        return context;
    }

    private static async Task<ClearingHouse> EnsureClearingHouseAsync(AchDbContext context, string code = "CENIT", string name = "CENIT")
    {
        var existing = await context.ClearingHouses.FirstOrDefaultAsync(x => x.Code == code);
        if (existing is not null) return existing;

        var config = new ClearingHouseConfig { ClearingHouseId = 9001, HolidayStrategy = "Colombian" };
        context.ClearingHouseConfigs.Add(config);
        await context.SaveChangesAsync();

        var clearingHouse = new ClearingHouse
        {
            Name = name,
            Code = code,
            OriginCode = "000101006",
            ClearingHouseId = config.Id
        };

        context.ClearingHouses.Add(clearingHouse);
        await context.SaveChangesAsync();
        return clearingHouse;
    }
}
