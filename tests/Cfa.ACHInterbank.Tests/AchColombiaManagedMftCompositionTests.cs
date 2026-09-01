using Cfa.ACHInterbank.Api.Controllers;
using Cfa.ACHInterbank.Api;
using Cfa.ACHInterbank.Application;
using Cfa.ACHInterbank.Application.ACH.Interfaces;
using Cfa.ACHInterbank.Application.ACH.Models;
using Cfa.ACHInterbank.Application.Features;
using Cfa.ACHInterbank.Application.JobsQuartz.Interfaces;
using Cfa.ACHInterbank.Domain.Entities.SchedulerTask;
using Cfa.ACHInterbank.Domain.Models.ACH;
using Cfa.ACHInterbank.Domain.Models.Configurations;
using Cfa.ACHInterbank.External;
using Cfa.ACHInterbank.External.Connections;
using Cfa.ACHInterbank.Persistence;
using Cfa.ACHInterbank.Persistence.ACH.Quartz.Jobs;
using Cfa.ACHInterbank.Persistence.ACH.Quartz.Jobs.Implementation;
using Cfa.ACHInterbank.Persistence.DataBase;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Moq;

namespace Cfa.ACHInterbank.Tests;

public sealed class AchColombiaManagedMftCompositionTests
{
    [Fact]
    public void ProductionComposition_WithManagedMftDisabled_ShouldResolveManagedMftAndSchedulerGraph()
    {
        var builder = WebApplication.CreateBuilder(new WebApplicationOptions
        {
            EnvironmentName = Environments.Development
        });
        builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Database:Provider"] = "Postgres",
            ["ConnectionStrings:PostgresConnection"] = "Host=localhost;Database=achinterbank_di;Username=test;Password=test",
            ["appSettings:tokenManager:secretKetJwt"] = "test-only-secret-key-with-at-least-32-bytes",
            ["appSettings:tokenManager:issuerJwt"] = "test-issuer",
            ["appSettings:tokenManager:audienceJwt"] = "test-audience",
            ["Quartz:Mode"] = "RAM",
            ["Quartz:JobStore:Mode"] = "RAM",
            ["AchColombiaManagedMft:Enabled"] = "false"
        });
        AppSettings.Settings = builder.Configuration.GetSection("appSettings").Get<AppSettings>()!.GetMethodExtensions();

        builder.Services
            .AddWebApi(builder.Configuration)
            .AddApplication()
            .AddPersistence(builder.Configuration, builder.Environment)
            .AddExternal(builder.Configuration);

        using var provider = builder.Services.BuildServiceProvider(new ServiceProviderOptions
        {
            ValidateOnBuild = true,
            ValidateScopes = true
        });
        using var scope = provider.CreateScope();

        Assert.IsType<AchColombiaManagedMftFolderAdapter>(scope.ServiceProvider.GetRequiredService<IAchColombiaManagedMftAdapter>());
        Assert.NotNull(scope.ServiceProvider.GetRequiredService<IAchColombiaManagedFileExchangeService>());
        var handlers = scope.ServiceProvider.GetServices<ITaskHandler>().ToArray();
        Assert.Contains(handlers, handler => handler is AchColombiaManagedMftOutboundHandler);
        Assert.Contains(handlers, handler => handler is AchColombiaManagedMftInboundHandler);
        Assert.NotNull(scope.ServiceProvider.GetRequiredService<DynamicJobExecutor>());
        Assert.NotNull(scope.ServiceProvider.GetRequiredService<DynamicJob>());
        Assert.NotNull(scope.ServiceProvider.GetRequiredService<NonConcurrentDynamicJob>());
    }

    [Fact]
    public async Task QuartzInboundHandler_ShouldInvokeSharedApplicationServiceAsAutomatic()
    {
        var service = new Mock<IAchColombiaManagedFileExchangeService>();
        service.Setup(x => x.ExecuteInboundAsync(AchManagedFileExecutionOrigin.Automatic, It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AchManagedFileExecutionResult(1, 1, 0, [Guid.NewGuid()]));
        await new AchColombiaManagedMftInboundHandler(service.Object).ExecuteAsync(new TaskDefinition(), default);
        service.VerifyAll();
    }

    [Fact]
    public async Task QuartzOutboundHandler_ShouldInvokeSharedApplicationServiceAsAutomatic()
    {
        await using var context = new AchDbContext(new DbContextOptionsBuilder<AchDbContext>().UseInMemoryDatabase($"handler-{Guid.NewGuid():N}").Options);
        context.ClearingHouses.Add(new ClearingHouse { Id = 1, ClearingHouseId = 1, Code = "ACHCOL", Name = "ACH Colombia", OriginCode = "1" });
        context.AchCycles.Add(new AchCycle { Id = "ACH-1", CycleName = "Ciclo 1", ProcessingDate = DateTime.UtcNow.Date, ClearingHouseId = 1 });
        await context.SaveChangesAsync();
        var service = new Mock<IAchColombiaManagedFileExchangeService>();
        service.Setup(x => x.ExecuteOutboundAsync("ACH-1", AchManagedFileExecutionOrigin.Automatic, It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>(), It.IsAny<Guid?>()))
            .ReturnsAsync(new AchManagedFileExecutionResult(1, 1, 0, [Guid.NewGuid()]));
        await new AchColombiaManagedMftOutboundHandler(context, service.Object).ExecuteAsync(new TaskDefinition(), default);
        service.VerifyAll();
    }

    [Fact]
    public void Api_ShouldAuthorizeDownload_AndExposeNoHardDeleteOrContentEdit()
    {
        var methods = typeof(AchColombiaFileExchangeController).GetMethods().Where(x => x.DeclaringType == typeof(AchColombiaFileExchangeController)).ToArray();
        var download = methods.Single(x => x.Name == nameof(AchColombiaFileExchangeController.Download));
        Assert.Contains(download.GetCustomAttributes(typeof(AuthorizeAttribute), true).Cast<AuthorizeAttribute>(), x => x.Policy == "CanReadAch");
        Assert.DoesNotContain(methods.SelectMany(x => x.GetCustomAttributes(true)), x => x.GetType().Name == "HttpDeleteAttribute");
        Assert.DoesNotContain(methods.SelectMany(x => x.GetCustomAttributes(typeof(HttpMethodAttribute), true).Cast<HttpMethodAttribute>()),
            x => x.Template?.Contains("content", StringComparison.OrdinalIgnoreCase) == true);
    }

    [Fact]
    public void PersistenceModel_ShouldEnforceDurableOutboundAndContentDuplicateProtection()
    {
        using var context = new AchDbContext(new DbContextOptionsBuilder<AchDbContext>().UseInMemoryDatabase("mft-model").Options);
        var entity = context.Model.FindEntityType(typeof(AchManagedFileTransfer))!;
        Assert.Contains(entity.GetIndexes(), x => x.IsUnique && x.Properties.Select(p => p.Name).SequenceEqual([nameof(AchManagedFileTransfer.AchFileExportId)]));
        Assert.Contains(entity.GetIndexes(), x => x.IsUnique && x.Properties.Select(p => p.Name).SequenceEqual([
            nameof(AchManagedFileTransfer.Direction), nameof(AchManagedFileTransfer.ContentSha256), nameof(AchManagedFileTransfer.FileSize)]));
        Assert.True(entity.FindProperty(nameof(AchManagedFileTransfer.ConcurrencyToken))!.IsConcurrencyToken);
    }
}
