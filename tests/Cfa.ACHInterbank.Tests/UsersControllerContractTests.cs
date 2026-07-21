using Cfa.ACHInterbank.Api.Controllers;
using Cfa.ACHInterbank.Application.Security.Dtos;
using Cfa.ACHInterbank.Application.Security.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;

namespace Cfa.ACHInterbank.Tests;

public sealed class UsersControllerContractTests
{
    [Fact]
    public async Task Create_Returns201WithNamedLocationRoute_AndGetReturnsCreatedUser()
    {
        var id = Guid.NewGuid();
        var user = new UserSummaryDto { Id = id, UserName = "usuario-prueba", IsActive = true };
        var service = new Mock<IUsersService>();
        service.Setup(x => x.CreateAsync(It.IsAny<SaveUserRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(UserOperationResult.Success(user));
        service.Setup(x => x.GetUserAsync(id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);
        var controller = new UsersController(service.Object);

        var create = await controller.CreateUserAsync(new SaveUserRequest { UserName = user.UserName }, default);
        var created = Assert.IsType<CreatedAtRouteResult>(create.Result);
        Assert.Equal(StatusCodes.Status201Created, created.StatusCode);
        Assert.Equal(UsersController.GetUserByIdRouteName, created.RouteName);
        Assert.Equal(id, Assert.IsType<Guid>(created.RouteValues!["id"]));

        var get = await controller.GetUserAsync(id, default);
        Assert.IsType<OkObjectResult>(get.Result);
    }
}
