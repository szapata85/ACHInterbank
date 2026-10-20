using Cfa.ACHInterbank.Api.Controllers;
using Cfa.ACHInterbank.Application.ACH.Interfaces;
using Cfa.ACHInterbank.Persistence.ACH.Services.Implementation;

namespace Cfa.ACHInterbank.Tests;

public class IncomingNachaCommandCenterSeparationTests
{
    [Fact]
    public void CapabilityContracts_ShouldSeparateQueriesFromManualOperations()
    {
        Assert.Equal(12, typeof(IIncomingNachaCommandCenterQueryService).GetMethods().Length);
        Assert.Equal(4, typeof(IIncomingNachaCommandCenterOperationsService).GetMethods().Length);
        Assert.Empty(typeof(IIncomingNachaCommandCenterService).GetMethods(
            System.Reflection.BindingFlags.Public
            | System.Reflection.BindingFlags.Instance
            | System.Reflection.BindingFlags.DeclaredOnly));

        Assert.Contains(typeof(IIncomingNachaCommandCenterQueryService), typeof(IncomingNachaCommandCenterQueryService).GetInterfaces());
        Assert.DoesNotContain(typeof(IIncomingNachaCommandCenterOperationsService), typeof(IncomingNachaCommandCenterQueryService).GetInterfaces());
        Assert.Contains(typeof(IIncomingNachaCommandCenterOperationsService), typeof(IncomingNachaCommandCenterOperationsService).GetInterfaces());
        Assert.DoesNotContain(typeof(IIncomingNachaCommandCenterQueryService), typeof(IncomingNachaCommandCenterOperationsService).GetInterfaces());
    }

    [Fact]
    public void Controller_ShouldDependOnSeparatedCapabilities_AndKeepOrphanResolutionSeparate()
    {
        var constructor = Assert.Single(typeof(IncomingNachaCommandCenterController).GetConstructors());
        var parameters = constructor.GetParameters().Select(x => x.ParameterType).ToArray();

        Assert.Equal(
            new[]
            {
                typeof(IIncomingNachaCommandCenterQueryService),
                typeof(IIncomingNachaCommandCenterOperationsService),
                typeof(IIncomingNachaOrphanManualResolutionService)
            },
            parameters);
        Assert.DoesNotContain(typeof(IIncomingNachaCommandCenterService), parameters);
    }
}
