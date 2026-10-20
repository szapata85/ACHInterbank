using System.Reflection;
using Cfa.ACHInterbank.Api.Controllers;
using Cfa.ACHInterbank.Application.ACH.Interfaces;
using Cfa.ACHInterbank.Application.ACH.Models;
using Cfa.ACHInterbank.Application.Security;
using Cfa.ACHInterbank.Domain.Entities.Transactions.Enums;
using Cfa.ACHInterbank.Domain.Models.ACH.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Moq;

namespace Cfa.ACHInterbank.Tests;

public class ClearingHouseTransactionPoliciesControllerTests
{
    [Fact]
    public void CanonicalApi_IsNestedUnderClearingHouseAndUsesConfigPolicies()
    {
        var route = typeof(ClearingHouseTransactionPoliciesController).GetCustomAttribute<RouteAttribute>();
        Assert.Equal("api/clearing-houses/{clearingHouseId:int}/transaction-policies", route!.Template);

        AssertPolicy(nameof(ClearingHouseTransactionPoliciesController.GetVersions), P1Policies.ConfigRead);
        AssertPolicy(nameof(ClearingHouseTransactionPoliciesController.GetCurrent), P1Policies.ConfigRead);
        AssertPolicy(nameof(ClearingHouseTransactionPoliciesController.GetById), P1Policies.ConfigRead);
        AssertPolicy(nameof(ClearingHouseTransactionPoliciesController.Preview), P1Policies.ConfigRead);
        AssertPolicy(nameof(ClearingHouseTransactionPoliciesController.CreateVersion), P1Policies.ConfigManage);
        AssertPolicy(nameof(ClearingHouseTransactionPoliciesController.UpdateMetadata), P1Policies.ConfigManage);
        AssertPolicy(nameof(ClearingHouseTransactionPoliciesController.CloseVersion), P1Policies.ConfigManage);
        AssertPolicy(nameof(ClearingHouseTransactionPoliciesController.ActivateVersion), P1Policies.ConfigManage);
    }

    [Fact]
    public async Task CreateVersion_DelegatesHouseScopedRequestAndReturnsCanonicalLocation()
    {
        var request = new CreateClearingHouseTransactionPolicyVersionRequest(
            TransactionTypeEnum.Debit,
            PrenotificationRequirementMode.Mandatory,
            3,
            new DateTime(2027, 1, 1),
            null,
            true,
            "Norma",
            "Referencia",
            null);
        var created = new ClearingHouseTransactionRuleDto(
            47, 9, "ACH Colombia", TransactionNature.Debit, TransactionTypeEnum.Debit,
            true, PrenotificationRequirementMode.Mandatory, 3, true,
            ValidationRequirementMode.Mandatory, true, true, request.EffectiveFrom, null,
            true, "Norma", "Referencia", string.Empty, DateTimeOffset.UtcNow, DateTimeOffset.UtcNow);
        var rules = new Mock<IClearingHouseTransactionRuleService>();
        rules.Setup(x => x.CreateVersionAsync(9, request, It.IsAny<CancellationToken>())).ReturnsAsync(created);
        var controller = new ClearingHouseTransactionPoliciesController(
            rules.Object,
            Mock.Of<ITransactionPrerequisitePolicyService>());

        var result = await controller.CreateVersion(9, request, CancellationToken.None);

        var response = Assert.IsType<CreatedAtActionResult>(result);
        Assert.Equal(nameof(ClearingHouseTransactionPoliciesController.GetById), response.ActionName);
        Assert.Equal(9, response.RouteValues!["clearingHouseId"]);
        Assert.Equal(47, response.RouteValues["id"]);
        rules.VerifyAll();
    }

    private static void AssertPolicy(string methodName, string expectedPolicy)
    {
        var method = typeof(ClearingHouseTransactionPoliciesController).GetMethod(methodName)!;
        var authorize = method.GetCustomAttributes<AuthorizeAttribute>().Single();
        Assert.Equal(expectedPolicy, authorize.Policy);
        Assert.DoesNotContain(
            method.GetCustomAttributes<AuthorizeAttribute>(),
            attribute => attribute.Policy is "CanReadAch" or "CanManageAch");
    }
}
