using Cfa.ACHInterbank.Domain.Models.ACH.Config;
using Cfa.ACHInterbank.Persistence.DataBase;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Cfa.ACHInterbank.Tests;

public sealed class NachaPublicationSnapshotStorageTests
{
    [Fact]
    public void PostgreSqlModel_ShouldUseUnboundedText_AndDiscoverCapacityMigration()
    {
        var options = new DbContextOptionsBuilder<AchDbContext>()
            .UseNpgsql("Host=localhost;Database=snapshot_model;Username=model;Password=model")
            .Options;
        using var context = new AchDbContext(options);

        AssertSnapshotJsonMapping(context, "text");
        context.Database.GetMigrations().Should().ContainSingle(migration =>
            migration.EndsWith("_CompleteNachaPublicationSnapshot", StringComparison.Ordinal));
    }

    [Fact]
    public void SqlServerModel_ShouldRemainUnboundedNvarchar_AndDiscoverAlignmentMigration()
    {
        var options = new DbContextOptionsBuilder<AchDbContext>()
            .UseSqlServer(
                "Server=localhost;Database=snapshot_model;User Id=model;Password=model;TrustServerCertificate=True",
                sql => sql.MigrationsAssembly("Cfa.ACHInterbank.Persistence.Migrations.SqlServer"))
            .Options;
        using var context = new AchDbContext(options);

        AssertSnapshotJsonMapping(context, "nvarchar(max)");
        context.Database.GetMigrations().Should().ContainSingle(migration =>
            migration.EndsWith("_CompleteNachaPublicationSnapshot", StringComparison.Ordinal));
    }

    private static void AssertSnapshotJsonMapping(AchDbContext context, string expectedColumnType)
    {
        var property = context.Model.FindEntityType(typeof(HistConfigSnapshot))!
            .FindProperty(nameof(HistConfigSnapshot.SnapshotJson))!;

        property.GetMaxLength().Should().BeNull();
        property.GetColumnType().Should().Be(expectedColumnType);
    }
}
