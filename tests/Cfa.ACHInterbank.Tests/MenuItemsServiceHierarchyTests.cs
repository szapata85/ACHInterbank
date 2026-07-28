using Cfa.ACHInterbank.Application.Navigation.Dtos;
using Cfa.ACHInterbank.Application.Navigation.Interfaces;
using Cfa.ACHInterbank.Api.Controllers;
using Cfa.ACHInterbank.Domain.Entities.Navigation;
using Cfa.ACHInterbank.Persistence.DataBase;
using Cfa.ACHInterbank.Persistence.Navigation.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;

namespace Cfa.ACHInterbank.Tests;

public class MenuItemsServiceHierarchyTests
{
    [Fact]
    public async Task UpdateAsync_ShouldRejectIndirectCycle_WhenDescendantIsSelectedAsParent()
    {
        await using var context = CreateContext();
        context.MenuItems.AddRange(
            Item(101, "Raíz", "/e2e/root"),
            Item(102, "Hijo", "/e2e/child", 101),
            Item(103, "Nieto", "/e2e/grandchild", 102));
        await context.SaveChangesAsync();

        var service = new MenuItemsService(context);
        var request = Request("Raíz", "/e2e/root", 103);

        var error = await Assert.ThrowsAsync<ArgumentException>(() => service.UpdateAsync(101, request));

        Assert.Contains("ciclo", error.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Null((await context.MenuItems.FindAsync(101))!.ParentId);
    }

    [Fact]
    public async Task UpdateAsync_ShouldAllowMovingItem_ToUnrelatedParent()
    {
        await using var context = CreateContext();
        context.MenuItems.AddRange(
            Item(201, "Raíz A", "/e2e/root-a"),
            Item(202, "Hijo", "/e2e/child-a", 201),
            Item(203, "Raíz B", "/e2e/root-b"));
        await context.SaveChangesAsync();

        var service = new MenuItemsService(context);

        var updated = await service.UpdateAsync(202, Request("Hijo", "/e2e/child-a", 203));

        Assert.NotNull(updated);
        Assert.Equal(203, updated.ParentId);
        Assert.Equal(203, (await context.MenuItems.FindAsync(202))!.ParentId);
    }

    private static AchDbContext CreateContext()
        => new(new DbContextOptionsBuilder<AchDbContext>()
            .UseInMemoryDatabase($"menu-hierarchy-{Guid.NewGuid():N}")
            .Options);

    private static MenuItem Item(int id, string label, string route, int? parentId = null)
        => new()
        {
            Id = id,
            MenuId = 1,
            Label = label,
            Route = route,
            Icon = "menu",
            Order = id,
            Exact = true,
            IsActive = true,
            ParentId = parentId
        };

    private static SaveMenuItemRequest Request(string label, string route, int? parentId)
        => new()
        {
            Label = label,
            Route = route,
            Icon = "menu",
            Order = 1,
            Exact = true,
            IsActive = true,
            ParentId = parentId
        };
}

public class MenuItemsControllerTests
{
    [Fact]
    public async Task CreateMenuItemAsync_ShouldReturn201WithCreatedItem_WithoutRouteGeneration()
    {
        var request = new SaveMenuItemRequest
        {
            Label = "Temporal",
            Route = "/temporal",
            Order = 1,
            IsActive = true
        };
        var created = new MenuItemAdminDto
        {
            Id = 301,
            Label = request.Label,
            Route = request.Route,
            Order = request.Order,
            IsActive = request.IsActive
        };
        var service = new Mock<IMenuItemsService>();
        service
            .Setup(candidate => candidate.CreateAsync(request, It.IsAny<CancellationToken>()))
            .ReturnsAsync(created);
        var controller = new MenuItemsController(service.Object);

        var result = await controller.CreateMenuItemAsync(request, CancellationToken.None);

        var objectResult = Assert.IsType<ObjectResult>(result.Result);
        Assert.Equal(StatusCodes.Status201Created, objectResult.StatusCode);
        Assert.Same(created, objectResult.Value);
    }
}
