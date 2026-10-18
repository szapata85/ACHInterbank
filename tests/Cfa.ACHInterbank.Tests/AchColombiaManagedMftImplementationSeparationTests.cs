using Cfa.ACHInterbank.Application.ACH.Interfaces;
using Cfa.ACHInterbank.Persistence.ACH.Services.Implementation;
using Cfa.ACHInterbank.Persistence.DataBase;

namespace Cfa.ACHInterbank.Tests;

public sealed class AchColombiaManagedMftImplementationSeparationTests
{
    [Fact]
    public void CompatibilityFacade_ShouldComposeR6BImplementationOwners_WithoutOwningPersistenceOrTransport()
    {
        var constructor = Assert.Single(typeof(AchColombiaManagedFileExchangeService).GetConstructors());
        var parameterTypes = constructor.GetParameters().Select(x => x.ParameterType).ToArray();

        Assert.Equal(new[]
        {
            typeof(AchColombiaManagedMftOutboundWorkflow),
            typeof(AchColombiaManagedMftInboundWorkflow),
            typeof(AchColombiaManagedMftRetryReprocessComponent),
            typeof(AchColombiaManagedMftLifecycleComponent),
            typeof(AchColombiaManagedMftQueryProjectionComponent),
            typeof(AchColombiaManagedMftAdministrationComponent),
            typeof(AchColombiaManagedMftCredentialComponent),
            typeof(IAchColombiaManagedMftConnectionTester)
        }, parameterTypes);

        Assert.DoesNotContain(typeof(AchDbContext), parameterTypes);
        Assert.DoesNotContain(typeof(IAchColombiaManagedMftAdapterResolver), parameterTypes);
        Assert.DoesNotContain(typeof(IAchColombiaManagedMftAdapter), parameterTypes);
    }

    [Fact]
    public void R6AContracts_ShouldRemainUnchangedWhileImplementationOwnershipIsSeparated()
    {
        Assert.Empty(typeof(IAchColombiaManagedFileExchangeService).GetMethods(
            System.Reflection.BindingFlags.Public
            | System.Reflection.BindingFlags.Instance
            | System.Reflection.BindingFlags.DeclaredOnly));

        Assert.Equal(2, typeof(IAchColombiaManagedMftExecutionService).GetMethods().Length);
        Assert.Equal(2, typeof(IAchColombiaManagedMftTransferReadService).GetMethods().Length);
        Assert.Equal(6, typeof(IAchColombiaManagedMftTransferOperationsService).GetMethods().Length);
        Assert.Equal(4, typeof(IAchColombiaManagedMftAdministrationService).GetMethods().Length);
        Assert.Single(typeof(IAchColombiaManagedMftCredentialAdministrationService).GetMethods());
    }

    [Fact]
    public void R6BComponents_ShouldExposeCohesiveImplementationResponsibilities()
    {
        Assert.Contains(typeof(AchColombiaManagedMftOutboundWorkflow).GetMethods(), x => x.Name == "ExecuteAsync");
        Assert.Contains(typeof(AchColombiaManagedMftInboundWorkflow).GetMethods(), x => x.Name == "ExecuteAsync");
        Assert.Contains(typeof(AchColombiaManagedMftRetryReprocessComponent).GetMethods(), x => x.Name == "RetryAsync");
        Assert.Contains(typeof(AchColombiaManagedMftRetryReprocessComponent).GetMethods(), x => x.Name == "ReprocessAsync");
        Assert.Contains(typeof(AchColombiaManagedMftLifecycleComponent).GetMethods(), x => x.Name == "ArchiveAsync");
        Assert.Contains(typeof(AchColombiaManagedMftLifecycleComponent).GetMethods(), x => x.Name == "RetireAsync");
        Assert.Contains(typeof(AchColombiaManagedMftLifecycleComponent).GetMethods(), x => x.Name == "DownloadAsync");
        Assert.Contains(typeof(AchColombiaManagedMftQueryProjectionComponent).GetMethods(), x => x.Name == "QueryAsync");
        Assert.Contains(typeof(AchColombiaManagedMftQueryProjectionComponent).GetMethods(), x => x.Name == "GetAsync");
        Assert.Contains(typeof(AchColombiaManagedMftAdministrationComponent).GetMethods(), x => x.Name == "UpdateAdministrationAsync");
        Assert.Contains(typeof(AchColombiaManagedMftCredentialComponent).GetMethods(), x => x.Name == "SetCredentialAsync");
    }
}
