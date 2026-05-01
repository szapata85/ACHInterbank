using System.Reflection;
using Cfa.ACHInterbank.Api.Controllers;
using Microsoft.AspNetCore.Authorization;
using Xunit;

namespace Cfa.ACHInterbank.Tests;

public class ExplicitAuthorizationCriticalControllersTests
{
    [Fact]
    public void TransactionsController_TieneAuthorizeEnController()
    {
        Assert.NotNull(typeof(TransactionsController).GetCustomAttribute<AuthorizeAttribute>());
    }

    [Fact]
    public void TransactionsController_Gets_UsanCanReadAch_Y_PostsCanManageAch()
    {
        AssertPolicy(nameof(TransactionsController.GetAll), "CanReadAch");
        AssertPolicy(nameof(TransactionsController.GetCompanyEntryDescriptions), "CanReadAch");
        AssertPolicy(nameof(TransactionsController.PreviewPolicy), "CanReadAch");
        AssertPolicy(nameof(TransactionsController.GetById), "CanReadAch");
        AssertPolicy(nameof(TransactionsController.CreateTransaction), "CanManageAch");
        AssertPolicy(nameof(TransactionsController.SubmitBulkIngestion), "CanManageAch");
        AssertPolicy(nameof(TransactionsController.CreateTransactionsBulk), "CanManageAch");
    }

    [Fact]
    public void AchTraceabilityController_TieneAuthorizeYPoliciesCorrectas()
    {
        Assert.NotNull(typeof(AchTraceabilityController).GetCustomAttribute<AuthorizeAttribute>());
        AssertPolicy(nameof(AchTraceabilityController.GetTransactionTraceability), "CanReadAch");
        AssertPolicy(nameof(AchTraceabilityController.GetTraceabilityReport), "CanReadAch");
        AssertPolicy(nameof(AchTraceabilityController.CertifyWithSol02), "CanManageAch");
    }

    [Fact]
    public void AchReturnsController_TieneAuthorizeYPoliciesCorrectas()
    {
        Assert.NotNull(typeof(AchReturnsController).GetCustomAttribute<AuthorizeAttribute>());
        AssertPolicy(nameof(AchReturnsController.GetTransactionsByCycle), "CanReadAch");
        AssertPolicy(nameof(AchReturnsController.GenerateFile), "CanManageAch");
    }

    private static void AssertPolicy(string methodName, string expectedPolicy)
    {
        var method = typeof(TransactionsController).GetMethod(methodName)
            ?? typeof(AchTraceabilityController).GetMethod(methodName)
            ?? typeof(AchReturnsController).GetMethod(methodName);
        Assert.NotNull(method);
        var attr = method!.GetCustomAttribute<AuthorizeAttribute>();
        Assert.NotNull(attr);
        Assert.Equal(expectedPolicy, attr!.Policy);
    }
}
