using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Migrations.Operations;
using PgMigration = Cfa.ACHInterbank.Persistence.DataBase.Migrations.Postgres.AddThirdPartyMasterData;
using SqlMigration = Cfa.ACHInterbank.Persistence.Migrations.SqlServer.DataBase.Migrations.SqlServer.AddThirdPartyMasterData;

namespace Cfa.ACHInterbank.Tests;

public sealed class ThirdPartyMasterDataMigrationTests
{
    [Fact]
    public void PostgreSqlAndSqlServerMigrations_HaveEquivalentAdditiveMasterShape()
    {
        var postgres = Operations(new PgMigration(), "Npgsql.EntityFrameworkCore.PostgreSQL");
        var sqlServer = Operations(new SqlMigration(), "Microsoft.EntityFrameworkCore.SqlServer");

        AssertProviderShape(postgres);
        AssertProviderShape(sqlServer);

        LogicalSignature(postgres).Should().Equal(LogicalSignature(sqlServer));
    }

    private static void AssertProviderShape(IReadOnlyList<MigrationOperation> operations)
    {
        var tables = operations.OfType<CreateTableOperation>().ToDictionary(x => x.Name, StringComparer.Ordinal);
        tables.Keys.Should().BeEquivalentTo(new[] { "ThirdParties", "ThirdPartyAccounts" });

        tables["ThirdParties"].Columns.Select(x => x.Name).Should().BeEquivalentTo(new[]
        {
            "Id", "CustomerId", "PersonType", "DocumentType", "DocumentNumberNormalized",
            "DisplayName", "Status", "CreatedAt", "UpdatedAt"
        });
        tables["ThirdParties"].ForeignKeys.Should().HaveCount(3);

        tables["ThirdPartyAccounts"].Columns.Select(x => x.Name).Should().BeEquivalentTo(new[]
        {
            "Id", "ThirdPartyId", "FinancialInstitutionId", "AccountType", "AccountNumberNormalized",
            "Status", "OriginSource", "CreatedAt", "UpdatedAt"
        });
        tables["ThirdPartyAccounts"].ForeignKeys.Should().HaveCount(2);

        var forbiddenFragments = new[]
        {
            "ClearingHouse", "TransactionNature", "ServiceType", "Prenotification", "Cycle", "Deadline", "Sla"
        };
        foreach (var table in tables.Values)
        {
            foreach (var column in table.Columns)
            {
                forbiddenFragments.Should().NotContain(fragment =>
                    column.Name.Contains(fragment, StringComparison.OrdinalIgnoreCase));
            }
        }

        var indexes = operations.OfType<CreateIndexOperation>().ToArray();
        indexes.Should().Contain(x =>
            x.Name == "UX_ThirdParties_Customer_Document"
            && x.IsUnique
            && x.Columns.SequenceEqual(new[] { "CustomerId", "DocumentType", "DocumentNumberNormalized" }));
        indexes.Should().Contain(x =>
            x.Name == "UX_ThirdPartyAccounts_ThirdParty_Institution_Type_Number"
            && x.IsUnique
            && x.Columns.SequenceEqual(new[] { "ThirdPartyId", "FinancialInstitutionId", "AccountType", "AccountNumberNormalized" }));
    }

    private static string[] LogicalSignature(IReadOnlyList<MigrationOperation> operations)
        => operations.OfType<CreateTableOperation>()
            .OrderBy(x => x.Name, StringComparer.Ordinal)
            .Select(x => $"T:{x.Name}:{string.Join(',', x.Columns.Select(c => c.Name).OrderBy(n => n, StringComparer.Ordinal))}:FK={x.ForeignKeys.Count}")
            .Concat(operations.OfType<CreateIndexOperation>()
                .OrderBy(x => x.Name, StringComparer.Ordinal)
                .Select(x => $"I:{x.Name}:{x.IsUnique}:{string.Join(',', x.Columns)}"))
            .ToArray();

    private static IReadOnlyList<MigrationOperation> Operations(Migration migration, string provider)
    {
        var builder = new MigrationBuilder(provider);
        var method = migration.GetType().GetMethod(
            "Up",
            System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)
            ?? throw new InvalidOperationException("Migration Up method not found.");
        method.Invoke(migration, new object?[] { builder });
        return builder.Operations;
    }
}
