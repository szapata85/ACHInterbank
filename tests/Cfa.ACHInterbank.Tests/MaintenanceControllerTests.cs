using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Cfa.ACHInterbank.Api.Controllers;
using Cfa.ACHInterbank.Application.DataBase;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Xunit;

namespace Cfa.ACHInterbank.Tests;

public class MaintenanceControllerTests
{
    [Fact]
    public async Task RunDbInitializer_InvokesSeedersInOrderAndReturnsOk()
    {
        // Arrange
        var executionOrder = new List<int>();

        var earlySeeder = new Mock<IDbSeeder>();
        earlySeeder.SetupGet(s => s.Order).Returns(0);
        earlySeeder
            .Setup(s => s.SeedAsync())
            .Callback(() => executionOrder.Add(earlySeeder.Object.Order))
            .Returns(Task.CompletedTask);

        var lateSeeder = new Mock<IDbSeeder>();
        lateSeeder.SetupGet(s => s.Order).Returns(5);
        lateSeeder
            .Setup(s => s.SeedAsync())
            .Callback(() => executionOrder.Add(lateSeeder.Object.Order))
            .Returns(Task.CompletedTask);

        var services = new ServiceCollection();
        services.AddSingleton(earlySeeder.Object);
        services.AddSingleton(lateSeeder.Object);

        var controller = new MaintenanceController(services.BuildServiceProvider());

        // Act
        var result = await controller.RunDbInitializer();

        // Assert
        Assert.Collection(
            executionOrder,
            order => Assert.Equal(0, order),
            order => Assert.Equal(5, order));

        earlySeeder.Verify(s => s.SeedAsync(), Times.Once);
        lateSeeder.Verify(s => s.SeedAsync(), Times.Once);

        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.NotNull(okResult.Value);

        var payload = okResult.Value!;
        var message = payload.GetType().GetProperty("Message")?.GetValue(payload)?.ToString();
        Assert.Equal("Seeding ejecutado correctamente desde Controller", message);

        var dateValue = payload.GetType().GetProperty("Date")?.GetValue(payload) as DateTime?;
        Assert.True(dateValue.HasValue);
        Assert.InRange(dateValue!.Value, DateTime.UtcNow.AddMinutes(-1), DateTime.UtcNow.AddMinutes(1));
    }
}
