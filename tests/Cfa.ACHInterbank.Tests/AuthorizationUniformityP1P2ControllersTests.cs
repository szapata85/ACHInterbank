using System.Reflection;
using Cfa.ACHInterbank.Api.Controllers;
using Microsoft.AspNetCore.Authorization;
using Xunit;

namespace Cfa.ACHInterbank.Tests;

public class AuthorizationUniformityP1P2ControllersTests
{
    [Fact]
    public void MaintenanceController_UsaAuthorizeYPolicyCanManageAch()
    {
        Assert.NotNull(typeof(MaintenanceController).GetCustomAttribute<AuthorizeAttribute>());
        AssertMethodPolicy(typeof(MaintenanceController), nameof(MaintenanceController.RunDbInitializer), "CanManageAch");
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
    public void SobreDigitalController_UsaAuthorizeYPolicyCanManageAch()
    {
        Assert.NotNull(typeof(SobreDigitalController).GetCustomAttribute<AuthorizeAttribute>());
        AssertMethodPolicy(typeof(SobreDigitalController), nameof(SobreDigitalController.Encrypt), "CanManageAch");
        AssertMethodPolicy(typeof(SobreDigitalController), nameof(SobreDigitalController.Decrypt), "CanManageAch");
        AssertMethodPolicy(typeof(SobreDigitalController), nameof(SobreDigitalController.testRSA), "CanManageAch");
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
