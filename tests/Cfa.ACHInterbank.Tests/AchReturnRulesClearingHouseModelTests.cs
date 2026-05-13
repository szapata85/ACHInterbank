using Cfa.ACHInterbank.Domain.Models.ACH;
using Cfa.ACHInterbank.Persistence.DataBase;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;

namespace Cfa.ACHInterbank.Tests;

public class AchReturnRulesClearingHouseModelTests
{
    [Fact]
    public async Task AchReturnCode_ShouldHaveClearingHouseId()
    {
        await using var context = await BuildContextAsync();
        var entity = context.Model.FindEntityType(typeof(AchReturnCode));
        Assert.NotNull(entity);
        Assert.NotNull(entity!.FindProperty(nameof(AchReturnCode.ClearingHouseId)));
    }

    [Fact]
    public async Task AchReturnPolicy_ShouldHaveClearingHouseId()
    {
        await using var context = await BuildContextAsync();
        var entity = context.Model.FindEntityType(typeof(AchReturnPolicy));
        Assert.NotNull(entity);
        Assert.NotNull(entity!.FindProperty(nameof(AchReturnPolicy.ClearingHouseId)));
    }

    [Fact]
    public async Task AchReturnOfReturnPolicy_ShouldHaveClearingHouseId()
    {
        await using var context = await BuildContextAsync();
        var entity = context.Model.FindEntityType(typeof(AchReturnOfReturnPolicy));
        Assert.NotNull(entity);
        Assert.NotNull(entity!.FindProperty(nameof(AchReturnOfReturnPolicy.ClearingHouseId)));
    }

    [Fact]
    public async Task ReturnRules_ShouldNotConfigureDefaultZeroForClearingHouseId()
    {
        await using var context = await BuildContextAsync();

        AssertNoDefaultValueMetadata(context.Model.FindEntityType(typeof(AchReturnCode))!, nameof(AchReturnCode.ClearingHouseId));
        AssertNoDefaultValueMetadata(context.Model.FindEntityType(typeof(AchReturnPolicy))!, nameof(AchReturnPolicy.ClearingHouseId));
        AssertNoDefaultValueMetadata(context.Model.FindEntityType(typeof(AchReturnOfReturnPolicy))!, nameof(AchReturnOfReturnPolicy.ClearingHouseId));
    }

    [Fact]
    public async Task ReturnRules_ShouldUseRestrictDeleteBehavior()
    {
        await using var context = await BuildContextAsync();

        AssertRestrictFk(context.Model.FindEntityType(typeof(AchReturnCode))!);
        AssertRestrictFk(context.Model.FindEntityType(typeof(AchReturnPolicy))!);
        AssertRestrictFk(context.Model.FindEntityType(typeof(AchReturnOfReturnPolicy))!);
    }

    [Fact]
    public async Task ReturnRules_ShouldHaveEffectiveDateFields()
    {
        await using var context = await BuildContextAsync();

        Assert.NotNull(context.Model.FindEntityType(typeof(AchReturnCode))!.FindProperty(nameof(AchReturnCode.EffectiveFrom)));
        Assert.NotNull(context.Model.FindEntityType(typeof(AchReturnCode))!.FindProperty(nameof(AchReturnCode.EffectiveTo)));
        Assert.NotNull(context.Model.FindEntityType(typeof(AchReturnPolicy))!.FindProperty(nameof(AchReturnPolicy.EffectiveFrom)));
        Assert.NotNull(context.Model.FindEntityType(typeof(AchReturnPolicy))!.FindProperty(nameof(AchReturnPolicy.EffectiveTo)));
        Assert.NotNull(context.Model.FindEntityType(typeof(AchReturnOfReturnPolicy))!.FindProperty(nameof(AchReturnOfReturnPolicy.EffectiveFrom)));
        Assert.NotNull(context.Model.FindEntityType(typeof(AchReturnOfReturnPolicy))!.FindProperty(nameof(AchReturnOfReturnPolicy.EffectiveTo)));
    }

    [Fact]
    public async Task ReturnRules_ShouldHaveDirectionAndFlowTypeFields()
    {
        await using var context = await BuildContextAsync();

        Assert.NotNull(context.Model.FindEntityType(typeof(AchReturnCode))!.FindProperty(nameof(AchReturnCode.FlowType)));
        Assert.NotNull(context.Model.FindEntityType(typeof(AchReturnPolicy))!.FindProperty(nameof(AchReturnPolicy.Direction)));
        Assert.NotNull(context.Model.FindEntityType(typeof(AchReturnPolicy))!.FindProperty(nameof(AchReturnPolicy.FlowType)));
        Assert.NotNull(context.Model.FindEntityType(typeof(AchReturnOfReturnPolicy))!.FindProperty(nameof(AchReturnOfReturnPolicy.Direction)));
        Assert.NotNull(context.Model.FindEntityType(typeof(AchReturnOfReturnPolicy))!.FindProperty(nameof(AchReturnOfReturnPolicy.FlowType)));
    }

    [Fact]
    public void ReturnRules_ShouldNotHaveInMemoryDefaultClearingHouseId()
    {
        Assert.Equal(0, new AchReturnCode().ClearingHouseId);
        Assert.Equal(0, new AchReturnPolicy().ClearingHouseId);
        Assert.Equal(0, new AchReturnOfReturnPolicy().ClearingHouseId);
    }

    private static void AssertNoDefaultValueMetadata(IEntityType entityType, string propertyName)
    {
        var property = entityType.FindProperty(propertyName)!;
        Assert.False(property.IsNullable);
        Assert.Equal(ValueGenerated.Never, property.ValueGenerated);
        Assert.Null(property.GetDefaultValueSql());
        Assert.Null(property.FindAnnotation("Relational:DefaultValue"));
        Assert.Null(property.FindAnnotation("Relational:DefaultValueSql"));
    }

    private static void AssertRestrictFk(IEntityType entityType)
    {
        var fk = entityType.GetForeignKeys().Single(x => x.Properties.Any(p => p.Name == "ClearingHouseId"));
        Assert.Equal(DeleteBehavior.Restrict, fk.DeleteBehavior);
    }

    private static async Task<AchDbContext> BuildContextAsync()
    {
        var connection = new SqliteConnection("DataSource=:memory:");
        await connection.OpenAsync();
        var options = new DbContextOptionsBuilder<AchDbContext>().UseSqlite(connection).Options;
        var context = new AchDbContext(options);
        await context.Database.EnsureCreatedAsync();
        return context;
    }
}
