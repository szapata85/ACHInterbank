using Cfa.ACHInterbank.Application.Configuration;
using Cfa.ACHInterbank.Application.ACH.Implementation.PaymentRails;
using Cfa.ACHInterbank.Application.ACH.Interfaces.PaymentRails;
using Cfa.ACHInterbank.Application.JobsQuartz.Interfaces;
using Cfa.ACHInterbank.Application.Services.TokenClient.Model;
using Cfa.ACHInterbank.Application.Validators.NachaValidator;
using Cfa.ACHInterbank.Application.Validators.TokenClientValidator;
using Cfa.ACHInterbank.Application.ACH.Responses.Processing.Validation;
using Cfa.ACHInterbank.Application.ACH.Responses.Validation;
using Cfa.ACHInterbank.Domain.Models.ACH;
using Cfa.ACHInterbank.Domain.Models.Configurations;
using FluentValidation;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

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

        services.AddSingleton(TimeProvider.System);

        services.AddMediatR(cfg => cfg.RegisterServicesFromAssemblyContaining<ApplicationMediatorEntryPoint>());

        // Payment rail strategy base contracts (Fase 1 bridge, no-op operational)
        services.AddSingleton<IClearingHouseToPaymentRailMapper, ClearingHouseToPaymentRailMapper>();
        services.AddSingleton<IPaymentRailOperationalStrategy, AchColombiaPaymentRailOperationalStrategy>();
        services.AddSingleton<IPaymentRailOperationalStrategy, CenitPaymentRailOperationalStrategy>();
        services.AddSingleton<IPaymentRailOperationalStrategy, UnknownPaymentRailOperationalStrategy>();
        services.AddSingleton<IPaymentRailOperationalStrategyResolver, PaymentRailOperationalStrategyResolver>();
        services.AddSingleton<IPaymentRailContextService, PaymentRailContextService>();
        services.AddSingleton<IPaymentRailShadowCompareService, PaymentRailShadowCompareService>();
        #endregion

        #region Services
        // Injection Dependency Traditional
        //services.AddSingleton<ITest, Test>();

        // Registro convencional mediante Scrutor.
        services.AddConventionServicesFromAssembly(
            typeof(DependencyInjectionService).Assembly,
            typeof(ITaskHandler));

        #endregion Services

        #region Validators
        services.AddSingleton<IValidator<TokenModelClient>, TokenClientValidator>();

        services.AddSingleton<IValidator<AddendaRecord>, AddendaRecordValidator>();
        services.AddSingleton<IValidator<BatchControl>, BatchControlValidator>();
        services.AddSingleton<IValidator<BatchHeader>, BatchHeaderValidator>();
        services.AddSingleton<IValidator<EntryDetail>, EntryDetailValidator>();
        services.AddSingleton<IValidator<FileControl>, FileControlValidator>();
        services.AddSingleton<IValidator<NachaHeader>, NachaHeaderValidator>();

        // Validators sin interfaz dedicada requeridos por casos de uso ACH Responses
        services.AddTransient<ProcesarRespuestaAchCommandValidator>();
        services.AddTransient<RegistrarRespuestaAchCommandValidator>();

        #endregion Validators


        return services;
    }
}

internal sealed class ApplicationMediatorEntryPoint
{
}
