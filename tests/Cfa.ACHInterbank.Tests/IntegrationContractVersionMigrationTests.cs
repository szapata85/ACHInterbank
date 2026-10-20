using System.Text.RegularExpressions;

namespace Cfa.ACHInterbank.Tests;

public sealed class IntegrationContractVersionMigrationTests
{
    private const string PostgresMigrationStem = "20261012231000_IntegrationsAddContractVersioningBaseline";
    private const string SqlServerMigrationStem = "20261012231100_IntegrationsAddContractVersioningBaseline";

    [Fact]
    public void DualProviderMigrations_ShouldCreateAdditiveContractVersionSchemaAndDeterministicBackfill()
    {
        var postgres = ReadRepoFile(
            "src", "Cfa.ACHInterbank.Persistence", "DataBase", "Migrations", "Postgres", $"{PostgresMigrationStem}.cs");
        var sqlServer = ReadRepoFile(
            "src", "Cfa.ACHInterbank.Persistence.Migrations.SqlServer", "DataBase", "Migrations", "SqlServer", $"{SqlServerMigrationStem}.cs");

        foreach (var source in new[] { postgres, sqlServer })
        {
            Assert.Contains("IntegrationContractVersions", source, StringComparison.Ordinal);
            Assert.Contains("IntegrationMethodParameters", source, StringComparison.Ordinal);
            Assert.Contains("IntegrationMappingSets", source, StringComparison.Ordinal);
            Assert.Contains("ContractVersionId", source, StringComparison.Ordinal);
            Assert.Contains("Revision", source, StringComparison.Ordinal);
            Assert.Contains("XmlElementName", source, StringComparison.Ordinal);
            Assert.Contains("XsdType", source, StringComparison.Ordinal);
            Assert.Contains("SupportStatus", source, StringComparison.Ordinal);
            Assert.Contains("baseline-v1", source, StringComparison.Ordinal);
            Assert.Contains("BaselineMigration", source, StringComparison.Ordinal);
            Assert.Contains("CFA-SOAP-UX-MAP-001/P0", source, StringComparison.Ordinal);
            Assert.Contains("CFA-SOAP-UX-MAP-001/P1.2", source, StringComparison.Ordinal);
            Assert.Contains("WSCFAACH.Proc_Contrapartidas", source, StringComparison.Ordinal);
            Assert.Contains("WSCFAACH.Proc_Transacciones", source, StringComparison.Ordinal);
            Assert.Contains("WSAXON.RegistrarRespuestaTransaccion", source, StringComparison.Ordinal);
            Assert.Contains("http://tempuri.org/IWSCFAACH/Proc_Contrapartidas", source, StringComparison.Ordinal);
            Assert.Contains("http://tempuri.org/IWSCFAACH/Proc_Transacciones", source, StringComparison.Ordinal);
            Assert.Contains("http://tempuri.org/IWSAxonRespuestaTransacciones/RegistrarRespuestaTransaccion", source, StringComparison.Ordinal);
            Assert.Contains("39b5693c-407b-505c-8e8a-e49e8558a82e", source, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("9f3a13b6-eb79-5e43-803b-4c97b71a0f5d", source, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("4a093eb5-beed-5b15-bc7c-5768c0e0547e", source, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("ReferentialAction.Restrict", source, StringComparison.Ordinal);
            Assert.Contains("non-baseline contract version data exists", source, StringComparison.Ordinal);

            AssertContractVersionColumnRemainsNullable(source, "IntegrationMethodParameters");
            AssertContractVersionColumnRemainsNullable(source, "IntegrationMappingSets");
        }

        Assert.Contains("WHERE NOT EXISTS", postgres, StringComparison.Ordinal);
        Assert.Contains("UPDATE \"IntegrationMethodParameters\"", postgres, StringComparison.Ordinal);
        Assert.Contains("UPDATE \"IntegrationMappingSets\"", postgres, StringComparison.Ordinal);

        Assert.Contains("IF NOT EXISTS", sqlServer, StringComparison.Ordinal);
        Assert.Contains("UPDATE p", sqlServer, StringComparison.Ordinal);
        Assert.Contains("FROM [IntegrationMethodParameters] p", sqlServer, StringComparison.Ordinal);
        Assert.Contains("UPDATE s", sqlServer, StringComparison.Ordinal);
        Assert.Contains("FROM [IntegrationMappingSets] s", sqlServer, StringComparison.Ordinal);
    }

    [Fact]
    public void ProviderSnapshots_ShouldRepresentNullableBackfillWindowAndRestrictiveHistoryLinks()
    {
        var snapshots = new[]
        {
            ReadRepoFile("src", "Cfa.ACHInterbank.Persistence", "DataBase", "Migrations", "Postgres", "AchDbContextModelSnapshot.cs"),
            ReadRepoFile("src", "Cfa.ACHInterbank.Persistence.Migrations.SqlServer", "DataBase", "Migrations", "SqlServer", "AchDbContextModelSnapshot.cs")
        };

        foreach (var snapshot in snapshots)
        {
            Assert.Contains("IntegrationContractVersion", snapshot, StringComparison.Ordinal);
            Assert.Contains("Property<Guid?>(\"ContractVersionId\")", snapshot, StringComparison.Ordinal);
            Assert.Contains("UX_IntegrationContractVersions_Method_VersionLabel", snapshot, StringComparison.Ordinal);
            Assert.Contains("IX_IntegrationContractVersions_Method_Status", snapshot, StringComparison.Ordinal);
            Assert.Contains("IX_IntegrationContractVersions_SourceDocumentHash", snapshot, StringComparison.Ordinal);
            Assert.Contains("WithMany(\"ContractVersions\")", snapshot, StringComparison.Ordinal);
            Assert.Contains("WithMany(\"Parameters\")", snapshot, StringComparison.Ordinal);
            Assert.Contains("WithMany(\"MappingSets\")", snapshot, StringComparison.Ordinal);
            Assert.Contains("OnDelete(DeleteBehavior.Restrict)", snapshot, StringComparison.Ordinal);
        }
    }

    [Fact]
    public void RegistrarRespuestaBaseline_ShouldContinueToSourceExactlySevenCurrentTechnicalParameters()
    {
        var bootstrapper = ReadRepoFile(
            "src", "Cfa.ACHInterbank.Persistence", "Integrations", "Services", "IntegrationCatalogBootstrapper.cs");

        var start = bootstrapper.IndexOf(
            "private static IReadOnlyCollection<ParameterSeedSpec> BuildRegistrarRespuestaTransaccionTechnicalCatalog()",
            StringComparison.Ordinal);
        Assert.True(start >= 0);

        var bodyStart = bootstrapper.IndexOf("return", start, StringComparison.Ordinal);
        var end = bootstrapper.IndexOf("private static", bodyStart + 1, StringComparison.Ordinal);
        var body = end >= 0 ? bootstrapper[bodyStart..end] : bootstrapper[bodyStart..];

        var expected = new[]
        {
            "idCanal",
            "nombreCanal",
            "idTransaccion",
            "idEstado",
            "causal",
            "idTransaccionAxon",
            "descripcionCausal"
        };

        var names = Regex.Matches(body, "Spec\\(\\\"(?<name>[^\\\"]+)\\\"")
            .Select(match => match.Groups["name"].Value)
            .ToArray();

        Assert.Equal(expected, names);
    }

    private static void AssertContractVersionColumnRemainsNullable(string source, string table)
    {
        var pattern = $"name: \\\"ContractVersionId\\\",\\s*table: \\\"{Regex.Escape(table)}\\\",[\\s\\S]{{0,180}}?nullable: true";
        Assert.Matches(new Regex(pattern, RegexOptions.CultureInvariant), source);
    }

    private static string ReadRepoFile(params string[] relativePath)
    {
        var repoRoot = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../../../"));
        return File.ReadAllText(Path.Combine(new[] { repoRoot }.Concat(relativePath).ToArray()));
    }
}
