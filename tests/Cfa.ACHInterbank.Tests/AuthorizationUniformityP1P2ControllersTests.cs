using System.Reflection;
using Cfa.ACHInterbank.Api.Controllers;
using Cfa.ACHInterbank.Application.Security;
using Microsoft.AspNetCore.Authorization;
using Xunit;

namespace Cfa.ACHInterbank.Tests;

public class AuthorizationUniformityP1P2ControllersTests
{
    [Fact]
    public void MaintenanceController_UsaAuthorizeYP1PoliciesMaintenance()
    {
        Assert.NotNull(typeof(MaintenanceController).GetCustomAttribute<AuthorizeAttribute>());
        AssertMethodPolicy(typeof(MaintenanceController), nameof(MaintenanceController.RunDbInitializer), P1Policies.MaintenanceSeed);
    }

    [Fact]
    public void RegulatoryCatalogsController_Gets_UsanCanReadAch()
    {
        Assert.NotNull(typeof(RegulatoryCatalogsController).GetCustomAttribute<AuthorizeAttribute>());
        AssertMethodPolicy(typeof(RegulatoryCatalogsController), nameof(RegulatoryCatalogsController.GetReturnCodes), "CanReadAch");
        AssertMethodPolicy(typeof(RegulatoryCatalogsController), nameof(RegulatoryCatalogsController.GetFileRejectionCodes), "CanReadAch");
        AssertMethodPolicy(typeof(RegulatoryCatalogsController), nameof(RegulatoryCatalogsController.GetTransactionTypePolicies), "CanReadAch");
        AssertMethodPolicy(typeof(RegulatoryCatalogsController), nameof(RegulatoryCatalogsController.GetReturnPolicies), "CanReadAch");
        AssertMethodPolicy(typeof(RegulatoryCatalogsController), nameof(RegulatoryCatalogsController.GetReturnOfReturnPolicies), "CanReadAch");
        AssertMethodPolicy(typeof(RegulatoryCatalogsController), nameof(RegulatoryCatalogsController.GetPrenotificationPolicies), "CanReadAch");
    }

    [Fact]
    public void SobreDigitalController_UsaAuthorizeYP1PoliciesDigitalEnvelope()
    {
        Assert.NotNull(typeof(SobreDigitalController).GetCustomAttribute<AuthorizeAttribute>());
        AssertMethodPolicy(typeof(SobreDigitalController), nameof(SobreDigitalController.Encrypt), P1Policies.DigitalEnvelopeEncrypt);
        AssertMethodPolicy(typeof(SobreDigitalController), nameof(SobreDigitalController.Decrypt), P1Policies.DigitalEnvelopeDecrypt);
        AssertMethodPolicy(typeof(SobreDigitalController), nameof(SobreDigitalController.testRSA), P1Policies.DigitalEnvelopeTest);
    }

    private static void AssertMethodPolicy(Type controllerType, string methodName, string expectedPolicy)
    {
        var method = controllerType.GetMethod(methodName);
        Assert.NotNull(method);
        var attr = method!.GetCustomAttribute<AuthorizeAttribute>();
        Assert.NotNull(attr);
        Assert.Equal(expectedPolicy, attr!.Policy);
    }
}
