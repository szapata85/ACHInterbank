using Cfa.ACHInterbank.Application.ACH.Models;
using Cfa.ACHInterbank.Domain.Entities.Transactions.Enums;
using Cfa.ACHInterbank.Domain.Models.ACH;
using Cfa.ACHInterbank.Domain.Models.ACH.Enums;
using Cfa.ACHInterbank.Persistence.ACH.Services.Implementation;
using Cfa.ACHInterbank.Persistence.DataBase;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace Cfa.ACHInterbank.Tests;

public class ClearingHouseTransactionRuleServiceTests
{
    [Fact]
    public async Task CreateVersionAsync_DerivesCompatibilityFieldsAndClosesPreviousVersion()
    {
        await using var context = CreateContext();
        var houseId = await SeedClearingHouseAsync(context);
        var sut = new ClearingHouseTransactionRuleService(context);

        var first = await sut.CreateVersionAsync(houseId, CreateRequest(
            TransactionTypeEnum.Debit,
            PrenotificationRequirementMode.Mandatory,
            3,
            new DateTime(2026, 1, 1)), CancellationToken.None);
        var second = await sut.CreateVersionAsync(houseId, CreateRequest(
            TransactionTypeEnum.Debit,
            PrenotificationRequirementMode.Optional,
            null,
            new DateTime(2027, 1, 1)), CancellationToken.None);

        context.ChangeTracker.Clear();
        var versions = await sut.GetVersionsAsync(houseId, TransactionTypeEnum.Debit, CancellationToken.None);
        Assert.Equal(2, versions.Count);
        Assert.Equal(new DateTime(2026, 12, 31), versions.Single(x => x.Id == first.Id).EffectiveTo);
        Assert.True(first.RequiresPrenotification);
        Assert.False(second.RequiresPrenotification);
        Assert.Equal(TransactionNature.Debit, second.TransactionNature);
        Assert.True(second.AppliesToNachaExport);
        Assert.True(second.AppliesToMonetaryTransactions);
    }

    [Fact]
    public async Task GetCurrentAsync_ResolvesExactlyOneVersionForRequestedDate()
    {
        await using var context = CreateContext();
        var houseId = await SeedClearingHouseAsync(context);
        var sut = new ClearingHouseTransactionRuleService(context);
        await sut.CreateVersionAsync(houseId, CreateRequest(
            TransactionTypeEnum.Debit,
            PrenotificationRequirementMode.Mandatory,
            3,
            new DateTime(2026, 1, 1)), CancellationToken.None);
        await sut.CreateVersionAsync(houseId, CreateRequest(
            TransactionTypeEnum.Debit,
            PrenotificationRequirementMode.Optional,
            null,
            new DateTime(2027, 1, 1)), CancellationToken.None);

        var historical = await sut.GetCurrentAsync(houseId, TransactionTypeEnum.Debit, new DateTime(2026, 6, 1), CancellationToken.None);
        var future = await sut.GetCurrentAsync(houseId, TransactionTypeEnum.Debit, new DateTime(2027, 6, 1), CancellationToken.None);

        Assert.Equal(PrenotificationRequirementMode.Mandatory, historical!.PrenotificationMode);
        Assert.Equal(3, historical.PrenotificationLeadBusinessDays);
        Assert.Equal(PrenotificationRequirementMode.Optional, future!.PrenotificationMode);
        Assert.Null(future.PrenotificationLeadBusinessDays);
    }

    [Fact]
    public async Task UpdateMetadataAsync_DoesNotModifyFunctionalDecision()
    {
        await using var context = CreateContext();
        var houseId = await SeedClearingHouseAsync(context);
        var sut = new ClearingHouseTransactionRuleService(context);
        var created = await sut.CreateVersionAsync(houseId, CreateRequest(
            TransactionTypeEnum.Debit,
            PrenotificationRequirementMode.Mandatory,
            3,
            new DateTime(2026, 1, 1)), CancellationToken.None);

        var updated = await sut.UpdateMetadataAsync(
            houseId,
            created.Id,
            new("Fuente corregida", "Referencia corregida", "Nota corregida"),
            CancellationToken.None);

        Assert.Equal(PrenotificationRequirementMode.Mandatory, updated.PrenotificationMode);
        Assert.Equal(3, updated.PrenotificationLeadBusinessDays);
        Assert.Equal(created.EffectiveFrom, updated.EffectiveFrom);
        Assert.Equal("Fuente corregida", updated.NormativeSource);
        Assert.Equal("Referencia corregida", updated.NormativeReference);
    }

    [Fact]
    public async Task CreateAsync_RejectsIncoherentCompatibilityAndNegativeLead()
    {
        await using var context = CreateContext();
        var houseId = await SeedClearingHouseAsync(context);
        var sut = new ClearingHouseTransactionRuleService(context);
        var incoherent = new CreateClearingHouseTransactionRuleRequest(
            houseId,
            TransactionNature.Credit,
            TransactionTypeEnum.Debit,
            true,
            PrenotificationRequirementMode.Mandatory,
            3,
            true,
            ValidationRequirementMode.Mandatory,
            true,
            true,
            new DateTime(2026, 1, 1),
            null,
            "Fuente",
            "Referencia",
            null);

        var natureError = await Assert.ThrowsAsync<InvalidOperationException>(
            () => sut.CreateAsync(incoherent, CancellationToken.None));
        Assert.Contains("naturaleza", natureError.Message, StringComparison.OrdinalIgnoreCase);

        var leadError = await Assert.ThrowsAsync<InvalidOperationException>(
            () => sut.CreateVersionAsync(houseId, CreateRequest(
                TransactionTypeEnum.Debit,
                PrenotificationRequirementMode.Mandatory,
                -1,
                new DateTime(2026, 1, 1)), CancellationToken.None));
        Assert.Contains("negativo", leadError.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task CreateVersionAsync_RejectsOverlapWithLaterVersion()
    {
        await using var context = CreateContext();
        var houseId = await SeedClearingHouseAsync(context);
        var sut = new ClearingHouseTransactionRuleService(context);
        await sut.CreateVersionAsync(houseId, CreateRequest(
            TransactionTypeEnum.Debit,
            PrenotificationRequirementMode.Optional,
            null,
            new DateTime(2028, 1, 1)), CancellationToken.None);

        var error = await Assert.ThrowsAsync<InvalidOperationException>(
            () => sut.CreateVersionAsync(houseId, CreateRequest(
                TransactionTypeEnum.Debit,
                PrenotificationRequirementMode.Mandatory,
                3,
                new DateTime(2027, 1, 1),
                new DateTime(2028, 2, 1)), CancellationToken.None));

        Assert.Contains("solapa", error.Message, StringComparison.OrdinalIgnoreCase);
    }

    private static CreateClearingHouseTransactionPolicyVersionRequest CreateRequest(
        TransactionTypeEnum type,
        PrenotificationRequirementMode mode,
        int? leadDays,
        DateTime from,
        DateTime? to = null)
        => new(type, mode, leadDays, from, to, true, "Norma de prueba", "REF-PRUEBA", "Prueba automatizada");

    private static AchDbContext CreateContext()
    {
        var connection = new SqliteConnection("DataSource=:memory:");
        connection.Open();
        var options = new DbContextOptionsBuilder<AchDbContext>()
            .UseSqlite(connection)
            .EnableSensitiveDataLogging()
            .Options;
        var context = new AchDbContext(options);
        context.Database.EnsureCreated();
        return context;
    }

    private static async Task<int> SeedClearingHouseAsync(AchDbContext context)
    {
        var config = new ClearingHouseConfig { ClearingHouseId = 741, HolidayStrategy = "Colombian" };
        context.ClearingHouseConfigs.Add(config);
        await context.SaveChangesAsync();
        var house = new ClearingHouse
        {
            Code = "ACHCOL",
            Name = "ACH Colombia",
            OriginCode = "000101006",
            ClearingHouseId = config.Id
        };
        context.ClearingHouses.Add(house);
        await context.SaveChangesAsync();
        return house.Id;
    }
}
