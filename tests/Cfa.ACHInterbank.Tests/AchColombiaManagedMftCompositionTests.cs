using Cfa.ACHInterbank.Api.Controllers;
using Cfa.ACHInterbank.Application.ACH.Interfaces;
using Cfa.ACHInterbank.Application.ACH.Models;
using Cfa.ACHInterbank.Domain.Entities.SchedulerTask;
using Cfa.ACHInterbank.Domain.Models.ACH;
using Cfa.ACHInterbank.Persistence.ACH.Quartz.Jobs.Implementation;
using Cfa.ACHInterbank.Persistence.DataBase;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.EntityFrameworkCore;
using Moq;

namespace Cfa.ACHInterbank.Tests;

public sealed class AchColombiaManagedMftCompositionTests
{
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
