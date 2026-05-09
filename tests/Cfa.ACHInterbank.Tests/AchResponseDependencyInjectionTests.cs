using Cfa.ACHInterbank.Application;
using Cfa.ACHInterbank.Application.ACH.Responses.Homologation.Interfaces;
using Cfa.ACHInterbank.Application.ACH.Responses.Processing.Interfaces;
using Cfa.ACHInterbank.Application.ACH.Responses.Notification.Interfaces;
using Cfa.ACHInterbank.Application.ACH.Responses.Processing.Validation;
using Cfa.ACHInterbank.Application.ACH.Responses.Repositories;
using Cfa.ACHInterbank.Application.ACH.Responses.Processing.Services;
using Cfa.ACHInterbank.Application.DataBase;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Xunit;

namespace Cfa.ACHInterbank.Tests;

public class AchResponseDependencyInjectionTests
{
    [Fact]
    public void AddApplication_ShouldRegisterProcesarRespuestaAchCommandValidator()
    {
        var services = new ServiceCollection();
        services.AddApplication();
        var sp = services.BuildServiceProvider();

        var validator = sp.GetService<ProcesarRespuestaAchCommandValidator>();

        Assert.NotNull(validator);
    }

    [Fact]
    public void AddApplication_ShouldRegisterProcesarRespuestaAchUseCaseDependencies()
    {
        var services = new ServiceCollection();
        services.AddApplication();
        services.AddSingleton(Mock.Of<IAchResponseRepository>());
        services.AddSingleton(Mock.Of<IAchResponseNotificationAttemptRepository>());
        services.AddSingleton(Mock.Of<IRespuestaAchStatusMappingService>());
        services.AddSingleton(Mock.Of<IUnitOfWork>());
        var sp = services.BuildServiceProvider();

        var sut = sp.GetService<IProcesarRespuestaAchUseCase>();

        Assert.NotNull(sut);
    }

    [Fact]
    public void AddApplication_ShouldRegisterAchResponseIdempotencyHashService()
    {
        var services = new ServiceCollection();
        services.AddApplication();
        var sp = services.BuildServiceProvider();

        var svc = sp.GetService<IAchResponseIdempotencyHashService>();

        Assert.NotNull(svc);
        Assert.IsType<AchResponseIdempotencyHashService>(svc);
    }

    [Fact]
    public void AddApplication_ShouldRegisterRespuestaAchStatusMappingService()
    {
        var services = new ServiceCollection();
        services.AddApplication();
        services.AddSingleton(Mock.Of<IAchResponseStatusMappingRepository>());
        var sp = services.BuildServiceProvider();

        var svc = sp.GetService<IRespuestaAchStatusMappingService>();

        Assert.NotNull(svc);
    }

    [Fact]
    public void AddApplication_ShouldRegisterNotificarRespuestaAchUseCaseDependencies()
    {
        var services = new ServiceCollection();
        services.AddApplication();
        services.AddSingleton(Mock.Of<IAchResponseRepository>());
        services.AddSingleton(Mock.Of<IAchResponseNotificationAttemptRepository>());
        services.AddSingleton(Mock.Of<IRespuestaTransaccionesAchGateway>());
        services.AddSingleton(Mock.Of<IUnitOfWork>());
        var sp = services.BuildServiceProvider();

        var sut = sp.GetService<INotificarRespuestaAchUseCase>();

        Assert.NotNull(sut);
    }

    [Fact]
    public void AddApplication_ShouldRegisterRegistrarRespuestaAchCommandMapper()
    {
        var services = new ServiceCollection();
        services.AddApplication();
        var sp = services.BuildServiceProvider();

        var mapper = sp.GetService<IRegistrarRespuestaAchCommandMapper>();

        Assert.NotNull(mapper);
    }

}
