using Cfa.ACHInterbank.Api.Controllers;
using Cfa.ACHInterbank.Application.Security;
using Microsoft.AspNetCore.Authorization;

namespace Cfa.ACHInterbank.Tests;

public sealed class CfaOperationalTransactionRestrictionsControllerAuthorizationTests
{
    [Fact]
    public void FineGrainedPermissions_ContainOperationalRestrictionPermissions()
    {
        Assert.Contains(FineGrainedPermissions.OperationalRestrictions.Read, FineGrainedPermissions.AllPermissions);
        Assert.Contains(FineGrainedPermissions.OperationalRestrictions.Manage, FineGrainedPermissions.AllPermissions);
    }

    [Fact]
    public void ReadEndpoints_RequireReadPermission_AndWriteEndpointsRequireManagePermission()
    {
        AssertPolicy(nameof(CfaOperationalTransactionRestrictionsController.Get), FineGrainedPermissions.OperationalRestrictions.Read);
        AssertPolicy(nameof(CfaOperationalTransactionRestrictionsController.GetById), FineGrainedPermissions.OperationalRestrictions.Read);
        AssertPolicy(nameof(CfaOperationalTransactionRestrictionsController.Create), FineGrainedPermissions.OperationalRestrictions.Manage);
        AssertPolicy(nameof(CfaOperationalTransactionRestrictionsController.Update), FineGrainedPermissions.OperationalRestrictions.Manage);
        AssertPolicy(nameof(CfaOperationalTransactionRestrictionsController.Activate), FineGrainedPermissions.OperationalRestrictions.Manage);
        AssertPolicy(nameof(CfaOperationalTransactionRestrictionsController.Deactivate), FineGrainedPermissions.OperationalRestrictions.Manage);
    }

    private static void AssertPolicy(string methodName, string expectedPolicy)
    {
        var method = typeof(CfaOperationalTransactionRestrictionsController)
            .GetMethods()
            .Single(x => x.Name == methodName);
        var attribute = Assert.Single(method.GetCustomAttributes(typeof(AuthorizeAttribute), inherit: true).Cast<AuthorizeAttribute>());
        Assert.Equal(expectedPolicy, attribute.Policy);
    }
}
