using System.Reflection;
using Cfa.ACHInterbank.Application.Configuration;
using Cfa.ACHInterbank.Application.Services.TokenClient.Model;
using Cfa.ACHInterbank.Application.Validators.TokenClientValidator;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;

namespace Cfa.ACHInterbank.Application;

public static class DependencyInjectionService
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        #region Configuration
        MapperBootstrapper.Configure();
        services.AddSingleton(MapperBootstrapper.Instance);
        #endregion

        #region Services
        // Injection Dependency Traditional
        //services.AddSingleton<ITest, Test>();
        //services.AddTransient<ILoggerManager, LoggerManager>();
        //services.AddSingleton<IGenerateToken, GenerateToken>();

        // Injection Dependency Dynamic
        Assembly.GetExecutingAssembly().GetTypes()
                .Where(t => t.IsClass && !t.IsAbstract) // Filtra solo las clases concretas
                .SelectMany(implementationType => implementationType.GetInterfaces().Where(o => o.Name.Contains(implementationType.Name))
                    .Select(interfacetype => new { Interface = interfacetype, Implementation = implementationType }))
                .ToList()
                .ForEach(pair =>
                {
                    switch (true)
                    {
                        case var _ when pair.Implementation.Name.Contains("Singleton"):
                            services.AddSingleton(pair.Interface, pair.Implementation);
                            break;
                        case var _ when pair.Implementation.Name.Contains("Scoped"):
                            services.AddScoped(pair.Interface, pair.Implementation);
                            break;
                        default:
                            services.AddTransient(pair.Interface, pair.Implementation);
                            break;
                    }
                });

        #endregion Services

        #region Validators
        services.AddSingleton<IValidator<TokenModelClient>, TokenClientValidator>();

        #endregion Validators


        return services;
    }
}
