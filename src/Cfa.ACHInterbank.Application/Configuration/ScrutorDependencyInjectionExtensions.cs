using Cfa.ACHInterbank.Domain.Models.Configurations;
using Microsoft.Extensions.DependencyInjection;
using System.Reflection;

namespace Cfa.ACHInterbank.Application.Configuration;

/// <summary>
/// Centraliza el registro convencional de servicios del backend usando Scrutor.
/// Mantiene la convención histórica I{NombreClase} y permite contratos especiales
/// como ITaskHandler/IDbSeeder cuando el nombre de la interfaz no coincide.
/// </summary>
public static class ScrutorDependencyInjectionExtensions
{
    public static IServiceCollection AddConventionServicesFromAssembly(
        this IServiceCollection services,
        Assembly assembly,
        params Type[] additionalServiceTypes)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(assembly);

        additionalServiceTypes ??= [];

        services.Scan(scan => scan
            .FromAssemblies(assembly)
                .AddClasses(classes => classes.Where(type =>
                    IsCandidate(type, additionalServiceTypes)
                    && type.IsDefined(typeof(SingletonAttribute), inherit: false)))
                    .As(type => ResolveServiceTypes(type, additionalServiceTypes))
                    .WithSingletonLifetime()

                .AddClasses(classes => classes.Where(type =>
                    IsCandidate(type, additionalServiceTypes)
                    && !type.IsDefined(typeof(SingletonAttribute), inherit: false)
                    && type.IsDefined(typeof(ScopedAttribute), inherit: false)))
                    .As(type => ResolveServiceTypes(type, additionalServiceTypes))
                    .WithScopedLifetime()

                .AddClasses(classes => classes.Where(type =>
                    IsCandidate(type, additionalServiceTypes)
                    && !type.IsDefined(typeof(SingletonAttribute), inherit: false)
                    && !type.IsDefined(typeof(ScopedAttribute), inherit: false)))
                    .As(type => ResolveServiceTypes(type, additionalServiceTypes))
                    .WithTransientLifetime());

        return services;
    }

    private static bool IsCandidate(Type implementationType, IReadOnlyCollection<Type> additionalServiceTypes)
        => ResolveServiceTypes(implementationType, additionalServiceTypes).Count != 0;

    private static IReadOnlyList<Type> ResolveServiceTypes(
        Type implementationType,
        IReadOnlyCollection<Type> additionalServiceTypes)
    {
        if (!implementationType.IsClass
            || implementationType.IsAbstract
            || implementationType.IsGenericTypeDefinition
            || implementationType.ContainsGenericParameters
            || implementationType.IsDefined(typeof(System.Runtime.CompilerServices.CompilerGeneratedAttribute), inherit: false)
            || !(implementationType.IsPublic || implementationType.IsNestedPublic))
        {
            return [];
        }

        var serviceType = implementationType.GetInterfaces()
            .FirstOrDefault(interfaceType =>
                interfaceType.Name == $"I{implementationType.Name}"
                || additionalServiceTypes.Contains(interfaceType));

        return serviceType is null ? [] : [serviceType];
    }
}
