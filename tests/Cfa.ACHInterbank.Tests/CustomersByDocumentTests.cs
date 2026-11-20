using System.Reflection;
using Cfa.ACHInterbank.Api.Controllers;
using Cfa.ACHInterbank.Application.Customers.Dtos;
using Cfa.ACHInterbank.Application.Customers.Interfaces;
using Cfa.ACHInterbank.Domain.Models.ACH;
using Cfa.ACHInterbank.Persistence.Customers.Services;
using Cfa.ACHInterbank.Persistence.DataBase;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Moq;

namespace Cfa.ACHInterbank.Tests;

public sealed class CustomersByDocumentTests
{
    [Fact]
    public async Task GetByDocumentAsync_MatchesTypeAndNumber_AndDoesNotExposeAccounts()
    {
        await using var context = CreateContext();
        context.Customers.AddRange(
            new Customer
            {
                Id = 101,
                DocumentType = "NIT",
                DocumentNumber = "1234567890",
                PersonType = "PJ",
                CompanyName = "Empresa Prueba",
                FirstName = "Empresa",
                LastName = "Prueba"
            },
            new Customer
            {
                Id = 102,
                DocumentType = "CC",
                DocumentNumber = "1234567890",
                PersonType = "PN",
                FirstName = "Ana",
                MiddleName = "María",
                LastName = "Pérez"
            });
        await context.SaveChangesAsync();

        var service = new CustomersService(context);
        var customer = await service.GetByDocumentAsync(" cc ", " 1234567890 ");

        Assert.NotNull(customer);
        Assert.Equal(102, customer.Id);
        Assert.Equal("CC", customer.DocumentType);
        Assert.Equal("1234567890", customer.DocumentNumber);
        Assert.Equal("Ana María Pérez", customer.FullName);
        Assert.DoesNotContain(typeof(CustomerDocumentLookupDto).GetProperties(),
            p => p.Name.Contains("Account", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task GetByDocumentAsync_ReturnsNull_WhenNoExactMatchExists()
    {
        await using var context = CreateContext();
        context.Customers.Add(new Customer
        {
            Id = 103,
            DocumentType = "NIT",
            DocumentNumber = "1234567890",
            PersonType = "PJ",
            FirstName = "Empresa",
            LastName = "Dos"
        });
        await context.SaveChangesAsync();

        var service = new CustomersService(context);

        Assert.Null(await service.GetByDocumentAsync("CC", "1234567890"));
        Assert.Null(await service.GetByDocumentAsync("NIT", "9999999999"));
    }

    [Theory]
    [InlineData(null, "1234567890")]
    [InlineData("  ", "1234567890")]
    [InlineData("CC", null)]
    [InlineData("CC", "  ")]
    public async Task GetByDocument_ReturnsBadRequest_WithoutQueryingService(
        string? documentType, string? documentNumber)
    {
        var service = new Mock<ICustomersService>(MockBehavior.Strict);
        var controller = new CustomersController(service.Object);

        var result = await controller.GetByDocument(documentType, documentNumber);

        Assert.IsType<BadRequestObjectResult>(result);
        service.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task GetByDocument_ReturnsNotFound_WhenCustomerIsMissing()
    {
        var service = new Mock<ICustomersService>(MockBehavior.Strict);
        service.Setup(x => x.GetByDocumentAsync("CC", "1234567890", It.IsAny<CancellationToken>()))
            .ReturnsAsync((CustomerDocumentLookupDto?)null);
        var controller = new CustomersController(service.Object);

        var result = await controller.GetByDocument(" cc ", " 1234567890 ");

        Assert.IsType<NotFoundResult>(result);
        service.VerifyAll();
    }

    [Fact]
    public async Task GetByDocument_ReturnsCustomerId_WhenFound()
    {
        var expected = new CustomerDocumentLookupDto
        {
            Id = 120,
            DocumentType = "CC",
            DocumentNumber = "1234567890",
            FullName = "Ana Pérez"
        };
        var service = new Mock<ICustomersService>(MockBehavior.Strict);
        service.Setup(x => x.GetByDocumentAsync("CC", "1234567890", It.IsAny<CancellationToken>()))
            .ReturnsAsync(expected);
        var controller = new CustomersController(service.Object);

        var response = await controller.GetByDocument("CC", "1234567890");

        var ok = Assert.IsType<OkObjectResult>(response);
        Assert.Same(expected, ok.Value);
        service.VerifyAll();
    }

    [Fact]
    public void GetByDocument_HasExplicitReadPermissionAndStaticRoute()
    {
        var method = typeof(CustomersController).GetMethod(nameof(CustomersController.GetByDocument));
        Assert.NotNull(method);
        Assert.Equal("by-document", method.GetCustomAttribute<HttpGetAttribute>()?.Template);
        Assert.Equal("CanReadAch", method.GetCustomAttribute<AuthorizeAttribute>()?.Policy);
    }

    private static AchDbContext CreateContext()
        => new(new DbContextOptionsBuilder<AchDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options);
}
