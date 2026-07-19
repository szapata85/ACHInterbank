using Cfa.ACHInterbank.Domain.Models.ACH;
using Cfa.ACHInterbank.Persistence.DataBase;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;

namespace Cfa.ACHInterbank.Tests;

public class FinancialPersistenceModelIntegrityTests
{
    [Fact]
    public void IncomingNachaProcessingEvent_ShouldUseOnlyTheDeclaredIngestionForeignKey()
    {
        using var context = BuildContext();
        var eventType = context.Model.FindEntityType(typeof(IncomingNachaProcessingEvent));

        Assert.NotNull(eventType);
        Assert.Null(eventType!.FindProperty("IncomingNachaFileIngestionId1"));
        Assert.DoesNotContain(eventType.GetProperties(), property =>
            property.IsShadowProperty() && property.Name.EndsWith("Id1", StringComparison.Ordinal));

        var foreignKeys = eventType.GetForeignKeys()
            .Where(foreignKey => foreignKey.PrincipalEntityType.ClrType == typeof(IncomingNachaFileIngestion))
            .ToList();

        var foreignKey = Assert.Single(foreignKeys);
        Assert.Single(foreignKey.Properties);
        Assert.Equal(nameof(IncomingNachaProcessingEvent.IncomingNachaFileIngestionId), foreignKey.Properties[0].Name);
        Assert.False(foreignKey.Properties[0].IsShadowProperty());
        Assert.True(foreignKey.IsRequired);
        Assert.Equal(DeleteBehavior.Restrict, foreignKey.DeleteBehavior);
        Assert.Equal(nameof(IncomingNachaProcessingEvent.Ingestion), foreignKey.DependentToPrincipal!.Name);
        Assert.Equal(nameof(IncomingNachaFileIngestion.ProcessingEvents), foreignKey.PrincipalToDependent!.Name);

        Assert.Contains(eventType.GetIndexes(), index =>
            index.Properties.Select(property => property.Name).SequenceEqual([
                nameof(IncomingNachaProcessingEvent.IncomingNachaFileIngestionId),
                nameof(IncomingNachaProcessingEvent.OccurredAtUtc)
            ]));
    }

    [Theory]
    [InlineData(typeof(AchBatch), nameof(AchBatch.TotalDebitAmount))]
    [InlineData(typeof(AchBatch), nameof(AchBatch.TotalCreditAmount))]
    [InlineData(typeof(AchTransaction), nameof(AchTransaction.Amount))]
    [InlineData(typeof(EntryDetail), nameof(EntryDetail.Amount))]
    [InlineData(typeof(BatchControl), nameof(BatchControl.TotalDebitAmount))]
    [InlineData(typeof(BatchControl), nameof(BatchControl.TotalCreditAmount))]
    [InlineData(typeof(FileControl), nameof(FileControl.TotalDebitAmount))]
    [InlineData(typeof(FileControl), nameof(FileControl.TotalCreditAmount))]
    public void MonetaryProperties_ShouldDeclarePortablePrecisionAndScale(Type entityClrType, string propertyName)
    {
        using var context = BuildContext();
        var property = context.Model.FindEntityType(entityClrType)!.FindProperty(propertyName);

        Assert.NotNull(property);
        Assert.Equal(18, property!.GetPrecision());
        Assert.Equal(2, property.GetScale());
    }

    private static AchDbContext BuildContext()
        => new(new DbContextOptionsBuilder<AchDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options);
}
