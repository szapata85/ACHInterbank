using Cfa.ACHInterbank.Domain.Models.ACH;
using Cfa.ACHInterbank.Persistence.DataBase;
using Microsoft.EntityFrameworkCore;

namespace Cfa.ACHInterbank.Tests;

public class AchReturnOfReturnGeneratedFileAuditModelTests
{
    [Fact]
    public void Model_ShouldConfigureReturnOfReturnGeneratedFileAudit()
    {
        using var context = BuildContext();
        var entity = context.Model.FindEntityType(typeof(AchReturnOfReturnGeneratedFileAudit));

        Assert.NotNull(entity);
        Assert.Equal("AchReturnOfReturnGeneratedFileAudits", entity!.GetTableName());
        Assert.False(entity.FindProperty(nameof(AchReturnOfReturnGeneratedFileAudit.FileName))!.IsNullable);
        var sha = entity.FindProperty(nameof(AchReturnOfReturnGeneratedFileAudit.ContentSha256))!;
        Assert.False(sha.IsNullable);
        Assert.Equal(64, sha.GetMaxLength());
        Assert.Equal(120, entity.FindProperty(nameof(AchReturnOfReturnGeneratedFileAudit.RequestedBy))!.GetMaxLength());
        Assert.Equal(120, entity.FindProperty(nameof(AchReturnOfReturnGeneratedFileAudit.Source))!.GetMaxLength());
    }

    [Fact]
    public void Model_ShouldConfigureReturnOfReturnGeneratedFileAuditFlow()
    {
        using var context = BuildContext();
        var entity = context.Model.FindEntityType(typeof(AchReturnOfReturnGeneratedFileAuditFlow));

        Assert.NotNull(entity);
        Assert.Equal("AchReturnOfReturnGeneratedFileAuditFlows", entity!.GetTableName());
        Assert.NotNull(entity.FindForeignKeys(entity.FindProperty(nameof(AchReturnOfReturnGeneratedFileAuditFlow.AchReturnOfReturnGeneratedFileAuditId))!).SingleOrDefault());
        Assert.NotNull(entity.FindForeignKeys(entity.FindProperty(nameof(AchReturnOfReturnGeneratedFileAuditFlow.ReturnOfReturnFlowId))!).SingleOrDefault());
        Assert.Contains(entity.GetIndexes(), x => x.IsUnique && x.Properties.Select(p => p.Name).SequenceEqual(new[] { nameof(AchReturnOfReturnGeneratedFileAuditFlow.AchReturnOfReturnGeneratedFileAuditId), nameof(AchReturnOfReturnGeneratedFileAuditFlow.ReturnOfReturnFlowId) }));
    }

    [Fact]
    public void Model_ShouldNotPersistGeneratedFileContent()
    {
        var props = typeof(AchReturnOfReturnGeneratedFileAudit).GetProperties().ToDictionary(x => x.Name, x => x.PropertyType);
        Assert.DoesNotContain("ContentText", props.Keys);
        Assert.DoesNotContain("Content", props.Keys);
        Assert.DoesNotContain("FileBytes", props.Keys);
        Assert.DoesNotContain(props.Values, x => x == typeof(byte[]));
    }

    [Fact]
    public void Model_ShouldKeepAuditSeparateFromAchReturnGenerated()
    {
        using var context = BuildContext();
        var auditEntity = context.Model.FindEntityType(typeof(AchReturnOfReturnGeneratedFileAudit));
        var generatedEntity = context.Model.FindEntityType(typeof(AchReturnGenerated));

        Assert.NotNull(auditEntity);
        Assert.NotNull(generatedEntity);
        Assert.NotEqual(generatedEntity!.GetTableName(), auditEntity!.GetTableName());
        Assert.DoesNotContain(auditEntity.GetNavigations(), x => x.TargetEntityType.ClrType == typeof(AchReturnGenerated));
    }

    private static AchDbContext BuildContext()
        => new(new DbContextOptionsBuilder<AchDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);
}
