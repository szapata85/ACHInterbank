using System.Reflection;
using System.Text.Json;
using Cfa.ACHInterbank.Api.Controllers;
using Cfa.ACHInterbank.Application.ACH.Interfaces;
using Cfa.ACHInterbank.Application.ACH.Models;
using Cfa.ACHInterbank.Application.Common;
using Cfa.ACHInterbank.Application.Customers.Dtos;
using Cfa.ACHInterbank.Application.Customers.Interfaces;
using Cfa.ACHInterbank.Domain.Models.ACH.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Moq;

namespace Cfa.ACHInterbank.Tests;

public sealed class CustomerThirdPartyAccountsByDocumentTests
{
    private static CustomerDocumentLookupDto Customer => new()
    {
        Id = 123,
        DocumentType = "CC",
        DocumentNumber = "1234567890",
        FullName = "Cliente de Prueba"
    };

    [Fact]
    public void Lookup_RequiresAuthenticationAndCanReadAch()
    {
        var controller = typeof(CustomerThirdPartyAccountsByDocumentController);
        Assert.NotEmpty(controller.GetCustomAttributes<AuthorizeAttribute>(inherit: true));
        Assert.Equal("api/third-party-accounts/by-customer-document",
            controller.GetCustomAttribute<RouteAttribute>()?.Template);
        var action = controller.GetMethod(nameof(CustomerThirdPartyAccountsByDocumentController.Get));
        Assert.NotNull(action);
        Assert.NotNull(action.GetCustomAttribute<HttpGetAttribute>());
        Assert.Equal("CanReadAch", action.GetCustomAttribute<AuthorizeAttribute>()?.Policy);
        Assert.Empty(action.GetCustomAttributes<AllowAnonymousAttribute>(inherit: true));
    }

    [Theory]
    [InlineData(null, "1234567890", 1, 20)]
    [InlineData(" ", "1234567890", 1, 20)]
    [InlineData("CC", null, 1, 20)]
    [InlineData("CC", "  ", 1, 20)]
    [InlineData("CC", "1234567890", 0, 20)]
    [InlineData("CC", "1234567890", 1, 0)]
    [InlineData("CC", "1234567890", 1, 101)]
    public async Task Lookup_BadRequest_DoesNotAccessRepositories(
        string? type, string? number, int page, int pageSize)
    {
        var customers = new Mock<ICustomersService>(MockBehavior.Strict);
        var accounts = new Mock<IThirdPartyAccountReadService>(MockBehavior.Strict);
        var controller = new CustomerThirdPartyAccountsByDocumentController(customers.Object, accounts.Object);

        var result = await controller.Get(type, number, page, pageSize);

        Assert.IsType<BadRequestObjectResult>(result);
        customers.VerifyNoOtherCalls();
        accounts.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task Lookup_NotFound_DoesNotLoadAnyAccounts()
    {
        var customers = new Mock<ICustomersService>(MockBehavior.Strict);
        var accounts = new Mock<IThirdPartyAccountReadService>(MockBehavior.Strict);
        customers.Setup(x => x.GetByDocumentAsync("CC", "1234567890", It.IsAny<CancellationToken>()))
            .ReturnsAsync((CustomerDocumentLookupDto?)null);
        var controller = new CustomerThirdPartyAccountsByDocumentController(customers.Object, accounts.Object);

        var result = await controller.Get(" cc ", " 1234567890 ");

        Assert.IsType<NotFoundResult>(result);
        customers.VerifyAll();
        accounts.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task Lookup_ReturnsOnlyCustomerScopedCanonicalAccountsAndChamberValidationStates()
    {
        var customers = new Mock<ICustomersService>(MockBehavior.Strict);
        var accounts = new Mock<IThirdPartyAccountReadService>(MockBehavior.Strict);
        var account = new ThirdPartyAccountReadDto
        {
            CustomerId = 123,
            ThirdPartyId = 20,
            ThirdPartyAccountId = 21,
            ThirdPartyDisplayName = "Tercero de Prueba",
            AccountNumberMasked = "****6789",
            DocumentNumberMasked = "****9012",
            Validations =
            [
                new ThirdPartyAccountValidationReadDto
                {
                    ClearingHouseId = 1,
                    ClearingHouseCode = "ACHCOL",
                    Status = ThirdPartyAccountValidationStatus.Accepted,
                    IsVerified = true,
                    LatestAttempt = new PrenotificationValidationAttemptReadDto { ReturnCode = null }
                },
                new ThirdPartyAccountValidationReadDto
                {
                    ClearingHouseId = 2,
                    ClearingHouseCode = "CENIT",
                    Status = ThirdPartyAccountValidationStatus.Rejected,
                    IsVerified = false,
                    LatestAttempt = new PrenotificationValidationAttemptReadDto { ReturnCode = "R03" }
                }
            ]
        };
        var page = new PagedResponse<ThirdPartyAccountReadDto>
        {
            Items = [account], Total = 1, Page = 2, PageSize = 10
        };
        customers.Setup(x => x.GetByDocumentAsync("CC", "1234567890", It.IsAny<CancellationToken>()))
            .ReturnsAsync(Customer);
        accounts.Setup(x => x.GetAsync(
                It.Is<ThirdPartyAccountReadQuery>(query =>
                    query.CustomerId == 123
                    && query.Page == 2
                    && query.PageSize == 10
                    && query.EligibilityContext == null
                    && query.Search == null
                    && query.ThirdPartyId == null),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(page);
        var controller = new CustomerThirdPartyAccountsByDocumentController(customers.Object, accounts.Object);

        var actionResult = await controller.Get(" cc ", " 1234567890 ", 2, 10);

        var ok = Assert.IsType<OkObjectResult>(actionResult);
        var response = Assert.IsType<CustomerThirdPartyAccountsByDocumentDto>(ok.Value);
        Assert.Equal(123, response.Customer.Id);
        Assert.Equal(2, response.Accounts.Page);
        Assert.Equal(1, response.Accounts.Total);
        var returned = Assert.Single(response.Accounts.Items);
        Assert.Equal(123, returned.CustomerId);
        Assert.True(Assert.Single(returned.Validations.Where(x => x.ClearingHouseCode == "ACHCOL")).IsVerified);
        Assert.False(Assert.Single(returned.Validations.Where(x => x.ClearingHouseCode == "CENIT")).IsVerified);
        Assert.Equal("R03", returned.Validations.Single(x => x.ClearingHouseCode == "CENIT").LatestAttempt?.ReturnCode);
        var serialized = JsonSerializer.Serialize(response);
        Assert.DoesNotContain("98765432106789", serialized, StringComparison.Ordinal);
        customers.VerifyAll();
        accounts.VerifyAll();
    }

    [Fact]
    public async Task Lookup_CustomerWithoutThirdParties_ReturnsEmptyPageRatherThanNotFound()
    {
        var customers = new Mock<ICustomersService>(MockBehavior.Strict);
        var accounts = new Mock<IThirdPartyAccountReadService>(MockBehavior.Strict);
        customers.Setup(x => x.GetByDocumentAsync("CC", "1234567890", It.IsAny<CancellationToken>()))
            .ReturnsAsync(Customer);
        accounts.Setup(x => x.GetAsync(
                It.Is<ThirdPartyAccountReadQuery>(q => q.CustomerId == 123),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PagedResponse<ThirdPartyAccountReadDto>
            {
                Items = [], Total = 0, Page = 1, PageSize = 20
            });
        var controller = new CustomerThirdPartyAccountsByDocumentController(customers.Object, accounts.Object);

        var result = await controller.Get("CC", "1234567890");

        var response = Assert.IsType<CustomerThirdPartyAccountsByDocumentDto>(
            Assert.IsType<OkObjectResult>(result).Value);
        Assert.Empty(response.Accounts.Items);
        Assert.Equal(0, response.Accounts.Total);
        customers.VerifyAll();
        accounts.VerifyAll();
    }
}
