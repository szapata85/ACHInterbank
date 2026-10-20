using Cfa.ACHInterbank.Persistence.DataBase;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Cfa.ACHInterbank.Tests;

public class IncomingNachaFileIngestionConfigurationTests
{
    [Fact]
    public void IncomingNachaFileIngestionConfiguration_ShouldDeclareUniquePartialIndex_ForCanonicalHashAndSize()
    {
        var options = new DbContextOptionsBuilder<AchDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        using var context = new AchDbContext(options);
        var entityType = context.Model.FindEntityType("Cfa.ACHInterbank.Domain.Models.ACH.IncomingNachaFileIngestion");
        Assert.NotNull(entityType);

        var index = entityType!.GetIndexes().FirstOrDefault(i =>
            i.Properties.Select(p => p.Name).SequenceEqual(["FileHashSha256", "FileSize"]));

        Assert.NotNull(index);
        Assert.True(index!.IsUnique);
        Assert.Equal("UX_IncomingNachaFileIngestions_FileHash_FileSize_Canonical", index.GetDatabaseName());

        var filter = index.GetFilter();
        Assert.True(
            filter is "\"IsReprocess\" = false" or "[IsReprocess] = 0",
            $"Filtro inesperado para índice canónico: {filter}");
    }
}
