using System.Text.Json;
using Cfa.ACHInterbank.Application.ACH.Interfaces;
using Cfa.ACHInterbank.Application.DataBase;
using Cfa.ACHInterbank.Persistence;
using Cfa.ACHInterbank.Persistence.ACH.Services.Implementation;
using Cfa.ACHInterbank.Persistence.ACH.Services.Implementation.Seeders;
using Cfa.ACHInterbank.Persistence.Security.Seeders;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Cfa.ACHInterbank.Tests;

public sealed class CfaOperationalRestrictionProductionIntegrationTests
{
    [Fact]
    public void AddPersistence_RegistersResolver_AndOperationalRestrictionSeederInExistingBootstrapPipeline()
    {
        var services = new ServiceCollection();
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Database:Provider"] = "postgres",
                ["ConnectionStrings:PostgresConnection"] = "Host=localhost;Database=ach_test;Username=test;Password=test"
            })
            .Build();

        services.AddPersistence(configuration);

        Assert.Contains(services, descriptor =>
            descriptor.ServiceType == typeof(ICfaOperationalTransactionRestrictionResolver)
            && descriptor.ImplementationType == typeof(CfaOperationalTransactionRestrictionResolver)
            && descriptor.Lifetime == ServiceLifetime.Scoped);

        Assert.Contains(services, descriptor =>
            descriptor.ServiceType == typeof(ICfaOperationalTransactionRestrictionAdminService)
            && descriptor.ImplementationType == typeof(CfaOperationalTransactionRestrictionAdminService)
            && descriptor.Lifetime == ServiceLifetime.Scoped);

        Assert.Contains(services, descriptor =>
            descriptor.ServiceType == typeof(IDbSeeder)
            && descriptor.ImplementationType == typeof(CfaOperationalTransactionRestrictionSeeder));

        Assert.Contains(services, descriptor =>
            descriptor.ServiceType == typeof(IDbSeeder)
            && descriptor.ImplementationType == typeof(CfaOperationalTransactionRestrictionsPermissionSeeder));
    }

    [Fact]
    public void TransactionPolicyService_DependsOnPersistedResolver_NotOptionsOrConfiguration()
    {
        var constructor = Assert.Single(typeof(TransactionPolicyService).GetConstructors());
        var parameterTypes = constructor.GetParameters().Select(parameter => parameter.ParameterType).ToArray();

        Assert.Contains(typeof(ICfaOperationalTransactionRestrictionResolver), parameterTypes);
        Assert.DoesNotContain(parameterTypes, type => type == typeof(IConfiguration));
        Assert.DoesNotContain(parameterTypes, IsOptionsType);
    }

    [Fact]
    public void OperationalRestrictionSeeder_DoesNotDependOnOptionsOrConfiguration()
    {
        var constructor = Assert.Single(typeof(CfaOperationalTransactionRestrictionSeeder).GetConstructors());
        var parameterTypes = constructor.GetParameters().Select(parameter => parameter.ParameterType).ToArray();

        Assert.DoesNotContain(parameterTypes, type => type == typeof(IConfiguration));
        Assert.DoesNotContain(parameterTypes, IsOptionsType);
    }

    [Theory]
    [InlineData("appsettings.json")]
    [InlineData("appsettings.Development.json")]
    public void ApiAppSettings_DoNotContainFunctionalTransactionPolicies(string fileName)
    {
        var path = ResolveRepoPath("src", "Cfa.ACHInterbank.Api", fileName);
        using var document = JsonDocument.Parse(File.ReadAllText(path));

        Assert.False(document.RootElement.TryGetProperty("TransactionPolicies", out _));
    }

    [Fact]
    public void PersistenceBootstrap_DoesNotBindLegacyTransactionPoliciesSection()
    {
        var path = ResolveRepoPath("src", "Cfa.ACHInterbank.Persistence", "DependencyInjectionService.cs");
        var source = File.ReadAllText(path);

        Assert.DoesNotContain("TransactionPolicyOptions", source, StringComparison.Ordinal);
        Assert.DoesNotContain("GetSection(\"TransactionPolicies\")", source, StringComparison.Ordinal);
    }

    private static bool IsOptionsType(Type type)
        => type.Namespace == "Microsoft.Extensions.Options"
           || (type.IsGenericType
               && type.GetGenericTypeDefinition().Namespace == "Microsoft.Extensions.Options");

    private static string ResolveRepoPath(params string[] parts)
    {
        var current = new DirectoryInfo(AppContext.BaseDirectory);
        while (current is not null)
        {
            var candidate = Path.Combine(new[] { current.FullName }.Concat(parts).ToArray());
            if (File.Exists(candidate))
            {
                return candidate;
            }

            current = current.Parent;
        }

        throw new FileNotFoundException($"No se encontró el archivo solicitado desde {AppContext.BaseDirectory}.");
    }
}
