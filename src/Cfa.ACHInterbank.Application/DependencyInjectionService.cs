using Cfa.ACHInterbank.Application.Configuration;
using Cfa.ACHInterbank.Application.ACH.Implementation.PaymentRails;
using Cfa.ACHInterbank.Application.ACH.Interfaces.PaymentRails;
using Cfa.ACHInterbank.Application.JobsQuartz.Interfaces;
using Cfa.ACHInterbank.Application.Services.TokenClient.Model;
using Cfa.ACHInterbank.Application.Validators.NachaValidator;
using Cfa.ACHInterbank.Application.Validators.TokenClientValidator;
using Cfa.ACHInterbank.Domain.Models.ACH;
using Cfa.ACHInterbank.Domain.Models.Configurations;
using FluentValidation;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Reflection;

namespace Cfa.ACHInterbank.Application;

public static class DependencyInjectionService
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        #region Configuration
        var loggerFactory = LoggerFactory.Create(builder =>
        {
            builder.AddConsole();
        });

        // usarlo con AutoMapper 15
        MapperBootstrapper.Configure(loggerFactory);

        services.AddSingleton(MapperBootstrapper.Instance);

        services.AddMediatR(cfg => cfg.RegisterServicesFromAssemblyContaining<ApplicationMediatorEntryPoint>());

        // Payment rail strategy base contracts (Fase 1 bridge, no-op operational)
        services.AddSingleton<IClearingHouseToPaymentRailMapper, ClearingHouseToPaymentRailMapper>();
        services.AddSingleton<IPaymentRailOperationalStrategy, AchColombiaPaymentRailOperationalStrategy>();
        services.AddSingleton<IPaymentRailOperationalStrategy, CenitPaymentRailOperationalStrategy>();
        services.AddSingleton<IPaymentRailOperationalStrategy, UnknownPaymentRailOperationalStrategy>();
        services.AddSingleton<IPaymentRailOperationalStrategyResolver, PaymentRailOperationalStrategyResolver>();
        #endregion

        #region Services
        // Injection Dependency Traditional
        //services.AddSingleton<ITest, Test>();

        // Injection Dependency Dynamic
        List<Type> types = Assembly.GetExecutingAssembly().GetTypes()
            .Where(t => t.IsClass
            && !t.IsAbstract
            && !t.IsGenericTypeDefinition
            && !t.ContainsGenericParameters
            && !t.IsDefined(typeof(System.Runtime.CompilerServices.CompilerGeneratedAttribute), inherit: false)
            && (t.IsPublic || t.IsNestedPublic))
            .ToList();

        foreach (var implementationType in types)
        {
            Type? interfaceType = implementationType.GetInterfaces()
                .FirstOrDefault(i => i.Name == $"I{implementationType.Name}" || i == typeof(ITaskHandler));

            if (interfaceType == null) continue;

            if (implementationType.GetCustomAttribute<SingletonAttribute>() != null)
            {
                services.AddSingleton(interfaceType, implementationType);
            }
            else if (implementationType.GetCustomAttribute<ScopedAttribute>() != null)
            {
                services.AddScoped(interfaceType, implementationType);
            }
            else
            {
                services.AddTransient(interfaceType, implementationType);
            }
        }

        #endregion Services

        #region Validators
        services.AddSingleton<IValidator<TokenModelClient>, TokenClientValidator>();

        services.AddSingleton<IValidator<AddendaRecord>, AddendaRecordValidator>();
        services.AddSingleton<IValidator<BatchControl>, BatchControlValidator>();
        services.AddSingleton<IValidator<BatchHeader>, BatchHeaderValidator>();
        services.AddSingleton<IValidator<EntryDetail>, EntryDetailValidator>();
        services.AddSingleton<IValidator<FileControl>, FileControlValidator>();
        services.AddSingleton<IValidator<NachaHeader>, NachaHeaderValidator>();

        #endregion Validators


        return services;
    }
}

internal sealed class ApplicationMediatorEntryPoint
{
}
