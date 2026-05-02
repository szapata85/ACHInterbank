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
    public void P1_Grupo1_ControllerYActions_ComposicionAuthorizeCorrecta()
    {
        AssertClassAuthorizeWithoutPolicy<BulkIngestionController>();
        AssertClassAuthorizeWithoutPolicy<IncomingNachaCommandCenterController>();
        AssertClassAuthorizeWithoutPolicy<NachaUploadController>();
        AssertClassAuthorizeWithoutPolicy<NachaController>();

        AssertActionPolicy<BulkIngestionController>(nameof(BulkIngestionController.Upload), P1Policies.BulkIngestionUpload);
        AssertActionPolicy<BulkIngestionController>(nameof(BulkIngestionController.GetBatch), P1Policies.BulkIngestionRead);
        AssertActionPolicy<BulkIngestionController>(nameof(BulkIngestionController.GetBatchItems), P1Policies.BulkIngestionRead);
        AssertActionPolicy<BulkIngestionController>(nameof(BulkIngestionController.GetBatchSummary), P1Policies.BulkIngestionRead);
        AssertActionPolicy<BulkIngestionController>(nameof(BulkIngestionController.Retry), P1Policies.BulkIngestionRetry);
        AssertActionPolicy<BulkIngestionController>(nameof(BulkIngestionController.Cancel), P1Policies.BulkIngestionCancel);

        AssertActionPolicy<IncomingNachaCommandCenterController>(nameof(IncomingNachaCommandCenterController.GetObservabilitySummary), P1Policies.CommandCenterRead);
        AssertActionPolicy<IncomingNachaCommandCenterController>(nameof(IncomingNachaCommandCenterController.GetIngestions), P1Policies.CommandCenterRead);
        AssertActionPolicy<IncomingNachaCommandCenterController>(nameof(IncomingNachaCommandCenterController.GetIngestionDetail), P1Policies.CommandCenterRead);
        AssertActionPolicy<IncomingNachaCommandCenterController>(nameof(IncomingNachaCommandCenterController.GetQueue), P1Policies.CommandCenterRead);
        AssertActionPolicy<IncomingNachaCommandCenterController>(nameof(IncomingNachaCommandCenterController.GetQueueDetail), P1Policies.CommandCenterRead);
        AssertActionPolicy<IncomingNachaCommandCenterController>(nameof(IncomingNachaCommandCenterController.RetryManual), P1Policies.CommandCenterRetry);
        AssertActionPolicy<IncomingNachaCommandCenterController>(nameof(IncomingNachaCommandCenterController.UnblockManual), P1Policies.CommandCenterUnblock);
        AssertActionPolicy<IncomingNachaCommandCenterController>(nameof(IncomingNachaCommandCenterController.RequeueManual), P1Policies.CommandCenterRequeue);
        AssertActionPolicy<IncomingNachaCommandCenterController>(nameof(IncomingNachaCommandCenterController.MarkFailedFinal), P1Policies.CommandCenterMarkFailedFinal);

        AssertActionPolicy<NachaUploadController>(nameof(NachaUploadController.UploadNachaFile), P1Policies.NachaUpload);
        AssertActionPolicy<NachaUploadController>(nameof(NachaUploadController.GetUploadedRecords), P1Policies.NachaRead);

        AssertActionPolicy<NachaController>(nameof(NachaController.SaveHeader), P1Policies.NachaGenerate);

        AssertActionDoesNotUseLegacyPermissions<BulkIngestionController>(nameof(BulkIngestionController.Upload));
        AssertActionDoesNotUseLegacyPermissions<IncomingNachaCommandCenterController>(nameof(IncomingNachaCommandCenterController.RetryManual));
        AssertActionDoesNotUseLegacyPermissions<NachaUploadController>(nameof(NachaUploadController.UploadNachaFile));
        AssertActionDoesNotUseLegacyPermissions<NachaController>(nameof(NachaController.SaveHeader));
    }

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

    [Fact]
    public async Task P1_Grupo1_AuthorizationService_CompatibilidadEsperada()
    {
        using var p = Provider();
        var auth = p.GetRequiredService<IAuthorizationService>();

        Assert.True((await auth.AuthorizeAsync(User(FineGrainedPermissions.BulkIngestion.Upload), null, P1Policies.BulkIngestionUpload)).Succeeded);
        Assert.True((await auth.AuthorizeAsync(User("CanManageAch"), null, P1Policies.BulkIngestionUpload)).Succeeded);
        Assert.False((await auth.AuthorizeAsync(User("CanReadAch"), null, P1Policies.BulkIngestionUpload)).Succeeded);

        Assert.True((await auth.AuthorizeAsync(User(FineGrainedPermissions.CommandCenter.Retry), null, P1Policies.CommandCenterRetry)).Succeeded);
        Assert.True((await auth.AuthorizeAsync(User("CanManageAch"), null, P1Policies.CommandCenterRetry)).Succeeded);
        Assert.False((await auth.AuthorizeAsync(User("CanReadAch"), null, P1Policies.CommandCenterRetry)).Succeeded);

        Assert.True((await auth.AuthorizeAsync(User(FineGrainedPermissions.Nacha.Upload), null, P1Policies.NachaUpload)).Succeeded);
        Assert.True((await auth.AuthorizeAsync(User(FineGrainedPermissions.Nacha.Generate), null, P1Policies.NachaGenerate)).Succeeded);
        Assert.False((await auth.AuthorizeAsync(new ClaimsPrincipal(new ClaimsIdentity()), null, P1Policies.NachaUpload)).Succeeded);
    }

    private static void AssertClassAuthorizeWithoutPolicy<TController>()
    {
        var attrs = typeof(TController).GetCustomAttributes<AuthorizeAttribute>(inherit: true).ToList();
        Assert.NotEmpty(attrs);
        Assert.Contains(attrs, a => string.IsNullOrWhiteSpace(a.Policy));
        Assert.DoesNotContain(attrs, a => !string.IsNullOrWhiteSpace(a.Policy) && a.Policy.Contains("P1Policies.", StringComparison.Ordinal));
    }

    private static void AssertActionPolicy<TController>(string actionName, string expectedPolicy)
    {
        var method = typeof(TController).GetMethod(actionName);
        Assert.NotNull(method);
        var attr = method!.GetCustomAttributes<AuthorizeAttribute>(inherit: true).FirstOrDefault();
        Assert.NotNull(attr);
        Assert.Equal(expectedPolicy, attr!.Policy);
    }

    private static void AssertActionDoesNotUseLegacyPermissions<TController>(string actionName)
    {
        var method = typeof(TController).GetMethod(actionName);
        Assert.NotNull(method);
        var attrs = method!.GetCustomAttributes<AuthorizeAttribute>(inherit: true).ToList();
        Assert.DoesNotContain(attrs, a => a.Policy is "CanReadAch" or "CanManageAch");
    }

    private static ServiceProvider Provider(){var s=new ServiceCollection();var c=new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string,string?>{{"appSettings:tokenManager:issuerJwt","i"},{"appSettings:tokenManager:audienceJwt","a"},{"appSettings:tokenManager:secretKetJwt","this-is-a-test-secret-key-with-32-bytes"}}).Build();s.AddExternal(c);return s.BuildServiceProvider();}
    private static ClaimsPrincipal User(string p)=>new(new ClaimsIdentity([new Claim("permission",p)],"t"));
}
