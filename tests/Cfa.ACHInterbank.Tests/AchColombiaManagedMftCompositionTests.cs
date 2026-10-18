using Cfa.ACHInterbank.Api.Controllers;
using Cfa.ACHInterbank.Api;
using Cfa.ACHInterbank.Application;
using Cfa.ACHInterbank.Application.ACH.Interfaces;
using Cfa.ACHInterbank.Application.ACH.Models;
using Cfa.ACHInterbank.Application.ACH.Models.ExternalFileNames;
using Cfa.ACHInterbank.Application.JobsQuartz.Interfaces;
using Cfa.ACHInterbank.Domain.Entities.SchedulerTask;
using Cfa.ACHInterbank.Domain.Models.ACH;
using Cfa.ACHInterbank.Domain.Models.Configurations;
using Cfa.ACHInterbank.External;
using Cfa.ACHInterbank.External.Connections;
using Cfa.ACHInterbank.Persistence;
using Cfa.ACHInterbank.Persistence.ACH.Quartz.Jobs;
using Cfa.ACHInterbank.Persistence.ACH.Quartz.Jobs.Implementation;
using Cfa.ACHInterbank.Persistence.ACH.Services.Implementation;
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
    public async Task MonitoringApi_ShouldForwardFiltersAndPreserveReadAuthorization()
    {
        var id = Guid.NewGuid();
        var service = new Mock<IAchColombiaManagedFileExchangeService>();
        service.Setup(x => x.QueryAsync(It.Is<AchManagedFileTransferQuery>(q => q.TransferId == id
            && q.FileName == "file.OUT" && q.Archived == false && q.PageNumber == 2 && q.PageSize == 25
            && q.Status == AchManagedFileTransferStatus.Uncertain), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<AchManagedFileTransferSummary>());
        var controller = new AchColombiaFileExchangeController(
            service.Object, service.Object, service.Object, service.Object, service.Object);
        Assert.Empty(await controller.Query(null, null, null, AchManagedFileTransferStatus.Uncertain,
            null, null, default, "file.OUT", id, false, 2, 25));
        service.VerifyAll();
        foreach (var methodName in new[] { nameof(controller.Query), nameof(controller.Get) })
            Assert.Contains(typeof(AchColombiaFileExchangeController).GetMethod(methodName)!
                .GetCustomAttributes(typeof(AuthorizeAttribute), true).Cast<AuthorizeAttribute>(), x => x.Policy == "CanReadAch");
    }

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
            ["appSettings:tokenManager:accessExpiration"] = "30",
            ["appSettings:tokenManager:expirationType"] = "m",
            ["appSettings:key"] = "12345678901234567890123456789012",
            ["Quartz:Mode"] = "RAM",
            ["Quartz:JobStore:Mode"] = "RAM",
            ["AchColombiaManagedMft:Enabled"] = "false"
        });
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

        Assert.IsType<AchColombiaManagedMftDispatchAdapter>(scope.ServiceProvider.GetRequiredService<IAchColombiaManagedMftAdapter>());
        Assert.NotNull(scope.ServiceProvider.GetRequiredService<IAchColombiaManagedMftAdapterResolver>());
        Assert.NotNull(scope.ServiceProvider.GetRequiredService<IAchColombiaManagedMftConnectionTester>());
        Assert.NotNull(scope.ServiceProvider.GetRequiredService<AchColombiaManagedMftFolderAdapter>());
        Assert.NotNull(scope.ServiceProvider.GetRequiredService<AchColombiaSftpMftAdapter>());
        Assert.NotNull(scope.ServiceProvider.GetRequiredService<AchColombiaManagedMftOutboundWorkflow>());
        Assert.NotNull(scope.ServiceProvider.GetRequiredService<AchColombiaManagedMftInboundWorkflow>());
        Assert.NotNull(scope.ServiceProvider.GetRequiredService<AchColombiaManagedMftRetryReprocessComponent>());
        Assert.NotNull(scope.ServiceProvider.GetRequiredService<AchColombiaManagedMftLifecycleComponent>());
        Assert.NotNull(scope.ServiceProvider.GetRequiredService<AchColombiaManagedMftQueryProjectionComponent>());
        Assert.NotNull(scope.ServiceProvider.GetRequiredService<AchColombiaManagedMftAdministrationComponent>());
        Assert.NotNull(scope.ServiceProvider.GetRequiredService<AchColombiaManagedMftCredentialComponent>());
        var facade = scope.ServiceProvider.GetRequiredService<IAchColombiaManagedFileExchangeService>();
        Assert.Same(facade, scope.ServiceProvider.GetRequiredService<IAchColombiaManagedMftExecutionService>());
        Assert.Same(facade, scope.ServiceProvider.GetRequiredService<IAchColombiaManagedMftTransferReadService>());
        Assert.Same(facade, scope.ServiceProvider.GetRequiredService<IAchColombiaManagedMftTransferOperationsService>());
        Assert.Same(facade, scope.ServiceProvider.GetRequiredService<IAchColombiaManagedMftAdministrationService>());
        Assert.Same(facade, scope.ServiceProvider.GetRequiredService<IAchColombiaManagedMftCredentialAdministrationService>());
        var handlers = scope.ServiceProvider.GetServices<ITaskHandler>().ToArray();
        Assert.Contains(handlers, handler => handler is AchColombiaManagedMftOutboundHandler);
        Assert.Contains(handlers, handler => handler is AchColombiaManagedMftInboundHandler);
        Assert.NotNull(scope.ServiceProvider.GetRequiredService<DynamicJobExecutor>());
        Assert.NotNull(scope.ServiceProvider.GetRequiredService<DynamicJob>());
        Assert.NotNull(scope.ServiceProvider.GetRequiredService<NonConcurrentDynamicJob>());
        Assert.NotNull(scope.ServiceProvider.GetRequiredService<IAchColombiaOperationalScheduleSynchronizer>());
    }

    [Fact]
    public async Task QuartzInboundHandler_ShouldInvokeSharedApplicationServiceAsAutomatic()
    {
        await using var context = new AchDbContext(new DbContextOptionsBuilder<AchDbContext>().UseInMemoryDatabase($"inbound-handler-{Guid.NewGuid():N}").Options);
        context.ClearingHouseConfigs.Add(new ClearingHouseConfig { Id = 1, ClearingHouseId = 1, TimeZoneId = "America/Bogota" });
        context.ClearingHouses.Add(new ClearingHouse { Id = 1, ClearingHouseId = 1, Code = "ACHCOL", Name = "ACH Colombia", OriginCode = "1" });
        context.AchCycles.Add(new AchCycle
        {
            Id = "ACH-1", CycleName = "Ciclo 1", ProcessingDate = new DateTime(2026, 9, 30),
            StartTime = new TimeSpan(19, 1, 0), EndTime = new TimeSpan(8, 30, 0), CutoffTime = new TimeSpan(8, 30, 0),
            OutputReleaseTime = new TimeSpan(9, 30, 0), ClearingHouseId = 1
        });
        await context.SaveChangesAsync();
        var service = new Mock<IAchColombiaManagedFileExchangeService>();
        service.Setup(x => x.ExecuteInboundAsync(AchManagedFileExecutionOrigin.Automatic, It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AchManagedFileExecutionResult(1, 1, 0, [Guid.NewGuid()]));
        var time = new Mock<IOperationalTimeSnapshotProvider>();
        time.Setup(x => x.CaptureNow()).Returns(new OperationalTimeSnapshot(
            new DateTime(2026, 9, 30, 12, 0, 0, DateTimeKind.Utc),
            new DateTime(2026, 9, 30, 7, 0, 0),
            new DateOnly(2026, 9, 30),
            "America/Bogota"));
        await new AchColombiaManagedMftInboundHandler(context, service.Object, time.Object, new OperationalCycleWindowResolver())
            .ExecuteAsync(new TaskDefinition(), default);
        service.VerifyAll();
    }

    [Fact]
    public async Task QuartzOutboundHandler_ShouldInvokeSharedApplicationServiceAsAutomatic()
    {
        await using var context = new AchDbContext(new DbContextOptionsBuilder<AchDbContext>().UseInMemoryDatabase($"handler-{Guid.NewGuid():N}").Options);
        context.ClearingHouseConfigs.Add(new ClearingHouseConfig { Id = 1, ClearingHouseId = 1, TimeZoneId = "America/Bogota" });
        context.ClearingHouses.Add(new ClearingHouse { Id = 1, ClearingHouseId = 1, Code = "ACHCOL", Name = "ACH Colombia", OriginCode = "1" });
        context.AchCycles.Add(new AchCycle
        {
            Id = "ACH-1", CycleName = "Ciclo 1", ProcessingDate = new DateTime(2026, 9, 30),
            StartTime = new TimeSpan(8, 31, 0), EndTime = new TimeSpan(11, 0, 0), CutoffTime = new TimeSpan(11, 0, 0), ClearingHouseId = 1
        });
        await context.SaveChangesAsync();
        var service = new Mock<IAchColombiaManagedFileExchangeService>();
        service.Setup(x => x.ExecuteOutboundAsync("ACH-1", AchManagedFileExecutionOrigin.Automatic, It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>(), It.IsAny<Guid?>()))
            .ReturnsAsync(new AchManagedFileExecutionResult(1, 1, 0, [Guid.NewGuid()]));
        var time = new Mock<IOperationalTimeSnapshotProvider>();
        time.Setup(x => x.CaptureNow()).Returns(new OperationalTimeSnapshot(
            new DateTime(2026, 9, 30, 15, 0, 0, DateTimeKind.Utc),
            new DateTime(2026, 9, 30, 10, 0, 0),
            new DateOnly(2026, 9, 30),
            "America/Bogota"));
        await new AchColombiaManagedMftOutboundHandler(context, service.Object, time.Object, new OperationalCycleWindowResolver())
            .ExecuteAsync(new TaskDefinition(), default);
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
    public void ConnectionTestApi_ShouldRequireManagementPermission()
    {
        var method = typeof(AchColombiaFileExchangeController).GetMethod(nameof(AchColombiaFileExchangeController.TestConnection))!;
        Assert.Contains(
            method.GetCustomAttributes(typeof(AuthorizeAttribute), true).Cast<AuthorizeAttribute>(),
            x => x.Policy == "CanManageAch");
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
