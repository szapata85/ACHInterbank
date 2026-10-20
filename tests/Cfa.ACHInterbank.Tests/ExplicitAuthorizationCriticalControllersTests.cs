using System.Reflection;
using Cfa.ACHInterbank.Api.Controllers;
using Cfa.ACHInterbank.Application.Security;
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
        AssertPolicy(nameof(TransactionsController.GetAll), P0Policies.TransactionsRead);
        AssertPolicy(nameof(TransactionsController.GetCompanyEntryDescriptions), P0Policies.TransactionsRead);
        AssertPolicy(nameof(TransactionsController.PreviewPolicy), P0Policies.TransactionsPolicyPreview);
        AssertPolicy(nameof(TransactionsController.GetById), P0Policies.TransactionsRead);
        AssertPolicy(nameof(TransactionsController.GetIntegrationResult), P0Policies.TransactionsRead);
        AssertPolicy(nameof(TransactionsController.CreateTransaction), P0Policies.TransactionsCreate);
        AssertPolicy(nameof(TransactionsController.SubmitBulkIngestion), P0Policies.TransactionsBulkSubmit);
        AssertPolicy(nameof(TransactionsController.CreateTransactionsBulk), P0Policies.TransactionsBulkSubmit);
    }

    [Fact]
    public void AchTraceabilityController_TieneAuthorizeYPoliciesCorrectas()
    {
        Assert.NotNull(typeof(AchTraceabilityController).GetCustomAttribute<AuthorizeAttribute>());
        AssertPolicy(nameof(AchTraceabilityController.GetTransactionTraceability), P0Policies.TraceabilityRead);
        AssertPolicy(nameof(AchTraceabilityController.GetTraceabilityReport), P0Policies.TraceabilityRead);
        AssertPolicy(nameof(AchTraceabilityController.CertifyWithSol02), P0Policies.TraceabilityCertifySol02);
    }

    [Fact]
    public void AchReturnsController_TieneAuthorizeYPoliciesCorrectas()
    {
        Assert.NotNull(typeof(AchReturnsController).GetCustomAttribute<AuthorizeAttribute>());
        AssertPolicy(nameof(AchReturnsController.GetTransactionsByCycle), P0Policies.ReturnsRead);
        AssertPolicy(nameof(AchReturnsController.GenerateFile), P0Policies.ReturnsGenerateFile);
    }

    [Fact]
    public void AchReturnOfReturnController_TieneAuthorizeYPoliciesCorrectas()
    {
        Assert.NotNull(typeof(AchReturnOfReturnController).GetCustomAttribute<AuthorizeAttribute>());
        AssertPolicy(nameof(AchReturnOfReturnController.Evaluate), P0Policies.ReturnsRead);
        AssertPolicy(nameof(AchReturnOfReturnController.GenerateAuditFile), P0Policies.ReturnsGenerateFile);
        AssertPolicy(nameof(AchReturnOfReturnController.GenerateNachaFile), P0Policies.ReturnsGenerateFile);
    }

    private static void AssertPolicy(string methodName, string expectedPolicy)
    {
        var method = typeof(TransactionsController).GetMethod(methodName)
            ?? typeof(AchTraceabilityController).GetMethod(methodName)
            ?? typeof(AchReturnsController).GetMethod(methodName)
            ?? typeof(AchReturnOfReturnController).GetMethod(methodName);
        Assert.NotNull(method);
        var attr = method!.GetCustomAttribute<AuthorizeAttribute>();
        Assert.NotNull(attr);
        Assert.Equal(expectedPolicy, attr!.Policy);
    }
}
