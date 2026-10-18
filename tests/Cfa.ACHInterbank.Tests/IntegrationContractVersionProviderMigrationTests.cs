using System.Data;
using System.Data.Common;
using Cfa.ACHInterbank.Persistence.DataBase;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql;
using Xunit;

namespace Cfa.ACHInterbank.Tests;

public enum CfaContractMigrationProvider
{
    SqlServer,
    PostgreSql
}

public static class CfaContractMigrationTestConfiguration
{
    public const string RequireDatabasesVariable = "CFA_P8_REQUIRE_DATABASES";

    public static string VariableName(CfaContractMigrationProvider provider)
        => provider == CfaContractMigrationProvider.SqlServer
            ? "CFA_P8_SQLSERVER_CONNECTION_STRING"
            : "CFA_P8_POSTGRES_CONNECTION_STRING";

    public static bool IsRequired()
        => IsTrue(Environment.GetEnvironmentVariable(RequireDatabasesVariable));

    public static bool IsConfigured(CfaContractMigrationProvider provider)
        => !string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable(VariableName(provider)));

    public static void EnsureConfigured(CfaContractMigrationProvider provider)
    {
        if (!IsConfigured(provider))
        {
            throw new InvalidOperationException(
                $"CFA P8 migration validation requires {VariableName(provider)}. " +
                $"Set {RequireDatabasesVariable}=true in CI so a missing provider is a hard failure.");
        }
    }

    private static bool IsTrue(string? value)
        => value is not null &&
           (value.Equals("1", StringComparison.OrdinalIgnoreCase)
            || value.Equals("true", StringComparison.OrdinalIgnoreCase)
            || value.Equals("yes", StringComparison.OrdinalIgnoreCase));
}

public sealed class CfaContractMigrationFactAttribute : FactAttribute
{
    public CfaContractMigrationFactAttribute(CfaContractMigrationProvider provider)
    {
        if (!CfaContractMigrationTestConfiguration.IsRequired()
            && !CfaContractMigrationTestConfiguration.IsConfigured(provider))
        {
            Skip = $"External DB gate omitted locally: set {CfaContractMigrationTestConfiguration.VariableName(provider)}.";
        }
    }
}

[Trait("Category", "CfaSoapP8Migration")]
public sealed class IntegrationContractVersionProviderMigrationTests
{
    [CfaContractMigrationFact(CfaContractMigrationProvider.SqlServer)]
    public Task CleanCreate_ShouldProduceVersionedContractSchemaAndExactlyOneActiveBaselinePerMethod_SqlServer()
        => CleanCreate_ShouldProduceVersionedContractSchemaAndExactlyOneActiveBaselinePerMethod(CfaContractMigrationProvider.SqlServer);

    [CfaContractMigrationFact(CfaContractMigrationProvider.PostgreSql)]
    public Task CleanCreate_ShouldProduceVersionedContractSchemaAndExactlyOneActiveBaselinePerMethod_PostgreSql()
        => CleanCreate_ShouldProduceVersionedContractSchemaAndExactlyOneActiveBaselinePerMethod(CfaContractMigrationProvider.PostgreSql);

    [CfaContractMigrationFact(CfaContractMigrationProvider.SqlServer)]
    public Task Upgrade_ShouldBackfillContractVersionAndPreserveLegacyParameterMappingAndRule_SqlServer()
        => Upgrade_ShouldBackfillContractVersionAndPreserveLegacyParameterMappingAndRule(CfaContractMigrationProvider.SqlServer);

    [CfaContractMigrationFact(CfaContractMigrationProvider.PostgreSql)]
    public Task Upgrade_ShouldBackfillContractVersionAndPreserveLegacyParameterMappingAndRule_PostgreSql()
        => Upgrade_ShouldBackfillContractVersionAndPreserveLegacyParameterMappingAndRule(CfaContractMigrationProvider.PostgreSql);

    private static async Task CleanCreate_ShouldProduceVersionedContractSchemaAndExactlyOneActiveBaselinePerMethod(
        CfaContractMigrationProvider provider)
    {
        await using var fixture = await ProviderFixture.CreateAsync(provider);
        CfaContractMigrationTestConfiguration.EnsureConfigured(provider);
        await using var context = fixture.CreateContext();

        await context.Database.MigrateAsync();

        var activeByMethod = await ReadActiveBaselineCountsAsync(context, provider);
        Assert.Equal(3, activeByMethod.Count);
        Assert.All(activeByMethod.Values, count => Assert.Equal(1, count));

        var contractVersionIdColumns = await ScalarAsync<long>(context, provider,
            provider == CfaContractMigrationProvider.SqlServer
                ? "SELECT COUNT(*) FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME IN ('IntegrationMethodParameters','IntegrationMappingSets') AND COLUMN_NAME = 'ContractVersionId'"
                : "SELECT COUNT(*) FROM information_schema.columns WHERE table_schema = current_schema() AND table_name IN ('IntegrationMethodParameters','IntegrationMappingSets') AND column_name = 'ContractVersionId'");
        Assert.Equal(2, contractVersionIdColumns);
    }

    private static async Task Upgrade_ShouldBackfillContractVersionAndPreserveLegacyParameterMappingAndRule(
        CfaContractMigrationProvider provider)
    {
        await using var fixture = await ProviderFixture.CreateAsync(provider);
        CfaContractMigrationTestConfiguration.EnsureConfigured(provider);
        await using var context = fixture.CreateContext();
        var migrator = context.Database.GetService<IMigrator>();
        var migrations = context.Database.GetMigrations().ToList();
        var baselineMigration = migrations.Single(id => id.EndsWith("_IntegrationsAddContractVersioningBaseline", StringComparison.Ordinal));
        var baselineIndex = migrations.IndexOf(baselineMigration);
        Assert.True(baselineIndex > 0);
        var previousMigration = migrations[baselineIndex - 1];

        await migrator.MigrateAsync(previousMigration);

        var legacy = await SeedLegacyContractDataAsync(context, provider);
        await migrator.MigrateAsync();

        var baselineId = await ScalarAsync<Guid>(context, provider,
            $"SELECT cv.{Q(provider, "Id")} FROM {Q(provider, "IntegrationContractVersions")} cv " +
            $"INNER JOIN {Q(provider, "IntegrationMethods")} m ON m.{Q(provider, "Id")} = cv.{Q(provider, "MethodId")} " +
            $"WHERE m.{Q(provider, "Code")} = 'WSCFAACH.Proc_Transacciones' AND cv.{Q(provider, "VersionLabel")} = 'baseline-v1'");

        var parameterVersionId = await ScalarAsync<Guid>(context, provider,
            $"SELECT {Q(provider, "ContractVersionId")} FROM {Q(provider, "IntegrationMethodParameters")} WHERE {Q(provider, "Id")} = {legacy.ParameterId}");
        var mappingVersionId = await ScalarAsync<Guid>(context, provider,
            $"SELECT {Q(provider, "ContractVersionId")} FROM {Q(provider, "IntegrationMappingSets")} WHERE {Q(provider, "Id")} = '{legacy.MappingSetId}'");
        var mappingRevision = await ScalarAsync<long>(context, provider,
            $"SELECT {Q(provider, "Revision")} FROM {Q(provider, "IntegrationMappingSets")} WHERE {Q(provider, "Id")} = '{legacy.MappingSetId}'");
        var ruleParameterId = await ScalarAsync<long>(context, provider,
            $"SELECT {Q(provider, "ParameterId")} FROM {Q(provider, "IntegrationMappingRules")} WHERE {Q(provider, "MappingSetId")} = '{legacy.MappingSetId}'");
        var parameterCount = await ScalarAsync<long>(context, provider,
            $"SELECT COUNT(*) FROM {Q(provider, "IntegrationMethodParameters")} WHERE {Q(provider, "Id")} = {legacy.ParameterId} AND {Q(provider, "ParameterPath")} = 'P8MigrationFixtureField'");
        var mappingCount = await ScalarAsync<long>(context, provider,
            $"SELECT COUNT(*) FROM {Q(provider, "IntegrationMappingSets")} WHERE {Q(provider, "Id")} = '{legacy.MappingSetId}'");

        Assert.Equal(baselineId, parameterVersionId);
        Assert.Equal(baselineId, mappingVersionId);
        Assert.True(mappingRevision >= 1);
        Assert.Equal(legacy.ParameterId, ruleParameterId);
        Assert.Equal(1, parameterCount);
        Assert.Equal(1, mappingCount);

        var activeByMethod = await ReadActiveBaselineCountsAsync(context, provider);
        Assert.All(activeByMethod.Values, count => Assert.Equal(1, count));

        await AssertVersionedUniquenessConstraintsRejectDuplicatesAsync(context, provider, baselineId, legacy.MethodId);
    }

    private static async Task<LegacySeed> SeedLegacyContractDataAsync(AchDbContext context, CfaContractMigrationProvider provider)
    {
        var now = DateTimeOffset.UtcNow;
        var methodId = await InsertIdentityAsync(context, provider, "IntegrationMethods", "Id", new Dictionary<string, object?>
        {
            ["Code"] = "WSCFAACH.Proc_Transacciones",
            ["DisplayName"] = "Proc_Transacciones migration fixture",
            ["SoapClientCode"] = "WscfaachSoapClient",
            ["IsActive"] = true,
            ["CreatedAt"] = now,
            ["UpdatedAt"] = now
        });

        var parameterId = await InsertIdentityAsync(context, provider, "IntegrationMethodParameters", "Id", new Dictionary<string, object?>
        {
            ["MethodId"] = methodId,
            ["ParameterPath"] = "P8MigrationFixtureField",
            ["DisplayName"] = "Fixture field",
            ["DescriptionEs"] = "Synthetic migration fixture",
            ["Category"] = "Test",
            ["ExampleValue"] = "fixture",
            ["UiHelpText"] = "Synthetic fixture",
            ["DataType"] = "string",
            ["Direction"] = 1,
            ["Cardinality"] = 1,
            ["Required"] = true,
            ["SortOrder"] = 999,
            ["IsActive"] = true,
            ["CreatedAt"] = now,
            ["UpdatedAt"] = now
        });

        var mappingSetId = Guid.NewGuid();
        await InsertAsync(context, provider, "IntegrationMappingSets", new Dictionary<string, object?>
        {
            ["Id"] = mappingSetId,
            ["MethodId"] = methodId,
            ["Name"] = "P8 migration fixture mapping",
            ["Version"] = 77,
            ["Status"] = 1,
            ["IsActive"] = true,
            ["Notes"] = "Synthetic migration fixture",
            ["PublishedAtUtc"] = null,
            ["PublishedBy"] = "",
            ["ValidationSummaryJson"] = "{}",
            ["CreatedAt"] = now,
            ["UpdatedAt"] = now
        });

        await InsertAsync(context, provider, "IntegrationMappingRules", new Dictionary<string, object?>
        {
            ["MappingSetId"] = mappingSetId,
            ["MethodId"] = methodId,
            ["ParameterId"] = parameterId,
            ["SourceKind"] = 1,
            ["SourceCatalogFieldId"] = null,
            ["SourceFieldPath"] = "fixture.field",
            ["FixedValue"] = null,
            ["DefaultValue"] = null,
            ["TransformationCode"] = null,
            ["FormatMask"] = null,
            ["Priority"] = 1,
            ["RequiredOverride"] = null,
            ["Enabled"] = true,
            ["ConditionExpression"] = null,
            ["CreatedAt"] = now,
            ["UpdatedAt"] = now
        });

        return new LegacySeed(methodId, parameterId, mappingSetId);
    }

    private static async Task AssertVersionedUniquenessConstraintsRejectDuplicatesAsync(
        AchDbContext context,
        CfaContractMigrationProvider provider,
        Guid baselineId,
        long methodId)
    {
        await Assert.ThrowsAnyAsync<DbException>(() => InsertAsync(context, provider, "IntegrationMethodParameters", new Dictionary<string, object?>
        {
            ["MethodId"] = methodId,
            ["ContractVersionId"] = baselineId,
            ["ParameterPath"] = "P8MigrationFixtureField",
            ["DisplayName"] = "Duplicate",
            ["DescriptionEs"] = "Synthetic duplicate",
            ["Category"] = "Test",
            ["ExampleValue"] = "fixture",
            ["UiHelpText"] = "Synthetic fixture",
            ["DataType"] = "string",
            ["Direction"] = 1,
            ["Cardinality"] = 1,
            ["Required"] = false,
            ["SortOrder"] = 1000,
            ["IsActive"] = true,
            ["XmlElementName"] = "P8MigrationFixtureField",
            ["XsdType"] = "string",
            ["Nillable"] = false,
            ["MinOccurs"] = 0,
            ["MaxOccurs"] = 1,
            ["SupportStatus"] = "Supported",
            ["CreatedAt"] = DateTimeOffset.UtcNow,
            ["UpdatedAt"] = DateTimeOffset.UtcNow
        }));

        await Assert.ThrowsAnyAsync<DbException>(() => InsertAsync(context, provider, "IntegrationContractVersions", new Dictionary<string, object?>
        {
            ["Id"] = Guid.NewGuid(),
            ["MethodId"] = methodId,
            ["VersionLabel"] = "p8-duplicate-active",
            ["Status"] = 3,
            ["OperationName"] = "Proc_Transacciones",
            ["TargetNamespace"] = "http://tempuri.org/",
            ["SoapAction"] = "http://tempuri.org/IWSCFAACH/Proc_Transacciones",
            ["SourceType"] = "SyntheticTest",
            ["SourceDocumentName"] = null,
            ["SourceDocumentHash"] = null,
            ["SourceReference"] = "CFA-SOAP-UX-MAP-001/P8.4-A",
            ["Notes"] = "Synthetic uniqueness probe",
            ["Revision"] = 1L,
            ["ImportedAtUtc"] = null,
            ["ImportedBy"] = null,
            ["ActivatedAtUtc"] = DateTime.UtcNow,
            ["ActivatedBy"] = "P8.4-A",
            ["CreatedAt"] = DateTimeOffset.UtcNow,
            ["UpdatedAt"] = DateTimeOffset.UtcNow
        }));
    }

    private static async Task<Dictionary<long, int>> ReadActiveBaselineCountsAsync(AchDbContext context, CfaContractMigrationProvider provider)
    {
        var connection = await OpenAsync(context);
        await using var command = connection.CreateCommand();
        command.CommandText =
            $"SELECT {Q(provider, "MethodId")}, COUNT(*) FROM {Q(provider, "IntegrationContractVersions")} " +
            $"WHERE {Q(provider, "Status")} = 3 AND {Q(provider, "VersionLabel")} = 'baseline-v1' GROUP BY {Q(provider, "MethodId")}";
        var result = new Dictionary<long, int>();
        await using var reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            result[Convert.ToInt64(reader.GetValue(0))] = Convert.ToInt32(reader.GetValue(1));
        }
        return result;
    }

    private static async Task<long> InsertIdentityAsync(
        AchDbContext context,
        CfaContractMigrationProvider provider,
        string table,
        string identityColumn,
        IReadOnlyDictionary<string, object?> values)
    {
        var connection = await OpenAsync(context);
        await using var command = BuildInsertCommand(connection, provider, table, values);
        command.CommandText += provider == CfaContractMigrationProvider.SqlServer
            ? $"; SELECT CAST(SCOPE_IDENTITY() AS bigint);"
            : $" RETURNING {Q(provider, identityColumn)};";
        var result = await command.ExecuteScalarAsync();
        Assert.NotNull(result);
        return Convert.ToInt64(result);
    }

    private static async Task InsertAsync(
        AchDbContext context,
        CfaContractMigrationProvider provider,
        string table,
        IReadOnlyDictionary<string, object?> values)
    {
        var connection = await OpenAsync(context);
        await using var command = BuildInsertCommand(connection, provider, table, values);
        await command.ExecuteNonQueryAsync();
    }

    private static DbCommand BuildInsertCommand(
        DbConnection connection,
        CfaContractMigrationProvider provider,
        string table,
        IReadOnlyDictionary<string, object?> values)
    {
        var command = connection.CreateCommand();
        var names = values.Keys.ToArray();
        var parameters = names.Select((_, index) => $"@p{index}").ToArray();
        command.CommandText = $"INSERT INTO {Q(provider, table)} ({string.Join(", ", names.Select(name => Q(provider, name)))}) VALUES ({string.Join(", ", parameters)})";
        for (var index = 0; index < names.Length; index++)
        {
            var parameter = command.CreateParameter();
            parameter.ParameterName = parameters[index];
            parameter.Value = values[names[index]] ?? DBNull.Value;
            command.Parameters.Add(parameter);
        }
        return command;
    }

    private static async Task<T> ScalarAsync<T>(
        AchDbContext context,
        CfaContractMigrationProvider provider,
        string sql)
    {
        _ = provider;
        var connection = await OpenAsync(context);
        await using var command = connection.CreateCommand();
        command.CommandText = sql;
        var result = await command.ExecuteScalarAsync();
        Assert.NotNull(result);
        if (typeof(T) == typeof(Guid))
        {
            return (T)(object)(result is Guid guid ? guid : Guid.Parse(Convert.ToString(result)!));
        }
        return (T)Convert.ChangeType(result, typeof(T), System.Globalization.CultureInfo.InvariantCulture);
    }

    private static async Task<DbConnection> OpenAsync(AchDbContext context)
    {
        var connection = context.Database.GetDbConnection();
        if (connection.State != ConnectionState.Open)
        {
            await connection.OpenAsync();
        }
        return connection;
    }

    private static string Q(CfaContractMigrationProvider provider, string identifier)
        => provider == CfaContractMigrationProvider.SqlServer ? $"[{identifier}]" : $"\"{identifier}\"";

    private sealed record LegacySeed(long MethodId, long ParameterId, Guid MappingSetId);

    private sealed class ProviderFixture : IAsyncDisposable
    {
        private const string SqlServerMigrationsAssembly = "Cfa.ACHInterbank.Persistence.Migrations.SqlServer";
        private readonly string _connectionString;
        private readonly string _adminConnectionString;
        private readonly string? _databaseName;
        private readonly string? _schemaName;

        private ProviderFixture(
            CfaContractMigrationProvider provider,
            string connectionString,
            string adminConnectionString,
            string? databaseName,
            string? schemaName)
        {
            Provider = provider;
            _connectionString = connectionString;
            _adminConnectionString = adminConnectionString;
            _databaseName = databaseName;
            _schemaName = schemaName;
        }

        public CfaContractMigrationProvider Provider { get; }

        public static async Task<ProviderFixture> CreateAsync(CfaContractMigrationProvider provider)
        {
            CfaContractMigrationTestConfiguration.EnsureConfigured(provider);
            var baseConnectionString = Environment.GetEnvironmentVariable(CfaContractMigrationTestConfiguration.VariableName(provider))!;

            if (provider == CfaContractMigrationProvider.SqlServer)
            {
                var admin = new SqlConnectionStringBuilder(baseConnectionString) { InitialCatalog = "master" };
                var databaseName = $"achinterbank_cfa_p8_{Guid.NewGuid():N}";
                await using var connection = new SqlConnection(admin.ConnectionString);
                await connection.OpenAsync();
                await using var command = connection.CreateCommand();
                command.CommandText = $"CREATE DATABASE [{databaseName}]";
                await command.ExecuteNonQueryAsync();
                var target = new SqlConnectionStringBuilder(baseConnectionString) { InitialCatalog = databaseName };
                return new ProviderFixture(provider, target.ConnectionString, admin.ConnectionString, databaseName, null);
            }

            var postgres = new NpgsqlConnectionStringBuilder(baseConnectionString);
            var schemaName = $"cfa_p8_{Guid.NewGuid():N}";
            await using (var connection = new NpgsqlConnection(postgres.ConnectionString))
            {
                await connection.OpenAsync();
                await using var command = connection.CreateCommand();
                command.CommandText = $"CREATE SCHEMA \"{schemaName}\"";
                await command.ExecuteNonQueryAsync();
            }
            postgres.SearchPath = schemaName;
            return new ProviderFixture(provider, postgres.ConnectionString, baseConnectionString, null, schemaName);
        }

        public AchDbContext CreateContext()
        {
            var options = new DbContextOptionsBuilder<AchDbContext>();
            if (Provider == CfaContractMigrationProvider.SqlServer)
            {
                options.UseSqlServer(_connectionString, sql => sql.MigrationsAssembly(SqlServerMigrationsAssembly));
            }
            else
            {
                options.UseNpgsql(_connectionString);
            }
            options.ConfigureWarnings(warnings => warnings.Ignore(RelationalEventId.PendingModelChangesWarning));
            return new AchDbContext(options.Options);
        }

        public async ValueTask DisposeAsync()
        {
            if (Provider == CfaContractMigrationProvider.SqlServer)
            {
                await using var connection = new SqlConnection(_adminConnectionString);
                await connection.OpenAsync();
                await using var command = connection.CreateCommand();
                command.CommandText = $"ALTER DATABASE [{_databaseName}] SET SINGLE_USER WITH ROLLBACK IMMEDIATE; DROP DATABASE [{_databaseName}]";
                await command.ExecuteNonQueryAsync();
                return;
            }

            await using var postgresConnection = new NpgsqlConnection(_adminConnectionString);
            await postgresConnection.OpenAsync();
            await using var postgresCommand = postgresConnection.CreateCommand();
            postgresCommand.CommandText = $"DROP SCHEMA IF EXISTS \"{_schemaName}\" CASCADE";
            await postgresCommand.ExecuteNonQueryAsync();
        }
    }
}
