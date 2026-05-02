using System.Reflection;
using System.Security.Claims;
using Cfa.ACHInterbank.Api.Controllers;
using Cfa.ACHInterbank.Application.Security;
using Cfa.ACHInterbank.External;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Cfa.ACHInterbank.Tests.Authorization;

public class P1FineGrainedPolicyMigrationTests
{
    [Fact]
    public async Task PoliciesP1_CompatibilidadOr()
    {
        await AssertPolicy(P1Policies.BulkIngestionRead, FineGrainedPermissions.BulkIngestion.Read, "CanReadAch", "CanManageAch");
        await AssertPolicy(P1Policies.BulkIngestionUpload, FineGrainedPermissions.BulkIngestion.Upload, "CanManageAch", "CanReadAch");
        await AssertPolicy(P1Policies.BulkIngestionRetry, FineGrainedPermissions.BulkIngestion.Retry, "CanManageAch", "CanReadAch");
        await AssertPolicy(P1Policies.BulkIngestionCancel, FineGrainedPermissions.BulkIngestion.Cancel, "CanManageAch", "CanReadAch");
        await AssertPolicy(P1Policies.CommandCenterRead, FineGrainedPermissions.CommandCenter.Read, "CanReadAch", "CanManageAch");
        await AssertPolicy(P1Policies.CommandCenterRetry, FineGrainedPermissions.CommandCenter.Retry, "CanManageAch", "CanReadAch");
        await AssertPolicy(P1Policies.CommandCenterUnblock, FineGrainedPermissions.CommandCenter.Unblock, "CanManageAch", "CanReadAch");
        await AssertPolicy(P1Policies.CommandCenterRequeue, FineGrainedPermissions.CommandCenter.Requeue, "CanManageAch", "CanReadAch");
        await AssertPolicy(P1Policies.CommandCenterMarkFailedFinal, FineGrainedPermissions.CommandCenter.MarkFailedFinal, "CanManageAch", "CanReadAch");
        await AssertPolicy(P1Policies.NachaRead, FineGrainedPermissions.Nacha.Read, "CanReadAch", "CanManageAch");
        await AssertPolicy(P1Policies.NachaUpload, FineGrainedPermissions.Nacha.Upload, "CanManageAch", "CanReadAch");
        await AssertPolicy(P1Policies.NachaGenerate, FineGrainedPermissions.Nacha.Generate, "CanManageAch", "CanReadAch");
    }

    private static async Task AssertPolicy(string policy, string fine, string okLegacy, string badLegacy)
    {
        using var p = Provider(); var auth = p.GetRequiredService<IAuthorizationService>();
        Assert.True((await auth.AuthorizeAsync(User(fine), null, policy)).Succeeded);
        Assert.True((await auth.AuthorizeAsync(User(okLegacy), null, policy)).Succeeded);
        Assert.False((await auth.AuthorizeAsync(User(badLegacy), null, policy)).Succeeded);
        Assert.False((await auth.AuthorizeAsync(new ClaimsPrincipal(new ClaimsIdentity()), null, policy)).Succeeded);
    }

    private static ServiceProvider Provider(){var s=new ServiceCollection();var c=new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string,string?>{{"appSettings:tokenManager:issuerJwt","i"},{"appSettings:tokenManager:audienceJwt","a"},{"appSettings:tokenManager:secretKetJwt","this-is-a-test-secret-key-with-32-bytes"}}).Build();s.AddExternal(c);return s.BuildServiceProvider();}
    private static ClaimsPrincipal User(string p)=>new(new ClaimsIdentity([new Claim("permission",p)],"t"));
}
