namespace Cfa.ACHInterbank.Tests;

public sealed class IntegrationContractVersionConstraintMigrationTests
{
    [Fact]
    public void DualProviderP13Migrations_ShouldEnforceVersionedUniquenessAndOnlyOneActiveWithoutDroppingLegacyRows()
    {
        var postgres = ReadRepoFile(
            "src", "Cfa.ACHInterbank.Persistence", "DataBase", "Migrations", "Postgres",
            "20261012233000_IntegrationsContractVersionConstraints.cs");
        var sqlServer = ReadRepoFile(
            "src", "Cfa.ACHInterbank.Persistence.Migrations.SqlServer", "DataBase", "Migrations", "SqlServer",
            "20261012233100_IntegrationsContractVersionConstraints.cs");

        foreach (var source in new[] { postgres, sqlServer })
        {
            Assert.Contains("UX_IntegrationContractVersions_Method_Active", source, StringComparison.Ordinal);
            Assert.Contains("UX_IntegrationMethodParameters_ContractVersion_ParameterPath", source, StringComparison.Ordinal);
            Assert.Contains("UX_IntegrationMethodParameters_Legacy_Method_ParameterPath", source, StringComparison.Ordinal);
            Assert.Contains("UX_IntegrationMappingSets_ContractVersion_Version", source, StringComparison.Ordinal);
            Assert.Contains("ContractVersionId", source, StringComparison.Ordinal);
            Assert.Contains("ParameterPath", source, StringComparison.Ordinal);
            Assert.Contains("Version", source, StringComparison.Ordinal);
            Assert.Contains("Status", source, StringComparison.Ordinal);
            Assert.Contains("downgrade refused", source, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("DropColumn", source, StringComparison.Ordinal);
            Assert.DoesNotContain("DropTable", source, StringComparison.Ordinal);
        }
    }

    [Fact]
    public void ProviderSnapshots_ShouldCarryConcurrencyAndFilteredP13Indexes()
    {
        var snapshots = new[]
        {
            ReadRepoFile("src", "Cfa.ACHInterbank.Persistence", "DataBase", "Migrations", "Postgres", "AchDbContextModelSnapshot.cs"),
            ReadRepoFile("src", "Cfa.ACHInterbank.Persistence.Migrations.SqlServer", "DataBase", "Migrations", "SqlServer", "AchDbContextModelSnapshot.cs")
        };

        foreach (var snapshot in snapshots)
        {
            Assert.Contains("UX_IntegrationContractVersions_Method_Active", snapshot, StringComparison.Ordinal);
            Assert.Contains("UX_IntegrationMethodParameters_ContractVersion_ParameterPath", snapshot, StringComparison.Ordinal);
            Assert.Contains("UX_IntegrationMethodParameters_Legacy_Method_ParameterPath", snapshot, StringComparison.Ordinal);
            Assert.Contains("UX_IntegrationMappingSets_ContractVersion_Version", snapshot, StringComparison.Ordinal);
            Assert.Contains("IsConcurrencyToken", snapshot, StringComparison.Ordinal);
            Assert.Contains("Property<Guid?>(\"ContractVersionId\")", snapshot, StringComparison.Ordinal);
        }
    }

    private static string ReadRepoFile(params string[] relativePath)
    {
        var repoRoot = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../../../"));
        return File.ReadAllText(Path.Combine(new[] { repoRoot }.Concat(relativePath).ToArray()));
    }
}
