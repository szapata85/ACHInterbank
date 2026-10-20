using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Cfa.ACHInterbank.Persistence.DataBase;

internal sealed class DatabaseInitializationHostedService : IHostedService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IConfiguration _configuration;
    private readonly ILogger<DatabaseInitializationHostedService> _logger;

    public DatabaseInitializationHostedService(
        IServiceScopeFactory scopeFactory,
        IConfiguration configuration,
        ILogger<DatabaseInitializationHostedService> logger)
    {
        _scopeFactory = scopeFactory;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        if (!_configuration.GetValue("Database:ApplyMigrations", false)
            && !_configuration.GetValue("Database:ApplySeed", false))
        {
            _logger.LogInformation("Skipping database initialization.");
            return;
        }

        using var scope = _scopeFactory.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AchDbContext>();

        if (_configuration.GetValue("Database:ApplyMigrations", false))
        {
            await context.Database.MigrateAsync(cancellationToken);
            _logger.LogInformation("Database migrations completed.");
        }

        if (_configuration.GetValue("Database:ApplySeed", false))
        {
            await DbInitializer.SeedAllAsync(scope.ServiceProvider);
            _logger.LogInformation("Database seed completed.");
        }
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
