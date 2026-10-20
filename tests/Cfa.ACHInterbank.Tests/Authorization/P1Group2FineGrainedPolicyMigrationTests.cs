using System.Security.Claims;
using Cfa.ACHInterbank.Api.Controllers;
using Cfa.ACHInterbank.Application.Security;
using Cfa.ACHInterbank.External;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Cfa.ACHInterbank.Tests.Authorization;

public class P1Group2FineGrainedPolicyMigrationTests
{
    [Fact]
    public void P1_Grupo2_ControllerYActions_ComposicionAuthorizeCorrecta()
    {
        AssertClassAuthorizeWithoutPolicy<CertificateManagementController>();
        AssertClassAuthorizeWithoutPolicy<DigitalEnvelopeCertificatesController>();
        AssertClassAuthorizeWithoutPolicy<SobreDigitalController>();
        AssertClassAuthorizeWithoutPolicy<NachaSecurityOperationsController>();

        AssertActionPolicy<CertificateManagementController>(nameof(CertificateManagementController.UploadPublicAsync), P1Policies.CertificatesUploadPublic);
        AssertActionPolicy<CertificateManagementController>(nameof(CertificateManagementController.UploadPrivateAsync), P1Policies.CertificatesRegisterPrivate);
        AssertActionPolicy<CertificateManagementController>(nameof(CertificateManagementController.ListAsync), P1Policies.CertificatesRead);
        AssertActionPolicy<CertificateManagementController>(nameof(CertificateManagementController.ListVersionsAsync), P1Policies.CertificatesRead);
        AssertActionPolicy<CertificateManagementController>(nameof(CertificateManagementController.ActivateAsync), P1Policies.CertificatesActivate);
        AssertActionPolicy<CertificateManagementController>(nameof(CertificateManagementController.RevokeAsync), P1Policies.CertificatesRevoke);
        AssertActionPolicy<CertificateManagementController>(nameof(CertificateManagementController.ValidateAsync), P1Policies.CertificatesValidate);
        AssertActionPolicy<CertificateManagementController>(nameof(CertificateManagementController.AuditAsync), P1Policies.CertificatesAudit);

        AssertActionPolicy<DigitalEnvelopeCertificatesController>(nameof(DigitalEnvelopeCertificatesController.GetAsync), P1Policies.CertificatesRead);
        AssertActionPolicy<DigitalEnvelopeCertificatesController>(nameof(DigitalEnvelopeCertificatesController.UploadAsync), P1Policies.CertificatesUploadPublic);
        AssertActionPolicy<DigitalEnvelopeCertificatesController>(nameof(DigitalEnvelopeCertificatesController.DeleteAsync), P1Policies.CertificatesRevoke);

        AssertActionPolicy<SobreDigitalController>(nameof(SobreDigitalController.Encrypt), P1Policies.DigitalEnvelopeEncrypt);
        AssertActionPolicy<SobreDigitalController>(nameof(SobreDigitalController.Decrypt), P1Policies.DigitalEnvelopeDecrypt);
        AssertActionPolicy<SobreDigitalController>(nameof(SobreDigitalController.testRSA), P1Policies.DigitalEnvelopeTest);

        AssertActionPolicy<NachaSecurityOperationsController>(nameof(NachaSecurityOperationsController.GeneratePlainAsync), P1Policies.NachaSecurityGenerateEncrypted);
        AssertActionPolicy<NachaSecurityOperationsController>(nameof(NachaSecurityOperationsController.GenerateEncryptedAsync), P1Policies.NachaSecurityGenerateEncrypted);
        AssertActionPolicy<NachaSecurityOperationsController>(nameof(NachaSecurityOperationsController.ManualEncryptAsync), P1Policies.NachaSecurityManualEncrypt);
        AssertActionPolicy<NachaSecurityOperationsController>(nameof(NachaSecurityOperationsController.ManualDecryptAsync), P1Policies.NachaSecurityManualDecrypt);
        AssertActionPolicy<NachaSecurityOperationsController>(nameof(NachaSecurityOperationsController.GetByOperationIdAsync), P1Policies.NachaSecurityRead);
        AssertActionPolicy<NachaSecurityOperationsController>(nameof(NachaSecurityOperationsController.AuditAsync), P1Policies.NachaSecurityRead);
        AssertActionPolicy<NachaSecurityOperationsController>(nameof(NachaSecurityOperationsController.DownloadAsync), P1Policies.NachaSecurityRead);
        AssertActionPolicy<NachaSecurityOperationsController>(nameof(NachaSecurityOperationsController.AuthorizeDownloadAsync), P1Policies.NachaSecurityAuthorizeDownload);
    }

    [Fact]
    public async Task PoliciesP1_Grupo2_CompatibilidadOr()
    {
        await AssertPolicy(P1Policies.CertificatesRead, FineGrainedPermissions.Certificates.Read, "CanReadAch", "CanManageAch");
        await AssertCertificateManagementPolicy(P1Policies.CertificatesUploadPublic, FineGrainedPermissions.Certificates.UploadPublic);
        await AssertCertificateManagementPolicy(P1Policies.CertificatesRegisterPrivate, FineGrainedPermissions.Certificates.RegisterPrivate);
        await AssertCertificateManagementPolicy(P1Policies.CertificatesActivate, FineGrainedPermissions.Certificates.Activate);
        await AssertCertificateManagementPolicy(P1Policies.CertificatesRevoke, FineGrainedPermissions.Certificates.Revoke);
        await AssertCertificateManagementPolicy(P1Policies.CertificatesValidate, FineGrainedPermissions.Certificates.Validate);
        await AssertCertificateManagementPolicy(P1Policies.CertificatesAudit, FineGrainedPermissions.Certificates.Audit);
        await AssertPolicy(P1Policies.DigitalEnvelopeEncrypt, FineGrainedPermissions.DigitalEnvelope.Encrypt, "CanManageAch", "CanReadAch");
        await AssertPolicy(P1Policies.DigitalEnvelopeDecrypt, FineGrainedPermissions.DigitalEnvelope.Decrypt, "CanManageAch", "CanReadAch");
        await AssertPolicy(P1Policies.DigitalEnvelopeTest, FineGrainedPermissions.DigitalEnvelope.Test, "CanManageAch", "CanReadAch");
        await AssertPolicy(P1Policies.NachaSecurityRead, FineGrainedPermissions.NachaSecurity.Read, "CanReadAch", "CanManageAch");
        await AssertPolicy(P1Policies.NachaSecurityGenerateEncrypted, FineGrainedPermissions.NachaSecurity.GenerateEncrypted, "CanManageAch", "CanReadAch");
        await AssertPolicy(P1Policies.NachaSecurityManualEncrypt, FineGrainedPermissions.NachaSecurity.ManualEncrypt, "CanManageAch", "CanReadAch");
        await AssertPolicy(P1Policies.NachaSecurityManualDecrypt, FineGrainedPermissions.NachaSecurity.ManualDecrypt, "CanManageAch", "CanReadAch");
        await AssertPolicy(P1Policies.NachaSecurityAuthorizeDownload, FineGrainedPermissions.NachaSecurity.AuthorizeDownload, "CanManageAch", "CanReadAch");
    }

    private static async Task AssertPolicy(string policy, string fine, string okLegacy, string badLegacy)
    {
        using var p = Provider();
        var auth = p.GetRequiredService<IAuthorizationService>();

        Assert.True((await auth.AuthorizeAsync(User(fine), null, policy)).Succeeded);
        Assert.True((await auth.AuthorizeAsync(User(okLegacy), null, policy)).Succeeded);
        Assert.False((await auth.AuthorizeAsync(User(badLegacy), null, policy)).Succeeded);
        Assert.False((await auth.AuthorizeAsync(new ClaimsPrincipal(new ClaimsIdentity()), null, policy)).Succeeded);
    }

    private static async Task AssertCertificateManagementPolicy(string policy, string fine)
    {
        using var p = Provider();
        var auth = p.GetRequiredService<IAuthorizationService>();

        Assert.True((await auth.AuthorizeAsync(User(fine), null, policy)).Succeeded);
        Assert.True((await auth.AuthorizeAsync(User(FineGrainedPermissions.CanManageCertificates), null, policy)).Succeeded);
        Assert.False((await auth.AuthorizeAsync(User("CanManageAch"), null, policy)).Succeeded);
        Assert.False((await auth.AuthorizeAsync(User("CanReadAch"), null, policy)).Succeeded);
        Assert.False((await auth.AuthorizeAsync(new ClaimsPrincipal(new ClaimsIdentity()), null, policy)).Succeeded);
    }

    private static void AssertClassAuthorizeWithoutPolicy<TController>()
    {
        var attrs = typeof(TController).GetCustomAttributes(typeof(AuthorizeAttribute), inherit: true)
            .Cast<AuthorizeAttribute>()
            .ToList();
        Assert.NotEmpty(attrs);
        Assert.Contains(attrs, a => string.IsNullOrWhiteSpace(a.Policy));
        Assert.DoesNotContain(attrs, a => a.Policy is "CanReadAch" or "CanManageAch");
    }

    private static void AssertActionPolicy<TController>(string actionName, string expectedPolicy)
    {
        var method = typeof(TController).GetMethod(actionName);
        Assert.NotNull(method);

        var attr = method!.GetCustomAttributes(typeof(AuthorizeAttribute), inherit: true)
            .Cast<AuthorizeAttribute>()
            .FirstOrDefault();
        Assert.NotNull(attr);
        Assert.Equal(expectedPolicy, attr!.Policy);
    }

    private static ServiceProvider Provider()
    {
        var services = new ServiceCollection();
        var config = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["appSettings:tokenManager:issuerJwt"] = "i",
            ["appSettings:tokenManager:audienceJwt"] = "a",
            ["appSettings:tokenManager:secretKetJwt"] = "this-is-a-test-secret-key-with-32-bytes"
        }).Build();

        services.AddExternal(config);
        return services.BuildServiceProvider();
    }

    private static ClaimsPrincipal User(string permission) =>
        new(new ClaimsIdentity([new Claim("permission", permission)], "t"));
}
