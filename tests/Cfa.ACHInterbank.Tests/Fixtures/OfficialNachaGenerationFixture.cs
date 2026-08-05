using Cfa.ACHInterbank.Persistence.ACH.Services.Implementation.Seeders;
using Cfa.ACHInterbank.Persistence.DataBase;
using Microsoft.EntityFrameworkCore;

namespace Cfa.ACHInterbank.Tests;

public sealed class OfficialNachaGenerationFixture : IAsyncLifetime
{
    private readonly string _directory = Path.Combine(Path.GetTempPath(), $"achinterbank-official-nacha-{Guid.NewGuid():N}");
    private string TemplatePath => Path.Combine(_directory, "official-profiles-template.db");

    public int SeedExecutions { get; private set; }

    public async Task InitializeAsync()
    {
        Directory.CreateDirectory(_directory);

        await using var context = CreateContext(TemplatePath);
        await context.Database.EnsureCreatedAsync();
        await new NachaConfigOfficialProfilesSeeder(context).SeedAsync();
        SeedExecutions++;
    }

    public Task<AchDbContext> CreateSeededContextAsync()
    {
        var databasePath = CreateDatabasePath();
        File.Copy(TemplatePath, databasePath);
        return Task.FromResult(CreateContext(databasePath));
    }

    public async Task<AchDbContext> CreateEmptyContextAsync()
    {
        var context = CreateContext(CreateDatabasePath());
        await context.Database.EnsureCreatedAsync();
        return context;
    }

    public Task DisposeAsync()
    {
        if (Directory.Exists(_directory))
        {
            Directory.Delete(_directory, recursive: true);
        }

        return Task.CompletedTask;
    }

    private string CreateDatabasePath() => Path.Combine(_directory, $"case-{Guid.NewGuid():N}.db");

    private static AchDbContext CreateContext(string databasePath)
    {
        var options = new DbContextOptionsBuilder<AchDbContext>()
            .UseSqlite($"Data Source={databasePath};Cache=Private;Foreign Keys=False;Pooling=False")
            .Options;

        return new AchDbContext(options);
    }
}
