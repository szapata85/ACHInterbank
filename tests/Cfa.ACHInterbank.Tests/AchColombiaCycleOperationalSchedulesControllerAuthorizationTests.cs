using Cfa.ACHInterbank.Api.Controllers;
using Microsoft.AspNetCore.Authorization;

namespace Cfa.ACHInterbank.Tests;

public sealed class AchColombiaCycleOperationalSchedulesControllerAuthorizationTests
{
    [Fact]
    public void ReadEndpointsUseExistingAchReadPolicy_AndWritesUseExistingAchManagePolicy()
    {
        AssertPolicy(nameof(AchColombiaCycleOperationalSchedulesController.GetAdministration), "CanReadAch");
        AssertPolicy(nameof(AchColombiaCycleOperationalSchedulesController.Get), "CanReadAch");
        AssertPolicy(nameof(AchColombiaCycleOperationalSchedulesController.GetByCycle), "CanReadAch");
        AssertPolicy(nameof(AchColombiaCycleOperationalSchedulesController.Create), "CanManageAch");
        AssertPolicy(nameof(AchColombiaCycleOperationalSchedulesController.Update), "CanManageAch");
        AssertPolicy(nameof(AchColombiaCycleOperationalSchedulesController.Deactivate), "CanManageAch");
    }

    private static void AssertPolicy(string methodName, string expectedPolicy)
    {
        var method = typeof(AchColombiaCycleOperationalSchedulesController)
            .GetMethods()
            .Single(x => x.Name == methodName);
        var attribute = Assert.Single(method
            .GetCustomAttributes(typeof(AuthorizeAttribute), inherit: true)
            .Cast<AuthorizeAttribute>());
        Assert.Equal(expectedPolicy, attribute.Policy);
    }
}
