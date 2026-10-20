using System.Reflection;
using Cfa.ACHInterbank.Api.Controllers;
using Cfa.ACHInterbank.Application.ACH.Interfaces;
using Cfa.ACHInterbank.Persistence.ACH.Quartz.Jobs.Implementation;
using Cfa.ACHInterbank.Persistence.ACH.Services.Implementation;

namespace Cfa.ACHInterbank.Tests;

public sealed class AchColombiaManagedMftInterfaceSegregationTests
{
    [Fact]
    public void CapabilityContracts_ShouldExposeOnlyPlanAuthorizedResponsibilities()
    {
        AssertMethods<IAchColombiaManagedMftExecutionService>(
            nameof(IAchColombiaManagedMftExecutionService.ExecuteOutboundAsync),
            nameof(IAchColombiaManagedMftExecutionService.ExecuteInboundAsync));
        AssertMethods<IAchColombiaManagedMftTransferReadService>(
            nameof(IAchColombiaManagedMftTransferReadService.QueryAsync),
            nameof(IAchColombiaManagedMftTransferReadService.GetAsync));
        AssertMethods<IAchColombiaManagedMftTransferOperationsService>(
            nameof(IAchColombiaManagedMftTransferOperationsService.RetryAsync),
            nameof(IAchColombiaManagedMftTransferOperationsService.ReprocessAsync),
            nameof(IAchColombiaManagedMftTransferOperationsService.ArchiveAsync),
            nameof(IAchColombiaManagedMftTransferOperationsService.RetireAsync),
            nameof(IAchColombiaManagedMftTransferOperationsService.DownloadAsync),
            nameof(IAchColombiaManagedMftTransferOperationsService.TestConnectionAsync));
        AssertMethods<IAchColombiaManagedMftAdministrationService>(
            nameof(IAchColombiaManagedMftAdministrationService.GetConfigurationAsync),
            nameof(IAchColombiaManagedMftAdministrationService.UpdateConfigurationAsync),
            nameof(IAchColombiaManagedMftAdministrationService.GetAdministrationAsync),
            nameof(IAchColombiaManagedMftAdministrationService.UpdateAdministrationAsync));
        AssertMethods<IAchColombiaManagedMftCredentialAdministrationService>(
            nameof(IAchColombiaManagedMftCredentialAdministrationService.SetCredentialAsync));
    }

    [Fact]
    public void CompatibilityFacade_ShouldAggregateCapabilitiesWithoutOwningNewOperations()
    {
        var facade = typeof(IAchColombiaManagedFileExchangeService);
        Assert.Empty(facade.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly));
        Assert.Equal(5, facade.GetInterfaces().Length);

        foreach (var capability in facade.GetInterfaces())
        {
            Assert.True(capability.IsAssignableFrom(typeof(AchColombiaManagedFileExchangeService)));
        }
    }

    [Fact]
    public void Consumers_ShouldDependOnCapabilityContractsRatherThanCompatibilityFacade()
    {
        AssertConstructorParameters(
            typeof(AchColombiaManagedMftOutboundHandler),
            typeof(IAchColombiaManagedMftExecutionService));
        AssertConstructorParameters(
            typeof(AchColombiaManagedMftInboundHandler),
            typeof(IAchColombiaManagedMftExecutionService));

        var controller = typeof(AchColombiaFileExchangeController).GetConstructors().Single();
        var parameterTypes = controller.GetParameters().Select(x => x.ParameterType).ToArray();
        Assert.Contains(typeof(IAchColombiaManagedMftExecutionService), parameterTypes);
        Assert.Contains(typeof(IAchColombiaManagedMftTransferReadService), parameterTypes);
        Assert.Contains(typeof(IAchColombiaManagedMftTransferOperationsService), parameterTypes);
        Assert.Contains(typeof(IAchColombiaManagedMftAdministrationService), parameterTypes);
        Assert.Contains(typeof(IAchColombiaManagedMftCredentialAdministrationService), parameterTypes);
        Assert.DoesNotContain(typeof(IAchColombiaManagedFileExchangeService), parameterTypes);
    }

    [Fact]
    public void CapabilityContracts_ShouldPreserveCancellation()
    {
        var contracts = new[]
        {
            typeof(IAchColombiaManagedMftExecutionService),
            typeof(IAchColombiaManagedMftTransferReadService),
            typeof(IAchColombiaManagedMftTransferOperationsService),
            typeof(IAchColombiaManagedMftAdministrationService),
            typeof(IAchColombiaManagedMftCredentialAdministrationService)
        };

        foreach (var method in contracts.SelectMany(x => x.GetMethods()))
        {
            Assert.Contains(method.GetParameters(), parameter => parameter.ParameterType == typeof(CancellationToken));
        }
    }

    private static void AssertMethods<T>(params string[] expected)
    {
        var actual = typeof(T).GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)
            .Select(x => x.Name)
            .OrderBy(x => x, StringComparer.Ordinal)
            .ToArray();
        Assert.Equal(expected.OrderBy(x => x, StringComparer.Ordinal), actual);
    }

    private static void AssertConstructorParameters(Type consumer, Type expectedCapability)
    {
        var parameters = consumer.GetConstructors().Single().GetParameters().Select(x => x.ParameterType).ToArray();
        Assert.Contains(expectedCapability, parameters);
        Assert.DoesNotContain(typeof(IAchColombiaManagedFileExchangeService), parameters);
    }
}
