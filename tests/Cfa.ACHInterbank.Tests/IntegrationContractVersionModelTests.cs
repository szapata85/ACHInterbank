using Cfa.ACHInterbank.Domain.Entities.Integrations;
using Cfa.ACHInterbank.Persistence.DataBase;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;

namespace Cfa.ACHInterbank.Tests;

public sealed class IntegrationContractVersionModelTests
{
    [Fact]
    public void ContractVersion_ShouldExposeTypedLifecycleSourceAndRevisionMetadata()
    {
        var contract = new IntegrationContractVersion
        {
            Status = IntegrationContractVersionStatusEnum.Validated,
            SourceType = IntegrationContractSourceTypeEnum.WsdlImport
        };

        Assert.Equal(IntegrationContractVersionStatusEnum.Validated, contract.Status);
        Assert.Equal(IntegrationContractSourceTypeEnum.WsdlImport, contract.SourceType);
        Assert.Equal(1L, contract.Revision);
        Assert.NotEqual(Guid.Empty, contract.Id);
    }

    [Fact]
    public void EfModel_ShouldConfigureVersionedContractAsAdditiveAndNonDestructive()
    {
        using var context = CreateContext();

        var contract = AssertEntity(context, typeof(IntegrationContractVersion), "IntegrationContractVersions");
        Assert.Equal(typeof(IntegrationContractVersionStatusEnum), contract.FindProperty(nameof(IntegrationContractVersion.Status))!.ClrType);
        Assert.Equal(typeof(IntegrationContractSourceTypeEnum), contract.FindProperty(nameof(IntegrationContractVersion.SourceType))!.ClrType);
        Assert.True(contract.FindProperty(nameof(IntegrationContractVersion.Revision))!.IsConcurrencyToken);
        Assert.NotNull(contract.FindProperty(nameof(IntegrationContractVersion.CreatedAt)));
        Assert.NotNull(contract.FindProperty(nameof(IntegrationContractVersion.UpdatedAt)));
        AssertUniqueIndex(contract, nameof(IntegrationContractVersion.MethodId), nameof(IntegrationContractVersion.VersionLabel));
        AssertIndex(contract, nameof(IntegrationContractVersion.MethodId), nameof(IntegrationContractVersion.Status));
        AssertUniqueIndex(contract, nameof(IntegrationContractVersion.MethodId));
        AssertIndex(contract, nameof(IntegrationContractVersion.SourceDocumentHash));
        AssertRestrictForeignKey(contract, typeof(IntegrationMethod), nameof(IntegrationContractVersion.MethodId));

        var method = AssertEntity(context, typeof(IntegrationMethod), "IntegrationMethods");
        Assert.NotNull(method.FindNavigation(nameof(IntegrationMethod.ContractVersions)));
        Assert.NotNull(method.FindProperty(nameof(IntegrationMethod.FunctionalDescription)));
        Assert.NotNull(method.FindProperty(nameof(IntegrationMethod.FunctionalCategory)));
        Assert.NotNull(method.FindProperty(nameof(IntegrationMethod.RiskLevel)));

        var parameter = AssertEntity(context, typeof(IntegrationMethodParameter), "IntegrationMethodParameters");
        Assert.True(parameter.FindProperty(nameof(IntegrationMethodParameter.ContractVersionId))!.IsNullable);
        Assert.NotNull(parameter.FindProperty(nameof(IntegrationMethodParameter.XmlElementName)));
        Assert.NotNull(parameter.FindProperty(nameof(IntegrationMethodParameter.XsdType)));
        Assert.NotNull(parameter.FindProperty(nameof(IntegrationMethodParameter.Nillable)));
        Assert.NotNull(parameter.FindProperty(nameof(IntegrationMethodParameter.MinOccurs)));
        Assert.True(parameter.FindProperty(nameof(IntegrationMethodParameter.MaxOccurs))!.IsNullable);
        Assert.NotNull(parameter.FindProperty(nameof(IntegrationMethodParameter.SupportStatus)));
        AssertRestrictForeignKey(parameter, typeof(IntegrationContractVersion), nameof(IntegrationMethodParameter.ContractVersionId));
        AssertUniqueIndex(parameter, nameof(IntegrationMethodParameter.MethodId), nameof(IntegrationMethodParameter.ParameterPath));
        AssertUniqueIndex(parameter, nameof(IntegrationMethodParameter.ContractVersionId), nameof(IntegrationMethodParameter.ParameterPath));

        var mappingSet = AssertEntity(context, typeof(IntegrationMappingSet), "IntegrationMappingSets");
        Assert.True(mappingSet.FindProperty(nameof(IntegrationMappingSet.ContractVersionId))!.IsNullable);
        Assert.Equal(typeof(long), mappingSet.FindProperty(nameof(IntegrationMappingSet.Revision))!.ClrType);
        Assert.True(mappingSet.FindProperty(nameof(IntegrationMappingSet.Revision))!.IsConcurrencyToken);
        AssertRestrictForeignKey(mappingSet, typeof(IntegrationContractVersion), nameof(IntegrationMappingSet.ContractVersionId));
        AssertIndex(mappingSet, nameof(IntegrationMappingSet.MethodId), nameof(IntegrationMappingSet.ContractVersionId), nameof(IntegrationMappingSet.Version));
        AssertUniqueIndex(mappingSet, nameof(IntegrationMappingSet.ContractVersionId), nameof(IntegrationMappingSet.Version));
    }

    private static AchDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<AchDbContext>()
            .UseSqlite("Data Source=:memory:")
            .Options;
        return new AchDbContext(options);
    }

    private static IEntityType AssertEntity(AchDbContext context, Type type, string tableName)
    {
        var entity = context.Model.FindEntityType(type);
        Assert.NotNull(entity);
        Assert.Equal(tableName, entity!.GetTableName());
        return entity;
    }

    private static void AssertUniqueIndex(IEntityType entity, params string[] properties)
    {
        Assert.Contains(entity.GetIndexes(), index =>
            index.IsUnique && index.Properties.Select(property => property.Name).SequenceEqual(properties));
    }

    private static void AssertIndex(IEntityType entity, params string[] properties)
    {
        Assert.Contains(entity.GetIndexes(), index =>
            index.Properties.Select(property => property.Name).SequenceEqual(properties));
    }

    private static void AssertRestrictForeignKey(IEntityType entity, Type principalType, string foreignKeyProperty)
    {
        var foreignKey = entity.GetForeignKeys().Single(candidate =>
            candidate.PrincipalEntityType.ClrType == principalType
            && candidate.Properties.Select(property => property.Name).SequenceEqual([foreignKeyProperty]));

        Assert.Equal(DeleteBehavior.Restrict, foreignKey.DeleteBehavior);
    }
}
