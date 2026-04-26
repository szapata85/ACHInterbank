using Cfa.ACHInterbank.Api.Controllers;
using Cfa.ACHInterbank.Application.ACH.Interfaces.PaymentRails;
using Cfa.ACHInterbank.Application.ACH.Models.PaymentRails;
using Cfa.ACHInterbank.Application.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Routing;
using Moq;
using System.Reflection;

namespace Cfa.ACHInterbank.Tests.PaymentRails;

public class PaymentRailCapabilityRegistryControllerTests
{
    [Fact]
    public void Controller_DeclaresOnlyGetHttpMethods_ForPublicActions()
    {
        var methods = typeof(PaymentRailCapabilityRegistryController)
            .GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly)
            .Where(m => !m.IsSpecialName)
            .ToList();

        methods.Should().NotBeEmpty();
        methods.All(m => m.GetCustomAttributes<HttpMethodAttribute>().All(a => a.GetType() == typeof(HttpGetAttribute))).Should().BeTrue();
        methods.Any(m => m.GetCustomAttributes<HttpMethodAttribute>().Any(a => a.GetType() == typeof(HttpPostAttribute) || a.GetType() == typeof(HttpPutAttribute) || a.GetType() == typeof(HttpPatchAttribute) || a.GetType() == typeof(HttpDeleteAttribute))).Should().BeFalse();
    }

    [Fact]
    public void Controller_Actions_UseFineGrainedReadOnlyPolicy()
    {
        var methods = typeof(PaymentRailCapabilityRegistryController)
            .GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly)
            .Where(m => m.GetCustomAttributes<HttpMethodAttribute>().Any())
            .ToList();

        methods.Should().NotBeEmpty();
        methods.Should().OnlyContain(m =>
            m.GetCustomAttributes<AuthorizeAttribute>().Any(a => a.Policy == FineGrainedPermissions.CanViewPaymentRailCapabilityRegistry));
    }

    [Fact]
    public void RegistryItem_DoesNotExposeSensitivePropertyNames()
    {
        var propertyNames = typeof(PaymentRailCapabilityRegistryItem)
            .GetProperties(BindingFlags.Instance | BindingFlags.Public)
            .Select(x => x.Name)
            .ToList();

        var forbiddenNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "Password",
            "Secret",
            "PrivateKey",
            "Token",
            "Identifier",
            "Iv"
        };

        propertyNames.Should().NotContain(name => forbiddenNames.Contains(name));
    }

    [Fact]
    public async Task GetCapabilitiesByRailAsync_WithInvalidRail_ReturnsBadRequest()
    {
        var service = new Mock<IPaymentRailCapabilityRegistryService>();
        service.Setup(x => x.GetEffectiveCapabilitiesByRailAsync("BAD_RAIL", null, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new ArgumentException("RailCode no soportado", "railCode"));

        var controller = new PaymentRailCapabilityRegistryController(service.Object);

        var result = await controller.GetCapabilitiesByRailAsync("BAD_RAIL", null, CancellationToken.None);

        result.Result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task GetCapabilityByRailAsync_WithUnknownCapability_ReturnsNotFound()
    {
        var service = new Mock<IPaymentRailCapabilityRegistryService>();
        var controller = new PaymentRailCapabilityRegistryController(service.Object);

        var result = await controller.GetCapabilityByRailAsync(PaymentRailCodes.Cenit, "NotInCatalog", null, CancellationToken.None);

        result.Result.Should().BeOfType<NotFoundObjectResult>();
        service.Verify(x => x.GetEffectiveCapabilityByRailAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<DateTime?>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task GetCapabilityByRailAsync_WhenServiceReturnsNull_ReturnsNotFound()
    {
        var service = new Mock<IPaymentRailCapabilityRegistryService>();
        service.Setup(x => x.GetEffectiveCapabilityByRailAsync(PaymentRailCodes.Cenit, PaymentRailCapabilityRegistryCodes.Netting, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync((PaymentRailCapabilityRegistryItem?)null);

        var controller = new PaymentRailCapabilityRegistryController(service.Object);

        var result = await controller.GetCapabilityByRailAsync(PaymentRailCodes.Cenit, PaymentRailCapabilityRegistryCodes.Netting, null, CancellationToken.None);

        result.Result.Should().BeOfType<NotFoundObjectResult>();
    }
}
