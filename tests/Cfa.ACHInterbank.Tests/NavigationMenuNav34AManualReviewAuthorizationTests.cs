using System.Reflection;
using Cfa.ACHInterbank.Api.Controllers;
using Cfa.ACHInterbank.Application.Security;
using Microsoft.AspNetCore.Authorization;

namespace Cfa.ACHInterbank.Tests;

public sealed class NavigationMenuNav34AManualReviewAuthorizationTests
{
    [Theory]
    [InlineData(typeof(AchResponsesController), nameof(AchResponsesController.Search), P1Policies.NachaRead)]
    [InlineData(typeof(AchResponseOperationsController), nameof(AchResponseOperationsController.ListOrphans), P1Policies.NachaRead)]
    [InlineData(typeof(AchResponseOperationsController), nameof(AchResponseOperationsController.BeginReview), P1Policies.NachaGenerate)]
    [InlineData(typeof(AchResponseOperationsController), nameof(AchResponseOperationsController.ResolveOrphan), P1Policies.NachaGenerate)]
    public void ManualReviewApi_SeparatesReadAndMutationPolicies(Type controllerType, string actionName, string expectedPolicy)
    {
        var action = controllerType.GetMethod(actionName, BindingFlags.Instance | BindingFlags.Public);

        Assert.NotNull(action);
        var authorize = action!.GetCustomAttribute<AuthorizeAttribute>();
        Assert.NotNull(authorize);
        Assert.Equal(expectedPolicy, authorize!.Policy);
    }

    [Fact]
    public void ManualReviewSpa_UsesOnlyReadAndManualDecisionOperations()
    {
        var repoRoot = RepositoryRoot();
        var component = File.ReadAllText(Path.Combine(
            repoRoot,
            "web",
            "ach-interbank-ui",
            "src",
            "app",
            "features",
            "ach-responses",
            "pages",
            "ach-response-manual-review-page.component.ts"));

        Assert.Contains("this.api.search(", component);
        Assert.Contains("this.api.getOrphans(", component);
        Assert.Contains("this.api.beginOrphanReview(", component);
        Assert.Contains("this.api.resolveOrphan(", component);
        Assert.DoesNotContain("this.api.requestReprocess(", component);
        Assert.DoesNotContain("this.api.process(", component);
        Assert.DoesNotContain("this.api.sendNotification(", component);
    }

    [Fact]
    public void ManualReviewMutationPolicy_DoesNotTreatCanReadAchAsManagement()
    {
        var repoRoot = RepositoryRoot();
        var dependencyInjection = File.ReadAllText(Path.Combine(
            repoRoot,
            "src",
            "Cfa.ACHInterbank.External",
            "DependencyInjectionService.cs"));

        var start = dependencyInjection.IndexOf("options.AddPolicy(P1Policies.NachaGenerate", StringComparison.Ordinal);
        Assert.True(start >= 0);
        var end = dependencyInjection.IndexOf("options.AddPolicy(P1Policies.NachaExport", start, StringComparison.Ordinal);
        Assert.True(end > start);
        var policyBlock = dependencyInjection[start..end];

        Assert.Contains("FineGrainedPermissions.Nacha.Generate", policyBlock);
        Assert.Contains("CanManageAch", policyBlock);
        Assert.DoesNotContain("CanReadAch", policyBlock);
    }

    private static string RepositoryRoot()
    {
        var current = new DirectoryInfo(AppContext.BaseDirectory);
        while (current is not null)
        {
            if (File.Exists(Path.Combine(current.FullName, "ACHInterbank.sln")))
                return current.FullName;
            current = current.Parent;
        }

        throw new DirectoryNotFoundException("No se encontró la raíz del repositorio.");
    }
}
