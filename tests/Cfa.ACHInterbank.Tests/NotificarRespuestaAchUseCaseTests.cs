using Cfa.ACHInterbank.Application.ACH.Responses.Models;
using Cfa.ACHInterbank.Application.ACH.Responses.Notification.Interfaces;
using Cfa.ACHInterbank.Application.ACH.Responses.Notification.Models;
using Cfa.ACHInterbank.Application.ACH.Responses.Notification.Services;
using Cfa.ACHInterbank.Application.ACH.Responses.Repositories;
using Cfa.ACHInterbank.Application.DataBase;
using Cfa.ACHInterbank.Domain.Models.ACH;
using Cfa.ACHInterbank.Domain.Models.ACH.Enums;
using Moq;
using Xunit;

namespace Cfa.ACHInterbank.Tests;

public class NotificarRespuestaAchUseCaseTests
{
    [Fact]
    public async Task ExecuteAsync_ShouldReturnValidationFailure_WhenAttemptIdInvalid()
    {
        var sut = BuildSut(out var attemptRepo, out var gateway, out var uow, out _);
        var result = await sut.ExecuteAsync(new NotificarRespuestaAchCommand(0, null));
        Assert.False(result.Procesada);
        attemptRepo.Verify(x => x.FindByIdAsync(It.IsAny<long>(), It.IsAny<CancellationToken>()), Times.Never);
        gateway.Verify(x => x.RegistrarRespuestaAsync(It.IsAny<RegistrarRespuestaAchCommand>(), It.IsAny<CancellationToken>()), Times.Never);
        uow.Verify(x => x.CommitAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task ExecuteAsync_ShouldReturnNotFound_WhenAttemptDoesNotExist()
    {
        var sut = BuildSut(out var attemptRepo, out var gateway, out var uow, out _);
        attemptRepo.Setup(x => x.FindByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync((AchResponseNotificationAttempt?)null);
        var result = await sut.ExecuteAsync(new NotificarRespuestaAchCommand(1, null));
        Assert.False(result.Encontrada);
        gateway.Verify(x => x.RegistrarRespuestaAsync(It.IsAny<RegistrarRespuestaAchCommand>(), It.IsAny<CancellationToken>()), Times.Never);
        uow.Verify(x => x.CommitAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task ExecuteAsync_ShouldReturnAlreadyProcessed_WhenAttemptIsNotPending()
    {
        var sut = BuildSut(out var attemptRepo, out var gateway, out var uow, out _);
        attemptRepo.Setup(x => x.FindByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(BuildAttempt(AchResponseNotificationStatus.Exitosa));
        var result = await sut.ExecuteAsync(new NotificarRespuestaAchCommand(1, null));
        Assert.True(result.YaProcesada);
        gateway.Verify(x => x.RegistrarRespuestaAsync(It.IsAny<RegistrarRespuestaAchCommand>(), It.IsAny<CancellationToken>()), Times.Never);
        uow.Verify(x => x.CommitAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public void ExecuteAsync_ShouldMapAttemptToRegistrarRespuestaAchCommand()
    {
        var mapper = new RegistrarRespuestaAchCommandMapper();
        var attempt = BuildAttempt(AchResponseNotificationStatus.Pendiente);
        var cmd = mapper.Map(attempt.AchResponse!, attempt);
        Assert.Equal(attempt.IdEstado, cmd.IdEstado);
        Assert.Equal(attempt.IdTransaccionServicioExterno, cmd.IdTransaccionServicioExterno);
        Assert.Equal(attempt.AchResponse!.CodigoCamaraCompensacion, cmd.CodigoCamaraCompensacion);
    }

    [Fact]
    public async Task ExecuteAsync_ShouldMarkAttemptSuccessAndResponseNotificada_WhenGatewaySuccess()
    {
        var sut = BuildSut(out var attemptRepo, out var gateway, out var uow, out _);
        var attempt = BuildAttempt(AchResponseNotificationStatus.Pendiente);
        attemptRepo.Setup(x => x.FindByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(attempt);
        gateway.Setup(x => x.RegistrarRespuestaAsync(It.IsAny<RegistrarRespuestaAchCommand>(), It.IsAny<CancellationToken>())).ReturnsAsync(new ResultadoRegistroRespuestaAch(false, null, null));

        var result = await sut.ExecuteAsync(new NotificarRespuestaAchCommand(1, null));

        Assert.True(result.Procesada);
        Assert.Equal(AchResponseNotificationStatus.Exitosa, attempt.EstadoNotificacion);
        Assert.Equal(AchResponseProcessingStatus.Notificada, attempt.AchResponse!.EstadoProcesamiento);
        uow.Verify(x => x.CommitAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ExecuteAsync_ShouldMarkFunctionalError_WhenGatewayReturnsExisteErrorTrue()
    {
        var sut = BuildSut(out var attemptRepo, out var gateway, out var uow, out _);
        var attempt = BuildAttempt(AchResponseNotificationStatus.Pendiente);
        attemptRepo.Setup(x => x.FindByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(attempt);
        gateway.Setup(x => x.RegistrarRespuestaAsync(It.IsAny<RegistrarRespuestaAchCommand>(), It.IsAny<CancellationToken>())).ReturnsAsync(new ResultadoRegistroRespuestaAch(true, "E01", "Err"));

        var result = await sut.ExecuteAsync(new NotificarRespuestaAchCommand(1, null));

        Assert.True(result.ExisteError);
        Assert.Equal(AchResponseNotificationStatus.ErrorFuncional, attempt.EstadoNotificacion);
        Assert.Equal(AchResponseProcessingStatus.ErrorFuncional, attempt.AchResponse!.EstadoProcesamiento);
        uow.Verify(x => x.CommitAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ExecuteAsync_ShouldMarkTechnicalErrorAndPendingRetry_WhenGatewayThrows()
    {
        var sut = BuildSut(out var attemptRepo, out var gateway, out var uow, out _);
        var attempt = BuildAttempt(AchResponseNotificationStatus.Pendiente);
        attemptRepo.Setup(x => x.FindByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(attempt);
        gateway.Setup(x => x.RegistrarRespuestaAsync(It.IsAny<RegistrarRespuestaAchCommand>(), It.IsAny<CancellationToken>())).ThrowsAsync(new TimeoutException("timeout"));

        var result = await sut.ExecuteAsync(new NotificarRespuestaAchCommand(1, null));

        Assert.True(result.ErrorTecnico);
        Assert.Equal(AchResponseNotificationStatus.ErrorTecnico, attempt.EstadoNotificacion);
        Assert.Equal(AchResponseProcessingStatus.PendienteReintento, attempt.AchResponse!.EstadoProcesamiento);
        Assert.False(string.IsNullOrWhiteSpace(attempt.ErrorTecnico));
        uow.Verify(x => x.CommitAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public void ExecuteAsync_ShouldNotReferenceSoapOrProviderTerms()
    {
        var types = new[]
        {
            typeof(NotificarRespuestaAchCommand), typeof(NotificarRespuestaAchResult), typeof(INotificarRespuestaAchUseCase),
            typeof(NotificarRespuestaAchUseCase), typeof(IRegistrarRespuestaAchCommandMapper), typeof(RegistrarRespuestaAchCommandMapper)
        };
        var forbidden = new[] { "Axon", "Soap", "Xml", "Wsdl", "Envelope", "idTransaccionAxon", "RegistrarRespuestaTransaccion" };
        foreach (var t in types)
            Assert.DoesNotContain(forbidden, x => (t.FullName ?? t.Name).Contains(x, StringComparison.OrdinalIgnoreCase));
    }

    private static NotificarRespuestaAchUseCase BuildSut(out Mock<IAchResponseNotificationAttemptRepository> attemptRepo, out Mock<IRespuestaTransaccionesAchGateway> gateway, out Mock<IUnitOfWork> uow, out Mock<IAchResponseRepository> responseRepo)
    {
        attemptRepo = new Mock<IAchResponseNotificationAttemptRepository>();
        gateway = new Mock<IRespuestaTransaccionesAchGateway>();
        uow = new Mock<IUnitOfWork>();
        responseRepo = new Mock<IAchResponseRepository>();
        return new NotificarRespuestaAchUseCase(responseRepo.Object, attemptRepo.Object, new RegistrarRespuestaAchCommandMapper(), gateway.Object, uow.Object);
    }

    private static AchResponseNotificationAttempt BuildAttempt(AchResponseNotificationStatus status)
        => new()
        {
            Id = 1,
            EstadoNotificacion = status,
            IdCanal = 1,
            NombreCanal = "CAN",
            IdTransaccion = "TX",
            IdEstado = 2,
            IdTransaccionServicioExterno = 99,
            AchResponse = new AchResponse { Id = Guid.NewGuid(), TipoRespuesta = TipoRespuestaAch.Transaccion, CodigoCamaraCompensacion = "ACH", EstadoProcesamiento = AchResponseProcessingStatus.Homologada }
        };
}
