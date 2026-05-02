using System.Reflection;
using Cfa.ACHInterbank.Api.Controllers;
using Cfa.ACHInterbank.Application.Security;
using Cfa.ACHInterbank.External;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Cfa.ACHInterbank.Tests.Authorization;

public class P0FineGrainedPolicyMigrationTests
{
    [Fact]
    public void PoliciesCompuestasP0_DebenExistir_ConCompatibilidadFinaYLegacy()
    {
        var services = new ServiceCollection();
        var config = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["appSettings:tokenManager:issuerJwt"] = "issuer-test",
            ["appSettings:tokenManager:audienceJwt"] = "audience-test",
            ["appSettings:tokenManager:secretKetJwt"] = "this-is-a-test-secret-key-with-32-bytes"
        }).Build();
        services.AddExternal(config);
        using var provider = services.BuildServiceProvider();
        var options = provider.GetRequiredService<IOptions<AuthorizationOptions>>().Value;

        Assert.NotNull(options.GetPolicy(P0Policies.TransactionsRead));
        Assert.NotNull(options.GetPolicy(P0Policies.TransactionsCreate));
        Assert.NotNull(options.GetPolicy(P0Policies.TransactionsBulkSubmit));
        Assert.NotNull(options.GetPolicy(P0Policies.TransactionsPolicyPreview));
        Assert.NotNull(options.GetPolicy(P0Policies.TraceabilityRead));
        Assert.NotNull(options.GetPolicy(P0Policies.TraceabilityCertifySol02));
        Assert.NotNull(options.GetPolicy(P0Policies.ReturnsRead));
        Assert.NotNull(options.GetPolicy(P0Policies.ReturnsGenerateFile));
    }

    [Fact]
    public void ControladoresP0_DebenUsarPoliciesCompuestas()
    {
        AssertPolicy(typeof(TransactionsController), nameof(TransactionsController.GetAll), P0Policies.TransactionsRead);
        AssertPolicy(typeof(TransactionsController), nameof(TransactionsController.GetCompanyEntryDescriptions), P0Policies.TransactionsRead);
        AssertPolicy(typeof(TransactionsController), nameof(TransactionsController.PreviewPolicy), P0Policies.TransactionsPolicyPreview);
        AssertPolicy(typeof(TransactionsController), nameof(TransactionsController.CreateTransaction), P0Policies.TransactionsCreate);
        AssertPolicy(typeof(TransactionsController), nameof(TransactionsController.SubmitBulkIngestion), P0Policies.TransactionsBulkSubmit);
        AssertPolicy(typeof(TransactionsController), nameof(TransactionsController.CreateTransactionsBulk), P0Policies.TransactionsBulkSubmit);
        AssertPolicy(typeof(TransactionsController), nameof(TransactionsController.GetById), P0Policies.TransactionsRead);

        AssertPolicy(typeof(AchTraceabilityController), nameof(AchTraceabilityController.CertifyWithSol02), P0Policies.TraceabilityCertifySol02);
        AssertPolicy(typeof(AchTraceabilityController), nameof(AchTraceabilityController.GetTransactionTraceability), P0Policies.TraceabilityRead);
        AssertPolicy(typeof(AchTraceabilityController), nameof(AchTraceabilityController.GetTraceabilityReport), P0Policies.TraceabilityRead);

        AssertPolicy(typeof(AchReturnsController), nameof(AchReturnsController.GetTransactionsByCycle), P0Policies.ReturnsRead);
        AssertPolicy(typeof(AchReturnsController), nameof(AchReturnsController.GenerateFile), P0Policies.ReturnsGenerateFile);
    }

    private static void AssertPolicy(Type type, string methodName, string expected)
    {
        var m = type.GetMethod(methodName, BindingFlags.Instance | BindingFlags.Public)!;
        var auth = m.GetCustomAttribute<AuthorizeAttribute>();
        Assert.NotNull(auth);
        Assert.Equal(expected, auth!.Policy);
        Assert.NotEqual("CanReadAch", auth.Policy);
        Assert.NotEqual("CanManageAch", auth.Policy);
    }
}
