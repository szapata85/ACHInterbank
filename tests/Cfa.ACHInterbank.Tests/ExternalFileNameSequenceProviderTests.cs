using Cfa.ACHInterbank.Application.ACH.Models.ExternalFileNames;
using Cfa.ACHInterbank.Persistence.ACH.Services.Implementation.ExternalFileNames;
using Cfa.ACHInterbank.Persistence.DataBase;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace Cfa.ACHInterbank.Tests;

public class ExternalFileNameSequenceProviderTests
{
    [Fact]
    public void Resolver_Selects_PostgresProvider_WhenNpgsqlProviderName()
    {
        var postgres = new FakeProvider("postgres");
        var sql = new FakeProvider("sqlserver");
        var resolver = new ExternalFileNameSequenceProviderResolver([postgres, sql]);

        var resolved = resolver.Resolve("Npgsql.EntityFrameworkCore.PostgreSQL");

        Assert.Same(postgres, resolved);
    }

    [Fact]
    public void Resolver_Selects_SqlServerProvider_WhenSqlServerProviderName()
    {
        var postgres = new FakeProvider("postgres");
        var sql = new FakeProvider("sqlserver");
        var resolver = new ExternalFileNameSequenceProviderResolver([postgres, sql]);

        var resolved = resolver.Resolve("Microsoft.EntityFrameworkCore.SqlServer");

        Assert.Same(sql, resolved);
    }

    [Fact]
    public void Resolver_ThrowsClearError_WhenNoProviderCanHandle()
    {
        var resolver = new ExternalFileNameSequenceProviderResolver([new FakeProvider("postgres")]);

        var ex = Assert.Throws<NotSupportedException>(() => resolver.Resolve("Microsoft.EntityFrameworkCore.SqlServer"));

        Assert.Contains("No ExternalFileName sequence provider", ex.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task SqlServerAdapter_Throws_NotSupportedException_ClearMessage()
    {
        var adapter = new SqlServerExternalFileNameSequenceService();

        var ex = await Assert.ThrowsAsync<NotSupportedException>(() => adapter.ReserveNextSequenceAsync(CreateContext()));

        Assert.Contains("not implemented", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task PostgresAdapter_Enforces_AchDailyLimit_36()
    {
        await using var harness = await CreateHarnessAsync();
        var adapter = new TestPostgresAdapter(harness.Context, 37);

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => adapter.ReserveNextSequenceAsync(CreateContext()));

        Assert.Contains("máximo 36", ex.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Application_And_Domain_DoNotReference_Npgsql()
    {
        var appFiles = Directory.GetFiles(Path.Combine(GetRepositoryRoot(), "src/Cfa.ACHInterbank.Application"), "*.cs", SearchOption.AllDirectories);
        var domainFiles = Directory.GetFiles(Path.Combine(GetRepositoryRoot(), "src/Cfa.ACHInterbank.Domain"), "*.cs", SearchOption.AllDirectories);

        var offenders = appFiles.Concat(domainFiles)
            .Where(path => File.ReadAllText(path).Contains("Npgsql", StringComparison.Ordinal))
            .ToList();

        Assert.True(offenders.Count == 0, $"Npgsql reference found outside Persistence: {string.Join(", ", offenders)}");
    }

    [Fact]
    public void ExternalFileName_Providers_KeepNpgsqlScopedToPostgresAdapter()
    {
        var files = Directory.GetFiles(Path.Combine(GetRepositoryRoot(), "src/Cfa.ACHInterbank.Persistence/ACH/Services/Implementation/ExternalFileNames"), "*.cs", SearchOption.AllDirectories)
            .Where(path => !path.EndsWith("PostgresExternalFileNameSequenceService.cs", StringComparison.Ordinal))
            .ToList();

        var offenders = files
            .Where(path => File.ReadAllText(path).Contains("Npgsql", StringComparison.Ordinal))
            .ToList();

        Assert.True(offenders.Count == 0, $"Unexpected Npgsql references: {string.Join(", ", offenders)}");
    }


    private static string GetRepositoryRoot()
    {
        var current = new DirectoryInfo(AppContext.BaseDirectory);
        while (current is not null)
        {
            if (File.Exists(Path.Combine(current.FullName, "ACHInterbank.sln")))
            {
                return current.FullName;
            }

            current = current.Parent;
        }

        throw new DirectoryNotFoundException("Could not resolve repository root from test base directory.");
    }

    private static ExternalFileNameContext CreateContext() => new()
    {
        ClearingHouseId = 1,
        ClearingHouseCode = "ACH",
        ProcessingDate = new DateTime(2026, 4, 20),
        ExternalFileType = ExternalFileType.NachaOut,
        Flow = ExternalFileFlow.Originacion,
        Direction = ExternalFileDirection.Outbound
    };

    private sealed class FakeProvider(string kind) : IExternalFileNameSequenceProvider
    {
        public bool CanHandle(string? providerName)
            => kind == "postgres"
                ? providerName?.Contains("Npgsql", StringComparison.OrdinalIgnoreCase) == true
                : providerName?.Contains("SqlServer", StringComparison.OrdinalIgnoreCase) == true;

        public Task<int> ReserveNextSequenceAsync(ExternalFileNameContext context, CancellationToken ct = default)
            => Task.FromResult(1);
    }

    private sealed class TestPostgresAdapter : PostgresExternalFileNameSequenceService
    {
        private readonly int _next;

        public TestPostgresAdapter(AchDbContext context, int next) : base(context)
        {
            _next = next;
        }

        protected override Task<int> ExecuteUpsertAsync(ExternalFileNameContext context, CancellationToken ct)
            => Task.FromResult(_next);
    }

    private static async Task<SqliteHarness> CreateHarnessAsync()
    {
        var connection = new SqliteConnection("DataSource=:memory:");
        await connection.OpenAsync();

        var options = new DbContextOptionsBuilder<AchDbContext>()
            .UseSqlite(connection)
            .Options;

        var context = new AchDbContext(options);
        await context.Database.EnsureCreatedAsync();
        return new SqliteHarness(connection, context);
    }

    private sealed class SqliteHarness(SqliteConnection connection, AchDbContext context) : IAsyncDisposable
    {
        public SqliteConnection Connection { get; } = connection;
        public AchDbContext Context { get; } = context;

        public async ValueTask DisposeAsync()
        {
            await Context.DisposeAsync();
            await Connection.DisposeAsync();
        }
    }
}
