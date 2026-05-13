using System.Linq;
using Cfa.ACHInterbank.Domain.Models.ACH;
using Cfa.ACHInterbank.Persistence.DataBase;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;

namespace Cfa.ACHInterbank.Tests;

public class AchReturnRulesClearingHouseModelTests
{
    [Fact]
    public void AchReturnCode_ShouldHaveClearingHouseId()
    {
        using var context = BuildContext();
        var entityType = context.Model.FindEntityType(typeof(AchReturnCode));
        Assert.NotNull(entityType?.FindProperty(nameof(AchReturnCode.ClearingHouseId)));
    }

    [Fact]
    public void AchReturnPolicy_ShouldHaveClearingHouseId()
    {
        using var context = BuildContext();
        var entityType = context.Model.FindEntityType(typeof(AchReturnPolicy));
        Assert.NotNull(entityType?.FindProperty(nameof(AchReturnPolicy.ClearingHouseId)));
    }

    [Fact]
    public void AchReturnOfReturnPolicy_ShouldHaveClearingHouseId()
    {
        using var context = BuildContext();
        var entityType = context.Model.FindEntityType(typeof(AchReturnOfReturnPolicy));
        Assert.NotNull(entityType?.FindProperty(nameof(AchReturnOfReturnPolicy.ClearingHouseId)));
    }

    [Fact]
    public void ReturnRules_ShouldNotConfigureDefaultZeroForClearingHouseId()
    {
        using var context = BuildContext();
        AssertNoDefaultValue<AchReturnCode>(context, nameof(AchReturnCode.ClearingHouseId));
        AssertNoDefaultValue<AchReturnPolicy>(context, nameof(AchReturnPolicy.ClearingHouseId));
        AssertNoDefaultValue<AchReturnOfReturnPolicy>(context, nameof(AchReturnOfReturnPolicy.ClearingHouseId));
    }

    [Fact]
    public void ReturnRules_ShouldUseRestrictDeleteBehavior()
    {
        using var context = BuildContext();
        AssertRestrictDeleteBehavior<AchReturnCode>(context);
        AssertRestrictDeleteBehavior<AchReturnPolicy>(context);
        AssertRestrictDeleteBehavior<AchReturnOfReturnPolicy>(context);
    }

    [Fact]
    public void ReturnRules_ShouldHaveEffectiveDateFields()
    {
        using var context = BuildContext();
        AssertHasProperty<AchReturnCode>(context, nameof(AchReturnCode.EffectiveFrom), false);
        AssertHasProperty<AchReturnCode>(context, nameof(AchReturnCode.EffectiveTo), true);
        AssertHasProperty<AchReturnPolicy>(context, nameof(AchReturnPolicy.EffectiveFrom), false);
        AssertHasProperty<AchReturnPolicy>(context, nameof(AchReturnPolicy.EffectiveTo), true);
        AssertHasProperty<AchReturnOfReturnPolicy>(context, nameof(AchReturnOfReturnPolicy.EffectiveFrom), false);
        AssertHasProperty<AchReturnOfReturnPolicy>(context, nameof(AchReturnOfReturnPolicy.EffectiveTo), true);
    }

    [Fact]
    public void ReturnRules_ShouldHaveDirectionAndFlowTypeFields()
    {
        using var context = BuildContext();
        AssertHasProperty<AchReturnCode>(context, nameof(AchReturnCode.FlowType), false);
        AssertHasProperty<AchReturnPolicy>(context, nameof(AchReturnPolicy.Direction), false);
        AssertHasProperty<AchReturnPolicy>(context, nameof(AchReturnPolicy.FlowType), false);
        AssertHasProperty<AchReturnOfReturnPolicy>(context, nameof(AchReturnOfReturnPolicy.Direction), false);
        AssertHasProperty<AchReturnOfReturnPolicy>(context, nameof(AchReturnOfReturnPolicy.FlowType), false);
    }

    private static AchDbContext BuildContext()
    {
        var connection = new SqliteConnection("DataSource=:memory:");
        connection.Open();
        var options = new DbContextOptionsBuilder<AchDbContext>().UseSqlite(connection).Options;
        var context = new AchDbContext(options);
        context.Database.EnsureCreated();
        return context;
    }

    private static void AssertRestrictDeleteBehavior<TEntity>(AchDbContext context)
    {
        var entityType = context.Model.FindEntityType(typeof(TEntity));
        var fk = entityType!.GetForeignKeys().Single(x => x.PrincipalEntityType.ClrType == typeof(ClearingHouse));
        Assert.Equal(DeleteBehavior.Restrict, fk.DeleteBehavior);
    }

    private static void AssertHasProperty<TEntity>(AchDbContext context, string propertyName, bool nullable)
    {
        var entityType = context.Model.FindEntityType(typeof(TEntity));
        var property = entityType!.FindProperty(propertyName);
        Assert.NotNull(property);
        Assert.Equal(nullable, property!.IsNullable);
    }

    private static void AssertNoDefaultValue<TEntity>(AchDbContext context, string propertyName)
    {
        var entityType = context.Model.FindEntityType(typeof(TEntity));
        var property = entityType!.FindProperty(propertyName);
        Assert.NotNull(property);
        Assert.False(property!.IsNullable);
        Assert.Equal(ValueGenerated.Never, property.ValueGenerated);
        Assert.Null(property.GetDefaultValueSql());
        Assert.DoesNotContain(
            property.GetAnnotations(),
            annotation => annotation.Name == "Relational:DefaultValue");
        Assert.DoesNotContain(
            property.GetAnnotations(),
            annotation => annotation.Name == "Relational:DefaultValueSql");
    }
}
